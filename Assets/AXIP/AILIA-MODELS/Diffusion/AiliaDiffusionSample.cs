/* AILIA Unity Plugin Diffusion Sample */
/* Copyright 2023 AXELL CORPORATION */

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ailiaSDK
{
	public class AiliaDiffusionSample : MonoBehaviour
	{
		public enum DiffusionModels
		{
			Inpainting,
			SuperResolution
		}

		//Settings
		public DiffusionModels diffusionModels = DiffusionModels.Inpainting;
		public bool gpu_mode = false;
		public GameObject UICanvas = null;

		//Result
		RawImage raw_image = null;
		Text label_text = null;
		Text mode_text = null;

		//AILIA
		private AiliaDiffusionInpainting inpainting = new AiliaDiffusionInpainting();
		private AiliaDiffusionSuperResolution super_resolution = new AiliaDiffusionSuperResolution();

		// Input source
		public AiliaImageSource AiliaImageSource;
		public AiliaImageSource AiliaImageSourceMask;
		public AiliaImageSource AiliaImageSourceMaskResize;

		// Image size
		private int InputWidth = -1;
		private int InputHeight = -1;
		private int OutputWidth = -1;
		private int OutputHeight = -1;

		// Output image
		Texture2D resultTexture2D;
		Texture2D originalTexture;
		Vector2 rawImageSize;

		// Diffusion steps
		private int ddim_num_steps = 5;
		private int step = 0;
		private const float SLEEP_TIME = 1.0f;
		private float sleep = SLEEP_TIME;
		private bool oneshot = true;

		bool modelPrepared;

		void Start()
		{
			UISetup();

			rawImageSize = raw_image.rectTransform.sizeDelta;

			CreateAiliaNet(diffusionModels, gpu_mode);
			LoadImage(diffusionModels);
			AllocateBuffer();
		}

		void UISetup()
		{
			Debug.Assert(UICanvas != null, "UICanvas is null");

			label_text = UICanvas.transform.Find("LabelText").GetComponent<Text>();
			mode_text = UICanvas.transform.Find("ModeLabel").GetComponent<Text>();
			raw_image = UICanvas.transform.Find("RawImage").GetComponent<RawImage>();
			raw_image.gameObject.SetActive(false);

			mode_text.text = "ailia Diffusion\nSpace key down to original image";
		}

		private void AllocateBuffer(){
			float rawImageRatio = rawImageSize.x / rawImageSize.y;
			float ratio = AiliaImageSource.Width / (float)AiliaImageSource.Height;
			raw_image.rectTransform.sizeDelta = new Vector2(ratio / rawImageRatio * rawImageSize.x, rawImageSize.y);

			switch (diffusionModels)
			{
				case DiffusionModels.Inpainting:
					InputWidth = 512;
					InputHeight = 512;

					AiliaImageSource.Resize(InputWidth, InputHeight);
					AiliaImageSourceMask.Resize(InputWidth, InputHeight);
					AiliaImageSourceMaskResize.Resize(InputWidth / 4, InputHeight / 4);
					break;
				case DiffusionModels.SuperResolution:
					InputWidth = 128;
					InputHeight = 128;

					AiliaImageSource.Resize(InputWidth, InputHeight);
					break;
			}

			OutputWidth = 512;
			OutputHeight = 512;

			resultTexture2D = new Texture2D(OutputWidth, OutputHeight, TextureFormat.RGBA32, false);
		}

		void Update()
		{
			bool imagePrepared = false;
			
			switch (diffusionModels)
			{
				case DiffusionModels.Inpainting:
					imagePrepared = !AiliaImageSource.IsPrepared || !AiliaImageSourceMask.IsPrepared || !AiliaImageSourceMaskResize.IsPrepared;
					break;
				case DiffusionModels.SuperResolution:
					imagePrepared = !AiliaImageSource.IsPrepared;
					break;
			}

			if (imagePrepared || !modelPrepared)
			{
				Debug.Log("Waiting prepare "+AiliaImageSource.IsPrepared+","+AiliaImageSourceMask.IsPrepared+","+AiliaImageSourceMaskResize.IsPrepared+","+modelPrepared);
				return;
			}

			if (sleep > 0){
				sleep = sleep - Time.deltaTime;
				if (sleep < 0.0f){
					sleep = 0.0f;
				}
			}

			if (oneshot && sleep == 0.0f)
			{
				Rect rect = new Rect(0, 0, InputWidth, InputHeight);
				Color32[] inputImage = null;
				Color32[] inputMask = null;
				Color32[] inputMaskResize = null;

				// Diffusion loop
				Color32[] outputImage = null;
				switch (diffusionModels)
				{
				case DiffusionModels.Inpainting:
					inputImage = AiliaImageSource.GetPixels32(rect, true);
					inputMask = AiliaImageSourceMask.GetPixels32(rect, true);
					inputMaskResize = AiliaImageSourceMaskResize.GetPixels32(rect, true);
					outputImage = inpainting.Predict(inputImage, inputMask, inputMaskResize, step, ddim_num_steps);
					break;
				case DiffusionModels.SuperResolution:
					inputImage = AiliaImageSource.GetPixels32(rect, true);
					outputImage = super_resolution.Predict(inputImage, step, ddim_num_steps);
					break;
				}

				step++;
				if (step >= ddim_num_steps){
					oneshot = false;
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

				// sleep
				sleep = SLEEP_TIME;

				if (label_text != null)
				{
					switch (diffusionModels)
					{
					case DiffusionModels.Inpainting:
						label_text.text = inpainting.GetProfile();
						break;
					case DiffusionModels.SuperResolution:
						label_text.text = super_resolution.GetProfile();
						break;
					}
				}
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
		void CreateAiliaNet(DiffusionModels modelType, bool gpu_mode = true)
		{
			string asset_path = Application.temporaryCachePath;

			var urlList = new List<ModelDownloadURL>();
			string serverFolderName = "";

			switch (modelType)
			{
				case DiffusionModels.Inpainting:
					serverFolderName = "latent-diffusion-inpainting";
					urlList.Add(new ModelDownloadURL() { folder_path = serverFolderName, file_name = "cond_stage_model.onnx.prototxt" });
					urlList.Add(new ModelDownloadURL() { folder_path = serverFolderName, file_name = "cond_stage_model.onnx" });
					urlList.Add(new ModelDownloadURL() { folder_path = serverFolderName, file_name = "diffusion_model.onnx.prototxt" });
					urlList.Add(new ModelDownloadURL() { folder_path = serverFolderName, file_name = "diffusion_model.onnx" });
					urlList.Add(new ModelDownloadURL() { folder_path = serverFolderName, file_name = "autoencoder.onnx.prototxt" });
					urlList.Add(new ModelDownloadURL() { folder_path = serverFolderName, file_name = "autoencoder.onnx" });
					break;
				case DiffusionModels.SuperResolution:
					serverFolderName = "latent-diffusion-superresolution";
					urlList.Add(new ModelDownloadURL() { folder_path = serverFolderName, file_name = "diffusion_model.onnx.prototxt", local_name = "diffusion_model_sr.onnx.prototxt" });
					urlList.Add(new ModelDownloadURL() { folder_path = serverFolderName, file_name = "diffusion_model.onnx", local_name =  "diffusion_model_sr.onnx"});
					urlList.Add(new ModelDownloadURL() { folder_path = serverFolderName, file_name = "first_stage_decode.onnx.prototxt" });
					urlList.Add(new ModelDownloadURL() { folder_path = serverFolderName, file_name = "first_stage_decode.onnx" });
					break;
			}

			AiliaDownload ailia_download = new AiliaDownload();
			ailia_download.DownloaderProgressPanel = UICanvas.transform.Find("DownloaderProgressPanel").gameObject;

			StartCoroutine(ailia_download.DownloadWithProgressFromURL(urlList, () =>
			{
				switch (modelType)
				{
					case DiffusionModels.Inpainting:
						modelPrepared = inpainting.Open(asset_path + "/" + "diffusion_model.onnx.prototxt", asset_path + "/" + "diffusion_model.onnx",asset_path + "/" + "autoencoder.onnx.prototxt", asset_path + "/" + "autoencoder.onnx", asset_path + "/" + "cond_stage_model.onnx.prototxt", asset_path + "/" + "cond_stage_model.onnx", gpu_mode);
						break;
					case DiffusionModels.SuperResolution:
						modelPrepared = super_resolution.Open(asset_path + "/" + "diffusion_model_sr.onnx.prototxt", asset_path + "/" + "diffusion_model_sr.onnx", asset_path + "/" + "first_stage_decode.onnx.prototxt", asset_path + "/" + "first_stage_decode.onnx", gpu_mode);
						break;
				}
			}));
		}

		void LoadImage(DiffusionModels diffusionModels)
		{
			switch (diffusionModels)
			{
				case DiffusionModels.Inpainting:
					AiliaImageSource.CreateSource("file://" + Application.dataPath + "/AXIP/AILIA-MODELS/Diffusion/SampleImage/inpainting.png");
					AiliaImageSourceMask.CreateSource("file://" + Application.dataPath + "/AXIP/AILIA-MODELS/Diffusion/SampleImage/inpainting_mask.png");
					AiliaImageSourceMaskResize.CreateSource("file://" + Application.dataPath + "/AXIP/AILIA-MODELS/Diffusion/SampleImage/inpainting_mask.png");
					break;
				case DiffusionModels.SuperResolution:
					AiliaImageSource.CreateSource("file://" + Application.dataPath + "/AXIP/AILIA-MODELS/Diffusion/SampleImage/super_resolution.jpg");
					break;
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
			inpainting.Close();
			super_resolution.Close();
		}

		void OnDestroy()
		{
			inpainting.Close();
			super_resolution.Close();
		}
	}
}