/* AILIA Unity Plugin Segmentation Sample */
/* Copyright 2018-2022 AXELL CORPORATION */

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ailiaSDK
{
	public class AiliaDepthEstimatorsSample: MonoBehaviour
	{
		// Model list
		public enum DepthEstimatorsModels
		{
			midas
		}

		// Settings
		public DepthEstimatorsModels depthEstimatorsModels = DepthEstimatorsModels.midas;
		public bool gpu_mode = false;
		public ComputeShader inputDataProcessingShader = null;
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
		private AiliaModel ailiaModel;
		private AiliaCamera ailia_camera = new AiliaCamera();

		// Input source
		AiliaImageSource AiliaImageSource;

		// Pre-and-Post processing Shader
		Material blendMaterial;
		int mainTexId;
		int blendTexId;
		int blendFlagId;
		int mainVFlipId;
		int blendVFlipId;

		// Model input and output
		int InputWidth;
		int InputHeight;
		int InputChannel;
		int OutputWidth;
		int OutputHeight;
		int OutputChannel;
		Texture2D labelTexture;
		Texture2D originalTexture;
		Vector2 rawImageSize;
		float[] output;
		float[] input;
		Color32[] outputImage;
		Color32[] colorPalette = AiliaImageUtil.CreatePalette(256, 127);

		bool modelPrepared = false;
		bool modelAllocated = false;

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
			ailiaModel = CreateAiliaNet(depthEstimatorsModels, gpu_mode);
			// Load sample image
			LoadImage(depthEstimatorsModels, AiliaImageSource);
		}

		void AllocateInputAndOutputTensor()
		{
			float rawImageRatio = rawImageSize.x / rawImageSize.y;
			float ratio = AiliaImageSource.Width / (float)AiliaImageSource.Height;
			raw_image.rectTransform.sizeDelta = new Vector2(ratio / rawImageRatio * rawImageSize.x, rawImageSize.y);

			SetShape(depthEstimatorsModels);

			// texture & buffer allocate
			labelTexture = new Texture2D(OutputWidth, OutputHeight, TextureFormat.RGBA32, false);
			AiliaImageSource.Resize(InputWidth, InputHeight);
			input = new float[InputWidth * InputHeight * InputChannel];
			output = new float[OutputWidth * OutputHeight * OutputChannel];
			outputImage = new Color32[OutputWidth * OutputHeight];
		}

		void UISetup()
		{
			Debug.Assert (UICanvas != null, "UICanvas is null");

			label_text = UICanvas.transform.Find("LabelText").GetComponent<Text>();
			mode_text = UICanvas.transform.Find("ModeLabel").GetComponent<Text>();
			raw_image = UICanvas.transform.Find("RawImage").GetComponent<RawImage>();
			raw_image.gameObject.SetActive(false);

			mode_text.text = "ailia Depth Estimation\nSpace key down to original image";
		}

		void Update()
		{
			if (!AiliaImageSource.IsPrepared || !modelPrepared)
			{
				return;
			}
			if (modelPrepared && !modelAllocated)
			{
				AllocateInputAndOutputTensor();
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
			Rect rect = new Rect(0, 0, InputWidth, InputHeight);
			long start_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
			Color32[] inputImage = null;
			if(camera_mode){
				int tex_width = ailia_camera.GetWidth();
				int tex_height = ailia_camera.GetHeight();
				inputImage = ailia_camera.GetPixels32(); // Bottom2Top format
				inputImage = ResizeImage(inputImage, tex_width, tex_height, InputWidth, InputHeight);	// Convert to Top2Bottom format
				InputDataProcessingCPU(depthEstimatorsModels, inputImage, input);
			}else{
				bool convert_to_top2bottom = true;	// Convert to Top2Bottom format
				if (!gpu_mode || inputDataProcessingShader == null)
				{
					inputImage = AiliaImageSource.GetPixels32(rect, convert_to_top2bottom);
					InputDataProcessingCPU(depthEstimatorsModels, inputImage, input);
				}
				else
				{
					originalTexture = AiliaImageSource.GetTexture(rect);
					InputDataProcessing(depthEstimatorsModels, originalTexture, input, convert_to_top2bottom);
				}
			}
			long end_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

			// Predict
			long start_time2 = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
			bool result = ailiaModel.Predict(output, input);

			// convert result to image
			if (depthEstimatorsModels == DepthEstimatorsModels.midas)
				LabelPaintMidas(output, outputImage);

			long end_time2 = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

			if (label_text != null)
			{
				label_text.text = ((end_time - start_time) + (end_time2 - start_time2)).ToString() + "ms\n" + ailiaModel.EnvironmentName();
			}

			// for viewer
			if (!gpu_mode || inputDataProcessingShader == null || camera_mode)
			{
				if(originalTexture == null){
					originalTexture = new Texture2D(InputWidth, InputHeight, TextureFormat.RGBA32, false);
				}
				originalTexture.SetPixels32(inputImage);
				originalTexture.Apply();
			}
			raw_image.texture = originalTexture;
			blendMaterial.SetTexture(mainTexId, originalTexture);

			labelTexture.SetPixels32(outputImage);
			labelTexture.Apply();
			blendMaterial.SetTexture(blendTexId, labelTexture);

			if (!gpu_mode || inputDataProcessingShader == null || camera_mode)
			{
				blendMaterial.SetFloat(mainVFlipId, 1);	//originalTexture is Top2Bottom
			}
			else
			{
				blendMaterial.SetFloat(mainVFlipId, 0);	//originalTexture is Bottom2Top
			}
			blendMaterial.SetFloat(blendVFlipId, 1);	//outputImage is Top2Bottom

			raw_image.gameObject.SetActive(true);

			oneshot = false;
		}

		// Download models and Create ailiaModel
		AiliaModel CreateAiliaNet(DepthEstimatorsModels modelType, bool gpu_mode = true)
		{
			string asset_path = Application.temporaryCachePath;
			string serverFolderName = "";
			string prototxtName = "";
			string onnxName = "";
			switch (modelType)
			{
				case DepthEstimatorsModels.midas:
					serverFolderName = "midas";
					prototxtName = "midas.onnx.prototxt";
					onnxName = "midas.onnx";
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

		void SetShape(DepthEstimatorsModels depthEstimatorsModels)
		{
			Ailia.AILIAShape shape = null;
			switch (depthEstimatorsModels)
			{
				case DepthEstimatorsModels.midas:
					shape = new Ailia.AILIAShape();
					shape.x = (uint)AiliaImageSource.Width/32*32;
					shape.y = (uint)AiliaImageSource.Height/32*32;
					shape.z = 3;
					shape.w = 1;
					shape.dim = 4;
					ailiaModel.SetInputShape(shape);
					InputWidth = AiliaImageSource.Width/32*32;
					InputHeight = AiliaImageSource.Height/32*32;
					InputChannel = 3;
					OutputWidth = AiliaImageSource.Width/32*32;
					OutputHeight = AiliaImageSource.Height/32*32;
					OutputChannel = 1;
					Debug.Log(OutputChannel+"/"+OutputWidth+"/"+OutputHeight);
					break;
			}
		}

		void LoadImage(DepthEstimatorsModels depthEstimatorsModels, AiliaImageSource ailiaImageSource)
		{
			switch (depthEstimatorsModels)
			{
				case DepthEstimatorsModels.midas:
					ailiaImageSource.CreateSource("file://" + Application.dataPath + "/AXIP/AILIA-MODELS/DepthEstimation/SampleImage/road.png");
					break;

			}
		}

		Color32 [] ResizeImage(Color32[] inputImage, int tex_width, int tex_height, int InputWidth, int InputHeight){
			Color32[] outputImage = new Color32[InputWidth * InputHeight];
			for(int y=0;y<InputHeight;y++){
				for(int x=0;x<InputWidth;x++){
					int x2 = x*tex_width/InputWidth;
					int y2 = y*tex_height/InputHeight;
					outputImage[(InputHeight-1-y)*InputHeight+x]=inputImage[y2*tex_width+x2];
				}
			}
			return outputImage;
		}

		void InputDataProcessingCPU(DepthEstimatorsModels depthEstimatorsModels, Color32[] inputImage, float[] processedInputBuffer)
		{
			float weight = 1f / 255f;
			float bias = 0;
			bool rgbRepeats = false;
			switch (depthEstimatorsModels)
			{
				case DepthEstimatorsModels.midas:
					InputDataProcessingCPU_PSP(inputImage, processedInputBuffer);
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
					// rrr...ggg...bbb...
					processedInputBuffer[i + inputImage.Length * 0] = (inputImage[i].r) * weight + bias;
					processedInputBuffer[i + inputImage.Length * 1] = (inputImage[i].g) * weight + bias;
					processedInputBuffer[i + inputImage.Length * 2] = (inputImage[i].b) * weight + bias;
				}
			}
		}

		void InputDataProcessingCPU_PSP(Color32[] inputImage, float[] processedInputBuffer)
		{
			for (int i = 0; i < inputImage.Length; i++)
			{
				// rrr...ggg...bbb...
				processedInputBuffer[i + inputImage.Length * 0] = (inputImage[i].r * (1f / 255f) - 0.485f) * (1f / 0.229f);
				processedInputBuffer[i + inputImage.Length * 1] = (inputImage[i].g * (1f / 255f) - 0.456f) * (1f / 0.224f);
				processedInputBuffer[i + inputImage.Length * 2] = (inputImage[i].b * (1f / 255f) - 0.406f) * (1f / 0.225f);
			}
		}

		ComputeBuffer cbuffer;
		void InputDataProcessing(DepthEstimatorsModels depthEstimatorsModels, Texture inputImage, float[] processedInputBuffer, bool upsideDown = false)
		{
			float weight = 1;
			float bias = 0;
			bool rgbRepeats = false;
			switch (depthEstimatorsModels)
			{
				case DepthEstimatorsModels.midas:
					InputDataProcessingPSP(inputImage, processedInputBuffer, upsideDown);
					return;
				default:
					break;
			}

			if (cbuffer == null || cbuffer.count != processedInputBuffer.Length)
			{
				if (cbuffer != null) cbuffer.Release();
				cbuffer = new ComputeBuffer(processedInputBuffer.Length, sizeof(float));
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
			inputDataProcessingShader.SetBuffer(kernelIndex, computeShaderResultId, cbuffer);
			inputDataProcessingShader.Dispatch(kernelIndex, inputImage.width / 32 + 1, inputImage.height / 32 + 1, 1);
			cbuffer.GetData(processedInputBuffer);
		}

		void InputDataProcessingPSP(Texture inputImage, float[] processedInputBuffer, bool upsideDown = false)
		{
			if (cbuffer == null || cbuffer.count != processedInputBuffer.Length)
			{
				if (cbuffer != null) cbuffer.Release();
				cbuffer = new ComputeBuffer(processedInputBuffer.Length, sizeof(float));
			}
			int kernelIndex = 0;
			if (upsideDown)
				kernelIndex = inputDataProcessingShader.FindKernel("ImageNetChannelFirstUpsideDown");
			else
				kernelIndex = inputDataProcessingShader.FindKernel("ImageNetChannelFirst");
			inputDataProcessingShader.SetInt(computeShaderWidthId, inputImage.width);
			inputDataProcessingShader.SetInt(computeShaderHeightId, inputImage.height);
			inputDataProcessingShader.SetTexture(kernelIndex, computeShaderTextureId, inputImage);
			inputDataProcessingShader.SetBuffer(kernelIndex, computeShaderResultId, cbuffer);
			inputDataProcessingShader.Dispatch(kernelIndex, inputImage.width / 32 + 1, inputImage.height / 32 + 1, 1);
			cbuffer.GetData(processedInputBuffer);
		}

		void LabelPaintMidas(float[] labelData, Color32[] pixelBuffer)
		{
			Debug.Assert(labelData.Length == pixelBuffer.Length, "wrong parameter");	
			float depth_max = 0;
			float depth_min = 10000;
			for (int i = 0; i < pixelBuffer.Length; i++)
			{
				if(depth_max < labelData[i]){
					depth_max = labelData[i];
				}
				if(depth_min > labelData[i]){
					depth_min = labelData[i];
				}
			}	 
			for (int i = 0; i < pixelBuffer.Length; i++)
			{
				labelData[i] = (float)(Math.Pow(2, 8) - 1) * (labelData[i] - depth_min) / (depth_max - depth_min);
				pixelBuffer[i].r = (byte)labelData[i];
				pixelBuffer[i].g = (byte)labelData[i];
				pixelBuffer[i].b = (byte)labelData[i];
				pixelBuffer[i].a = (byte)255;
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
			if (cbuffer != null) cbuffer.Release();
		}
	}
}