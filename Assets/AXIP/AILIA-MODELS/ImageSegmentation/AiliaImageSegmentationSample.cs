/* AILIA Unity Plugin Detector Sample */
/* Copyright 2018-2019 AXELL CORPORATION */

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ailiaSDK
{
	public class AiliaImageSegmentationSample : MonoBehaviour
	{
		public enum ImageSegmentaionModels
		{
			HRNetV2_W18_Small_v2,
			HRNetV2_W18_Small_v1,
			HRNetV2_W48,
			hair_segmentation,
			pspnet_hair_segmentation,
			deeplabv3,
			u2net,
			human_part_segmentation,
		}
		//Settings
		public ImageSegmentaionModels imageSegmentaionModels = ImageSegmentaionModels.HRNetV2_W18_Small_v2;
		public bool gpu_mode = false;
		public ComputeShader inputDataProcessingShader = null;
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
		int channelLastKernel;
		int channelLastUpsideDownKernel;
		int channelFirstKernel;
		int channelFirstUpsideDownKernel;

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
		Texture2D labelTexture;
		Texture2D originalTexture;
		Vector2 rawImageSize;
		float[] output;
		float[] input;
		Color32[] outputImage;
		Color32[] colorPalette = AiliaImageUtil.CreatePalette(256, 127);

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

			AiliaInit();
		}

		void AiliaInit()
		{
			// Create Ailia
			ailiaModel = CreateAiliaNet(imageSegmentaionModels, gpu_mode);
			// Load sample image
			LoadImage(imageSegmentaionModels, AiliaImageSource);
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

			if (output == null)
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
					InputDataProcessingCPU(imageSegmentaionModels, inputImage, input);
				}
				else
				{
					originalTexture = AiliaImageSource.GetTexture(rect);
					InputDataProcessing(imageSegmentaionModels, originalTexture, input, true);
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
				else if (imageSegmentaionModels == ImageSegmentaionModels.u2net)
					LabelPaintU2net(output, outputImage, Color.white); 
				else
					LabelPaint(output, outputImage, OutputChannel, colorPalette);

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

				labelTexture.SetPixels32(outputImage);
				labelTexture.Apply();
				blendMaterial.SetTexture(blendTexId, labelTexture);

				blendMaterial.SetFloat(mainVFlipId, 0);
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
				case ImageSegmentaionModels.human_part_segmentation:
					serverFolderName = "human_part_segmentation";
					prototxtName = "resnet-lip.onnx.prototxt";
					onnxName = "resnet-lip.onnx";
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
				
				case ImageSegmentaionModels.human_part_segmentation:
					shape = ailiaModel.GetInputShape();
					InputWidth = (int)shape.x;
					InputHeight = (int)shape.y;
					InputChannel = (int)shape.z;
					shape = ailiaModel.GetOutputShape();
					OutputWidth = (int)shape.x;
					OutputHeight = (int)shape.y;
					OutputChannel = (int)shape.z;
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
				case ImageSegmentaionModels.human_part_segmentation:
					ailiaImageSource.CreateSource("file://" + Application.dataPath + "/AXIP/AILIA-MODELS/ImageSegmentation/SampleImage/demo.jpg");
					break;
			}
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
				case ImageSegmentaionModels.human_part_segmentation:
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

		void LabelPaintU2net(float[] labelData, Color32[] pixelBuffer, Color32 color)
		{
			Debug.Assert(labelData.Length == pixelBuffer.Length, "wrong parameter");	
			for (int i = 0; i < pixelBuffer.Length; i++)
			{
				pixelBuffer[i] = color;
				pixelBuffer[i].a = (byte)(Mathf.Clamp01(labelData[i]) * 255);
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