/* AILIA Unity Plugin Segment Anything Sample */
/* Copyright 2025 AXELL CORPORATION and ax Inc. */

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

public class SegmentAnything2Model
{
    // Ailia models
    private const string encoderWeightPath = "image_encoder_hiera_l.onnx";
    private const string encoderProtoPath = "image_encoder_hiera_l.onnx.prototxt";
    private const string decoderWeightPath = "mask_decoder_hiera_l.onnx";
    private const string decoderProtoPath = "mask_decoder_hiera_l.onnx.prototxt";

    // private const string memAttentionWeightPath = "memory_attention_hiera_l.opt.onnx";
    // private const string memAttentionProtoPath = "memory_attention_hiera_l.opt.onnx.prototxt";
    // private const string encoderMemWeightPath = "memory_encoder_hiera_l.onnx";
    // private const string encoderMemProtoPath = "memory_encoder_hiera_l.onnx.prototxt";
    private const string promptWeightPath = "prompt_encoder_hiera_l.onnx";
    private const string promptProtoPath = "prompt_encoder_hiera_l.onnx.prototxt";

    // private const string mlpWeightPath = "mlp_hiera_l.onnx";
    // private const string mlpProtoPath = "mlp_hiera_l.onnx.prototxt";

    private List<Vector2Int> clickPoints = new();
    private List<Boolean> clickPointLabels = new();
    private Rect boxCoords = new();
    private bool addBoxCoords => boxCoords.width > 0 && boxCoords.height > 0;
    private int targetSize = 1024;
    private ailia.AiliaModel encoder;
    private ailia.AiliaModel decoder;
    private ailia.AiliaModel memAttention;
    private ailia.AiliaModel encoderMem;
    private ailia.AiliaModel prompt;
    private ailia.AiliaModel mlp;

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
    private bool saveFrame = false;
    private float[] encoderOutput = null;
    private float[][] highResFeats = null;
    private BackboneOutputs backboneData;
    private System.Random _rng = new System.Random();

    public void SetBoxCoords(Rect box)
    {
        boxCoords = box;

        Debug.Log($"boxCoords set to {box.xMin},{box.yMin} {box.xMax},{box.yMax}");
    }

    public void AddClickPoint(int x, int y, bool negativePoint = false)
    {
        clickPoints.Add(new Vector2Int(x, y));
        clickPointLabels.Add(!negativePoint);
    }

    public void ResetClickPoint()
    {
        clickPoints = new();
        clickPointLabels = new();
        boxCoords = new();
    }

    public List<ModelDownloadURL> GetModelURLs(ImageSegmentaion2Models modelType)
    {
        List<ModelDownloadURL> modelDownloadURLs = new List<ModelDownloadURL>();
        string serverFolderName = "segment-anything-2";

        modelDownloadURLs.Add(
            new ModelDownloadURL() { folder_path = serverFolderName, file_name = encoderWeightPath }
        );
        modelDownloadURLs.Add(
            new ModelDownloadURL() { folder_path = serverFolderName, file_name = encoderProtoPath }
        );
        modelDownloadURLs.Add(
            new ModelDownloadURL() { folder_path = serverFolderName, file_name = decoderWeightPath }
        );
        modelDownloadURLs.Add(
            new ModelDownloadURL() { folder_path = serverFolderName, file_name = decoderProtoPath }
        );
        // modelDownloadURLs.Add(
        //     new ModelDownloadURL() { folder_path = serverFolderName, file_name = memAttentionWeightPath }
        // );
        // modelDownloadURLs.Add(
        //     new ModelDownloadURL() { folder_path = serverFolderName, file_name = memAttentionProtoPath }
        // );
        // modelDownloadURLs.Add(
        //     new ModelDownloadURL() { folder_path = serverFolderName, file_name = encoderMemWeightPath }
        // );
        // modelDownloadURLs.Add(
        //     new ModelDownloadURL() { folder_path = serverFolderName, file_name = encoderMemProtoPath }
        // );
        modelDownloadURLs.Add(
            new ModelDownloadURL() { folder_path = serverFolderName, file_name = promptWeightPath }
        );
        modelDownloadURLs.Add(
            new ModelDownloadURL() { folder_path = serverFolderName, file_name = promptProtoPath }
        );
        // modelDownloadURLs.Add(
        //     new ModelDownloadURL() { folder_path = serverFolderName, file_name = mlpWeightPath }
        // );
        // modelDownloadURLs.Add(
        //     new ModelDownloadURL() { folder_path = serverFolderName, file_name = mlpProtoPath }
        // );

        return modelDownloadURLs;
    }

