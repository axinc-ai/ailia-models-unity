/* AILIA Unity Plugin Segmentation Sample */
/* Copyright 2018-2022 AXELL CORPORATION */

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using ailia;

namespace ailiaSDK
{
    // Model list
    public enum ImageSegmentaion2Models
    {
        // HRNetV2_W18_Small_v2,
        // HRNetV2_W18_Small_v1,
        // HRNetV2_W48,
        // hair_segmentation,
        // pspnet_hair_segmentation,
        // deeplabv3,
        // u2net,
        // modnet,
        image_encoder_hiera_l
    }

    public class AiliaImageSegmentation2Sample : MonoBehaviour
    {
        // Settings
        public ImageSegmentaion2Models ImageSegmentaion2Models =
            ImageSegmentaion2Models.image_encoder_hiera_l;
        public bool gpu_mode = false;
        public GameObject UICanvas = null;
        public bool camera_mode = true;
        public int camera_id = 0;

        // Result
        RawImage raw_image = null;
        Text label_text = null;
        Text mode_text = null;
        private bool oneshot = true;

        // AILIA
        private AiliaCamera ailia_camera = new AiliaCamera();

        // Input source
        AiliaImageSource AiliaImageSource;
        public Texture2D image_source_hrnet = null;
        public Texture2D image_source_hair_segmentation = null;
        public Texture2D image_source_pspnet_hair_segmentation = null;
        public Texture2D image_source_deeplabv3 = null;
        public Texture2D image_source_u2net = null;
        public Texture2D image_source_modnet = null;
        public Texture2D image_source_sam1 = null;

        // Pre-and-Post processing Shader
        Material blendMaterial;
        int mainTexId;
        int blendTexId;
        int blendFlagId;
        int mainVFlipId;
        int blendVFlipId;

        // Model input and output

        Texture2D labelTexture;
        Texture2D originalTexture;
        Vector2 rawImageSize;

        // Segment Anything Model
        private Segmentation2Model seg2Model;
        private SegmentAnything2Model sam2Model;
        private bool isDraggingForBox = false;
        private Rect boxRect = new();

        bool modelPrepared = false;
        bool modelAllocated = false;
        string envName = "";

        async void Start()
        {
            Debug.Log("Start");
            AiliaLicense.CheckAndDownloadLicense();
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

            // for Processing
            AiliaInit();

            // for Camera
            if (camera_mode)
            {
                bool crop_square = false;
                ailia_camera.CreateCamera(camera_id, crop_square);
            }
        }

        void AiliaInit()
        {
            // Create Ailia
            CreateAiliaNet(ImageSegmentaion2Models, gpu_mode);

            // Load sample image
            LoadImage(ImageSegmentaion2Models, AiliaImageSource);
        }

        void UISetup()
        {
            Debug.Assert(UICanvas != null, "UICanvas is null");

            label_text = UICanvas.transform.Find("LabelText").GetComponent<Text>();
            mode_text = UICanvas.transform.Find("ModeLabel").GetComponent<Text>();
            raw_image = UICanvas.transform.Find("RawImage").GetComponent<RawImage>();
            raw_image.gameObject.SetActive(false);

            if (ImageSegmentaion2Models == ImageSegmentaion2Models.image_encoder_hiera_l)
            {
                mode_text.text =
                    "ailia Image Segmentation 2\n"
                    + "Left/Right click: positive/negative point\n"
                    + "Middle click: drag to define border box\n"
                    + "Space key down to reset";
            }
            else
            {
                mode_text.text = "ailia Image Segmentation 2";
            }
        }

        Color32[] VerticalFlip(Color32[] inputImage, int InputWidth, int InputHeight)
        {
            Color32[] outputImage = new Color32[InputWidth * InputHeight];
            for (int y = 0; y < InputHeight; y++)
            {
                for (int x = 0; x < InputWidth; x++)
                {
                    outputImage[(InputHeight - 1 - y) * InputWidth + x] = inputImage[
                        y * InputWidth + x
                    ];
                }
            }
            return outputImage;
        }

