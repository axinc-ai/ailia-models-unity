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
	public enum ImageSegmentaionModels
	{
		HRNetV2_W18_Small_v2,
		HRNetV2_W18_Small_v1,
		HRNetV2_W48,
		hair_segmentation,
		pspnet_hair_segmentation,
		deeplabv3,
		u2net,
		modnet,
		segment_anything1
	}

	public class AiliaImageSegmentationSample : MonoBehaviour
	{
		// Settings
		public ImageSegmentaionModels imageSegmentaionModels = ImageSegmentaionModels.HRNetV2_W18_Small_v2;
		public bool gpu_mode = false;
		public GameObject UICanvas = null;
		public bool camera_mode = true;
		public int camera_id = 0;

		// Result
		RawImage raw_image = null;
		Text label_text = null;
		Text mode_text = null;
		private bool oneshot = true;

		// compute shader id
		int computeShaderWeightId;
		int computeShaderBiasId;
		int computeShaderWidthId;
		int computeShaderHeightId;
		int computeShaderTextureId;
		int computeShaderResultId;
		int channelLastKernel;
		int channelLastUpsideDownKernel;
		int channelFirstKernel;
		int channelFirstUpsideDownKernel;

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
		private SegmentationModel segModel;
		private SegmentAnythingModel samModel;
		private bool isDraggingForBox = false;
		private Rect boxRect = new ();

		bool modelPrepared = false;
		bool modelAllocated = false;
		string envName = "";

		async void Start()
		{
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
			if(camera_mode){
				bool crop_square = false;
				ailia_camera.CreateCamera(camera_id, crop_square);
			}
		}

		void AiliaInit()
		{
			// Create Ailia
			CreateAiliaNet(imageSegmentaionModels, gpu_mode);
            
			// Load sample image
			LoadImage(imageSegmentaionModels, AiliaImageSource);

			// Resize
			if (imageSegmentaionModels == ImageSegmentaionModels.segment_anything1){
				AiliaImageSource.Resize(1024, 1024);
			}
		}

		void UISetup()
		{
			Debug.Assert (UICanvas != null, "UICanvas is null");

			label_text = UICanvas.transform.Find("LabelText").GetComponent<Text>();
			mode_text = UICanvas.transform.Find("ModeLabel").GetComponent<Text>();
			raw_image = UICanvas.transform.Find("RawImage").GetComponent<RawImage>();
			raw_image.gameObject.SetActive(false);

			if (imageSegmentaionModels == ImageSegmentaionModels.segment_anything1) {
				mode_text.text = "ailia Image Segmentation\n" +
					"Left/Right click: positive/negative point\n" +
					"Middle click: drag to define border box\n" +
					"Space key down to original image";
			} else {
				mode_text.text = "ailia Image Segmentation";
			}
		}
		Color32 [] VerticalFlip(Color32[] inputImage, int InputWidth, int InputHeight){
			Color32[] outputImage = new Color32[InputWidth * InputHeight];
			for(int y=0;y<InputHeight;y++){
				for(int x=0;x<InputWidth;x++){
					outputImage[(InputHeight-1-y)*InputHeight+x]=inputImage[y*InputWidth+x];
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
				if (imageSegmentaionModels != ImageSegmentaionModels.segment_anything1){
					segModel.AllocateInputAndOutputTensor(imageSegmentaionModels, AiliaImageSource.Width, AiliaImageSource.Height);
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
			if(camera_mode){
				inputImageWidth = ailia_camera.GetWidth();
				inputImageHeight = ailia_camera.GetHeight();
				inputImage = ailia_camera.GetPixels32(); // Bottom2Top format
				inputImage = VerticalFlip(inputImage, inputImageWidth, inputImageHeight); // Top2Bottom format
			}else{
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
			if (imageSegmentaionModels == ImageSegmentaionModels.segment_anything1)
			{
				if (samModel.GetClickPoints(0).Length == 0)
				{
					samModel.AddClickPoint(inputImageWidth / 4, inputImageHeight / 4 + 30);
				}

				samModel.ProcessFrame(inputImage, inputImageWidth, inputImageHeight);
				result = samModel.success;
            }
			else
			{
				result = segModel.ProcessFrame(imageSegmentaionModels, inputImage, inputImageWidth, inputImageHeight);
            }


			if (!result)
			{
				oneshot = false;
				return;
			}

			// convert result to image
			Color32[] outputImage;
			int outputWidth, outputHeight;
			if (imageSegmentaionModels == ImageSegmentaionModels.segment_anything1)
			{
				outputImage = samModel.visualizedResult.GetPixels32();
				outputWidth = samModel.visualizedResult.width;
				outputHeight = samModel.visualizedResult.height;
				Debug.Log("InputWidth" + inputImageWidth + "/" + inputImageHeight);
				Debug.Log("samModel.visualizedResult" + samModel.visualizedResult.width + "/" + samModel.visualizedResult.height);
			}
			else{
				(outputImage, outputWidth, outputHeight) = segModel.PostProcesss(imageSegmentaionModels, inputImageWidth, inputImageHeight);
			}

			long end_time2 = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

			if (label_text != null)
			{
				label_text.text = ((end_time - start_time) + (end_time2 - start_time2)).ToString() + "ms\n" + envName;
			}

			// for viewer
			if(originalTexture == null){
				originalTexture = new Texture2D(outputWidth, outputHeight, TextureFormat.RGBA32, false);
			}
			if(labelTexture == null){
				labelTexture = new Texture2D(outputWidth, outputHeight, TextureFormat.RGBA32, false);
			}
			originalTexture.SetPixels32(inputImage);
			originalTexture.Apply();
			raw_image.texture = originalTexture;
			blendMaterial.SetTexture(mainTexId, originalTexture);

			Debug.Log("originalTexture" + originalTexture.width + "/" + originalTexture.height);
			Debug.Log("labelTexture" + labelTexture.width + "/" + labelTexture.height);

			labelTexture.SetPixels32(outputImage);
			labelTexture.Apply();
			blendMaterial.SetTexture(blendTexId, labelTexture);

			blendMaterial.SetFloat(mainVFlipId, 1);	//originalTexture is Top2Bottom
			blendMaterial.SetFloat(blendVFlipId, 1);	//outputImage is Top2Bottom


			float rawImageRatio = rawImageSize.x / rawImageSize.y;
			float ratio = inputImageWidth/ (float)inputImageHeight;
			raw_image.rectTransform.sizeDelta = new Vector2(ratio / rawImageRatio * rawImageSize.x, rawImageSize.y);

			raw_image.gameObject.SetActive(true);

			oneshot = false;
		}

		// Download models and Create ailiaModel
		void CreateAiliaNet(ImageSegmentaionModels modelType, bool gpu_mode = true)
		{
			AiliaDownload ailia_download = new AiliaDownload();
			ailia_download.DownloaderProgressPanel = UICanvas.transform.Find("DownloaderProgressPanel").gameObject;
			List<ModelDownloadURL> urlList = null;

			if (imageSegmentaionModels == ImageSegmentaionModels.segment_anything1)
			{
				samModel = new SegmentAnythingModel();
				urlList = samModel.GetModelURLs();
			}
			else
			{
				segModel = new SegmentationModel();
				urlList = segModel.GetModelURLs(imageSegmentaionModels);
            }

            StartCoroutine(ailia_download.DownloadWithProgressFromURL(urlList, () =>
            {
                if (imageSegmentaionModels == ImageSegmentaionModels.segment_anything1)
                {
                    modelPrepared = samModel.InitializeModels(gpu_mode);
					envName = samModel.EnvironmentName();
                }
                else
                {
                    modelPrepared = segModel.InitializeModels(imageSegmentaionModels, gpu_mode);
					envName = segModel.EnvironmentName();
                }
            }));
		}


		void LoadImage(ImageSegmentaionModels imageSegmentaionModels, AiliaImageSource ailiaImageSource)
		{
			switch (imageSegmentaionModels)
			{
				case ImageSegmentaionModels.HRNetV2_W18_Small_v2:
				case ImageSegmentaionModels.HRNetV2_W18_Small_v1:
				case ImageSegmentaionModels.HRNetV2_W48:
					ailiaImageSource.CreateSource(image_source_hrnet);
					break;
				case ImageSegmentaionModels.hair_segmentation:
					ailiaImageSource.CreateSource(image_source_hair_segmentation);
					break;
				case ImageSegmentaionModels.pspnet_hair_segmentation:
					ailiaImageSource.CreateSource(image_source_pspnet_hair_segmentation);
					break;
				case ImageSegmentaionModels.deeplabv3:
					ailiaImageSource.CreateSource(image_source_deeplabv3);
					break;
				case ImageSegmentaionModels.u2net:
					ailiaImageSource.CreateSource(image_source_u2net);
					break;
                case ImageSegmentaionModels.modnet:
                    ailiaImageSource.CreateSource(image_source_modnet);
                    break;
                case ImageSegmentaionModels.segment_anything1:
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

            int x = Mathf.Clamp(Mathf.RoundToInt((mousePos.x - rawImageRect.x) * widthRatio), 0, raw_image.texture.width - 1);
            int y = raw_image.texture.height - 1 - Mathf.Clamp(Mathf.RoundToInt((mousePos.y - rawImageRect.y) * heightRatio), 0, raw_image.texture.height - 1);

            if (rawImageRect.Contains(mousePos))
            {
                if (leftClick || rightClick)
                {
                    samModel?.AddClickPoint(x, y, rightClick);
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

                samModel.SetBoxCoords(boxRect);
				oneshot = true;
            }
        }

        private void OnGUI()
        {
            if (!raw_image.isActiveAndEnabled || raw_image.texture == null)
            {
                return;
            }
            
			if (boxRect.width > 0 && boxRect.height > 0 || isDraggingForBox)
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
                int xMax = ((int)(isDraggingForBox ? Input.mousePosition.x : (boxRect.xMax / widthRatio + rawImageRect.x)));
                int yMax = ((int)(isDraggingForBox ? (Screen.height - Input.mousePosition.y) : (boxRect.yMax / heightRatio + rawImageRect.y)));

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
				lineArea.x = xMax - thickness;//Right
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
			if (samModel != null){
				samModel.Destroy();
			}
			if (segModel != null){
				segModel.Destroy();
			}
		}
	}
}