using ailia;
using ailiaSDK;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class SegmentAnythingModel
{
    // Ailia models
    private const string encoderWeightPath = "vit_b_01ec64.onnx";
    private const string decoderWeightPath = "sam_b_01ec64.onnx";
    private const string encoderProtoPath = "vit_b_01ec64.onnx.prototxt";
    private const string decoderProtoPath = "sam_b_01ec64.onnx.prototxt";
    private const uint EncoderOutChannels = 256;
    private const uint EncoderOutSize = 64;
    private const uint DecoderMaskInputSize = 256;
    private List<Vector2Int> clickPoints = new();
    private List<Boolean> clickPointLabels = new();
    private int targetSize = 1024;
    private AiliaImageSource imageSource;
    private ailia.AiliaModel encoder;
    private ailia.AiliaModel decoder;

    public bool modelsInitialized { get; private set; } = false;
    public bool isProcessing { get; private set; } = false;
    public bool success { get; private set; } = false; 
    private bool gpuMode = false;
    private bool showClickPoints = true;

    // Normalization constants (ImageNet)
    private readonly float[] Mean = { 0.485f, 0.456f, 0.406f };
    private readonly float[] Std = { 0.229f, 0.224f, 0.225f };

    // For async cancellation
    private CancellationTokenSource cancellationTokenSource;
    private bool isQuitting = false;

    // Visualization-related fields
    public Texture2D visualizedResult { get; private set; }
    private readonly Color32 MaskColor = new Color32(255, 0, 0, 255);

    public void AddClickPoint(int x, int y, bool negativePoint = false)
    {
        clickPoints.Add(new Vector2Int(x, y));
        clickPointLabels.Add(!negativePoint);
    }

    public (int, int) GetInputSize(AiliaImageSource imageSource)
    {
        float scale = (float)targetSize / Math.Max(imageSource.Width, imageSource.Height);

        return (((int)(scale * imageSource.Width)), ((int)(scale * imageSource.Height)));
    }
    public Ailia.AILIAShape GetOutputShape()
    {
        return decoder.GetBlobShape(decoder.GetOutputBlobList()[0]);
    }

    public List<ModelDownloadURL> GetModelURLs()
    {
        List<ModelDownloadURL> modelDownloadURLs = new List<ModelDownloadURL>();
        string serverFolderName = "segment-anything";

        modelDownloadURLs.Add(new ModelDownloadURL() { folder_path = serverFolderName, file_name = encoderWeightPath });
        modelDownloadURLs.Add(new ModelDownloadURL() { folder_path = serverFolderName, file_name = encoderProtoPath });
        modelDownloadURLs.Add(new ModelDownloadURL() { folder_path = serverFolderName, file_name = decoderWeightPath });
        modelDownloadURLs.Add(new ModelDownloadURL() { folder_path = serverFolderName, file_name = decoderProtoPath });

        return modelDownloadURLs;
    }

    // Initialize Ailia models
    public async Task InitializeModelsAsync(CancellationToken cancellationToken, bool gpuMode)
    {
        if (modelsInitialized) return;

        try
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            string encPath = System.IO.Path.Combine(Application.streamingAssetsPath, encoderWeightPath);
            string decPath = System.IO.Path.Combine(Application.streamingAssetsPath, decoderWeightPath);
            string encProtoPath = System.IO.Path.Combine(Application.streamingAssetsPath, encoderProtoPath);
            string decProtoPath = System.IO.Path.Combine(Application.streamingAssetsPath, decoderProtoPath);

            encoder = new ailia.AiliaModel();
            decoder = new ailia.AiliaModel();

            uint memory_mode = ailia.Ailia.AILIA_MEMORY_REDUCE_CONSTANT | ailia.Ailia.AILIA_MEMORY_REDUCE_CONSTANT_WITH_INPUT_INITIALIZER | ailia.Ailia.AILIA_MEMORY_REUSE_INTERSTAGE;
            memory_mode = ailia.Ailia.AILIA_MEMORY_REDUCE_INTERSTAGE;
            encoder.SetMemoryMode(memory_mode);

            this.gpuMode = gpuMode;
            if (gpuMode)
            {
                encoder.Environment(ailia.Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
                decoder.Environment(ailia.Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
            }

            bool encOpened = false;
            bool decOpened = false;

            await Task.Run(() => {
                if (cancellationToken.IsCancellationRequested) return;
                encOpened = encoder.OpenFile(encProtoPath, encPath);

                if (cancellationToken.IsCancellationRequested) return;
                decOpened = decoder.OpenFile(decProtoPath, decPath);
            }, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            if (!encOpened || !decOpened)
            {
                throw new Exception("Failed to open SAM model files");
            }

            modelsInitialized = true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error while loading SAM model: {e.Message}\n{e.StackTrace}");
            modelsInitialized = false;
        }
    }

    public float[,] GetClickPoints(int imageHeight)
    {
        float[,] points = new float[clickPoints.Count, 2];
        int i = 0;

        foreach (var point in clickPoints)
        {
            points[i, 0] = point.x;
            points[i, 1] = imageHeight - 1 - point.y;

            i += 1;
        }

        return points;
    }

    private float[] GetPointLabels()
    {
        return clickPointLabels.Select(l => l ? 1f : -1f).ToArray();
    }

    // Run inference with AiliaSDK
    private async Task<(bool[][,], float[])> RunInferenceAsync(float[,] pointCoords, float[] pointLabels, CancellationToken cancellationToken)
    {
        if (isQuitting || cancellationToken.IsCancellationRequested || !modelsInitialized || encoder == null || decoder == null)
        {
            return (new bool[0][,], new float[0]);
        }

        int imgHeight = imageSource.Height;
        int imgWidth = imageSource.Width;

        // Preprocess image
        var (inputData, inputSize) = PreprocessImage(imageSource);

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            int imgIndex = encoder.FindBlobIndexByName("img");

            // Set encoder input shape (1x3x1024x1024)
            ailia.Ailia.AILIAShape encInputShape = new ailia.Ailia.AILIAShape();
            encInputShape.dim = 4;
            encInputShape.w = 1;                // batch=1
            encInputShape.z = 3;                // channels=3 (RGB)
            encInputShape.y = (uint)targetSize; // height=1024
            encInputShape.x = (uint)targetSize; // width=1024

            if (isQuitting || encoder == null)
            {
                return (new bool[0][,], new float[0]);
            }

            bool shapeSetResult = encoder.SetInputBlobShape(encInputShape, imgIndex);
            if (isQuitting || !shapeSetResult)
            {
                Debug.LogError("Failed to set encoder input shape: " + encoder.Status);
                return (new bool[0][,], new float[0]);
            }

            bool dataSetResult = encoder.SetInputBlobData(inputData, imgIndex);
            Debug.Log($"updated SET DATA {dataSetResult}");

            // Run encoder
            bool encResult = encoder.Update();

            cancellationToken.ThrowIfCancellationRequested();
            if (isQuitting || encoder == null || decoder == null)
            {
                return (new bool[0][,], new float[0]);
            }

            if (!encResult)
            {
                Debug.LogError("Encoder inference failed: " + encoder.Status);
                return (new bool[0][,], new float[0]);
            }

            uint outputBlobIndex = encoder.GetOutputBlobList()[0];
            Ailia.AILIAShape outputShape = encoder.GetBlobShape(outputBlobIndex);
            //float[] encoderOutput = new float[1 * EncoderOutChannels * EncoderOutSize * EncoderOutSize]; // 1x256x64x64
            float[] encoderOutput = new float[outputShape.w * outputShape.z * outputShape.y * outputShape.x];

            Debug.Log(outputShape + " " + encoderOutput.Length + " " + (EncoderOutChannels * EncoderOutSize * EncoderOutSize));

            encoder.GetBlobData(encoderOutput, outputBlobIndex);

            // Scale coordinates to match target size
            float[,] scaledCoords = ApplyCoordinateScaling(pointCoords, imgHeight, imgWidth);

            // Prepare point coordinates and labels
            int pointCount = pointLabels.Length;
            float[] flattenedCoords = new float[pointCount * 2];
            for (int i = 0; i < pointCount; i++)
            {
                flattenedCoords[i * 2] = scaledCoords[i, 0];
                flattenedCoords[i * 2 + 1] = scaledCoords[i, 1];
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (isQuitting || decoder == null)
            {
                return (new bool[0][,], new float[0]);
            }

            // Get blob indices
            int imgEmbedIndex = decoder.FindBlobIndexByName("image_embeddings");
            int pointCoordsIndex = decoder.FindBlobIndexByName("point_coords");
            int pointLabelsIndex = decoder.FindBlobIndexByName("point_labels");

            if (imgEmbedIndex < 0 || pointCoordsIndex < 0 || pointLabelsIndex < 0)
            {
                Debug.LogError("Failed to find required common blob indices");
                return (new bool[0][,], new float[0]);
            }

            int maskInputIndex = decoder.FindBlobIndexByName("mask_input");
            int hasMaskInputIndex = decoder.FindBlobIndexByName("has_mask_input");
            int origImSizeIndex = decoder.FindBlobIndexByName("orig_im_size");

            if (maskInputIndex < 0 || hasMaskInputIndex < 0 || origImSizeIndex < 0)
            {
                Debug.LogError("Failed to find required SAM1 blob indices");
                return (new bool[0][,], new float[0]);
            }
            
            // Define blob shapes

            // image_embeddings: 1x256x64x64
            ailia.Ailia.AILIAShape embedShape = new ailia.Ailia.AILIAShape();
            embedShape.dim = 4;
            embedShape.w = 1;                     // batch=1
            embedShape.z = EncoderOutChannels;    // channel=256
            embedShape.y = EncoderOutSize;        // height=64
            embedShape.x = EncoderOutSize;        // width=64

            // point_coords: 1x1x2
            ailia.Ailia.AILIAShape coordsShape = new ailia.Ailia.AILIAShape();
            coordsShape.dim = 3;
            coordsShape.z = 1;                    // batch=1
            coordsShape.y = (uint)pointCount;     // points count (1)
            coordsShape.x = 2;                    // 2D coords (x,y)

            // point_labels: 1x1
            ailia.Ailia.AILIAShape labelsShape = new ailia.Ailia.AILIAShape();
            labelsShape.dim = 2;
            labelsShape.y = 1;                    // batch=1
            labelsShape.x = (uint)pointCount;     // points count (1)

            // mask_input: 1x1x256x256
            ailia.Ailia.AILIAShape maskInputShape = new ailia.Ailia.AILIAShape();
            maskInputShape.dim = 4;
            maskInputShape.w = 1;                 // batch=1
            maskInputShape.z = 1;                 // channel=1
            maskInputShape.y = DecoderMaskInputSize;    // height=64
            maskInputShape.x = DecoderMaskInputSize;    // width=64

            // has_mask_input: 1
            ailia.Ailia.AILIAShape hasMaskInputShape = new ailia.Ailia.AILIAShape();
            hasMaskInputShape.dim = 1;
            hasMaskInputShape.x = 1;

            // orig_im_size: 2
            ailia.Ailia.AILIAShape origImSizeShape = new ailia.Ailia.AILIAShape();
            origImSizeShape.dim = 1;
            origImSizeShape.x = 2;

            // Set blob shapes
            bool embedShapeResult = decoder.SetInputBlobShape(embedShape, (uint)imgEmbedIndex);
            bool coordsShapeResult = decoder.SetInputBlobShape(coordsShape, (uint)pointCoordsIndex);
            bool labelsShapeResult = decoder.SetInputBlobShape(labelsShape, (uint)pointLabelsIndex);
            bool maskInputShapeResult = decoder.SetInputBlobShape(maskInputShape, (uint)maskInputIndex);
            bool hasMaskInputShapeResult = decoder.SetInputBlobShape(hasMaskInputShape, (uint)hasMaskInputIndex);
            bool origImSizeShapeResult = decoder.SetInputBlobShape(origImSizeShape, (uint)origImSizeIndex);

            if (!embedShapeResult || !coordsShapeResult || !labelsShapeResult || !maskInputShapeResult || !hasMaskInputShapeResult || !origImSizeShapeResult)
            {
                Debug.LogError("Failed to set input blob shapes");
                return (new bool[0][,], new float[0]);
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (isQuitting || decoder == null)
            {
                return (new bool[0][,], new float[0]);
            }

            // Set blob data
            bool setEmbedResult = decoder.SetInputBlobData(encoderOutput, (uint)imgEmbedIndex);
            bool setCoordsResult = decoder.SetInputBlobData(flattenedCoords, (uint)pointCoordsIndex);
            bool setLabelsResult = decoder.SetInputBlobData(pointLabels, (uint)pointLabelsIndex);
            float[] maskInput = new float[1 * 1 * DecoderMaskInputSize * DecoderMaskInputSize]; // 1x1x64x64
            float[] hasMaskInput = new float[1] { 0 }; // 1
            float[] origImSizeInput = new float[2] { imageSource.Height, imageSource.Width }; // 2
            bool maskInputResult = decoder.SetInputBlobData(maskInput, (uint)maskInputIndex);
            bool hasMaskInputResult = decoder.SetInputBlobData(hasMaskInput, (uint)hasMaskInputIndex);
            bool origImSizeResult = decoder.SetInputBlobData(origImSizeInput, (uint)origImSizeIndex);

            if (!setEmbedResult || !setCoordsResult || !setLabelsResult || !maskInputResult || !hasMaskInputResult || !origImSizeResult)
            {
                Debug.LogError("Failed to set input blob data");
                return (new bool[0][,], new float[0]);
            }

            // Run decoder
            bool decResult = decoder.Update();
            
            cancellationToken.ThrowIfCancellationRequested();
            if (isQuitting || decoder == null)
            {
                return (new bool[0][,], new float[0]);
            }

            if (!decResult)
            {
                Debug.LogError("Decoder inference failed: " + decoder.Status + " [" + decoder.GetErrorDetail() + "]");
                return (new bool[0][,], new float[0]);
            }

            // Get output blobs
            uint[] outputBlobs = decoder.GetOutputBlobList();

            cancellationToken.ThrowIfCancellationRequested();
            if (isQuitting || decoder == null)
            {
                return (new bool[0][,], new float[0]);
            }

            // Identify mask and score blobs
            int masksBlobIndex = -1;
            int scoresBlobIndex = -1;

            if (outputBlobs != null)
            {
                for (int i = 0; i < outputBlobs.Length; i++)
                {
                    if (cancellationToken.IsCancellationRequested || decoder == null)
                    {
                        return (new bool[0][,], new float[0]);
                    }

                    uint blobIdx = outputBlobs[i];
                    ailia.Ailia.AILIAShape outShape = decoder.GetBlobShape(blobIdx);

                    if (outShape.dim == 4)
                        masksBlobIndex = (int)blobIdx;   // 4D tensor is mask
                    else if (outShape.dim == 2)
                        scoresBlobIndex = (int)blobIdx;  // 2D tensor is score
                }
            }

            if (masksBlobIndex < 0 || scoresBlobIndex < 0)
            {
                Debug.LogError("Could not find mask and score blobs");
                return (new bool[0][,], new float[0]);
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (isQuitting || decoder == null)
            {
                return (new bool[0][,], new float[0]);
            }

            // Get blob shapes
            ailia.Ailia.AILIAShape maskShape = decoder.GetBlobShape((uint)masksBlobIndex);
            ailia.Ailia.AILIAShape scoreShape = decoder.GetBlobShape((uint)scoresBlobIndex);

            // Typically: masks=1x4x256x256, scores=1x4
            int numMasks = (int)scoreShape.x;  // Number of mask candidates (usually 4)

            // Calculate buffer sizes
            int maskWidth = (int)maskShape.x;
            int maskHeight = (int)maskShape.y;
            int maskArea = maskWidth * maskHeight;
            int totalMaskSize = numMasks * maskArea;

            // Allocate buffers
            float[] maskOutput = new float[totalMaskSize];
            float[] scoreOutput = new float[numMasks];

            cancellationToken.ThrowIfCancellationRequested();
            if (isQuitting || decoder == null)
            {
                return (new bool[0][,], new float[0]);
            }

            // Get output data
            bool getMaskResult = decoder.GetBlobData(maskOutput, (uint)masksBlobIndex);
            bool getScoreResult = decoder.GetBlobData(scoreOutput, (uint)scoresBlobIndex);

            if (!getMaskResult || !getScoreResult)
            {
                Debug.LogError("Failed to get output data");
                return (new bool[0][,], new float[0]);
            }

            // Allow UI thread to process
            await Task.Yield();

            cancellationToken.ThrowIfCancellationRequested();

            // Convert masks to proper format
            bool[][,] masks = new bool[numMasks][,];

            for (int i = 0; i < numMasks; i++)
            {
                if (cancellationToken.IsCancellationRequested || isQuitting)
                {
                    return (new bool[0][,], new float[0]);
                }

                float[,,,] maskTensor = ReshapeToTensor(maskOutput, i, maskHeight, maskWidth, maskArea);

                masks[i] = PostprocessMask(maskTensor, inputSize.Item1, inputSize.Item2, imgHeight, imgWidth);
            }

            return (masks, scoreOutput);
        }
        catch (OperationCanceledException)
        {
            return (new bool[0][,], new float[0]);
        }
        catch (Exception e)
        {
            Debug.LogError("Inference error: " + e.Message + "\n" + e.StackTrace);
            return (new bool[0][,], new float[0]);
        }
    }

    // Helper to convert 1D array to 4D array
    private float[,,,] ReshapeToTensor(float[] data, int maskIndex, int height, int width, int area)
    {
        float[,,,] tensor = new float[1, 1, height, width];

        int offset = maskIndex * area;
        for (int h = 0; h < height; h++)
        {
            int rowOffset = h * width;
            for (int w = 0; w < width; w++)
            {
                tensor[0, 0, h, w] = data[offset + rowOffset + w];
            }
        }

        return tensor;
    }

    // Scale coordinates to target size
    private float[,] ApplyCoordinateScaling(float[,] coords, int imgHeight, int imgWidth)
    {
        float scale = (float)targetSize / Math.Max(imgHeight, imgWidth);
        float[,] scaledCoords = new float[coords.GetLength(0), coords.GetLength(1)];

        for (int i = 0; i < coords.GetLength(0); i++)
        {
            scaledCoords[i, 0] = coords[i, 0] * scale;
            scaledCoords[i, 1] = coords[i, 1] * scale;
        }

        return scaledCoords;
    }

    // Overlay mask on original image
    private Texture2D CreateMaskedImage(bool[,] mask, AiliaImageSource imageSource)
    {
        int imageWidth = imageSource.Width;
        int imageHeight = imageSource.Height;
        Texture2D result = new Texture2D(imageWidth, imageHeight, TextureFormat.ARGB32, false);
        
        Color32[] pixels = imageSource.GetPixels32(new Rect(0, 0, imageSource.Width, imageSource.Height), true);

        int maskHeight = mask.GetLength(0);
        int maskWidth = mask.GetLength(1);

        // Apply mask to original image - optimized inner loop
        for (int y = 0; y < maskHeight; y++)
        {
            int unityY = imageHeight - y - 1;
            int rowOffset = unityY * imageWidth;

            for (int x = 0; x < maskWidth; x++)
            {
                int pixelIndex = rowOffset + x;

                if (pixelIndex >= 0 && pixelIndex < pixels.Length && mask[y, x])
                {
                    Color32 originalColor = pixels[pixelIndex];
                    pixels[pixelIndex] = new Color32(
                        (byte)Mathf.Lerp(originalColor.r, MaskColor.r, 0.4f),
                        (byte)Mathf.Lerp(originalColor.g, MaskColor.g, 0.4f),
                        (byte)Mathf.Lerp(originalColor.b, MaskColor.b, 0.4f),
                        MaskColor.a
                    );
                }
            }
        }

        result.SetPixels32(pixels);
        result.Apply();
        return result;
    }

    // Visualize clicked points
    private Texture2D DrawClickPoints(float[,] coords, float[] labels, Texture2D image)
    {
        Texture2D result = new Texture2D(image.width, image.height, image.format, false);
        Graphics.CopyTexture(image, result);
        Color32[] pixels = result.GetPixels32();

        int numPoints = coords.GetLength(0);
        int markerSize = 15;

        for (int i = 0; i < numPoints; i++)
        {
            int px = Mathf.Clamp((int)coords[i, 0], 0, image.width - 1);
            int origY = (int)coords[i, 1];

            // Convert to Unity coordinates
            int py = image.height - origY - 1;
            py = Mathf.Clamp(py, 0, image.height - 1);

            Color32 markerColor = labels[i] == 1 ? new Color32(0, 255, 0, 255) : new Color32(0, 0, 255, 255);

            Debug.Log($"{coords[i, 0]},{coords[i, 1]} => {px}, {py}");

            // Draw marker with bounds checking in the loop
            for (int dy = -markerSize; dy <= markerSize; dy++)
            {
                for (int dx = -markerSize; dx <= markerSize; dx++)
                {
                    if (Math.Abs(dx) == Math.Abs(dy))
                    {
                        int nx = px + dx;
                        int ny = py + dy;

                        if (nx >= 0 && nx < image.width && ny >= 0 && ny < image.height)
                        {
                            int idx = ny * image.width + nx;
                            pixels[idx] = markerColor;
                        }
                    }
                }
            }
        }

        result.SetPixels32(pixels);
        result.Apply();
        return result;
    }

    // Preprocess image for model input
    private (float[], ValueTuple<int, int>) PreprocessImage(AiliaImageSource imageSource)
    {
        int imgHeight = imageSource.Height;
        int imgWidth = imageSource.Width;

        Texture2D image = new Texture2D(imgWidth, imgHeight, TextureFormat.ARGB32, false);
        image.SetPixels32(imageSource.GetPixels32(new Rect(0, 0, imgWidth, imgHeight), true));
        image.Apply();

        

        // Scale to fit in target size while maintaining aspect ratio
        float scale = (float)targetSize / Math.Max(imgHeight, imgWidth);
        int scaledWidth = (int)(imgWidth * scale);
        int scaledHeight = (int)(imgHeight * scale);

        // Use persistent RenderTexture to avoid allocation
        RenderTexture rt = RenderTexture.GetTemporary(scaledWidth, scaledHeight, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(image, rt);

        RenderTexture.active = rt;
        Texture2D resizedImage = new Texture2D(scaledWidth, scaledHeight, TextureFormat.RGBA32, false);
        resizedImage.ReadPixels(new Rect(0, 0, scaledWidth, scaledHeight), 0, 0);
        resizedImage.Apply();
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);

        // Flip Y coordinates to match expected input format
        UnityEngine.Color[] pixels = resizedImage.GetPixels();
        UnityEngine.Color[] flippedPixels = new UnityEngine.Color[pixels.Length];

        // Optimize row-by-row copying
        for (int y = 0; y < scaledHeight; y++)
        {
            int srcRowStart = y * scaledWidth;
            int dstRowStart = (scaledHeight - y - 1) * scaledWidth;

            Array.Copy(pixels, srcRowStart, flippedPixels, dstRowStart, scaledWidth);
        }

        Texture2D flippedImage = new Texture2D(scaledWidth, scaledHeight, TextureFormat.RGBA32, false);
        flippedImage.SetPixels(flippedPixels);
        flippedImage.Apply();
        GameObject.Destroy(resizedImage);

        // Create CHW layout data array (channel, height, width)
        float[] normalizedData = new float[3 * targetSize * targetSize];

        // Optimize by computing constants outside the inner loop
        int ch0Offset = 0;
        int ch1Offset = targetSize * targetSize;
        int ch2Offset = 2 * targetSize * targetSize;

        // Fill with normalized values and padding
        for (int y = 0; y < targetSize; y++)
        {
            for (int x = 0; x < targetSize; x++)
            {
                int baseIdx = y * targetSize + x;

                if (y < scaledHeight && x < scaledWidth)
                {
                    UnityEngine.Color pixel = flippedImage.GetPixel(x, y);
                    normalizedData[ch0Offset + baseIdx] = (pixel.r - Mean[0]) / Std[0];
                    normalizedData[ch1Offset + baseIdx] = (pixel.g - Mean[1]) / Std[1];
                    normalizedData[ch2Offset + baseIdx] = (pixel.b - Mean[2]) / Std[2];
                }
            }
        }

        GameObject.Destroy(flippedImage);

        return (normalizedData, (scaledHeight, scaledWidth));
    }

    // Post-process mask outputs
    private bool[,] PostprocessMask(float[,,,] mask, int inputHeight, int inputWidth, int origHeight, int origWidth)
    {
        int maskHeight = mask.GetLength(2);
        int maskWidth = mask.GetLength(3);

        // First resize to targetSize
        float[,] resizedMask = new float[targetSize, targetSize];
        for (int y = 0; y < targetSize; y++)
        {
            for (int x = 0; x < targetSize; x++)
            {
                float srcY = y * (float)maskHeight / targetSize;
                float srcX = x * (float)maskWidth / targetSize;

                int y0 = (int)Math.Floor(srcY);
                int y1 = Math.Min(y0 + 1, maskHeight - 1);
                int x0 = (int)Math.Floor(srcX);
                int x1 = Math.Min(x0 + 1, maskWidth - 1);

                float wy = srcY - y0;
                float wx = srcX - x0;

                // Bilinear interpolation
                resizedMask[y, x] = (1 - wy) * (1 - wx) * mask[0, 0, y0, x0] +
                                    wy * (1 - wx) * mask[0, 0, y1, x0] +
                                    (1 - wy) * wx * mask[0, 0, y0, x1] +
                                    wy * wx * mask[0, 0, y1, x1];
            }
        }

        // Crop to input size
        float[,] croppedMask = new float[inputHeight, inputWidth];
        for (int y = 0; y < inputHeight; y++)
        {
            for (int x = 0; x < inputWidth; x++)
            {
                croppedMask[y, x] = resizedMask[y, x];
            }
        }

        // Resize to original image size
        bool[,] finalMask = new bool[origHeight, origWidth];
        for (int y = 0; y < origHeight; y++)
        {
            float srcY = y * (float)inputHeight / origHeight;
            int y0 = (int)Math.Floor(srcY);
            int y1 = Math.Min(y0 + 1, inputHeight - 1);
            float wy = srcY - y0;

            for (int x = 0; x < origWidth; x++)
            {
                float srcX = x * (float)inputWidth / origWidth;
                int x0 = (int)Math.Floor(srcX);
                int x1 = Math.Min(x0 + 1, inputWidth - 1);
                float wx = srcX - x0;

                // Bilinear interpolation
                float val = (1 - wy) * (1 - wx) * croppedMask[y0, x0] +
                            wy * (1 - wx) * croppedMask[y1, x0] +
                            (1 - wy) * wx * croppedMask[y0, x1] +
                            wy * wx * croppedMask[y1, x1];

                finalMask[y, x] = val > 0.0f; // Apply threshold
            }
        }

        return finalMask;
    }


    // Process the current video frame
    public async Task ProcessFrameAsync(CancellationToken cancellationToken, AiliaImageSource imageSource)
    {
        if (!modelsInitialized || isProcessing || isQuitting)
        {
            return;
        }

        this.imageSource = imageSource;

        try
        {
            isProcessing = true;

            // Set up point coords for inference
            float[,] coords = GetClickPoints(imageSource.Height);
            float[] labels = GetPointLabels();

            var (masks, scores) = await RunInferenceAsync(coords, labels, cancellationToken);

            if (isQuitting || cancellationToken.IsCancellationRequested)
            {
                return;
            }

            if (masks != null && masks.Length > 0 && scores != null && scores.Length > 0)
            {
                // Find best mask
                int bestMaskIndex = 0;
                float maxScore = float.MinValue;
                for (int i = 0; i < scores.Length; i++)
                {
                    if (scores[i] > maxScore)
                    {
                        maxScore = scores[i];
                        bestMaskIndex = i;
                    }
                }

                // TODO: remove for visualisation
                if (visualizedResult != null)
                {
                    GameObject.Destroy(visualizedResult);
                }

                visualizedResult = CreateMaskedImage(masks[bestMaskIndex], imageSource);

                // Only draw click points if the showClickPoints flag is enabled
                if (showClickPoints)
                {
                    visualizedResult = DrawClickPoints(coords, labels, visualizedResult);
                }

                success = true;
            }
            else
            {
                Debug.LogWarning($"No valid masks");
                success = false;
            }
        }
        catch (OperationCanceledException)
        {
            // Silent cancellation
            success = false;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error in ProcessCurrentFrameAsync: {e.Message}\n{e.StackTrace}");
            success = false;
        }
        finally
        {
            isProcessing = false;
        }
    }

}