        void Update()
        {
            HandleClick(Input.GetMouseButton(0), Input.GetMouseButton(1), Input.GetMouseButton(2));

            if (AiliaImageSource == null || !AiliaImageSource.IsPrepared || !modelPrepared)
            {
                return;
            }
            if (modelPrepared && !modelAllocated)
            {
                if (ImageSegmentaion2Models != ImageSegmentaion2Models.image_encoder_hiera_l)
                {
                    seg2Model.AllocateInputAndOutputTensor(
                        ImageSegmentaion2Models,
                        AiliaImageSource.Width,
                        AiliaImageSource.Height
                    );
                }
                modelAllocated = true;
            }
            if (camera_mode && !ailia_camera.IsEnable())
            {
                return;
            }

            // When space key down, draw original image
            if (Input.GetKey(KeyCode.Space))
            {
                if (ImageSegmentaion2Models == ImageSegmentaion2Models.image_encoder_hiera_l)
                {
                    sam2Model.ResetClickPoint();
                    boxRect = new();
                    oneshot = true;
                }
                blendMaterial.SetFloat(blendFlagId, 0);
            }
            else
            {
                blendMaterial.SetFloat(blendFlagId, 1);
            }

            // Only one shot processing for image mode
            if (!oneshot && !camera_mode)
            {
                return;
            }

            // Make input data
            long start_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
            Color32[] inputImage = null;
            int inputImageWidth = 0;
            int inputImageHeight = 0;
            if (camera_mode)
            {
                inputImageWidth = ailia_camera.GetWidth();
                inputImageHeight = ailia_camera.GetHeight();
                inputImage = ailia_camera.GetPixels32(); // Bottom2Top format
                inputImage = VerticalFlip(inputImage, inputImageWidth, inputImageHeight); // Top2Bottom format
            }
            else
            {
                bool convert_to_top2bottom = true;
                inputImageWidth = AiliaImageSource.Width;
                inputImageHeight = AiliaImageSource.Height;
                Rect rect = new Rect(0, 0, inputImageWidth, inputImageHeight);
                inputImage = AiliaImageSource.GetPixels32(rect, convert_to_top2bottom); // Top2Bottom format
            }

            long end_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

            // Predict
            long start_time2 = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

            bool result = false;
            if (ImageSegmentaion2Models == ImageSegmentaion2Models.image_encoder_hiera_l)
            {
                if (!sam2Model.EmbeddingExist())
                {
                    if (sam2Model.GetClickPoints(0).Length == 0)
                    {
                        sam2Model.AddClickPoint(inputImageWidth / 4, inputImageHeight / 4 + 30);
                    }
                }
                if (camera_mode || !sam2Model.EmbeddingExist())
                {
                    sam2Model.ProcessEmbedding(inputImage, inputImageWidth, inputImageHeight);
                }
                sam2Model.ProcessMask(inputImage, inputImageWidth, inputImageHeight);
                result = sam2Model.success;
            }
            else
            {
                result = seg2Model.ProcessFrame(
                    ImageSegmentaion2Models,
                    inputImage,
                    inputImageWidth,
                    inputImageHeight
                );
            }

            if (!result)
            {
                oneshot = false;
                return;
            }

            // convert result to image
            Color32[] outputImage;
            int outputWidth,
                outputHeight;
            if (ImageSegmentaion2Models == ImageSegmentaion2Models.image_encoder_hiera_l)
            {
                outputImage = sam2Model.visualizedResult.GetPixels32();
                outputWidth = sam2Model.visualizedResult.width;
                outputHeight = sam2Model.visualizedResult.height;
            }
            else
            {
                (outputImage, outputWidth, outputHeight) = seg2Model.PostProcesss(
                    ImageSegmentaion2Models,
                    inputImageWidth,
                    inputImageHeight
                );
            }

            long end_time2 = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

            if (label_text != null)
            {
                label_text.text =
                    ((end_time - start_time) + (end_time2 - start_time2)).ToString()
                    + "ms\n"
                    + envName;
            }

            // for viewer
            if (originalTexture == null)
            {
                originalTexture = new Texture2D(
                    outputWidth,
                    outputHeight,
                    TextureFormat.RGBA32,
                    false
                );
            }
            if (labelTexture == null)
            {
                labelTexture = new Texture2D(
                    outputWidth,
                    outputHeight,
                    TextureFormat.RGBA32,
                    false
                );
            }
            originalTexture.SetPixels32(inputImage);
            originalTexture.Apply();
            raw_image.texture = originalTexture;
            blendMaterial.SetTexture(mainTexId, originalTexture);

            labelTexture.SetPixels32(outputImage);
            labelTexture.Apply();
            blendMaterial.SetTexture(blendTexId, labelTexture);

            blendMaterial.SetFloat(mainVFlipId, 1); //originalTexture is Top2Bottom
            blendMaterial.SetFloat(blendVFlipId, 1); //outputImage is Top2Bottom

            float rawImageRatio = rawImageSize.x / rawImageSize.y;
            float ratio = inputImageWidth / (float)inputImageHeight;
            raw_image.rectTransform.sizeDelta = new Vector2(
                ratio / rawImageRatio * rawImageSize.x,
                rawImageSize.y
            );

            raw_image.gameObject.SetActive(true);

            oneshot = false;
        }

