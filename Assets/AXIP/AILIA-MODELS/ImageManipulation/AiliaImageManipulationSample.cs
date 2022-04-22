/* AILIA Unity Plugin Detector Sample */
/* Copyright 2018-2019 AXELL CORPORATION */

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ailiaSDK
{
    public class AiliaImageManipulationSample : MonoBehaviour
    {
        public enum ImageManipulationModels
        {
            SRResNet,
            Noise2Noise,
            IlluminationCorrection,
            Colorization
        }
        //Settings
        public ImageManipulationModels imageManipulationModels = ImageManipulationModels.SRResNet;
        public bool gpu_mode = false;
        public ComputeShader inputDataProcessingShader = null;
        public ComputeShader outputDataToTextureShader = null;
        public GameObject UICanvas = null;
        public bool oneshot = true;

        //Result
        RawImage raw_image = null;
        Text label_text = null;
        Text mode_text = null;

        // compute shader id
        int computeShaderWeightId;
        int computeShaderBiasId;
        int computeShaderWidthId;
        int computeShaderHeightId;
        int computeShaderTextureId;
        int computeShaderResultId;
        int computeShaderInputBufferId;
        int computeShaderResultTextureId;
        int channelLastKernel;
        int channelLastUpsideDownKernel;
        int channelFirstKernel;
        int channelFirstUpsideDownKernel;
        int channelFirstToTextureKernel;

        //AILIA
        private AiliaModel ailiaModel;

        // Input source
        AiliaImageSource AiliaImageSource;

        // shader
        Material blendMaterial;
        int mainTexId;
        int blendTexId;
        int blendFlagId;
        int mainVFlipId;
        int blendVFlipId;

        int InputWidth;
        int InputHeight;
        int InputChannel;
        int OutputWidth;
        int OutputHeight;
        int OutputChannel;
        Texture2D resultTexture2D;
        RenderTexture resultRenderTexture;
        Texture2D originalTexture;
        Vector2 rawImageSize;
        float[] output;
        float[] input;
        Color32[] outputImage;


        Texture2D baseTexture; // use for colorization
        bool modelPrepared;

        void Start()
        {
            UISetup();

            AiliaImageSource = gameObject.GetComponent<AiliaImageSource>();

            // for Rendering 
            blendMaterial = new Material(Shader.Find("Ailia/AlphaBlending2Tex"));
            mainTexId = Shader.PropertyToID("_MainTex");
            blendTexId = Shader.PropertyToID("_BlendTex");
            blendFlagId = Shader.PropertyToID("blendFlag");
            mainVFlipId = Shader.PropertyToID("mainVFlip");
            blendVFlipId = Shader.PropertyToID("blendVFlip");
            raw_image.material = blendMaterial;

            rawImageSize = raw_image.rectTransform.sizeDelta;

            if (inputDataProcessingShader != null)
            {
                computeShaderWeightId = Shader.PropertyToID("weight");
                computeShaderBiasId = Shader.PropertyToID("bias");
                computeShaderWidthId = Shader.PropertyToID("width");
                computeShaderHeightId = Shader.PropertyToID("height");
                computeShaderTextureId = Shader.PropertyToID("texure");
                computeShaderResultId = Shader.PropertyToID("result_buffer");
                channelLastKernel = inputDataProcessingShader.FindKernel("ChannelLast");
                channelLastUpsideDownKernel = inputDataProcessingShader.FindKernel("ChannelLastUpsideDown");
                channelFirstKernel = inputDataProcessingShader.FindKernel("ChannelFirst");
                channelFirstUpsideDownKernel = inputDataProcessingShader.FindKernel("ChannelFirstUpsideDown");
            }
            if (outputDataToTextureShader != null)
            {
                computeShaderWidthId = Shader.PropertyToID("width");
                computeShaderHeightId = Shader.PropertyToID("height");
                computeShaderInputBufferId = Shader.PropertyToID("input_buffer");
                computeShaderResultTextureId = Shader.PropertyToID("result_texure");
                channelFirstToTextureKernel = outputDataToTextureShader.FindKernel("ChannelFirstToTexture");
            }

            AiliaInit();
        }

        void AiliaInit()
        {
            // Create Ailia
            ailiaModel = CreateAiliaNet(imageManipulationModels, gpu_mode);
            // Load sample image
            LoadImage(imageManipulationModels, AiliaImageSource);
        }

        void UISetup()
        {
            Debug.Assert(UICanvas != null, "UICanvas is null");

            label_text = UICanvas.transform.Find("LabelText").GetComponent<Text>();
            mode_text = UICanvas.transform.Find("ModeLabel").GetComponent<Text>();
            raw_image = UICanvas.transform.Find("RawImage").GetComponent<RawImage>();
            raw_image.gameObject.SetActive(false);

            mode_text.text = "ailia Image Manipulation\nSpace key down to original image";
        }

        void Update()
        {
            if (!AiliaImageSource.IsPrepared || !modelPrepared)
            {
                return;
            }

            if (output == null)
            {
                float rawImageRatio = rawImageSize.x / rawImageSize.y;
                float ratio = AiliaImageSource.Width / (float)AiliaImageSource.Height;
                raw_image.rectTransform.sizeDelta = new Vector2(ratio / rawImageRatio * rawImageSize.x, rawImageSize.y);

                SetShape(imageManipulationModels);

                // texture & buffer allocate
                if (!gpu_mode || outputDataToTextureShader == null)
                {
                    resultTexture2D = new Texture2D(OutputWidth, OutputHeight, TextureFormat.RGBA32, false);
                }
                else
                {
                    resultRenderTexture = new RenderTexture(OutputWidth, OutputHeight, 0);
                    resultRenderTexture.enableRandomWrite = true;
                    resultRenderTexture.Create();
                }

                baseTexture = AiliaImageSource.GetTexture(new Rect(0, 0, AiliaImageSource.Width, AiliaImageSource.Height));

                AiliaImageSource.Resize(InputWidth, InputHeight);
                input = new float[InputWidth * InputHeight * InputChannel];
                output = new float[OutputWidth * OutputHeight * OutputChannel];
                outputImage = new Color32[OutputWidth * OutputHeight];
            }

            if (oneshot)
            {
                oneshot = false;

                // Make input data
                Rect rect = new Rect(0, 0, InputWidth, InputHeight);
                long start_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

                Color32[] inputImage = null;
                if (!gpu_mode || inputDataProcessingShader == null)
                {
                    inputImage = AiliaImageSource.GetPixels32(rect, true);
                    InputDataPocessingCPU(imageManipulationModels, inputImage, input);
                }
                else
                {
                    originalTexture = AiliaImageSource.GetTexture(rect);
                    InputDataPocessing(imageManipulationModels, originalTexture, input, true);
                }
                long end_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

                // Predict
                long start_time2 = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
                bool result = ailiaModel.Predict(output, input);

                // convert result to image
                if (!gpu_mode || outputDataToTextureShader == null)
                {
                    OutputDataProcessingCPU(imageManipulationModels, output, outputImage, inputImage);
                    if (imageManipulationModels == ImageManipulationModels.Colorization)
                    {
                        resultTexture2D.SetPixels32(outputImage);
                        resultTexture2D.Apply();

                        {
                            var rTexture = AiliaImageUtil.ResizeTexture(resultTexture2D, baseTexture.width, baseTexture.height);
                            var base32 = baseTexture.GetPixels32();
                            var dst32 = rTexture.GetPixels32();

                            Color32[] dstColorBuffer = new Color32[dst32.Length];

                            for (int i = 0; i < dst32.Length; i++)
                            {
                                var px = i % baseTexture.width;
                                var py = (baseTexture.height - 1 - (i / baseTexture.width)) * baseTexture.width;

                                var baselab = AiliaColorConv.Color2Lab(base32[i]);
                                var dstlab = AiliaColorConv.Color2Lab(dst32[px + py]);

                                var resultlab = new AiliaColorConv.LAB(baselab.L, dstlab.A, dstlab.B);
                                dstColorBuffer[px + py] = AiliaColorConv.Lab2Color(resultlab);
                            }

                            outputImage = dstColorBuffer;
                        }

                        resultTexture2D = new Texture2D(baseTexture.width, baseTexture.height);
                    }
                }
                else
                {
                    OutputDataProcessing(output, resultRenderTexture);
                }

                long end_time2 = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

                if (label_text != null)
                {
                    label_text.text = ((end_time - start_time) + (end_time2 - start_time2)).ToString() + "ms\n" + ailiaModel.EnvironmentName();
                }

                // for viewer
                if (!gpu_mode || inputDataProcessingShader == null)
                {
                    originalTexture = new Texture2D(InputWidth, InputHeight, TextureFormat.RGBA32, false);
                    originalTexture.SetPixels32(inputImage);
                    originalTexture.Apply();
                }
                raw_image.texture = originalTexture;
                blendMaterial.SetTexture(mainTexId, originalTexture);

                if (!gpu_mode || outputDataToTextureShader == null)
                {
                    resultTexture2D.SetPixels32(outputImage);
                    resultTexture2D.Apply();

                    blendMaterial.SetTexture(blendTexId, resultTexture2D);
                }
                else
                {
                    blendMaterial.SetTexture(blendTexId, resultRenderTexture);
                }

                blendMaterial.SetFloat(mainVFlipId, 1);
                blendMaterial.SetFloat(blendVFlipId, 1);

                raw_image.gameObject.SetActive(true);
            }

            // When space key down, draw original image
            if (Input.GetKey(KeyCode.Space))
            {
                blendMaterial.SetFloat(blendFlagId, 0);
            }
            else
            {
                blendMaterial.SetFloat(blendFlagId, 1);
            }
        }

        // Download models and Create ailiaModel
        AiliaModel CreateAiliaNet(ImageManipulationModels modelType, bool gpu_mode = true)
        {
            string asset_path = Application.temporaryCachePath;
            string serverFolderName = "";
            string prototxtName = "";
            string onnxName = "";
            switch (modelType)
            {
                case ImageManipulationModels.SRResNet:
                    serverFolderName = "srresnet";
                    prototxtName = "srresnet.opt.onnx.prototxt";
                    onnxName = "srresnet.opt.onnx";
                    break;
                case ImageManipulationModels.Noise2Noise:
                    serverFolderName = "noise2noise";
                    prototxtName = "noise2noise_gaussian.onnx.prototxt";
                    onnxName = "noise2noise_gaussian.onnx";
                    break;
                case ImageManipulationModels.IlluminationCorrection:
                    serverFolderName = "illnet";
                    prototxtName = "illnet.onnx.prototxt";
                    onnxName = "illnet.onnx";
                    break;
                case ImageManipulationModels.Colorization:
                    serverFolderName = "colorization";
                    prototxtName = "colorizer.onnx.prototxt";
                    onnxName = "colorizer.onnx";
                    break;
            }

            ailiaModel = new AiliaModel();
            if (gpu_mode)
            {
                // call before OpenFile
                ailiaModel.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
            }

            AiliaDownload ailia_download = new AiliaDownload();
            ailia_download.DownloaderProgressPanel = UICanvas.transform.Find("DownloaderProgressPanel").gameObject;
            var urlList = new List<ModelDownloadURL>();
            urlList.Add(new ModelDownloadURL() { folder_path = serverFolderName, file_name = prototxtName });
            urlList.Add(new ModelDownloadURL() { folder_path = serverFolderName, file_name = onnxName });

            StartCoroutine(ailia_download.DownloadWithProgressFromURL(urlList, () =>
            {
                modelPrepared = ailiaModel.OpenFile(asset_path + "/" + prototxtName, asset_path + "/" + onnxName);
            }));

            return ailiaModel;
        }

        void SetShape(ImageManipulationModels imageSegmentaionModels)
        {
            Ailia.AILIAShape shape = null;

            switch (imageSegmentaionModels)
            {
                case ImageManipulationModels.SRResNet:
                case ImageManipulationModels.Noise2Noise:
                    shape = ailiaModel.GetInputShape();
                    InputWidth = (int)shape.x;
                    InputHeight = (int)shape.y;
                    InputChannel = (int)shape.z;
                    shape = ailiaModel.GetOutputShape();
                    OutputWidth = (int)shape.x;
                    OutputHeight = (int)shape.y;
                    OutputChannel = (int)shape.z;
                    break;
                case ImageManipulationModels.IlluminationCorrection:
                    shape = new Ailia.AILIAShape();
                    shape.x = (uint)AiliaImageSource.Width;
                    shape.y = (uint)AiliaImageSource.Height;
                    shape.z = 3;
                    shape.w = 1;
                    shape.dim = 4;
                    ailiaModel.SetInputShape(shape);
                    InputWidth = AiliaImageSource.Width;
                    InputHeight = AiliaImageSource.Height;
                    InputChannel = 3;
                    OutputWidth = AiliaImageSource.Width;
                    OutputHeight = AiliaImageSource.Height;
                    OutputChannel = 3;
                    break;
#if false
                case ImageManipulationModels.Colorization:
                    shape = new Ailia.AILIAShape();
                    shape.x = (uint)AiliaImageSource.Width;
                    shape.y = (uint)AiliaImageSource.Height;
                    shape.z = 1;
                    shape.w = 1;
                    shape.dim = 4;
                    ailiaModel.SetInputShape(shape);
                    InputWidth = AiliaImageSource.Width;
                    InputHeight = AiliaImageSource.Height;
                    InputChannel = 1;
                    OutputWidth = AiliaImageSource.Width;
                    OutputHeight = AiliaImageSource.Height;
                    OutputChannel = 2;
                    break;
#else
                // 256x256に縮小してみる
                case ImageManipulationModels.Colorization:
                    shape = ailiaModel.GetInputShape();
                    shape.z = 1;
                    shape.w = 1;
                    shape.dim = 4;
                    ailiaModel.SetInputShape(shape);
                    InputWidth = (int)shape.x;
                    InputHeight = (int)shape.y;
                    InputChannel = 1;
                    OutputWidth = InputWidth; // AiliaImageSource.Width;
                    OutputHeight = InputHeight; // AiliaImageSource.Height;
                    OutputChannel = 2;
                    break;
#endif // ~true
            }
        }

        void LoadImage(ImageManipulationModels imageManipulationModels, AiliaImageSource ailiaImageSource)
        {
            switch (imageManipulationModels)
            {
                case ImageManipulationModels.SRResNet:
                    ailiaImageSource.CreateSource("file://" + Application.dataPath + "/AXIP/AILIA-MODELS/ImageManipulation/SampleImage/lenna.png");
                    break;
                case ImageManipulationModels.Noise2Noise:
                    ailiaImageSource.CreateSource("file://" + Application.dataPath + "/AXIP/AILIA-MODELS/ImageManipulation/SampleImage/monarch-gaussian-noisy.png");
                    break;
                case ImageManipulationModels.IlluminationCorrection:
                    ailiaImageSource.CreateSource("file://" + Application.dataPath + "/AXIP/AILIA-MODELS/ImageManipulation/SampleImage/illumination_correction_test.png");
                    break;
                case ImageManipulationModels.Colorization:
                    ailiaImageSource.CreateSource("file://" + Application.dataPath + "/AXIP/AILIA-MODELS/ImageManipulation/SampleImage/ansel_adams1.png");
                    break;
            }
        }

        void InputDataPocessingCPU(ImageManipulationModels imageManipulationModels, Color32[] inputImage, float[] processedInputBuffer)
        {
            float weight = 1f / 255f;
            float bias = 0;
            bool rgbRepeats = false;
            switch (imageManipulationModels)
            {
                case ImageManipulationModels.IlluminationCorrection:
                    weight = 1f / 127.5f;
                    bias = -1;
                    break;

                case ImageManipulationModels.Colorization:
                    if (rgbRepeats)
                    {
                        for (int i = 0; i < inputImage.Length; i++)
                        {
                            var r = (inputImage[i].r) * weight + bias;
                            var g = (inputImage[i].g) * weight + bias;
                            var b = (inputImage[i].b) * weight + bias;

                            var lab = AiliaColorConv.Color2Lab(new Color(r, g, b));
                            processedInputBuffer[i * 3 + 0] = (float)lab.L;
                            processedInputBuffer[i * 3 + 1] = (float)lab.A;
                            processedInputBuffer[i * 3 + 2] = (float)lab.B;
                        }

                    }
                    else
                    {
                        for (int i = 0; i < inputImage.Length; i++)
                        {
                            var r = (inputImage[i].r) * weight + bias;
                            var g = (inputImage[i].g) * weight + bias;
                            var b = (inputImage[i].b) * weight + bias;
                            var lab = AiliaColorConv.Color2Lab(new Color(r, g, b));

                            processedInputBuffer[i + inputImage.Length * 0] = (float)lab.L;
                        }
                    }
                    return;
                default:
                    break;
            }

            // flatten input data
            if (rgbRepeats)
            {
                for (int i = 0; i < inputImage.Length; i++)
                {
                    // rgbrgbrgb...
                    processedInputBuffer[i * 3 + 0] = (inputImage[i].r) * weight + bias;
                    processedInputBuffer[i * 3 + 1] = (inputImage[i].g) * weight + bias;
                    processedInputBuffer[i * 3 + 2] = (inputImage[i].b) * weight + bias;
                }
            }
            else
            {
                for (int i = 0; i < inputImage.Length; i++)
                {
                    processedInputBuffer[i + inputImage.Length * 0] = (inputImage[i].r) * weight + bias;
                    processedInputBuffer[i + inputImage.Length * 1] = (inputImage[i].g) * weight + bias;
                    processedInputBuffer[i + inputImage.Length * 2] = (inputImage[i].b) * weight + bias;
                }
            }
        }

        ComputeBuffer inputCBuffer;
        void InputDataPocessing(ImageManipulationModels imageSegmentaionModels, Texture inputImage, float[] processedInputBuffer, bool upsideDown = false)
        {
            float weight = 1;
            float bias = 0;
            bool rgbRepeats = false;
            switch (imageSegmentaionModels)
            {
                case ImageManipulationModels.IlluminationCorrection:
                    weight = 2;
                    bias = -1;
                    break;
                default:
                    break;
            }

            if (inputCBuffer == null || inputCBuffer.count != processedInputBuffer.Length)
            {
                if (inputCBuffer != null) inputCBuffer.Release();
                inputCBuffer = new ComputeBuffer(processedInputBuffer.Length, sizeof(float));
            }

            int kernelIndex;
            if (rgbRepeats)
            {
                if (upsideDown) kernelIndex = channelLastUpsideDownKernel;
                else kernelIndex = channelLastKernel;
            }
            else
            {
                if (upsideDown) kernelIndex = channelFirstUpsideDownKernel;
                else kernelIndex = channelFirstKernel;
            }
            inputDataProcessingShader.SetFloat(computeShaderWeightId, weight);
            inputDataProcessingShader.SetFloat(computeShaderBiasId, bias);
            inputDataProcessingShader.SetInt(computeShaderWidthId, inputImage.width);
            inputDataProcessingShader.SetInt(computeShaderHeightId, inputImage.height);
            inputDataProcessingShader.SetTexture(kernelIndex, computeShaderTextureId, inputImage);
            inputDataProcessingShader.SetBuffer(kernelIndex, computeShaderResultId, inputCBuffer);
            inputDataProcessingShader.Dispatch(kernelIndex, inputImage.width / 32 + 1, inputImage.height / 32 + 1, 1);
            inputCBuffer.GetData(processedInputBuffer);
        }

        void OutputDataProcessingCPU(ImageManipulationModels imageManipulationModels, float[] outputData, Color32[] pixelBuffer, Color32[] srcBuffer = null)
        {
            switch (imageManipulationModels)
            {
                case ImageManipulationModels.Colorization:
                    // FIXME: stretch to original size
                    for (int i = 0; i < pixelBuffer.Length; i++)
                    {
                        var lab = AiliaColorConv.Color2Lab(srcBuffer[i]);
                        var nlab = new AiliaColorConv.LAB(
                            lab.l,
                            outputData[i + 0 * pixelBuffer.Length],
                            outputData[i + 1 * pixelBuffer.Length]
                        );

                        pixelBuffer[i] = AiliaColorConv.Lab2Color(nlab);
                    }
                    break;
                default:
                    for (int i = 0; i < pixelBuffer.Length; i++)
                    {
                        pixelBuffer[i].r = (byte)Mathf.Clamp(outputData[i + 0 * pixelBuffer.Length] * 255, 0, 255);
                        pixelBuffer[i].g = (byte)Mathf.Clamp(outputData[i + 1 * pixelBuffer.Length] * 255, 0, 255);
                        pixelBuffer[i].b = (byte)Mathf.Clamp(outputData[i + 2 * pixelBuffer.Length] * 255, 0, 255);
                        pixelBuffer[i].a = 255;
                    }
                    break;

            }
        }

        ComputeBuffer outputCbuffer;
        void OutputDataProcessing(float[] outputData, RenderTexture resultTexture)
        {
            if (outputCbuffer == null || outputCbuffer.count != outputData.Length)
            {
                if (outputCbuffer != null) outputCbuffer.Release();
                outputCbuffer = new ComputeBuffer(outputData.Length, sizeof(float));
            }
            outputCbuffer.SetData(outputData);
            outputDataToTextureShader.SetInt(computeShaderWidthId, resultTexture.width);
            outputDataToTextureShader.SetInt(computeShaderHeightId, resultTexture.height);
            outputDataToTextureShader.SetBuffer(channelFirstToTextureKernel, computeShaderInputBufferId, outputCbuffer);
            outputDataToTextureShader.SetTexture(channelFirstToTextureKernel, computeShaderResultTextureId, resultTexture);

            outputDataToTextureShader.Dispatch(channelFirstToTextureKernel, resultTexture.width / 32 + 1, resultTexture.height / 32 + 1, 1);
        }

        void OnApplicationQuit()
        {
            DestroyAiliaDetector();
        }

        void OnDestroy()
        {
            DestroyAiliaDetector();
        }

        private void DestroyAiliaDetector()
        {
            ailiaModel.Close();
            if (inputCBuffer != null) inputCBuffer.Release();
            if (outputCbuffer != null) outputCbuffer.Release();
        }
    }
}