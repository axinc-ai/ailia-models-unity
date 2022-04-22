/* AILIA Unity Plugin Detector Sample */
/* Copyright 2018-2019 AXELL CORPORATION */

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ailiaSDK
{
	public class AiliaAdainSample : MonoBehaviour
	{
		//Settings
		public bool gpu_mode = false;
		public ComputeShader inputDataProcessingShader = null;
		public ComputeShader adainDataProcessingShader = null;
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
		int computeShaderContentBufferId;
		int computeShaderStyleBufferId;
		int computeShaderAdainAlphaId;
		int channelFirstKernel;
		int channelFirstUpsideDownKernel;
		int AdaptiveInstanceNormalizationKernelId;
		int OutputToTextureKernelId;

		//AILIA
		AiliaModel ailiaModelVgg;
		AiliaModel ailiaModelDecoder;

		// Input source
		AiliaImageSource ContentImageSource;
		AiliaImageSource StyleImageSource;

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
		RenderTexture resultRenderTexture;
		Texture2D contentTexture;
		Texture2D styleTexture;
		Vector2 rawImageSize;
		float[] vggContentOutput;
		float[] vggStyleOutput;
		float[] decoderInput;
		float[] output;
		float[] contentImage;
		float[] styleImage;

		bool modelPrepared;

		void Start()
		{
			UISetup();

			// for Rendering 
			blendMaterial = new Material(Shader.Find("Ailia/AlphaBlending2Tex"));
			mainTexId = Shader.PropertyToID("_MainTex");
			blendTexId = Shader.PropertyToID("_BlendTex");
			blendFlagId = Shader.PropertyToID("blendFlag");
			mainVFlipId = Shader.PropertyToID("mainVFlip");
			blendVFlipId = Shader.PropertyToID("blendVFlip");
			raw_image.material = blendMaterial;

			rawImageSize = raw_image.rectTransform.sizeDelta;

			// for data processing
			computeShaderWeightId = Shader.PropertyToID("weight");
			computeShaderBiasId = Shader.PropertyToID("bias");
			computeShaderWidthId = Shader.PropertyToID("width");
			computeShaderHeightId = Shader.PropertyToID("height");
			computeShaderTextureId = Shader.PropertyToID("texure");
			computeShaderResultId = Shader.PropertyToID("result_buffer");
			computeShaderInputBufferId = Shader.PropertyToID("input_buffer");
			computeShaderResultTextureId = Shader.PropertyToID("result_texure");
			computeShaderContentBufferId = Shader.PropertyToID("content_buffer");
			computeShaderStyleBufferId = Shader.PropertyToID("style_buffer");
			computeShaderAdainAlphaId = Shader.PropertyToID("adainAlpha");

			channelFirstKernel = inputDataProcessingShader.FindKernel("ChannelFirst");
			channelFirstUpsideDownKernel = inputDataProcessingShader.FindKernel("ChannelFirstUpsideDown");
			AdaptiveInstanceNormalizationKernelId = adainDataProcessingShader.FindKernel("AdaptiveInstanceNormalization");
			OutputToTextureKernelId = outputDataToTextureShader.FindKernel("ChannelFirstToTexture");

			AiliaInit();
		}

		void AiliaInit()
		{
			// Create Ailia
			ailiaModelVgg = new AiliaModel();
			ailiaModelDecoder = new AiliaModel();
			CreateAdainNet(ailiaModelVgg, ailiaModelDecoder, gpu_mode);

			// Load sample image
			ContentImageSource = gameObject.AddComponent<AiliaImageSource>();
			StyleImageSource = gameObject.AddComponent<AiliaImageSource>();
			ContentImageSource.CreateSource("file://" + Application.dataPath + "/AXIP/AILIA-MODELS/StyleTransfer/SampleImage/cornell.jpg");
			StyleImageSource.CreateSource("file://" + Application.dataPath + "/AXIP/AILIA-MODELS/StyleTransfer/SampleImage/woman_with_hat_matisse.jpg");
		}

		void UISetup()
		{
			Debug.Assert(UICanvas != null, "UICanvas is null");

			label_text = UICanvas.transform.Find("LabelText").GetComponent<Text>();
			mode_text = UICanvas.transform.Find("ModeLabel").GetComponent<Text>();
			raw_image = UICanvas.transform.Find("RawImage").GetComponent<RawImage>();
			raw_image.gameObject.SetActive(false);

			mode_text.text = "ailia Adain\nSpace key down to switch image (<color=#f66>result</color> -> style -> original)";
		}

		int previewMode = 0;
		void Update()
		{
			if (!ContentImageSource.IsPrepared || !StyleImageSource.IsPrepared || !modelPrepared)
			{
				return;
			}

			if (vggContentOutput == null)
			{
				float rawImageRatio = rawImageSize.x / rawImageSize.y;
				float ratio = ContentImageSource.Width / (float)ContentImageSource.Height;
				raw_image.rectTransform.sizeDelta = new Vector2(ratio / rawImageRatio * rawImageSize.x, rawImageSize.y);

				SetShape();

				// texture & buffer allocate
				resultRenderTexture = new RenderTexture(OutputWidth, OutputHeight, 0, RenderTextureFormat.ARGBFloat);
				resultRenderTexture.enableRandomWrite = true;
				resultRenderTexture.Create();

				contentTexture = ContentImageSource.GetTexture(AiliaImageUtil.Crop.No);
				styleTexture = StyleImageSource.GetTexture(AiliaImageUtil.Crop.No);
				ContentImageSource.Resize(InputWidth, InputHeight);
				StyleImageSource.Resize(InputWidth, InputHeight);
			}

			if (oneshot)
			{
				oneshot = false;

				// Make input data
				long start_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
				InputDataPocessing(ContentImageSource.GetTexture(AiliaImageUtil.Crop.No), contentImage, true);
				InputDataPocessing(StyleImageSource.GetTexture(AiliaImageUtil.Crop.No), styleImage, true);
				long end_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

				// Predict
				long start_time2 = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
				bool result = ailiaModelVgg.Predict(vggContentOutput, contentImage);
				result = ailiaModelVgg.Predict(vggStyleOutput, styleImage);

				MiddleDataPocessing(vggContentOutput, vggStyleOutput, decoderInput);
				result = ailiaModelDecoder.Predict(output, decoderInput);
				// convert result to image
				OutputDataProcessing(output, resultRenderTexture);

				long end_time2 = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

				if (label_text != null)
				{
					label_text.text = ((end_time - start_time) + (end_time2 - start_time2)).ToString() + "ms\n" + ailiaModelVgg.EnvironmentName();
				}

				// for viewer
				raw_image.texture = contentTexture;
				blendMaterial.SetTexture(mainTexId, contentTexture);
				blendMaterial.SetTexture(blendTexId, resultRenderTexture);

				blendMaterial.SetFloat(mainVFlipId, 0);
				blendMaterial.SetFloat(blendVFlipId, 1);
				blendMaterial.SetFloat(blendFlagId, 1);

				raw_image.gameObject.SetActive(true);
			}

			// When space key down, draw original image
			if (Input.GetKeyDown(KeyCode.Space))
			{
				previewMode++;
				if (previewMode > 2) previewMode = 0;
				float rawImageRatio;
				float ratio;
				switch (previewMode)
				{
					case 0:
						rawImageRatio = rawImageSize.x / rawImageSize.y;
						ratio = contentTexture.width / (float)contentTexture.height;
						raw_image.rectTransform.sizeDelta = new Vector2(ratio / rawImageRatio * rawImageSize.x, rawImageSize.y);
						blendMaterial.SetFloat(blendFlagId, 1);
						blendMaterial.SetFloat(blendVFlipId, 1);
						blendMaterial.SetTexture(blendTexId, resultRenderTexture);
						mode_text.text = "ailia Adain\nSpace key down to switch image (<color=#f66>result</color> -> style -> original)";
						break;
					case 1:
						rawImageRatio = rawImageSize.x / rawImageSize.y;
						ratio = styleTexture.width / (float)styleTexture.height;
						raw_image.rectTransform.sizeDelta = new Vector2(ratio / rawImageRatio * rawImageSize.x, rawImageSize.y);
						blendMaterial.SetFloat(blendVFlipId, 0);
						blendMaterial.SetTexture(blendTexId, styleTexture);
						mode_text.text = "ailia Adain\nSpace key down to switch image (result -> <color=#f66>style</color> -> original)";
						break;
					case 2:
						rawImageRatio = rawImageSize.x / rawImageSize.y;
						ratio = contentTexture.width / (float)contentTexture.height;
						raw_image.rectTransform.sizeDelta = new Vector2(ratio / rawImageRatio * rawImageSize.x, rawImageSize.y);
						blendMaterial.SetFloat(blendFlagId, 0);
						blendMaterial.SetFloat(blendVFlipId, 1);
						mode_text.text = "ailia Adain\nSpace key down to switch image (result -> style -> <color=#f66>original</color>)";
						break;
				}
			}
		}

		// Download models and Create ailiaModel
		void CreateAdainNet(AiliaModel ailiaModelVgg, AiliaModel ailiaModelDecoder, bool gpu_mode = true)
		{
			string asset_path = Application.temporaryCachePath;
			string serverFolderName = "adain";

			if (gpu_mode)
			{
				// call before OpenFile
				ailiaModelVgg.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
				ailiaModelDecoder.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
			}

			AiliaDownload ailia_download = new AiliaDownload();
			ailia_download.DownloaderProgressPanel = UICanvas.transform.Find("DownloaderProgressPanel").gameObject;
			var urlList = new List<ModelDownloadURL>();
			urlList.Add(new ModelDownloadURL() { folder_path = serverFolderName, file_name = "adain-vgg.onnx.prototxt" });
			urlList.Add(new ModelDownloadURL() { folder_path = serverFolderName, file_name = "adain-vgg.onnx" });
			urlList.Add(new ModelDownloadURL() { folder_path = serverFolderName, file_name = "adain-decoder.onnx.prototxt" });
			urlList.Add(new ModelDownloadURL() { folder_path = serverFolderName, file_name = "adain-decoder.onnx" });

			StartCoroutine(ailia_download.DownloadWithProgressFromURL(urlList, () =>
			{
				modelPrepared = ailiaModelVgg.OpenFile(asset_path + "/adain-vgg.onnx.prototxt", asset_path + "/adain-vgg.onnx") &
								ailiaModelDecoder.OpenFile(asset_path + "/adain-decoder.onnx.prototxt", asset_path + "/adain-decoder.onnx");
			}));
		}

		void SetShape()
		{
			var shape = ailiaModelVgg.GetInputShape();
			InputWidth = (int)shape.x;
			InputHeight = (int)shape.y;
			InputChannel = (int)shape.z;
			contentImage = new float[InputWidth * InputHeight * InputChannel];
			styleImage = new float[InputWidth * InputHeight * InputChannel];

			shape = ailiaModelVgg.GetOutputShape();
			vggContentOutput = new float[shape.x * shape.y * shape.z];
			vggStyleOutput = new float[shape.x * shape.y * shape.z];

			shape = ailiaModelDecoder.GetInputShape();
			decoderInput = new float[shape.x * shape.y * shape.z];

			shape = ailiaModelDecoder.GetOutputShape();
			OutputWidth = (int)shape.x;
			OutputHeight = (int)shape.y;
			OutputChannel = (int)shape.z;
			output = new float[OutputWidth * OutputHeight * OutputChannel];
		}

		ComputeBuffer inputCBuffer;
		void InputDataPocessing(Texture inputImage, float[] processedInputBuffer, bool upsideDown = false)
		{
			if (inputCBuffer == null || inputCBuffer.count != processedInputBuffer.Length)
			{
				if (inputCBuffer != null) inputCBuffer.Release();
				inputCBuffer = new ComputeBuffer(processedInputBuffer.Length, sizeof(float));
			}

			int kernelIndex;
			if (upsideDown) kernelIndex = channelFirstUpsideDownKernel;
			else kernelIndex = channelFirstKernel;

			inputDataProcessingShader.SetFloat(computeShaderWeightId, 1);
			inputDataProcessingShader.SetFloat(computeShaderBiasId, 0);
			inputDataProcessingShader.SetInt(computeShaderWidthId, inputImage.width);
			inputDataProcessingShader.SetInt(computeShaderHeightId, inputImage.height);
			inputDataProcessingShader.SetTexture(kernelIndex, computeShaderTextureId, inputImage);
			inputDataProcessingShader.SetBuffer(kernelIndex, computeShaderResultId, inputCBuffer);
			inputDataProcessingShader.Dispatch(kernelIndex, inputImage.width / 32 + 1, inputImage.height / 32 + 1, 1);
			inputCBuffer.GetData(processedInputBuffer);
		}

		ComputeBuffer[] middleCBuffer = new ComputeBuffer[3];
		void MiddleDataPocessing(float[] content, float[] style, float[] decoderInput)
		{
			if (middleCBuffer[0] == null || middleCBuffer[0].count != style.Length)
			{
				if (middleCBuffer[0] != null) middleCBuffer[0].Release();
				middleCBuffer[0] = new ComputeBuffer(content.Length, sizeof(float));
			}
			if (middleCBuffer[1] == null || middleCBuffer[1].count != style.Length)
			{
				if (middleCBuffer[1] != null) middleCBuffer[1].Release();
				middleCBuffer[1] = new ComputeBuffer(style.Length, sizeof(float));
			}
			if (middleCBuffer[2] == null || middleCBuffer[2].count != decoderInput.Length)
			{
				if (middleCBuffer[2] != null) middleCBuffer[2].Release();
				middleCBuffer[2] = new ComputeBuffer(decoderInput.Length, sizeof(float));
			}

			middleCBuffer[0].SetData(content);
			middleCBuffer[1].SetData(style);
			adainDataProcessingShader.SetFloat(computeShaderAdainAlphaId, 1);
			adainDataProcessingShader.SetBuffer(AdaptiveInstanceNormalizationKernelId, computeShaderContentBufferId, middleCBuffer[0]);
			adainDataProcessingShader.SetBuffer(AdaptiveInstanceNormalizationKernelId, computeShaderStyleBufferId, middleCBuffer[1]);
			adainDataProcessingShader.SetBuffer(AdaptiveInstanceNormalizationKernelId, computeShaderResultId, middleCBuffer[2]);
			adainDataProcessingShader.Dispatch(AdaptiveInstanceNormalizationKernelId, 32, 1, 1);
			middleCBuffer[2].GetData(decoderInput);
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
			outputDataToTextureShader.SetBuffer(OutputToTextureKernelId, computeShaderInputBufferId, outputCbuffer);
			outputDataToTextureShader.SetTexture(OutputToTextureKernelId, computeShaderResultTextureId, resultTexture);
			outputDataToTextureShader.Dispatch(OutputToTextureKernelId, resultTexture.width / 32 + 1, resultTexture.height / 32 + 1, 1);
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
			ailiaModelVgg.Close();
			ailiaModelDecoder.Close();
			if (inputCBuffer != null) inputCBuffer.Release();
			if (outputCbuffer != null) outputCbuffer.Release();
			for(int i = 0; i < 3; i++)
			{
				if (middleCBuffer[i] != null) middleCBuffer[i].Release();
			}
		}
	}
}