        // Download models and Create ailiaModel
        void CreateAiliaNet(ImageSegmentaion2Models modelType, bool gpu_mode = true)
        {
            AiliaDownload ailia_download = new AiliaDownload();
            ailia_download.DownloaderProgressPanel = UICanvas.transform
                .Find("DownloaderProgressPanel")
                .gameObject;
            List<ModelDownloadURL> urlList = null;

            if (ImageSegmentaion2Models == ImageSegmentaion2Models.image_encoder_hiera_l)
            {
                sam2Model = new SegmentAnything2Model();
                urlList = sam2Model.GetModelURLs(ImageSegmentaion2Models);
            }
            else
            {
                seg2Model = new Segmentation2Model();
                urlList = seg2Model.GetModelURLs(ImageSegmentaion2Models);
            }

            StartCoroutine(
                ailia_download.DownloadWithProgressFromURL(
                    urlList,
                    () =>
                    {
                        if (ImageSegmentaion2Models == ImageSegmentaion2Models.image_encoder_hiera_l)
                        {
                            modelPrepared = sam2Model.InitializeModels(
                                ImageSegmentaion2Models,
                                gpu_mode
                            );
                            envName = sam2Model.EnvironmentName();
                        }
                        else
                        {
                            modelPrepared = seg2Model.InitializeModels(
                                ImageSegmentaion2Models,
                                gpu_mode
                            );
                            envName = seg2Model.EnvironmentName();
                        }
                    }
                )
            );
        }

        void LoadImage(
            ImageSegmentaion2Models ImageSegmentaion2Models,
            AiliaImageSource ailiaImageSource
        )
        {
            switch (ImageSegmentaion2Models)
            {
                // case ImageSegmentaion2Models.HRNetV2_W18_Small_v2:
                // case ImageSegmentaion2Models.HRNetV2_W18_Small_v1:
                // case ImageSegmentaion2Models.HRNetV2_W48:
                //     ailiaImageSource.CreateSource(image_source_hrnet);
                //     break;
                // case ImageSegmentaion2Models.hair_segmentation:
                //     ailiaImageSource.CreateSource(image_source_hair_segmentation);
                //     break;
                // case ImageSegmentaion2Models.pspnet_hair_segmentation:
                //     ailiaImageSource.CreateSource(image_source_pspnet_hair_segmentation);
                //     break;
                // case ImageSegmentaion2Models.deeplabv3:
                //     ailiaImageSource.CreateSource(image_source_deeplabv3);
                //     break;
                // case ImageSegmentaion2Models.u2net:
                //     ailiaImageSource.CreateSource(image_source_u2net);
                //     break;
                // case ImageSegmentaion2Models.modnet:
                //     ailiaImageSource.CreateSource(image_source_modnet);
                //     break;
                case ImageSegmentaion2Models.image_encoder_hiera_l:
                    ailiaImageSource.CreateSource(image_source_sam1);
                    break;
            }
        }

