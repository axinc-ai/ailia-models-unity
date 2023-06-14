/* AILIA Unity Plugin Super Resolution Sample */
/* Copyright 2023 AXELL CORPORATION */

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ailiaSDK
{
    public class AiliaSuperResolutionSample : MonoBehaviour
    {
        public enum ImageSuperResolutionModels
        {
            SRResNet,
            RealESRGAN,
            RealESRGANAnime
        }
        //Settings
        public ImageSuperResolutionModels superResolutionModels = ImageSuperResolutionModels.SRResNet;
        public bool gpu_mode = false;
        public GameObject UICanvas = null;
        public bool oneshot = true;

        //Result
        RawImage raw_image = null;
        Text label_text = null;
        Text mode_text = null;

        //AILIA
        private AiliaModel ailiaModel;

        // Input source
        AiliaImageSource AiliaImageSource;

        // shader
        int InputWidth;
        int InputHeight;
        int InputChannel;
        int OutputWidth;
        int OutputHeight;
        int OutputChannel;
        Texture2D resultTexture2D;
        Texture2D originalTexture;
        Vector2 rawImageSize;
        float[] output;
        float[] input;
        Color32[] outputImage;


        bool modelPrepared;

        void Start()
        {
            UISetup();

            AiliaImageSource = gameObject.GetComponent<AiliaImageSource>();
            rawImageSize = raw_image.rectTransform.sizeDelta;

            AiliaInit();
        }

        void AiliaInit()
        {
            // Create Ailia
            ailiaModel = CreateAiliaNet(superResolutionModels, gpu_mode);
            // Load sample image
            LoadImage(superResolutionModels, AiliaImageSource);
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

                SetShape(superResolutionModels);

                // texture & buffer allocate
                resultTexture2D = new Texture2D(OutputWidth, OutputHeight, TextureFormat.RGBA32, false);

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
                inputImage = AiliaImageSource.GetPixels32(rect, true);
                InputDataPocessingCPU(superResolutionModels, inputImage, input);
                long end_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

                // Predict
                long start_time2 = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
                bool result = ailiaModel.Predict(output, input);
                long end_time2 = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

                // convert result to image
                long start_time3 = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
                OutputDataProcessingCPU(superResolutionModels, output, outputImage, inputImage);
                long end_time3 = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

                if (label_text != null)
                {
                    string text = "Size " + OutputWidth.ToString() + "x" + OutputHeight.ToString() +"\n";
                    text += "Pre "+(end_time - start_time).ToString() " ms\n";
                    text += "Predict " + (end_time2 - start_time2).ToString() + "ms\n";
                    text += "Post " + (end_time3 - start_time3).ToString() + " ms\n";
                    text += ailiaModel.EnvironmentName();
                    label_text.text = text;
                }

                // T2B (Model) to B2T (Unity)
                VerticalFlip(InputWidth, InputHeight, inputImage);
                VerticalFlip(OutputWidth, OutputHeight, outputImage);

                // for viewer
                originalTexture = new Texture2D(InputWidth, InputHeight, TextureFormat.RGBA32, false);
                originalTexture.SetPixels32(inputImage);
                originalTexture.Apply();

                resultTexture2D = new Texture2D(OutputWidth, OutputHeight, TextureFormat.RGBA32, false);
                resultTexture2D.SetPixels32(outputImage);
                resultTexture2D.Apply();

                raw_image.texture = resultTexture2D;
                raw_image.gameObject.SetActive(true);
            }

            // When space key down, draw original image
            if (Input.GetKey(KeyCode.Space))
            {
                raw_image.texture = originalTexture;
            }
            else
            {
                raw_image.texture = resultTexture2D;
            }
        }

        // Download models and Create ailiaModel
        AiliaModel CreateAiliaNet(ImageSuperResolutionModels modelType, bool gpu_mode = true)
        {
            string asset_path = Application.temporaryCachePath;
            string serverFolderName = "";
            string prototxtName = "";
            string onnxName = "";
            switch (modelType)
            {
                case ImageSuperResolutionModels.SRResNet:
                    serverFolderName = "srresnet";
                    prototxtName = "srresnet.opt.onnx.prototxt";
                    onnxName = "srresnet.opt.onnx";
                    break;
                case ImageSuperResolutionModels.RealESRGAN:
                    serverFolderName = "real-esrgan";
                    prototxtName = "RealESRGAN.opt.onnx.prototxt";
                    onnxName = "RealESRGAN.opt.onnx";
                    break;
                case ImageSuperResolutionModels.RealESRGANAnime:
                    serverFolderName = "real-esrgan";
                    prototxtName = "RealESRGAN_anime.opt.onnx.prototxt";
                    onnxName = "RealESRGAN_anime.opt.onnx";
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

        void SetShape(ImageSuperResolutionModels imageSegmentaionModels)
        {
            Ailia.AILIAShape shape = null;

            switch (imageSegmentaionModels)
            {
                case ImageSuperResolutionModels.SRResNet:
                    // Get input image shape (tile)
                    shape = ailiaModel.GetInputShape();
                    InputWidth = (int)shape.x;
                    InputHeight = (int)shape.y;
                    InputChannel = (int)shape.z;

                    // Get output image shape (tile)
                    shape = ailiaModel.GetOutputShape();
                    OutputWidth = (int)shape.x;
                    OutputHeight = (int)shape.y;
                    OutputChannel = (int)shape.z;
                    break;
                case ImageSuperResolutionModels.RealESRGAN:
                case ImageSuperResolutionModels.RealESRGANAnime:
                    // Set input image shape
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

                    // Get output image shape
                    shape = ailiaModel.GetOutputShape();
                    OutputWidth = (int)shape.x;
                    OutputHeight = (int)shape.y;
                    OutputChannel = (int)shape.z;
                    break;
            }
        }

        void LoadImage(ImageSuperResolutionModels superResolutionModels, AiliaImageSource ailiaImageSource)
        {
            switch (superResolutionModels)
            {
                case ImageSuperResolutionModels.SRResNet:
                    ailiaImageSource.CreateSource("file://" + Application.dataPath + "/AXIP/AILIA-MODELS/superResolution/SampleImage/lenna.png");
                    break;
                case ImageSuperResolutionModels.RealESRGAN:
                    ailiaImageSource.CreateSource("file://" + Application.dataPath + "/AXIP/AILIA-MODELS/superResolution/SampleImage/real_esrgan.jpg");
                    break;
                case ImageSuperResolutionModels.RealESRGANAnime:
                    ailiaImageSource.CreateSource("file://" + Application.dataPath + "/AXIP/AILIA-MODELS/superResolution/SampleImage/real_esrgan_anime.jpg");
                    break;
            }
        }

        void InputDataPocessingCPU(ImageSuperResolutionModels superResolutionModels, Color32[] inputImage, float[] processedInputBuffer)
        {
            float weight = 1f / 255f;
            float bias = 0;
            bool rgbRepeats = false;

            // SRResNet : Channel First, RGB, /255f
            // RealESGGAN : Channel First, RGB, /255f

            for (int i = 0; i < inputImage.Length; i++)
            {
                processedInputBuffer[i + inputImage.Length * 0] = (inputImage[i].r) * weight + bias;
                processedInputBuffer[i + inputImage.Length * 1] = (inputImage[i].g) * weight + bias;
                processedInputBuffer[i + inputImage.Length * 2] = (inputImage[i].b) * weight + bias;
            }
        }

        void OutputDataProcessingCPU(ImageSuperResolutionModels superResolutionModels, float[] outputData, Color32[] pixelBuffer, Color32[] srcBuffer = null)
        {
            for (int i = 0; i < pixelBuffer.Length; i++)
            {
                pixelBuffer[i].r = (byte)Mathf.Clamp(outputData[i + 0 * pixelBuffer.Length] * 255, 0, 255);
                pixelBuffer[i].g = (byte)Mathf.Clamp(outputData[i + 1 * pixelBuffer.Length] * 255, 0, 255);
                pixelBuffer[i].b = (byte)Mathf.Clamp(outputData[i + 2 * pixelBuffer.Length] * 255, 0, 255);
                pixelBuffer[i].a = 255;
            }
        }

        void VerticalFlip(int width, int height, Color32[] image){
            for (int y = 0; y < height / 2; y++){
                for (int x = 0; x < width; x++){
                    Color32 temp = image[y * width + x];
                    image[y * width + x] = image[(height - 1 - y) * width + x];
                    image[(height - 1 - y) * width + x] = temp;
                }
            }
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
        }
    }
}