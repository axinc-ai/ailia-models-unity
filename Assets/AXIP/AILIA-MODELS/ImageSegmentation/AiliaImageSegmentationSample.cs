/* AILIA Unity Plugin Segmentation Sample */
/* Copyright 2018-2022 AXELL CORPORATION */

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ailiaSDK
{
	public class AiliaImageSegmentationSample : MonoBehaviour
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
			modnet
		}

		// Settings
		public ImageSegmentaionModels imageSegmentaionModels = ImageSegmentaionModels.HRNetV2_W18_Small_v2;
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
			ailiaModel = CreateAiliaNet(imageSegmentaionModels, gpu_mode);
			// Load sample image
			LoadImage(imageSegmentaionModels, AiliaImageSource);
		}

		void AllocateInputAndOutputTensor()
		{
			float rawImageRatio = rawImageSize.x / rawImageSize.y;
			float ratio = AiliaImageSource.Width / (float)AiliaImageSource.Height;
			raw_image.rectTransform.sizeDelta = new Vector2(ratio / rawImageRatio * rawImageSize.x, rawImageSize.y);

			SetShape(imageSegmentaionModels);

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

			mode_text.text = "ailia Image Segmentation\nSpace key down to original image";
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
				InputDataProcessingCPU(imageSegmentaionModels, inputImage, input);
			}else{
				bool convert_to_top2bottom = true;	// Convert to Top2Bottom format
				if (!gpu_mode || inputDataProcessingShader == null)
				{
					inputImage = AiliaImageSource.GetPixels32(rect, convert_to_top2bottom);
					InputDataProcessingCPU(imageSegmentaionModels, inputImage, input);
				}
				else
				{
					originalTexture = AiliaImageSource.GetTexture(rect);
					InputDataProcessing(imageSegmentaionModels, originalTexture, input, convert_to_top2bottom);
				}
			}
			long end_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

			// Predict
			long start_time2 = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
			bool result = ailiaModel.Predict(output, input);

			// convert result to image
			if (imageSegmentaionModels == ImageSegmentaionModels.hair_segmentation)
				LabelPaintHairSegmentation(output, outputImage, Color.red);
			else if (imageSegmentaionModels == ImageSegmentaionModels.pspnet_hair_segmentation)
				LabelPaintPspnet(output, outputImage, Color.red);
			else if (imageSegmentaionModels == ImageSegmentaionModels.u2net || imageSegmentaionModels == ImageSegmentaionModels.modnet)
				LabelPaintU2netModnet(output, outputImage, Color.green); 
			else
				LabelPaint(output, outputImage, OutputChannel, colorPalette);

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
		AiliaModel CreateAiliaNet(ImageSegmentaionModels modelType, bool gpu_mode = true)
		{
			string asset_path = Application.temporaryCachePath;
			string serverFolderName = "";
			string prototxtName = "";
			string onnxName = "";
			switch (modelType)
			{
				case ImageSegmentaionModels.HRNetV2_W18_Small_v2:
					serverFolderName = "hrnet";
					prototxtName = "HRNetV2-W18-Small-v2.onnx.prototxt";
					onnxName = "HRNetV2-W18-Small-v2.onnx";
					break;
				case ImageSegmentaionModels.HRNetV2_W18_Small_v1:
					serverFolderName = "hrnet";
					prototxtName = "HRNetV2-W18-Small-v1.onnx.prototxt";
					onnxName = "HRNetV2-W18-Small-v1.onnx";
					break;
				case ImageSegmentaionModels.HRNetV2_W48:
					serverFolderName = "hrnet";
					prototxtName = "HRNetV2-W48.onnx.prototxt";
					onnxName = "HRNetV2-W48.onnx";
					break;
				case ImageSegmentaionModels.hair_segmentation:
					serverFolderName = "hair_segmentation";
					prototxtName = "hair_segmentation.opt.onnx.prototxt";
					onnxName = "hair_segmentation.opt.onnx";
					break;
				case ImageSegmentaionModels.pspnet_hair_segmentation:
					serverFolderName = "pspnet-hair-segmentation";
					prototxtName = "pspnet-hair-segmentation.onnx.prototxt";
					onnxName = "pspnet-hair-segmentation.onnx";
					break;
				case ImageSegmentaionModels.deeplabv3:
					serverFolderName = "deeplabv3";
					prototxtName = "deeplabv3.opt.onnx.prototxt";
					onnxName = "deeplabv3.opt.onnx";
					break;
				case ImageSegmentaionModels.u2net:
					serverFolderName = "u2net";
					prototxtName = "u2net_opset11.onnx.prototxt";
					onnxName = "u2net_opset11.onnx";
					break;
				case ImageSegmentaionModels.modnet:
					serverFolderName = "modnet";
					prototxtName = "modnet.opt.onnx.prototxt";
					onnxName = "modnet.opt.onnx";
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

		void SetShape(ImageSegmentaionModels imageSegmentaionModels)
		{
			Ailia.AILIAShape shape = null;
			switch (imageSegmentaionModels)
			{
				case ImageSegmentaionModels.HRNetV2_W18_Small_v2:
				case ImageSegmentaionModels.HRNetV2_W18_Small_v1:
				case ImageSegmentaionModels.HRNetV2_W48:
				case ImageSegmentaionModels.pspnet_hair_segmentation:
				case ImageSegmentaionModels.deeplabv3:
					shape = ailiaModel.GetInputShape();
					InputWidth = (int)shape.x;
					InputHeight = (int)shape.y;
					InputChannel = (int)shape.z;
					shape = ailiaModel.GetOutputShape();
					OutputWidth = (int)shape.x;
					OutputHeight = (int)shape.y;
					OutputChannel = (int)shape.z;
					break;

				case ImageSegmentaionModels.hair_segmentation:
					shape = new Ailia.AILIAShape();
					shape.x = 3;
					shape.y = (uint)AiliaImageSource.Width;
					shape.z = (uint)AiliaImageSource.Height;
					shape.w = 1;
					shape.dim = 4;
					ailiaModel.SetInputShape(shape);
					InputWidth = AiliaImageSource.Width;
					InputHeight = AiliaImageSource.Height;
					InputChannel = 3;
					OutputWidth = AiliaImageSource.Width;
					OutputHeight = AiliaImageSource.Height;
					OutputChannel = 1;
					break;

				case ImageSegmentaionModels.u2net:
					shape = ailiaModel.GetInputShape();
					InputWidth = (int)shape.x;
					InputHeight = (int)shape.y;
					InputChannel = (int)shape.z;
					shape = ailiaModel.GetOutputShape();
					OutputWidth = (int)shape.x;
					OutputHeight = (int)shape.y;
					OutputChannel = (int)shape.z;
					break;

				case ImageSegmentaionModels.modnet:
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
					shape = ailiaModel.GetOutputShape();
					OutputWidth = (int)shape.x;
					OutputHeight = (int)shape.y;
					OutputChannel = (int)shape.z;
					Debug.Log(""+OutputChannel+"/"+OutputWidth+"/"+OutputHeight);
					//OutputWidth = AiliaImageSource.Width/32*32;
					//OutputHeight = AiliaImageSource.Height/32*32;
					//OutputChannel = 1;
					break;
			}
		}

		void LoadImage(ImageSegmentaionModels imageSegmentaionModels, AiliaImageSource ailiaImageSource)
		{
			switch (imageSegmentaionModels)
			{
				case ImageSegmentaionModels.HRNetV2_W18_Small_v2:
				case ImageSegmentaionModels.HRNetV2_W18_Small_v1:
				case ImageSegmentaionModels.HRNetV2_W48:
					ailiaImageSource.CreateSource("file://" + Application.dataPath + "/AXIP/AILIA-MODELS/ImageSegmentation/SampleImage/road.png");
					break;
				case ImageSegmentaionModels.hair_segmentation:
					ailiaImageSource.CreateSource("file://" + Application.dataPath + "/AXIP/AILIA-MODELS/ImageSegmentation/SampleImage/hair.jpg");
					break;
				case ImageSegmentaionModels.pspnet_hair_segmentation:
					ailiaImageSource.CreateSource("file://" + Application.dataPath + "/AXIP/AILIA-MODELS/ImageSegmentation/SampleImage/hair2.jpg");
					break;
				case ImageSegmentaionModels.deeplabv3:
					ailiaImageSource.CreateSource("file://" + Application.dataPath + "/AXIP/AILIA-MODELS/ImageSegmentation/SampleImage/couple.jpg");
					break;
				case ImageSegmentaionModels.u2net:
					ailiaImageSource.CreateSource("file://" + Application.dataPath + "/AXIP/AILIA-MODELS/ImageSegmentation/SampleImage/girl.png");
					break;
				case ImageSegmentaionModels.modnet:
					ailiaImageSource.CreateSource("file://" + Application.dataPath + "/AXIP/AILIA-MODELS/ImageSegmentation/SampleImage/modnet.jpg");
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

		void InputDataProcessingCPU(ImageSegmentaionModels imageSegmentaionModels, Color32[] inputImage, float[] processedInputBuffer)
		{
			float weight = 1f / 255f;
			float bias = 0;
			bool rgbRepeats = false;
			switch (imageSegmentaionModels)
			{
				case ImageSegmentaionModels.hair_segmentation:
					rgbRepeats = true;
					break;
				case ImageSegmentaionModels.pspnet_hair_segmentation:
					InputDataProcessingCPU_PSP(inputImage, processedInputBuffer);
					return;
				case ImageSegmentaionModels.deeplabv3:
					weight = 1f / 127.5f;
					bias = -1;
					break;
				case ImageSegmentaionModels.modnet:
					weight = 1f / 127.5f;
					bias = -1;
					break;
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
		void InputDataProcessing(ImageSegmentaionModels imageSegmentaionModels, Texture inputImage, float[] processedInputBuffer, bool upsideDown = false)
		{
			float weight = 1;
			float bias = 0;
			bool rgbRepeats = false;
			switch (imageSegmentaionModels)
			{
				case ImageSegmentaionModels.hair_segmentation:
					rgbRepeats = true;
					break;
				case ImageSegmentaionModels.pspnet_hair_segmentation:
					InputDataProcessingPSP(inputImage, processedInputBuffer, upsideDown);
					return;
				case ImageSegmentaionModels.deeplabv3:
					weight = 2;
					bias = -1;
					break;
				case ImageSegmentaionModels.u2net:
					weight = 2;
					break;
				case ImageSegmentaionModels.modnet:
					weight = 2;
					bias = -1;
					break;
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

		void LabelPaint(float[] labelData, Color32[] pixelBuffer, int channel, Color32[] palette)
		{
			Debug.Assert(palette.Length >= channel, "wrong palette size");
			Debug.Assert(labelData.Length == pixelBuffer.Length * channel, "wrong parameter");

			for (int i = 0; i < pixelBuffer.Length; i++)
			{
				int maxIndex = 0;
				float maxValue = labelData[i + 0 * (pixelBuffer.Length)];
				for (int c = 1; c < channel; c++)
				{
					if (maxValue < labelData[i + c * pixelBuffer.Length])
					{
						maxValue = labelData[i + c * pixelBuffer.Length];
						maxIndex = c;
					}
				}
				pixelBuffer[i] = palette[maxIndex];
			}
		}

		void LabelPaintHairSegmentation(float[] labelData, Color32[] pixelBuffer, Color32 color)
		{
			Debug.Assert(labelData.Length == pixelBuffer.Length, "wrong parameter");

			for (int i = 0; i < pixelBuffer.Length; i++)
			{
				pixelBuffer[i] = color;
				pixelBuffer[i].a = (byte)(Mathf.Clamp01(labelData[i]) * 255);
			}
		}

		void LabelPaintPspnet(float[] labelData, Color32[] pixelBuffer, Color32 color)
		{
			Debug.Assert(labelData.Length == pixelBuffer.Length, "wrong parameter");

			for (int i = 0; i < pixelBuffer.Length; i++)
			{
				pixelBuffer[i] = color;

				float k = Mathf.Exp(labelData[i]);
				pixelBuffer[i].a = (byte)(k / (1.0f + k) * 255);
			}
		}

		void LabelPaintU2netModnet(float[] labelData, Color32[] pixelBuffer, Color32 color)
		{
			Debug.Assert(labelData.Length == pixelBuffer.Length, "wrong parameter");	
			for (int i = 0; i < pixelBuffer.Length; i++)
			{
				pixelBuffer[i] = color;
				pixelBuffer[i].a = (byte)(255 - Mathf.Clamp01(labelData[i]) * 255);
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