        void HandleClick(bool leftClick, bool rightClick, bool middleClick)
        {
            if (!raw_image.isActiveAndEnabled || raw_image.texture == null)
            {
                return;
            }

            Vector3[] corners = new Vector3[4];
            raw_image.rectTransform.GetWorldCorners(corners);

            Rect rawImageRect = new Rect(
                corners[0].x,
                Screen.height - corners[2].y,
                corners[2].x - corners[0].x,
                corners[2].y - corners[0].y
            );

            Vector2 mousePos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);

            float widthRatio = raw_image.texture.width / rawImageRect.width;
            float heightRatio = raw_image.texture.height / rawImageRect.height;

            int x = Mathf.Clamp(
                Mathf.RoundToInt((mousePos.x - rawImageRect.x) * widthRatio),
                0,
                raw_image.texture.width - 1
            );
            int y =
                raw_image.texture.height
                - 1
                - Mathf.Clamp(
                    Mathf.RoundToInt((mousePos.y - rawImageRect.y) * heightRatio),
                    0,
                    raw_image.texture.height - 1
                );

            if (rawImageRect.Contains(mousePos))
            {
                if (leftClick || rightClick)
                {
                    sam2Model?.AddClickPoint(x, y, rightClick);
                    oneshot = true;

                    Debug.Log($"Click registered at: {x}, {y}");
                }

                if (middleClick && !isDraggingForBox)
                {
                    isDraggingForBox = true;
                    boxRect.xMin = x;
                    boxRect.yMin = y;
                }
            }

            if (!middleClick && isDraggingForBox)
            {
                isDraggingForBox = false;

                float firstX = boxRect.xMin;
                float firstY = boxRect.yMin;

                boxRect.xMin = Math.Min(firstX, x);
                boxRect.yMin = Math.Min(firstY, y);
                boxRect.xMax = Math.Max(firstX, x);
                boxRect.yMax = Math.Max(firstY, y);

                sam2Model.SetBoxCoords(boxRect);
                oneshot = true;
            }
        }

        private void OnGUI()
        {
            if (!raw_image.isActiveAndEnabled || raw_image.texture == null)
            {
                return;
            }

            if ((boxRect.width > 0 && boxRect.height > 0) || isDraggingForBox)
            {
                Vector3[] corners = new Vector3[4];
                raw_image.rectTransform.GetWorldCorners(corners);

                Rect rawImageRect = new Rect(
                    corners[0].x,
                    Screen.height - corners[2].y,
                    corners[2].x - corners[0].x,
                    corners[2].y - corners[0].y
                );

                float widthRatio = raw_image.texture.width / rawImageRect.width;
                float heightRatio = raw_image.texture.height / rawImageRect.height;

                int xMin = (int)(boxRect.xMin / widthRatio + rawImageRect.x);
                int yMin = (int)(boxRect.yMin / heightRatio + rawImageRect.y);
                int xMax = (
                    (int)(
                        isDraggingForBox
                            ? Input.mousePosition.x
                            : (boxRect.xMax / widthRatio + rawImageRect.x)
                    )
                );
                int yMax = (
                    (int)(
                        isDraggingForBox
                            ? (Screen.height - Input.mousePosition.y)
                            : (boxRect.yMax / heightRatio + rawImageRect.y)
                    )
                );

                float thickness = 2;
                Rect lineArea = new Rect();
                lineArea.xMin = xMin;
                lineArea.yMin = yMin;
                lineArea.xMax = xMax;
                lineArea.yMax = yMax;

                lineArea.y = yMin - thickness; //Bottom
                lineArea.height = thickness; //Top line
                GUI.DrawTexture(lineArea, Texture2D.whiteTexture);

                lineArea.y = yMax - thickness; //Bottom
                GUI.DrawTexture(lineArea, Texture2D.whiteTexture);

                lineArea.height = yMin - yMax;
                lineArea.width = thickness; //Left
                GUI.DrawTexture(lineArea, Texture2D.whiteTexture);
                lineArea.x = xMax - thickness; //Right
                GUI.DrawTexture(lineArea, Texture2D.whiteTexture);
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

        void DestroyAiliaDetector()
        {
            if (sam2Model != null)
            {
                sam2Model.Destroy();
            }
            if (seg2Model != null)
            {
                seg2Model.Destroy();
            }
        }
    }
}