    // Initialize Ailia models
    public bool InitializeModels(ImageSegmentaion2Models modelType, bool gpuMode)
    {
        if (modelsInitialized)
            return true;

        try
        {
            string encPath = System.IO.Path.Combine(
                Application.temporaryCachePath,
                encoderWeightPath
            );
            string encProtoPath = System.IO.Path.Combine(
                Application.temporaryCachePath,
                encoderProtoPath
            );
            string decPath = System.IO.Path.Combine(
                Application.temporaryCachePath,
                decoderWeightPath
            );
            string decProtoPath = System.IO.Path.Combine(
                Application.temporaryCachePath,
                decoderProtoPath
            );
            // string memAttPath = System.IO.Path.Combine(
            //     Application.temporaryCachePath,
            //     memAttentionWeightPath
            // );
            // string memAttProtoPath = System.IO.Path.Combine(
            //     Application.temporaryCachePath,
            //     memAttentionProtoPath
            // );
            // string encMemPath = System.IO.Path.Combine(
            //     Application.temporaryCachePath,
            //     encoderMemWeightPath
            // );
            // string encMemProtoPath = System.IO.Path.Combine(
            //     Application.temporaryCachePath,
            //     encoderMemProtoPath
            // );
            string pmtPath = System.IO.Path.Combine(
                Application.temporaryCachePath,
                promptWeightPath
            );
            string pmtProtoPath = System.IO.Path.Combine(
                Application.temporaryCachePath,
                promptProtoPath
            );
            // string mlpPath = System.IO.Path.Combine(
            //     Application.temporaryCachePath,
            //     mlpWeightPath
            // );
            // string mlpProPath = System.IO.Path.Combine(
            //     Application.temporaryCachePath,
            //     mlpProtoPath
            // );

            encoder = new ailia.AiliaModel();
            decoder = new ailia.AiliaModel();
            // memAttention = new ailia.AiliaModel();
            // encoderMem = new ailia.AiliaModel();
            prompt = new ailia.AiliaModel();
            // mlp = new ailia.AiliaModel();

            uint memory_mode =
                ailia.Ailia.AILIA_MEMORY_REDUCE_CONSTANT
                | ailia.Ailia.AILIA_MEMORY_REDUCE_CONSTANT_WITH_INPUT_INITIALIZER
                | ailia.Ailia.AILIA_MEMORY_REUSE_INTERSTAGE;
            memory_mode = ailia.Ailia.AILIA_MEMORY_REDUCE_INTERSTAGE;
            encoder.SetMemoryMode(memory_mode);
            decoder.SetMemoryMode(memory_mode);
            // memAttention.SetMemoryMode(memory_mode);
            // encoderMem.SetMemoryMode(memory_mode);
            prompt.SetMemoryMode(memory_mode);
            // mlp.SetMemoryMode(memory_mode);

            this.gpuMode = gpuMode;
            if (gpuMode)
            {
                encoder.Environment(ailia.Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
                decoder.Environment(ailia.Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
                // memAttention.Environment(ailia.Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
                // encoderMem.Environment(ailia.Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
                prompt.Environment(ailia.Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
                // mlp.Environment(ailia.Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
            }

            bool encOpened = false;
            bool decOpened = false;
            // bool memOpened = false;
            // bool encMemOpened = false;
            bool promptOpened = false;
            // bool mlpOpened = false;

            encOpened = encoder.OpenFile(encProtoPath, encPath);
            decOpened = decoder.OpenFile(decProtoPath, decPath);
            // memOpened = memAttention.OpenFile(memAttProtoPath, memAttPath);
            // encMemOpened = encoderMem.OpenFile(encMemProtoPath, encMemPath);
            promptOpened = prompt.OpenFile(pmtProtoPath, pmtPath);
            // mlpOpened = mlp.OpenFile(mlpProPath, mlpPath);

            if (!encOpened || !decOpened || !promptOpened)
            {
                throw new Exception("Failed to open SAM 2 model files");
            }

            modelsInitialized = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error while loading SAM model: {e.Message}\n{e.StackTrace}");
            modelsInitialized = false;
        }

        return modelsInitialized;
    }

    public float[,] GetClickPoints(int imageHeight)
    {
        float[,] points = new float[clickPoints.Count + (addBoxCoords ? 2 : 0), 2];
        int i = 0;

        foreach (var point in clickPoints)
        {
            points[i, 0] = point.x;
            points[i, 1] = point.y;

            i += 1;
        }

        if (addBoxCoords)
        {
            points[clickPoints.Count, 0] = boxCoords.xMin;
            points[clickPoints.Count, 1] = boxCoords.yMin;
            points[clickPoints.Count + 1, 0] = boxCoords.xMax;
            points[clickPoints.Count + 1, 1] = boxCoords.yMax;
        }

        return points;
    }

    private float[] GetPointLabels()
    {
        float[] labels = new float[clickPoints.Count + (addBoxCoords ? 2 : 0)];

        for (int i = 0; i < clickPoints.Count; ++i)
        {
            labels[i] = (clickPointLabels[i] ? 1f : 0f);
        }

        if (addBoxCoords)
        {
            labels[clickPoints.Count] = 2;
            labels[clickPoints.Count + 1] = 3;
        }

        return labels;
    }

    // Run Embedding

    private void RunEmbedding(Color32[] image, int imgWidth, int imgHeight)
    {
        if (isQuitting || !modelsInitialized || encoder == null || decoder == null)
        {
            return;
        }

        float[,,,] inputTensor = PreprocessImage(image, imgWidth, imgHeight, targetSize);
        // var (inputData, inputSize) = PreprocessImage2(image, imgWidth, imgHeight);
        float[] nchw = Flatten4D(inputTensor);
        // float[] nchw = inputData;

        try
        {
            int imgIndex = encoder.FindBlobIndexByName("input_image");

            // Set encoder input shape (1x3x1024x1024)
            ailia.Ailia.AILIAShape encInputShape = new ailia.Ailia.AILIAShape();
            encInputShape.dim = 4;
            encInputShape.w = 1; // batch=1
            encInputShape.z = 3; // channels=3 (RGB)
            encInputShape.y = (uint)targetSize; // height=1024
            encInputShape.x = (uint)targetSize; // width=1024

            if (isQuitting || encoder == null)
            {
                return;
            }

            bool shapeSetResult = encoder.SetInputBlobShape(encInputShape, imgIndex);
            if (isQuitting || !shapeSetResult)
            {
                Debug.LogError("Failed to set encoder input shape: " + encoder.Status);
                return;
            }

            bool dataSetResult = encoder.SetInputBlobData(nchw, imgIndex);
            // Debug.Log($"updated SET DATA {dataSetResult}");

            // Run encoder
            bool encResult = encoder.Update();

            if (isQuitting || encoder == null || decoder == null)
            {
                return;
            }

            if (!encResult)
            {
                Debug.LogError("Encoder inference failed: " + encoder.Status);
                return;
            }

            int visionFeaturesBlobIndex = encoder.FindBlobIndexByName("vision_features");
            int visionPosEnc0BlobIndex = encoder.FindBlobIndexByName("vision_pos_enc_0");
            int visionPosEnc1BlobIndex = encoder.FindBlobIndexByName("vision_pos_enc_1");
            int visionPosEnc2BlobIndex = encoder.FindBlobIndexByName("vision_pos_enc_2");
            int backboneFpn0BlobIndex = encoder.FindBlobIndexByName("backbone_fpn_0");
            int backboneFpn1BlobIndex = encoder.FindBlobIndexByName("backbone_fpn_1");
            int backboneFpn2BlobIndex = encoder.FindBlobIndexByName("backbone_fpn_2");

            if (
                visionFeaturesBlobIndex < 0
                || visionPosEnc0BlobIndex < 0
                || visionPosEnc1BlobIndex < 0
                || visionPosEnc2BlobIndex < 0
                || backboneFpn0BlobIndex < 0
                || backboneFpn1BlobIndex < 0
                || backboneFpn2BlobIndex < 0
            )
            {
                Debug.LogError("Could not find required blobs");
                return;
            }

            if (isQuitting || encoder == null)
            {
                return;
            }

            // Get blob shapes
            ailia.Ailia.AILIAShape visionFeaturesBlobShape = encoder.GetBlobShape(
                (uint)visionFeaturesBlobIndex
            );

            float[] visionFeaturesOutput = new float[
                visionFeaturesBlobShape.w
                    * visionFeaturesBlobShape.z
                    * visionFeaturesBlobShape.y
                    * visionFeaturesBlobShape.x
            ];

            ailia.Ailia.AILIAShape visionPosEnc0BlobShape = encoder.GetBlobShape(
                (uint)visionPosEnc0BlobIndex
            );

            float[] visionPosEnc0Output = new float[
                visionPosEnc0BlobShape.w
                    * visionPosEnc0BlobShape.z
                    * visionPosEnc0BlobShape.y
                    * visionPosEnc0BlobShape.x
            ];

            ailia.Ailia.AILIAShape visionPosEnc1BlobShape = encoder.GetBlobShape(
                (uint)visionPosEnc1BlobIndex
            );

            float[] visionPosEnc1Output = new float[
                visionPosEnc1BlobShape.w
                    * visionPosEnc1BlobShape.z
                    * visionPosEnc1BlobShape.y
                    * visionPosEnc1BlobShape.x
            ];

            ailia.Ailia.AILIAShape visionPosEnc2BlobShape = encoder.GetBlobShape(
                (uint)visionPosEnc2BlobIndex
            );

            float[] visionPosEnc2Output = new float[
                visionPosEnc2BlobShape.w
                    * visionPosEnc2BlobShape.z
                    * visionPosEnc2BlobShape.y
                    * visionPosEnc2BlobShape.x
            ];

            ailia.Ailia.AILIAShape backboneFpn0BlobShape = encoder.GetBlobShape(
                (uint)backboneFpn0BlobIndex
            );

            float[] backboneFpn0Output = new float[
                backboneFpn0BlobShape.w
                    * backboneFpn0BlobShape.z
                    * backboneFpn0BlobShape.y
                    * backboneFpn0BlobShape.x
            ];

            ailia.Ailia.AILIAShape backboneFpn1BlobShape = encoder.GetBlobShape(
                (uint)backboneFpn1BlobIndex
            );

            float[] backboneFpn1Output = new float[
                backboneFpn1BlobShape.w
                    * backboneFpn1BlobShape.z
                    * backboneFpn1BlobShape.y
                    * backboneFpn1BlobShape.x
            ];

            ailia.Ailia.AILIAShape backboneFpn2BlobShape = encoder.GetBlobShape(
                (uint)backboneFpn2BlobIndex
            );

            float[] backboneFpn2Output = new float[
                backboneFpn2BlobShape.w
                    * backboneFpn2BlobShape.z
                    * backboneFpn2BlobShape.y
                    * backboneFpn2BlobShape.x
            ];

            encoder.GetBlobData(visionFeaturesOutput, visionFeaturesBlobIndex);
            encoder.GetBlobData(visionPosEnc0Output, visionPosEnc0BlobIndex);
            encoder.GetBlobData(visionPosEnc1Output, visionPosEnc1BlobIndex);
            encoder.GetBlobData(visionPosEnc2Output, visionPosEnc2BlobIndex);
            encoder.GetBlobData(backboneFpn0Output, backboneFpn0BlobIndex);
            encoder.GetBlobData(backboneFpn1Output, backboneFpn1BlobIndex);
            encoder.GetBlobData(backboneFpn2Output, backboneFpn2BlobIndex);

            float[][,,,] visionPosEnc = new float[][,,,]
            {
                ReshapeTo4D(
                    visionPosEnc0Output,
                    (int)visionPosEnc0BlobShape.w,
                    (int)visionPosEnc0BlobShape.z,
                    (int)visionPosEnc0BlobShape.y,
                    (int)visionPosEnc0BlobShape.x
                ),
                ReshapeTo4D(
                    visionPosEnc1Output,
                    (int)visionPosEnc1BlobShape.w,
                    (int)visionPosEnc1BlobShape.z,
                    (int)visionPosEnc1BlobShape.y,
                    (int)visionPosEnc1BlobShape.x
                ),
                ReshapeTo4D(
                    visionPosEnc2Output,
                    (int)visionPosEnc2BlobShape.w,
                    (int)visionPosEnc2BlobShape.z,
                    (int)visionPosEnc2BlobShape.y,
                    (int)visionPosEnc2BlobShape.x
                ),
            };

            float[][,,,] backboneFpn = new float[][,,,]
            {
                ReshapeTo4D(
                    backboneFpn0Output,
                    (int)backboneFpn0BlobShape.w,
                    (int)backboneFpn0BlobShape.z,
                    (int)backboneFpn0BlobShape.y,
                    (int)backboneFpn0BlobShape.x
                ),
                ReshapeTo4D(
                    backboneFpn1Output,
                    (int)backboneFpn1BlobShape.w,
                    (int)backboneFpn1BlobShape.z,
                    (int)backboneFpn1BlobShape.y,
                    (int)backboneFpn1BlobShape.x
                ),
                ReshapeTo4D(
                    backboneFpn2Output,
                    (int)backboneFpn2BlobShape.w,
                    (int)backboneFpn2BlobShape.z,
                    (int)backboneFpn2BlobShape.y,
                    (int)backboneFpn2BlobShape.x
                )
            };

            backboneData.visionFeatures = ReshapeTo4D(
                visionFeaturesOutput,
                (int)visionFeaturesBlobShape.w,
                (int)visionFeaturesBlobShape.z,
                (int)visionFeaturesBlobShape.y,
                (int)visionFeaturesBlobShape.x
            );
            backboneData.visionPosEnc = visionPosEnc;
            backboneData.backboneFpn = backboneFpn;

            float[][,,] visionFeats = PrepareBackboneFeatures(backboneData);

            // no_mem_embed = self.trunc_normal((1, 1, 256), std=0.02).astype(np.float32)
            int hidden_dim = 256;
            float[,,] noMemEmbed = TruncNormal(1, 1, hidden_dim, 0.02f);
            float[,,] lastFeat = visionFeats[visionFeats.Length - 1];
            float[,,] updatedFeat = BroadcastAdd3D(lastFeat, noMemEmbed);
            visionFeats[visionFeats.Length - 1] = updatedFeat;

            (int height, int width)[] bbFeatSizes = new (int, int)[]
            {
                (256, 256),
                (128, 128),
                (64, 64)
            };

            // feats = [
            //     np.transpose(feat, (1, 2, 0)).reshape(1, -1, *feat_size)
            //     for feat, feat_size in zip(vision_feats[::-1], bb_feat_sizes[::-1])
            // ][::-1]
            float[][,,,] featsArray = new float[visionFeats.Length][,,,];
            for (int i = 0; i < visionFeats.Length; i++)
            {
                int revIndex = visionFeats.Length - 1 - i;
                float[,,] feat = visionFeats[revIndex]; // shape: (HW, 1, C)
                (int H, int W) = bbFeatSizes[revIndex]; // H, W

                int HW = feat.GetLength(0);
                int one = feat.GetLength(1); // should be 1
                int C = feat.GetLength(2);

                if (HW != H * W)
                    throw new InvalidOperationException($"Expected HW={H * W}, but got {HW}");

                // Transpose to (1, C, HW)
                float[,,] transposed = new float[1, C, HW];
                for (int hw = 0; hw < HW; hw++)
                {
                    for (int c = 0; c < C; c++)
                    {
                        transposed[0, c, hw] = feat[hw, 0, c];
                    }
                }

                // Reshape (1, C, HW) → (1, C, H, W)
                float[,,,] reshaped = new float[1, C, H, W];
                for (int hw = 0; hw < HW; hw++)
                {
                    int h = hw / W;
                    int w = hw % W;
                    for (int c = 0; c < C; c++)
                    {
                        reshaped[0, c, h, w] = transposed[0, c, hw];
                    }
                }

                featsArray[i] = reshaped;
            }

            Array.Reverse(featsArray);
            // features = {"image_embed": feats[-1], "high_res_feats": feats[:-1]}

            float[,,,] lastFeatElement = featsArray[featsArray.Length - 1];

            float[][,,,] allButLast = featsArray.Take(featsArray.Length - 1).ToArray();

            encoderOutput = new float[
                visionFeaturesBlobShape.w
                    * visionFeaturesBlobShape.z
                    * visionFeaturesBlobShape.y
                    * visionFeaturesBlobShape.x
            ];

            encoderOutput = Flatten4D(lastFeatElement);
            Debug.Log("encoderOutput");
            highResFeats = new float[featsArray.Length - 1][];
            for (int i = 0; i < allButLast.Length; i++)
            {
                highResFeats[i] = Flatten4D(allButLast[i]);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Inference error: " + e.Message + "\n" + e.StackTrace);
        }
    }

    private float[,,] BroadcastAdd3D(float[,,] a, float[,,] b)
    {
        int a0 = a.GetLength(0);
        int a1 = a.GetLength(1);
        int a2 = a.GetLength(2);

        int b0 = b.GetLength(0);
        int b1 = b.GetLength(1);
        int b2 = b.GetLength(2);

        // Check dimension compatibility (broadcasting rules)
        if ((b0 != 1 && b0 != a0) || (b1 != 1 && b1 != a1) || (b2 != 1 && b2 != a2))
            throw new ArgumentException("Incompatible shapes for broadcasting.");

        float[,,] result = new float[a0, a1, a2];

        for (int i = 0; i < a0; i++)
        {
            for (int j = 0; j < a1; j++)
            {
                for (int k = 0; k < a2; k++)
                {
                    float valA = a[i, j, k];
                    float valB = b[b0 == 1 ? 0 : i, b1 == 1 ? 0 : j, b2 == 1 ? 0 : k];

                    result[i, j, k] = valA + valB;
                }
            }
        }

        return result;
    }

    private float[,] ResizeBilinear(float[,] input, int newH, int newW)
    {
        int srcH = input.GetLength(0);
        int srcW = input.GetLength(1);
        float[,] result = new float[newH, newW];

        float scaleY = (float)srcH / newH;
        float scaleX = (float)srcW / newW;

        for (int y = 0; y < newH; y++)
        {
            float srcY = y * scaleY;
            int y0 = (int)Math.Floor(srcY);
            int y1 = Math.Min(y0 + 1, srcH - 1);
            float wy = srcY - y0;

            for (int x = 0; x < newW; x++)
            {
                float srcX = x * scaleX;
                int x0 = (int)Math.Floor(srcX);
                int x1 = Math.Min(x0 + 1, srcW - 1);
                float wx = srcX - x0;

                float top = (1 - wx) * input[y0, x0] + wx * input[y0, x1];
                float bottom = (1 - wx) * input[y1, x0] + wx * input[y1, x1];
                result[y, x] = (1 - wy) * top + wy * bottom;
            }
        }

        return result;
    }

    private float[][,,] PrepareBackboneFeatures(BackboneOutputs backboneData)
    {
        int numFeatureLevels = 3;

        float[][,,,] featureMaps = backboneData.backboneFpn.TakeLast(numFeatureLevels).ToArray();
        // for (int i = 0; i < featureMaps.Length; i++)
        // {
        //     Debug.Log(featureMaps[i].Length);
        // }
        // float[][] visionPosEmbeds = backboneData.visionPosEnc.TakeLast(numFeatureLevels).ToArray();

        // float[][,,,] visionPosEmbeds4D = new float[3][,,,]
        // {
        //     ReshapeTo4D(visionPosEmbeds[0], 1, 256, 256, 256),
        //     ReshapeTo4D(visionPosEmbeds[1], 1, 256, 128, 128),
        //     ReshapeTo4D(visionPosEmbeds[2], 1, 256, 64, 64)
        // };

        // float[][,,,] featureMaps4D = new float[3][,,,]
        // {
        //     ReshapeTo4D(featureMaps[0], 1, 128, 128, 128),
        //     ReshapeTo4D(featureMaps[1], 1, 256, 64, 64),
        //     ReshapeTo4D(featureMaps[2], 1, 256, 64, 64)
        // };

        float[][,,] visionFeats = new float[numFeatureLevels][,,];

        for (int i = 0; i < numFeatureLevels; i++)
        {
            float[,,,] x = featureMaps[i];
            int N = x.GetLength(0);
            int C = x.GetLength(1);
            int H = x.GetLength(2);
            int W = x.GetLength(3);
            int HW = H * W;

            float[,,] output = new float[HW, N, C];
            for (int n = 0; n < N; n++)
            {
                for (int c = 0; c < C; c++)
                {
                    for (int h = 0; h < H; h++)
                    {
                        for (int w = 0; w < W; w++)
                        {
                            int hw = h * W + w;
                            output[hw, n, c] = x[n, c, h, w];
                        }
                    }
                }
            }

            visionFeats[i] = output;
        }

        // (int, int)[] feat_sizes = visionPosEmbeds4D
        //     .Select(x => (height: x.GetLength(2), width: x.GetLength(3)))
        //     .ToArray();

        // float[][,,] processed = ProcessVisionPosEmbeds(visionPosEmbeds4D);

        return visionFeats;
    }

    public float[,,] TruncNormal(
        int dim0,
        int dim1,
        int dim2,
        float std = 0.02f,
        float a = -2f,
        float b = 2f
    )
    {
        float[,,] values = new float[dim0, dim1, dim2];

        for (int i = 0; i < dim0; i++)
            for (int j = 0; j < dim1; j++)
                for (int k = 0; k < dim2; k++)
                {
                    float val;
                    do
                    {
                        val = NextGaussian(0f, std);
                    } while (val < a * std || val > b * std);
                    values[i, j, k] = val;
                }

        return values;
    }

    // Generates one sample from a Gaussian distribution using Box-Muller transform
    private float NextGaussian(float mean = 0, float stdDev = 1)
    {
        // Use Box-Muller transform to generate a normal distributed value
        double u1 = 1.0 - _rng.NextDouble(); // uniform(0,1] random doubles
        double u2 = 1.0 - _rng.NextDouble();
        double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2); // random normal(0,1)
        return (float)(mean + stdDev * randStdNormal);
    }

    private float[] Flatten4D(float[,,,] input)
    {
        int N = input.GetLength(0);
        int C = input.GetLength(1);
        int H = input.GetLength(2);
        int W = input.GetLength(3);

        float[] flat = new float[N * C * H * W];
        int index = 0;

        for (int n = 0; n < N; n++)
            for (int c = 0; c < C; c++)
                for (int h = 0; h < H; h++)
                    for (int w = 0; w < W; w++)
                        flat[index++] = input[n, c, h, w];

        return flat;
    }

    private float[,,] ReshapeTo3D(float[] flat, int z, int y, int x)
    {
        if (flat.Length != z * y * x)
            throw new ArgumentException(
                $"Input length {flat.Length} does not match shape ({z}, {y}, {x})"
            );

        float[,,] result = new float[z, y, x];
        int index = 0;

        for (int i = 0; i < z; i++)
            for (int j = 0; j < y; j++)
                for (int k = 0; k < x; k++)
                    result[i, j, k] = flat[index++];

        return result;
    }

    private float[,,,] ReshapeTo4D(float[] array, int w, int z, int y, int x)
    {
        if (array.Length != w * z * y * x)
            throw new ArgumentException(
                "flatArray length does not match the product of dimensions"
            );

        float[,,,] array4D = new float[w, z, y, x];
        int index = 0;
        for (int i = 0; i < w; i++)
        {
            for (int j = 0; j < z; j++)
            {
                for (int k = 0; k < y; k++)
                {
                    for (int l = 0; l < x; l++)
                    {
                        array4D[i, j, k, l] = array[index++];
                    }
                }
            }
        }
        return array4D;
    }

    private float[,,] FourReshapeTo3D(float[,,,] input)
    {
        int d0 = input.GetLength(0);
        int d1 = input.GetLength(1);
        int d2 = input.GetLength(2);
        int d3 = input.GetLength(3);

        int d23 = d2 * d3;
        float[,,] output = new float[d0, d1, d23];

        for (int i0 = 0; i0 < d0; i0++)
        {
            for (int i1 = 0; i1 < d1; i1++)
            {
                for (int i2 = 0; i2 < d2; i2++)
                {
                    for (int i3 = 0; i3 < d3; i3++)
                    {
                        output[i0, i1, i2 * d3 + i3] = input[i0, i1, i2, i3];
                    }
                }
            }
        }

        return output;
    }

    // Transpose axes (2, 0, 1) of 3D array
    // input shape: [d0, d1, d2]
    // output shape: [d2, d0, d1]
    private float[,,] Transpose201(float[,,] input)
    {
        int d0 = input.GetLength(0);
        int d1 = input.GetLength(1);
        int d2 = input.GetLength(2);

        float[,,] output = new float[d2, d0, d1];

        for (int i0 = 0; i0 < d0; i0++)
        {
            for (int i1 = 0; i1 < d1; i1++)
            {
                for (int i2 = 0; i2 < d2; i2++)
                {
                    output[i2, i0, i1] = input[i0, i1, i2];
                }
            }
        }

        return output;
    }

    // Process all vision_pos_embeds arrays similarly to the python list comprehension
    private float[][,,] ProcessVisionPosEmbeds(float[][,,,] visionPosEmbeds4D)
    {
        int n = visionPosEmbeds4D.Length;
        float[][,,] output = new float[n][,,];

        for (int i = 0; i < n; i++)
        {
            float[,,,] current4D = visionPosEmbeds4D[i];
            float[,,] reshaped = FourReshapeTo3D(current4D);
            float[,,] transposed = Transpose201(reshaped);
            output[i] = transposed;
        }
        return output;
    }

    private struct InputData
    {
        public Vector2[] inputPoints;
        public int[] inputLabels;
        public Vector4? inputBox; // x1, y1, x2, y2 or similar
    }

    private struct BackboneOutputs
    {
        public float[,,,] visionFeatures;
        public float[][,,,] visionPosEnc;
        public float[][,,,] backboneFpn;
    }

    private InputData GetInputPoint(
        List<Vector2> posPoints,
        List<Vector2> negPoints,
        Vector4? box = null
    )
    {
        // Handle nulls like Python version
        if (posPoints == null)
        {
            if (negPoints == null && box == null)
            {
                posPoints = new List<Vector2> { new Vector2(0, 0) }; // <-- replace with actual default POINT1
            }
            else
            {
                posPoints = new List<Vector2>();
            }
        }

        if (negPoints == null)
        {
            negPoints = new List<Vector2>();
        }

        List<Vector2> inputPoints = new List<Vector2>();
        List<int> inputLabels = new List<int>();

        foreach (var pt in posPoints)
        {
            inputPoints.Add(pt);
            inputLabels.Add(1);
        }

        foreach (var pt in negPoints)
        {
            inputPoints.Add(pt);
            inputLabels.Add(0);
        }

        return new InputData
        {
            inputPoints = inputPoints.ToArray(),
            inputLabels = inputLabels.ToArray(),
            inputBox = box
        };
    }

    List<Vector2> ConvertFloatArrayToVector2List(float[,] points)
    {
        int rows = points.GetLength(0);
        int cols = points.GetLength(1);

        if (cols != 2)
            throw new System.ArgumentException("Input must have shape [N, 2]");

        List<Vector2> result = new List<Vector2>();
        for (int i = 0; i < rows; i++)
        {
            float x = points[i, 0];
            float y = points[i, 1];
            result.Add(new Vector2(x, y));
        }

        return result;
    }

    public struct ConcatPoints
    {
        public Vector2[] coords;
        public int[] labels;
    }

    // Run Inference
    private (bool[][,], float[]) RunInference(
        int imgWidth,
        int imgHeight,
        float[,] pointCoords,
        float[] pointLabels
    )
    {
        if (
            isQuitting || !modelsInitialized || encoder == null || decoder == null || prompt == null
        )
        {
            return (new bool[0][,], new float[0]);
        }
        if (pointCoords.Length == 0)
        {
            return (new bool[0][,], new float[0]);
        }

        try
        {
            // Call the method
            List<Vector2> posPoints = ConvertFloatArrayToVector2List(pointCoords);
            InputData inputData = GetInputPoint(posPoints, null, null);

            // // Unpack the result
            Vector2[] point_coords = inputData.inputPoints;
            int[] point_labels = inputData.inputLabels;
            Vector4? input_box = inputData.inputBox;

            ConcatPoints? concatPoints = null;

            if (point_coords != null && point_coords.Length > 0)
            {
                concatPoints = new ConcatPoints { coords = point_coords, labels = point_labels };
            }

            if (input_box.HasValue)
            {
                // Convert Vector4 box → 2 corner points
                Vector4 box = input_box.Value;
                Vector2 topLeft = new Vector2(box.x, box.y);
                Vector2 bottomRight = new Vector2(box.z, box.w);

                Vector2[] boxCoords = new Vector2[] { topLeft, bottomRight };
                int[] boxLabels = new int[] { 2, 3 };

                if (point_coords != null && point_coords.Length > 0)
                {
                    // Merge box points + labels with user points
                    Vector2[] concatCoords = new Vector2[boxCoords.Length + point_coords.Length];
                    int[] concatLabels = new int[boxLabels.Length + point_labels.Length];

                    boxCoords.CopyTo(concatCoords, 0);
                    point_coords.CopyTo(concatCoords, boxCoords.Length);

                    boxLabels.CopyTo(concatLabels, 0);
                    point_labels.CopyTo(concatLabels, boxLabels.Length);

                    concatPoints = new ConcatPoints
                    {
                        coords = concatCoords,
                        labels = concatLabels
                    };
                }
                else
                {
                    // Only box points
                    concatPoints = new ConcatPoints { coords = boxCoords, labels = boxLabels };
                }
            }
            else if (point_coords != null && point_coords.Length > 0)
            {
                // Only point prompts
                concatPoints = new ConcatPoints { coords = point_coords, labels = point_labels };
            }

            if (concatPoints == null)
            {
                throw new System.Exception("concatPoints must exist");
            }

            float[,] scaledCoords = ApplyCoordinateScaling(pointCoords, imgHeight, imgWidth);

            // Prepare point coordinates and labels
            int pointCount = pointLabels.Length;
            float[] flattenedCoords = new float[pointCount * 2];
            for (int i = 0; i < pointCount; i++)
            {
                flattenedCoords[i * 2] = scaledCoords[i, 0];
                flattenedCoords[i * 2 + 1] = scaledCoords[i, 1];
            }

            // float[,,] maskInputDummy;
            float[] maskInputDummy;
            float[] masksEnable;

            // if (maskInput == null)
            // {
            maskInputDummy = new float[1 * 256 * 256];
            // maskInputDummy = new float[1, 256, 256];
            masksEnable = new float[1] { 0f };
            // }
            // else
            // {
            //     maskInputDummy = maskInput;
            //     masksEnable = new int[] { 1 };
            // }

            // Get blob indices
            int promptCoordsIndex = prompt.FindBlobIndexByName("coords");
            int promptLabelsIndex = prompt.FindBlobIndexByName("labels");
            int promptMasksIndex = prompt.FindBlobIndexByName("masks");
            int promptMasksEnabledIndex = prompt.FindBlobIndexByName("masks_enable");
            if (
                promptCoordsIndex < 0
                || promptLabelsIndex < 0
                || promptMasksIndex < 0
                || promptMasksEnabledIndex < 0
            )
            {
                Debug.LogError("Failed to find required common blob indices");
                return (new bool[0][,], new float[0]);
            }

            // point_coords: 1x1x2
            ailia.Ailia.AILIAShape concatPointShape = new ailia.Ailia.AILIAShape();
            concatPointShape.dim = 3;
            concatPointShape.z = 1; // batch=1
            concatPointShape.y = 1; // points count (1)
            concatPointShape.x = 2; // 2D coords (x,y)

            // // point_labels: 1x1
            ailia.Ailia.AILIAShape labelsShape = new ailia.Ailia.AILIAShape();
            labelsShape.dim = 2;
            labelsShape.y = 1; // batch=1
            labelsShape.x = 1; // points count (1)

            // maskunput dummy
            ailia.Ailia.AILIAShape masksShape = new ailia.Ailia.AILIAShape();
            masksShape.dim = 3;
            masksShape.z = 1;
            masksShape.y = 256;
            masksShape.x = 256;

            ailia.Ailia.AILIAShape masksEnableShape = new ailia.Ailia.AILIAShape();
            masksEnableShape.dim = 1;
            masksEnableShape.x = 1;

            bool concatPointShapeResult = prompt.SetInputBlobShape(
                concatPointShape,
                (uint)promptCoordsIndex
            );
            bool labelsShapeResult = prompt.SetInputBlobShape(labelsShape, (uint)promptLabelsIndex);

            bool masksShapeResult = prompt.SetInputBlobShape(masksShape, (uint)promptMasksIndex);

            bool masksEnabledShapeResult = prompt.SetInputBlobShape(
                masksEnableShape,
                (uint)promptMasksEnabledIndex
            );

            for (int i = 0; i < flattenedCoords.Length; i++)
            {
                Debug.Log(flattenedCoords[i]);
            }
            bool setCoordsResult = prompt.SetInputBlobData(
                flattenedCoords,
                (uint)promptCoordsIndex
            );

            bool setlabelsResult = prompt.SetInputBlobData(pointLabels, (uint)promptLabelsIndex);

            bool setMaskseResult = prompt.SetInputBlobData(maskInputDummy, (uint)promptMasksIndex);

            bool setMasksEnabledResult = prompt.SetInputBlobData(
                masksEnable,
                (uint)promptMasksEnabledIndex
            );

            if (!setCoordsResult || !setlabelsResult || !masksEnabledShapeResult)
            {
                Debug.LogError("Failed to set required common blob indices");
                return (new bool[0][,], new float[0]);
            }

            // Run prompt
            bool promptResult = prompt.Update();

            if (isQuitting || prompt == null)
            {
                return (new bool[0][,], new float[0]);
            }

            if (!promptResult)
            {
                Debug.LogError(
                    "Prompt inference failed: "
                        + prompt.Status
                        + " ["
                        + prompt.GetErrorDetail()
                        + "]"
                );
                return (new bool[0][,], new float[0]);
            }

            if (isQuitting || prompt == null)
            {
                return (new bool[0][,], new float[0]);
            }

            int sparseEmbeddingsBlobIndex = prompt.FindBlobIndexByName("sparse_embeddings");
            int denseEmbeddingsBlobIndex = prompt.FindBlobIndexByName("dense_embeddings");
            int densePeBlobIndex = prompt.FindBlobIndexByName("dense_pe");

            if (
                sparseEmbeddingsBlobIndex < 0
                || denseEmbeddingsBlobIndex < 0
                || densePeBlobIndex < 0
            )
            {
                Debug.LogError(
                    "Could not find sparse_embeddings, dense_embeddings and dense_pe blobs"
                );
                return (new bool[0][,], new float[0]);
            }

            if (isQuitting || prompt == null)
            {
                return (new bool[0][,], new float[0]);
            }

            // Get blob shapes
            ailia.Ailia.AILIAShape sparseEmbeddingsBlobShape = prompt.GetBlobShape(
                (uint)sparseEmbeddingsBlobIndex
            );

            float[] sparseEmbeddingsOutput = new float[
                sparseEmbeddingsBlobShape.z
                    * sparseEmbeddingsBlobShape.y
                    * sparseEmbeddingsBlobShape.x
            ];

            ailia.Ailia.AILIAShape denseEmbeddingsBlobShape = prompt.GetBlobShape(
                (uint)denseEmbeddingsBlobIndex
            );

            float[] denseEmbeddingsOutput = new float[
                denseEmbeddingsBlobShape.w
                    * denseEmbeddingsBlobShape.z
                    * denseEmbeddingsBlobShape.y
                    * denseEmbeddingsBlobShape.x
            ];

            ailia.Ailia.AILIAShape densePeBlobShape = prompt.GetBlobShape((uint)densePeBlobIndex);

            float[] densePeOutput = new float[
                densePeBlobShape.w * densePeBlobShape.z * densePeBlobShape.y * densePeBlobShape.x
            ];

            prompt.GetBlobData(sparseEmbeddingsOutput, sparseEmbeddingsBlobIndex);
            prompt.GetBlobData(denseEmbeddingsOutput, denseEmbeddingsBlobIndex);
            prompt.GetBlobData(densePeOutput, densePeBlobIndex);

            // Identify mask and score blobs
            int imageEmbeddingsIndex = decoder.FindBlobIndexByName("image_embeddings");
            int imagePeIndex = decoder.FindBlobIndexByName("image_pe");
            int sparsePromptEmbeddingsIndex = decoder.FindBlobIndexByName(
                "sparse_prompt_embeddings"
            );
            int densePromptEmbeddingsIndex = decoder.FindBlobIndexByName("dense_prompt_embeddings");
            int highResFeatures1Index = decoder.FindBlobIndexByName("high_res_features1");
            int highResFeatures2Index = decoder.FindBlobIndexByName("high_res_features2");

            ailia.Ailia.AILIAShape imageEmbeddingsShape = new ailia.Ailia.AILIAShape();
            imageEmbeddingsShape.dim = 4;
            imageEmbeddingsShape.w = 1; // batch=1
            imageEmbeddingsShape.z = 256; // channel=256
            imageEmbeddingsShape.y = 64; // height=64
            imageEmbeddingsShape.x = 64; // width=64

            ailia.Ailia.AILIAShape imagePeShape = new ailia.Ailia.AILIAShape();
            imagePeShape.dim = 4;
            imagePeShape.w = densePeBlobShape.w; // batch=1
            imagePeShape.z = densePeBlobShape.z; // channel=256
            imagePeShape.y = densePeBlobShape.y; // height=64
            imagePeShape.x = densePeBlobShape.x; // width=64

            ailia.Ailia.AILIAShape sparsePromptEmbeddingsShape = new ailia.Ailia.AILIAShape();
            sparsePromptEmbeddingsShape.dim = 3;
            sparsePromptEmbeddingsShape.z = sparseEmbeddingsBlobShape.z;
            sparsePromptEmbeddingsShape.y = sparseEmbeddingsBlobShape.y;
            sparsePromptEmbeddingsShape.x = sparseEmbeddingsBlobShape.x;

            ailia.Ailia.AILIAShape densePromptEmbeddingsShape = new ailia.Ailia.AILIAShape();
            densePromptEmbeddingsShape.dim = 4;
            densePromptEmbeddingsShape.w = denseEmbeddingsBlobShape.w;
            densePromptEmbeddingsShape.z = denseEmbeddingsBlobShape.z;
            densePromptEmbeddingsShape.y = denseEmbeddingsBlobShape.y;
            densePromptEmbeddingsShape.x = denseEmbeddingsBlobShape.x;

            ailia.Ailia.AILIAShape highResFeatures1Shape = new ailia.Ailia.AILIAShape();
            highResFeatures1Shape.dim = 4;
            highResFeatures1Shape.w = 1; // batch=1
            highResFeatures1Shape.z = 32; // channel=256
            highResFeatures1Shape.y = 256; // height=64
            highResFeatures1Shape.x = 256; // width=64

            ailia.Ailia.AILIAShape highResFeatures2Shape = new ailia.Ailia.AILIAShape();
            highResFeatures2Shape.dim = 4;
            highResFeatures2Shape.w = 1; // batch=1
            highResFeatures2Shape.z = 64; // channel=256
            highResFeatures2Shape.y = 128; // height=64
            highResFeatures2Shape.x = 128; // width=64

            if (imageEmbeddingsIndex < 0 || imagePeIndex < 0)
            {
                Debug.LogError("Could not find mask and score blobs");
                return (new bool[0][,], new float[0]);
            }

            if (isQuitting || decoder == null)
            {
                return (new bool[0][,], new float[0]);
            }

            bool imageEmbeddingsShapeResult = decoder.SetInputBlobShape(
                imageEmbeddingsShape,
                (uint)imageEmbeddingsIndex
            );

            bool imagePeShapeResult = decoder.SetInputBlobShape(imagePeShape, (uint)imagePeIndex);

            bool sparsePromptEmbeddingsShapeResult = decoder.SetInputBlobShape(
                sparsePromptEmbeddingsShape,
                (uint)sparsePromptEmbeddingsIndex
            );

            bool densePromptEmbeddingsShapeResult = decoder.SetInputBlobShape(
                densePromptEmbeddingsShape,
                (uint)densePromptEmbeddingsIndex
            );

            bool highResFeatures1ShapeResult = decoder.SetInputBlobShape(
                highResFeatures1Shape,
                (uint)highResFeatures1Index
            );

            bool highResFeatures2ShapeResult = decoder.SetInputBlobShape(
                highResFeatures2Shape,
                (uint)highResFeatures2Index
            );

            bool imageEmbeddingsResult = decoder.SetInputBlobData(
                encoderOutput,
                (uint)imageEmbeddingsIndex
            );

            bool imagePeResult = decoder.SetInputBlobData(densePeOutput, (uint)imagePeIndex);

            bool sparseEmbeddingsResult = decoder.SetInputBlobData(
                sparseEmbeddingsOutput,
                (uint)sparsePromptEmbeddingsIndex
            );

            bool denseEmbeddingsResult = decoder.SetInputBlobData(
                denseEmbeddingsOutput,
                (uint)densePromptEmbeddingsIndex
            );

            bool highResFeatures1Result = decoder.SetInputBlobData(
                highResFeats[0],
                (uint)highResFeatures1Index
            );

            bool highResFeatures2Result = decoder.SetInputBlobData(
                highResFeats[1],
                (uint)highResFeatures2Index
            );

            // Run decoder
            bool decoderResult = decoder.Update();

            if (isQuitting || decoder == null)
            {
                return (new bool[0][,], new float[0]);
            }

            if (!decoderResult)
            {
                Debug.LogError(
                    "Prompt inference failed: "
                        + decoder.Status
                        + " ["
                        + decoder.GetErrorDetail()
                        + "]"
                );
                return (new bool[0][,], new float[0]);
            }

            // masks, iou_pred, sam_tokens_out, object_score_logits  = mask_decoder.run
            // Get blob shapes
            int masksBlobIndex = decoder.FindBlobIndexByName("masks");
            int iouPredBlobIndex = decoder.FindBlobIndexByName("iou_pred");
            int samTokensOutBlobIndex = decoder.FindBlobIndexByName("sam_tokens_out");
            int objectScoreLogitsBlobIndex = decoder.FindBlobIndexByName("object_score_logits");
            ailia.Ailia.AILIAShape masksBlobShape = decoder.GetBlobShape((uint)masksBlobIndex);
            float[] masksBlobOutput = new float[
                masksBlobShape.w * masksBlobShape.z * masksBlobShape.y * masksBlobShape.x
            ];

            ailia.Ailia.AILIAShape iouPredBlobShape = decoder.GetBlobShape((uint)iouPredBlobIndex);
            float[] iouPredBlobOutput = new float[iouPredBlobShape.y * iouPredBlobShape.x];

            ailia.Ailia.AILIAShape samTokensOutBlobShape = decoder.GetBlobShape(
                (uint)samTokensOutBlobIndex
            );
            float[] samTokensOutBlobOutput = new float[
                samTokensOutBlobShape.z * samTokensOutBlobShape.y * samTokensOutBlobShape.x
            ];

            ailia.Ailia.AILIAShape objectScoreLogitsBlobShape = decoder.GetBlobShape(
                (uint)objectScoreLogitsBlobIndex
            );
            float[] objectScoreLogitsBlobOutput = new float[
                objectScoreLogitsBlobShape.y * objectScoreLogitsBlobShape.x
            ];

            bool getMasksBlobResult = decoder.GetBlobData(masksBlobOutput, (uint)masksBlobIndex);
            bool getiouPredBlobResult = decoder.GetBlobData(
                iouPredBlobOutput,
                (uint)iouPredBlobIndex
            );
            bool getsamTokensBlobResult = decoder.GetBlobData(
                samTokensOutBlobOutput,
                (uint)samTokensOutBlobIndex
            );

            bool objectScoreLogitsBlobResult = decoder.GetBlobData(
                objectScoreLogitsBlobOutput,
                (uint)objectScoreLogitsBlobIndex
            );

            float[,,,] masks = ReshapeTo4D(
                masksBlobOutput,
                (int)masksBlobShape.w,
                (int)masksBlobShape.z,
                (int)masksBlobShape.y,
                (int)masksBlobShape.x
            );

            float[,] iouPred = ReshapeTo2D(
                iouPredBlobOutput,
                (int)iouPredBlobShape.y,
                (int)iouPredBlobShape.x
            );
            float[,,] samTokens = ReshapeTo3D(
                samTokensOutBlobOutput,
                (int)samTokensOutBlobShape.z,
                (int)samTokensOutBlobShape.y,
                (int)samTokensOutBlobShape.x
            );
            float[,] objectScore = ReshapeTo2D(
                objectScoreLogitsBlobOutput,
                (int)objectScoreLogitsBlobShape.y,
                (int)objectScoreLogitsBlobShape.x
            );
            // masks = masks[:, 1:, :, :] 1
            // float[,,,] lowResMasks = SliceChannelDim(masks);
            // iou_pred = iou_pred[:, 1:] 2
            // iouPred = Slice2DColumns(iouPred, 1);
            // sam_tokens_out = mask_tokens_out[:, 1:]
            // samTokens = Slice3D_Dim1_From1(samTokens, 1);
            // masks = self.postprocess_masks(
            //     low_res_masks, orig_hw
            // )
            // float[,,,] resized = PostprocessMasks(lowResMasks, new int[] { imgHeight, imgWidth });
            // low_res_masks 3
            // Clip(ref lowResMasks, -32.0f, -32.0f);
            // float maskThreshold = 0.0f;
            // 1
            // bool[,,,] binaryMask = ThresholdMask(resized, maskThreshold);

            // sorted_ind = np.argsort(scores)[::-1]
            // int[] sortedInd = ArgsortDescending(iouPred);

            // bool[,,,] newMasks = ReorderMasks(binaryMask, sortedInd);

            // bool[][,] masksResult = new bool[1][,];
            // masksResult[0] = PostprocessMask(resized, imgHeight, imgWidth);
            // Debug.Log(iouPred[0, 0]);

            if (isQuitting || decoder == null)
            {
                return (new bool[0][,], new float[0]);
            }

            int numMasks = (int)iouPredBlobShape.x;
            // Debug.Log(numMasks);
            int maskWidth = (int)masksBlobShape.x;
            int maskHeight = (int)masksBlobShape.y;
            int maskArea = maskWidth * maskHeight;
            bool[][,] masksResult = new bool[numMasks][,];

            // Debug.Log(maskWidth);
            // Debug.Log(maskHeight);
            // Debug.Log(maskArea);
            // Debug.Log(masksBlobOutput.Length);
            // float[] maskUpscaled = ResizeMask1D(masksBlobOutput, maskHeight, maskWidth, imgHeight, imgWidth);

            for (int i = 0; i < numMasks; i++)
            {
                float[,,,] maskTensor = ReshapeToTensor(
                    masksBlobOutput,
                    i,
                    maskHeight,
                    maskWidth,
                    maskArea
                );
                masksResult[i] = PostprocessMask(maskTensor, imgHeight, imgWidth);
            }

            return (masksResult, iouPredBlobOutput);
        }
        catch (Exception e)
        {
            Debug.LogError("Inference error: " + e.Message + "\n" + e.StackTrace);
            return (new bool[0][,], new float[0]);
        }
    }

    // private bool[,,,] ReorderMasks(bool[,,,] masks, int[] sortedInd)
    // {
    //     int n = sortedInd.Length;
    //     int c = masks.GetLength(1);
    //     int h = masks.GetLength(2);
    //     int w = masks.GetLength(3);

    //     bool[,,,] reordered = new bool[n, c, h, w];

    //     for (int i = 0; i < n; i++)
    //     {
    //         int srcIndex = sortedInd[i];
    //         for (int j = 0; j < c; j++)
    //             for (int y = 0; y < h; y++)
    //                 for (int x = 0; x < w; x++)
    //                     reordered[i, j, y, x] = masks[srcIndex, j, y, x];
    //     }

    //     return reordered;
    // }

    // private int[] ArgsortDescending(float[,] scores)
    // {
    //     int rows = scores.GetLength(0);
    //     int cols = scores.GetLength(1);
    //     int total = rows * cols;

    //     // Create index-value pairs
    //     var indexed = new List<(int index, float value)>(total);
    //     for (int i = 0; i < rows; i++)
    //         for (int j = 0; j < cols; j++)
    //             indexed.Add((i * cols + j, scores[i, j]));

    //     // Sort descending by value
    //     var sorted = indexed.OrderByDescending(x => x.value).Select(x => x.index).ToArray();

    //     return sorted;
    // }

    // private float[] ResizeMask1D(float[] input, int inH, int inW, int outH, int outW)
    // {
    //     float[] output = new float[outH * outW];
    //     float yScale = (float)inH / outH;
    //     float xScale = (float)inW / outW;

    //     for (int y = 0; y < outH; y++)
    //     {
    //         float inY = y * yScale;
    //         int y0 = (int)Math.Floor(inY);
    //         int y1 = Math.Min(y0 + 1, inH - 1);
    //         float yLerp = inY - y0;

    //         for (int x = 0; x < outW; x++)
    //         {
    //             float inX = x * xScale;
    //             int x0 = (int)Math.Floor(inX);
    //             int x1 = Math.Min(x0 + 1, inW - 1);
    //             float xLerp = inX - x0;

    //             // Get pixel values
    //             float v00 = input[y0 * inW + x0];
    //             float v01 = input[y0 * inW + x1];
    //             float v10 = input[y1 * inW + x0];
    //             float v11 = input[y1 * inW + x1];

    //             float top = (1 - xLerp) * v00 + xLerp * v01;
    //             float bottom = (1 - xLerp) * v10 + xLerp * v11;
    //             float value = (1 - yLerp) * top + yLerp * bottom;

    //             output[y * outW + x] = value;
    //         }
    //     }

    //     return output;
    // }

    // private void Clip(ref float[,,,] array, float minValue, float maxValue)
    // {
    //     int d0 = array.GetLength(0);
    //     int d1 = array.GetLength(1);
    //     int d2 = array.GetLength(2);
    //     int d3 = array.GetLength(3);

    //     for (int i0 = 0; i0 < d0; i0++)
    //         for (int i1 = 0; i1 < d1; i1++)
    //             for (int i2 = 0; i2 < d2; i2++)
    //                 for (int i3 = 0; i3 < d3; i3++)
    //                 {
    //                     if (array[i0, i1, i2, i3] < minValue)
    //                         array[i0, i1, i2, i3] = minValue;
    //                     else if (array[i0, i1, i2, i3] > maxValue)
    //                         array[i0, i1, i2, i3] = maxValue;
    //                 }
    // }

    // private bool[,,,] ThresholdMask(float[,,,] input, float threshold)
    // {
    //     int d0 = input.GetLength(0);
    //     int d1 = input.GetLength(1);
    //     int d2 = input.GetLength(2);
    //     int d3 = input.GetLength(3);

    //     bool[,,,] mask = new bool[d0, d1, d2, d3];

    //     for (int i0 = 0; i0 < d0; i0++)
    //         for (int i1 = 0; i1 < d1; i1++)
    //             for (int i2 = 0; i2 < d2; i2++)
    //                 for (int i3 = 0; i3 < d3; i3++)
    //                     mask[i0, i1, i2, i3] = input[i0, i1, i2, i3] > threshold;

    //     return mask;
    // }

    // private float[,,,] PostprocessMasks(float[,,,] masks, int[] origHW)
    // {
    //     int N = masks.GetLength(0);
    //     int C = masks.GetLength(1);
    //     int H = masks.GetLength(2);
    //     int W = masks.GetLength(3);

    //     int newH = origHW[0];
    //     int newW = origHW[1];
    //     float[,,,] output = new float[N, C, newH, newW];

    //     for (int n = 0; n < N; n++)
    //     {
    //         for (int c = 0; c < C; c++)
    //         {
    //             // Extract [H, W] slice for mask[n, c]
    //             float[,] src = new float[H, W];
    //             for (int h = 0; h < H; h++)
    //                 for (int w = 0; w < W; w++)
    //                     src[h, w] = masks[n, c, h, w];

    //             // Resize using bilinear interpolation
    //             float[,] resized = ResizeBilinear(src, newH, newW);

    //             // Store into output[n, c]
    //             for (int h = 0; h < newH; h++)
    //                 for (int w = 0; w < newW; w++)
    //                     output[n, c, h, w] = resized[h, w];
    //         }
    //     }

    //     return output;
    // }

    // private float[,,,] SliceChannelDim(float[,,,] input)
    // {
    //     int N = input.GetLength(0);
    //     int C = input.GetLength(1);
    //     int H = input.GetLength(2);
    //     int W = input.GetLength(3);

    //     float[,,,] result = new float[N, C - 1, H, W];

    //     for (int n = 0; n < N; n++)
    //         for (int c = 1; c < C; c++) // start at 1
    //             for (int h = 0; h < H; h++)
    //                 for (int w = 0; w < W; w++)
    //                     result[n, c - 1, h, w] = input[n, c, h, w];

    //     return result;
    // }

    // private float[,,] Slice3D_Dim1_From1(float[,,] input, int start)
    // {
    //     int D0 = input.GetLength(0); // A
    //     int D1 = input.GetLength(1); // B
    //     int D2 = input.GetLength(2); // C

    //     if (start >= D1)
    //         throw new ArgumentException("Start index is out of bounds for axis 1");

    //     float[,,] output = new float[D0, D1 - start, D2];

    //     for (int i = 0; i < D0; i++)
    //         for (int j = 1; j < D1; j++)
    //             for (int k = 0; k < D2; k++)
    //                 output[i, j - start, k] = input[i, j, k];

    //     return output;
    // }

    private float[,] ReshapeTo2D(float[] flat, int rows, int cols)
    {
        if (flat.Length != rows * cols)
            throw new ArgumentException("Size mismatch between flat array and target shape.");

        float[,] result = new float[rows, cols];

        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                result[r, c] = flat[r * cols + c];

        return result;
    }

    // private float[,] Slice2DColumns(float[,] input, int startCol)
    // {
    //     int rows = input.GetLength(0);
    //     int cols = input.GetLength(1);

    //     if (startCol >= cols)
    //         throw new ArgumentException("startCol is out of range.");

    //     int newCols = cols - startCol;
    //     float[,] result = new float[rows, newCols];

    //     for (int r = 0; r < rows; r++)
    //         for (int c = 0; c < newCols; c++)
    //             result[r, c] = input[r, c + startCol];

    //     return result;
    // }

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
        float scale = GetScale(imgWidth, imgHeight);
        float[,] scaledCoords = new float[coords.GetLength(0), coords.GetLength(1)];

        for (int i = 0; i < coords.GetLength(0); i++)
        {
            scaledCoords[i, 0] = coords[i, 0] * scale;
            scaledCoords[i, 1] = coords[i, 1] * scale;
        }

        return scaledCoords;
    }

    // Overlay mask on original image
    private Texture2D CreateMaskedImage(
        bool[,] mask,
        Color32[] pixels,
        int imageWidth,
        int imageHeight
    )
    {
        Texture2D result = new Texture2D(imageWidth, imageHeight, TextureFormat.ARGB32, false);

        int maskHeight = mask.GetLength(0);
        int maskWidth = mask.GetLength(1);

        // Apply mask to original image - optimized inner loop
        for (int y = 0; y < maskHeight; y++)
        {
            int unityY = y;
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

    private Texture2D CreateEmptyMaskedImage(Color32[] pixels, int imageWidth, int imageHeight)
    {
        Texture2D result = new Texture2D(imageWidth, imageHeight, TextureFormat.ARGB32, false);

        // Apply mask to original image - optimized inner loop
        for (int y = 0; y < imageHeight; y++)
        {
            int unityY = y;
            int rowOffset = unityY * imageWidth;

            for (int x = 0; x < imageWidth; x++)
            {
                int pixelIndex = rowOffset + x;

                if (pixelIndex >= 0 && pixelIndex < pixels.Length)
                {
                    Color32 originalColor = pixels[pixelIndex];
                    pixels[pixelIndex] = originalColor;
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
            if (labels[i] >= 2)
            {
                continue;
            }

            int px = Mathf.Clamp((int)coords[i, 0], 0, image.width - 1);
            int origY = (int)coords[i, 1];

            // Convert to Unity coordinates
            int py = origY;
            py = Mathf.Clamp(py, 0, image.height - 1);

            Color32 markerColor =
                labels[i] == 1 ? new Color32(0, 255, 0, 255) : new Color32(0, 0, 255, 255);

            //Debug.Log($"{coords[i, 0]},{coords[i, 1]} => {px}, {py}");

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

    private float GetScale(int texWidth, int texHeight)
    {
        return targetSize / (float)Mathf.Max(texWidth, texHeight);
    }

    // Preprocess image for model input
    private (float[], ValueTuple<int, int>) PreprocessImage2(
        Color32[] camera,
        int texWidth,
        int texHeight
    )
    {
        // Create CHW layout data array (channel, height, width)
        float[] normalizedData = new float[3 * targetSize * targetSize];

        // Optimize by computing constants outside the inner loop
        int ch0Offset = 0;
        int ch1Offset = targetSize * targetSize;
        int ch2Offset = 2 * targetSize * targetSize;

        float scale = GetScale(texWidth, texHeight);

        // Fill with normalized values and padding
        for (int y = 0; y < targetSize; y++)
        {
            for (int x = 0; x < targetSize; x++)
            {
                float fx = x / scale;
                float fy = y / scale;
                Color32 v = Bilinear(camera, texWidth, texHeight, fx, fy);

                int baseIdx = y * targetSize + x; // Top2Bottom

                normalizedData[ch0Offset + baseIdx] = (v.r / 255.0f - Mean[0]) / Std[0];
                normalizedData[ch1Offset + baseIdx] = (v.g / 255.0f - Mean[1]) / Std[1];
                normalizedData[ch2Offset + baseIdx] = (v.b / 255.0f - Mean[2]) / Std[2];
            }
        }

        return (normalizedData, (texHeight, texWidth));
    }

    private Color32 Bilinear(Color32[] face, int w, int h, float fx, float fy)
    {
        // Debug.Log("Bilinear");
        int x2 = (int)fx;
        int y2 = (int)fy;
        float xa = 1.0f - (fx - x2);
        float xb = 1.0f - xa;
        float ya = 1.0f - (fy - y2);
        float yb = 1.0f - ya;
        if (x2 >= w || y2 >= h || x2 < 0 || y2 < 0){
            return new Color32(0, 0, 0, 255);
        }
        Color32 c1 = face[y2 * w + x2];
        Color32 c2 = (x2+1 < w) ? face[y2 * w + x2 + 1] : c1;
        Color32 c3 = (y2+1 < h) ? face[(y2 + 1) * w + x2] : c1;
        Color32 c4 = (x2+1 < w && y2+1 < h) ? face[(y2 + 1) * w + x2 + 1] : c1;
        byte r = (byte)(c1.r * xa * ya + c2.r * xb * ya + c3.r * xa * yb + c4.r * xb * yb);
        byte g = (byte)(c1.g * xa * ya + c2.g * xb * ya + c3.g * xa * yb + c4.g * xb * yb);
        byte b = (byte)(c1.b * xa * ya + c2.b * xb * ya + c3.b * xa * yb + c4.b * xb * yb);
        return new Color32(r, g, b, 255);
    }

    private float[,,,] PreprocessImage(
        Color32[] inputImage,
        int originalWidth,
        int originalHeight,
        int imageSize
    )
    {
        // 2. Resize image to (imageSize, imageSize)
        float scale = GetScale(originalWidth, originalHeight);
        int newWidth = imageSize;
        int newHeight = imageSize;
        Color32[] resizedImage = ResizeColor32(
            inputImage,
            originalWidth,
            originalHeight,
            newWidth,
            newHeight
        );

        // 3. Convert Color32[] to float[height, width, channels] normalized 0-1
        float[,,] floatImage = Color32ArrayToFloatArray(resizedImage, newHeight, newWidth);

        // 4. Normalize per channel (subtract mean, divide std)
        float[] mean = new float[] { 0.485f, 0.456f, 0.406f };
        float[] std = new float[] { 0.229f, 0.224f, 0.225f };

        for (int h = 0; h < newHeight; h++)
        {
            for (int w = 0; w < newWidth; w++)
            {
                for (int c = 0; c < 3; c++)
                {
                    floatImage[h, w, c] = (floatImage[h, w, c] - mean[c]) / std[c];
                }
            }
        }

        // 5. Transpose (H, W, C) => (C, H, W)
        float[,,] chw = TransposeHWCtoCHW(floatImage);

        // 6. Add batch dimension => (1, C, H, W)
        float[,,,] batch = new float[1, 3, newHeight, newWidth];
        for (int c = 0; c < 3; c++)
            for (int h = 0; h < newHeight; h++)
                for (int w = 0; w < newWidth; w++)
                    batch[0, c, h, w] = chw[c, h, w];

        return batch;
    }

    // Helper: Resize Color32[] with nearest neighbor (or implement your own)
    private Color32[] ResizeColor32(
        Color32[] src,
        int srcWidth,
        int srcHeight,
        int dstWidth,
        int dstHeight
    )
    {
        Color32[] dst = new Color32[dstWidth * dstHeight];
        for (int y = 0; y < dstHeight; y++)
        {
            int srcY = y * srcHeight / dstHeight;
            for (int x = 0; x < dstWidth; x++)
            {
                int srcX = x * srcWidth / dstWidth;
                dst[y * dstWidth + x] = src[srcY * srcWidth + srcX];
            }
        }
        return dst;
    }

    // Helper: Convert Color32[] to float[height,width,channels], normalized to [0..1]
    private float[,,] Color32ArrayToFloatArray(Color32[] image, int height, int width)
    {
        float[,,] result = new float[height, width, 3];
        for (int i = 0; i < height * width; i++)
        {
            Color32 c = image[i];
            int h = i / width;
            int w = i % width;
            result[h, w, 0] = c.r / 255f;
            result[h, w, 1] = c.g / 255f;
            result[h, w, 2] = c.b / 255f;
        }
        return result;
    }

    // Helper: Transpose (H,W,C) => (C,H,W)
    private float[,,] TransposeHWCtoCHW(float[,,] input)
    {
        int H = input.GetLength(0);
        int W = input.GetLength(1);
        int C = input.GetLength(2);
        float[,,] output = new float[C, H, W];
        for (int h = 0; h < H; h++)
            for (int w = 0; w < W; w++)
                for (int c = 0; c < C; c++)
                    output[c, h, w] = input[h, w, c];
        return output;
    }

    private bool[,] PostprocessMask(float[,,,] mask, int origHeight, int origWidth)
    {
        int maskHeight = mask.GetLength(2);
        int maskWidth = mask.GetLength(3);

        // Final resize to targetSize
        bool[,] finalMask = new bool[origHeight, origWidth];

        for (int y = 0; y < origHeight; y++)
        {
            float srcY = y * maskHeight / origHeight;
            int y0 = (int)Math.Floor(srcY);
            int y1 = Math.Min(y0 + 1, maskHeight - 1);
            float wy = srcY - y0;

            for (int x = 0; x < origWidth; x++)
            {
                float srcX = x * maskWidth / origWidth;
                int x0 = (int)Math.Floor(srcX);
                int x1 = Math.Min(x0 + 1, maskWidth - 1);
                float wx = srcX - x0;

                // Bilinear interpolation
                float val =
                    (1 - wy) * (1 - wx) * mask[0, 0, y0, x0]
                    + wy * (1 - wx) * mask[0, 0, y1, x0]
                    + (1 - wy) * wx * mask[0, 0, y0, x1]
                    + wy * wx * mask[0, 0, y1, x1];

                finalMask[y, x] = val > 0;
            }
        }

        return finalMask;
    }

    // Calculate embedding of input image
    // image : top bottom format
    public void ProcessEmbedding(Color32[] image, int imageWidth, int imageHeight)
    {
        if (!modelsInitialized || isProcessing || isQuitting)
        {
            return;
        }
        RunEmbedding(image, imageWidth, imageHeight);
    }

    // Check embedding exist
    public bool EmbeddingExist()
    {
        return encoderOutput != null;
    }

    // Calculate mask of input image
    // image : top bottom format
    public void ProcessMask(Color32[] image, int imageWidth, int imageHeight)
    {
        if (!modelsInitialized || isProcessing || isQuitting)
        {
            return;
        }

        try
        {
            isProcessing = true;

            // Set up point coords for inference
            float[,] coords = GetClickPoints(imageHeight);
            float[] labels = GetPointLabels();

            string coordsLog = $"Points input ({labels.Length}): ";
            for (int i = 0; i < labels.Length; i += 1)
            {
                coordsLog += $"({coords[i, 0]},{-coords[i, 1] - imageHeight + 1})[{labels[i]}]";
            }

            var (masks, scores) = RunInference(imageWidth, imageHeight, coords, labels);

            if (isQuitting)
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

                visualizedResult = CreateMaskedImage(
                    masks[bestMaskIndex],
                    image,
                    imageWidth,
                    imageHeight
                );

                // Only draw click points if the showClickPoints flag is enabled
                if (showClickPoints)
                {
                    visualizedResult = DrawClickPoints(coords, labels, visualizedResult);
                }

                if (saveFrame)
                {
                    SaveFrameAsPNG(visualizedResult);
                }

                success = true;
            }
            else
            {
                //Debug.LogWarning($"No valid masks");
                visualizedResult = CreateEmptyMaskedImage(image, imageWidth, imageHeight);
                success = true;
            }
        }
        catch (OperationCanceledException)
        {
            // Silent cancellation
            success = false;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error in ProcessCurrentFrame: {e.Message}\n{e.StackTrace}");
            success = false;
        }
        finally
        {
            isProcessing = false;
        }
    }

    private string CreateOutputDirectory()
    {
        try
        {
            // Create base directory using Application.persistentDataPath for cross-platform support
            string directory = System.IO.Path.Combine(Application.persistentDataPath, "ailiaSAM1");

            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
                Debug.Log($"Created output directory: {directory}");
            }

            return directory;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to create output directory: {e.Message}");
        }

        return Application.persistentDataPath;
    }

    private void SaveFrameAsPNG(Texture2D texture)
    {
        try
        {
            string fileNamePrefix = "output";
            // Create path to output directory
            string directory = System.IO.Path.Combine(
                Application.persistentDataPath,
                CreateOutputDirectory()
            );

            // Ensure directory exists
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }

            // Generate filename with frame number and optional timestamp
            string fileName = $"{fileNamePrefix}.png";

            // Full path to file
            string filePath = System.IO.Path.Combine(directory, fileName);

            // Convert texture to PNG bytes
            byte[] bytes = texture.EncodeToPNG();

            // Save synchronously to ensure completion
            System.IO.File.WriteAllBytes(filePath, bytes);

            Debug.Log($"Saved output to {fileName}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error saving output: {e.Message}\n{e.StackTrace}");
        }
    }

    public void Destroy()
    {
        encoder.Close();
        decoder.Close();
    }

    public string EnvironmentName()
    {
        return encoder.EnvironmentName();
    }
}
