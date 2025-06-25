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
    private const string promptWeightPath = "prompt_encoder_hiera_l.onnx";
    private const string promptProtoPath = "prompt_encoder_hiera_l.onnx.prototxt";

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

        // Debug.Log($"boxCoords set to {box.xMin},{box.yMin} {box.xMax},{box.yMax}");
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

    public List<ModelDownloadURL> GetModelURLs(ImageSegmentaionModels modelType)
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
        modelDownloadURLs.Add(
            new ModelDownloadURL() { folder_path = serverFolderName, file_name = promptWeightPath }
        );
        modelDownloadURLs.Add(
            new ModelDownloadURL() { folder_path = serverFolderName, file_name = promptProtoPath }
        );

        return modelDownloadURLs;
    }

    // Initialize Ailia models
    public bool InitializeModels(ImageSegmentaionModels modelType, bool gpuMode)
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
            string pmtPath = System.IO.Path.Combine(
                Application.temporaryCachePath,
                promptWeightPath
            );
            string pmtProtoPath = System.IO.Path.Combine(
                Application.temporaryCachePath,
                promptProtoPath
            );

            encoder = new ailia.AiliaModel();
            decoder = new ailia.AiliaModel();
            prompt = new ailia.AiliaModel();

            uint memory_mode =
                ailia.Ailia.AILIA_MEMORY_REDUCE_CONSTANT
                | ailia.Ailia.AILIA_MEMORY_REDUCE_CONSTANT_WITH_INPUT_INITIALIZER
                | ailia.Ailia.AILIA_MEMORY_REUSE_INTERSTAGE;
            memory_mode = ailia.Ailia.AILIA_MEMORY_REDUCE_INTERSTAGE;
            encoder.SetMemoryMode(memory_mode);
            decoder.SetMemoryMode(memory_mode);
            prompt.SetMemoryMode(memory_mode);

            this.gpuMode = gpuMode;
            if (gpuMode)
            {
                encoder.Environment(ailia.Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
                decoder.Environment(ailia.Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
                prompt.Environment(ailia.Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
            }

            bool encOpened = false;
            bool decOpened = false;
            bool promptOpened = false;

            encOpened = encoder.OpenFile(encProtoPath, encPath);
            decOpened = decoder.OpenFile(decProtoPath, decPath);
            promptOpened = prompt.OpenFile(pmtProtoPath, pmtPath);

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
        if (isQuitting || !modelsInitialized || encoder == null)
        {
            return;
        }

        float[,,,] inputTensor = PreprocessImage(image, imgWidth, imgHeight, targetSize);
        float[] nchwInput = Flatten4D(inputTensor);

        try
        {
            int imgIndex = encoder.FindBlobIndexByName("input_image");

            // Set encoder input shape (1x3x1024x1024)
            ailia.Ailia.AILIAShape encInputShape = new ailia.Ailia.AILIAShape();
            encInputShape.dim = 4;
            encInputShape.w = 1;
            encInputShape.z = 3;
            encInputShape.y = (uint)targetSize;
            encInputShape.x = (uint)targetSize;

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

            bool dataSetResult = encoder.SetInputBlobData(nchwInput, imgIndex);
            // Debug.Log($"updated SET DATA {dataSetResult}");

            bool encResult = encoder.Update();

            if (isQuitting || encoder == null)
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

            float[][,,,] featsArray = new float[visionFeats.Length][,,,];
            for (int i = 0; i < visionFeats.Length; i++)
            {
                int revIndex = visionFeats.Length - 1 - i;
                float[,,] feat = visionFeats[revIndex]; // shape: (HW, 1, C)
                (int H, int W) = bbFeatSizes[revIndex]; // H, W

                int HW = feat.GetLength(0);
                int one = feat.GetLength(1);
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

            float[,,,] lastFeatElement = featsArray[featsArray.Length - 1];

            float[][,,,] allButLast = featsArray.Take(featsArray.Length - 1).ToArray();

            encoderOutput = new float[
                visionFeaturesBlobShape.w
                    * visionFeaturesBlobShape.z
                    * visionFeaturesBlobShape.y
                    * visionFeaturesBlobShape.x
            ];

            encoderOutput = Flatten4D(lastFeatElement);
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

    private float[][,,] PrepareBackboneFeatures(BackboneOutputs backboneData)
    {
        int numFeatureLevels = 3;

        float[][,,,] featureMaps = backboneData.backboneFpn.TakeLast(numFeatureLevels).ToArray();

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
        double u1 = 1.0 - _rng.NextDouble();
        double u2 = 1.0 - _rng.NextDouble();
        double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
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

    private struct BackboneOutputs
    {
        public float[,,,] visionFeatures;
        public float[][,,,] visionPosEnc;
        public float[][,,,] backboneFpn;
    }

    // Run Inference
    private (bool[][,], float[]) RunInference(
        int imgWidth,
        int imgHeight,
        float[,] pointCoords,
        float[] pointLabels
    )
    {
        if (isQuitting || !modelsInitialized || decoder == null || prompt == null)
        {
            return (new bool[0][,], new float[0]);
        }
        if (pointCoords.Length == 0)
        {
            return (new bool[0][,], new float[0]);
        }

        try
        {
            float[,] scaledCoords = ApplyCoordinateScaling(pointCoords, imgHeight, imgWidth);

            // Prepare point coordinates and labels
            int pointCount = pointLabels.Length;
            float[] flattenedCoords = new float[pointCount * 2];
            for (int i = 0; i < pointCount; i++)
            {
                flattenedCoords[i * 2] = scaledCoords[i, 0];
                flattenedCoords[i * 2 + 1] = scaledCoords[i, 1];
            }

            float[] maskInputDummy;
            float[] masksEnable;

            int maskInputDummyChannel = 1;
            int maskInputDummyHeight = 256;
            int maskInputDummyWidth = 256;
            maskInputDummy = new float[
                maskInputDummyChannel * maskInputDummyHeight * maskInputDummyWidth
            ];
            masksEnable = new float[1] { 0f };

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

            ailia.Ailia.AILIAShape concatPointShape = new ailia.Ailia.AILIAShape();
            concatPointShape.dim = 3;
            concatPointShape.z = 1;
            concatPointShape.y = (uint)pointCount;
            concatPointShape.x = 2;

            ailia.Ailia.AILIAShape labelsShape = new ailia.Ailia.AILIAShape();
            labelsShape.dim = 2;
            labelsShape.y = 1;
            labelsShape.x = (uint)pointCount; // points count (1)

            ailia.Ailia.AILIAShape masksShape = new ailia.Ailia.AILIAShape();
            masksShape.dim = 3;
            masksShape.z = (uint)maskInputDummyChannel;
            masksShape.y = (uint)maskInputDummyHeight;
            masksShape.x = (uint)maskInputDummyWidth;

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

            if (
                !concatPointShapeResult
                || !labelsShapeResult
                || !masksShapeResult
                || !masksEnabledShapeResult
            )
            {
                Debug.LogError("Failed to set input blob shapes");
                return (new bool[0][,], new float[0]);
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

            if (!setCoordsResult || !setlabelsResult || !setMaskseResult || !setMasksEnabledResult)
            {
                Debug.LogError("Failed to set blob data");
                return (new bool[0][,], new float[0]);
            }

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
                    "Could not find sparse_embeddings, dense_embeddings and dense_pe indices"
                );
                return (new bool[0][,], new float[0]);
            }

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

            int imageEmbeddingsIndex = decoder.FindBlobIndexByName("image_embeddings");
            int imagePeIndex = decoder.FindBlobIndexByName("image_pe");
            int sparsePromptEmbeddingsIndex = decoder.FindBlobIndexByName(
                "sparse_prompt_embeddings"
            );
            int densePromptEmbeddingsIndex = decoder.FindBlobIndexByName("dense_prompt_embeddings");
            int highResFeatures1Index = decoder.FindBlobIndexByName("high_res_features1");
            int highResFeatures2Index = decoder.FindBlobIndexByName("high_res_features2");

            if (
                imageEmbeddingsIndex < 0
                || imagePeIndex < 0
                || sparsePromptEmbeddingsIndex < 0
                || densePromptEmbeddingsIndex < 0
                || highResFeatures1Index < 0
                || highResFeatures2Index < 0
            )
            {
                Debug.LogError("Could not find required indices");
                return (new bool[0][,], new float[0]);
            }

            if (isQuitting || decoder == null)
            {
                return (new bool[0][,], new float[0]);
            }

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
            highResFeatures2Shape.y = 128; // height=128
            highResFeatures2Shape.x = 128; // width=128

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

            if (
                !imageEmbeddingsShapeResult
                || !imagePeShapeResult
                || !sparsePromptEmbeddingsShapeResult
                || !densePromptEmbeddingsShapeResult
                || !highResFeatures1ShapeResult
                || !highResFeatures2ShapeResult
            )
            {
                Debug.LogError("Failed to set input blob shapes");
                return (new bool[0][,], new float[0]);
            }

            if (isQuitting || decoder == null)
            {
                return (new bool[0][,], new float[0]);
            }

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

            if (
                !imageEmbeddingsResult
                || !imagePeResult
                || !sparseEmbeddingsResult
                || !denseEmbeddingsResult
                || !highResFeatures1Result
                || !highResFeatures2Result
            )
            {
                Debug.LogError("Failed to set input blob data");
                return (new bool[0][,], new float[0]);
            }

            if (isQuitting || decoder == null)
            {
                return (new bool[0][,], new float[0]);
            }

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

            int masksBlobIndex = decoder.FindBlobIndexByName("masks");
            int iouPredBlobIndex = decoder.FindBlobIndexByName("iou_pred");
            int samTokensOutBlobIndex = decoder.FindBlobIndexByName("sam_tokens_out");
            int objectScoreLogitsBlobIndex = decoder.FindBlobIndexByName("object_score_logits");

            if (
                masksBlobIndex < 0
                || iouPredBlobIndex < 0
                || samTokensOutBlobIndex < 0
                || objectScoreLogitsBlobIndex < 0
            )
            {
                Debug.LogError("Could not find required indices");
                return (new bool[0][,], new float[0]);
            }

            if (isQuitting || decoder == null)
            {
                return (new bool[0][,], new float[0]);
            }

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

            if (
                !getMasksBlobResult
                || !getiouPredBlobResult
                || !getsamTokensBlobResult
                || !objectScoreLogitsBlobResult
            )
            {
                Debug.LogError("Failed to get blob data");
                return (new bool[0][,], new float[0]);
            }

            if (isQuitting || decoder == null)
            {
                return (new bool[0][,], new float[0]);
            }

            int numMasks = (int)iouPredBlobShape.x;
            int maskWidth = (int)masksBlobShape.x;
            int maskHeight = (int)masksBlobShape.y;
            int maskArea = maskWidth * maskHeight;
            bool[][,] masksResult = new bool[numMasks][,];

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
        float[,] scaledCoords = new float[coords.GetLength(0), coords.GetLength(1)];

        for (int i = 0; i < coords.GetLength(0); i++)
        {
            scaledCoords[i, 0] = coords[i, 0] * targetSize / imgWidth;
            scaledCoords[i, 1] = coords[i, 1] * targetSize / imgHeight;
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

    private float[,,,] PreprocessImage(
        Color32[] inputImage,
        int originalWidth,
        int originalHeight,
        int imageSize
    )
    {
        // Resize image to (imageSize, imageSize)
        int newWidth = imageSize;
        int newHeight = imageSize;
        Color32[] resizedImage = ResizeColor32(
            inputImage,
            originalWidth,
            originalHeight,
            newWidth,
            newHeight
        );

        // Convert Color32[] to float[height, width, channels]
        float[,,] floatImage = Color32ArrayToFloatArray(resizedImage, newHeight, newWidth);

        for (int h = 0; h < newHeight; h++)
        {
            for (int w = 0; w < newWidth; w++)
            {
                for (int c = 0; c < 3; c++)
                {
                    floatImage[h, w, c] = (floatImage[h, w, c] - Mean[c]) / Std[c];
                }
            }
        }

        // Transpose (H, W, C) => (C, H, W)
        float[,,] chw = TransposeHWCtoCHW(floatImage);

        // Add batch dimension => (1, C, H, W)
        float[,,,] batch = new float[1, 3, newHeight, newWidth];
        for (int c = 0; c < 3; c++)
            for (int h = 0; h < newHeight; h++)
                for (int w = 0; w < newWidth; w++)
                    batch[0, c, h, w] = chw[c, h, w];

        return batch;
    }

    // Helper: Resize Color32[] with nearest neighbor
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

    // Transpose (H,W,C) => (C,H,W)
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
        prompt.Close();
    }

    public string EnvironmentName()
    {
        return encoder.EnvironmentName();
    }
}
