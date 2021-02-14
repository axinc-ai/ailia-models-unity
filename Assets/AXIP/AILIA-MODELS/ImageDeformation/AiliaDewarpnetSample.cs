/* AILIA Unity Plugin Detector Sample */
/* Copyright 2018-2019 AXELL CORPORATION */

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ailiaSDK
{
	public class AiliaDewarpnetSample : MonoBehaviour
	{
		//Settings
		public bool gpu_mode = false;
		public ComputeShader inputDataProcessingShader = null;
		public ComputeShader dewarpnetDataProcessingShader = null;
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
		int channelFirstKernel;
		int channelFirstUpsideDownKernel;
		int resizeKernel;
		int bmOutputToUVTextureKernel;

		//AILIA
		AiliaModel ailiaModelWC;
		AiliaModel ailiaModelBM;

		// Input source
		AiliaImageSource AiliaImageSource;

		// shader
		Material blendMaterial;
		int mainTexId;
		int uvTexId;
		int blendFlagId;
		int mainVFlipId;

		int InputWidth;
		int InputHeight;
		int InputChannel;
		int OutputWidth;
		int OutputHeight;
		int OutputChannel;
		RenderTexture resultRenderTexture;
		Texture2D originalTexture;
		Vector2 rawImageSize;
		float[] outputWC;
		float[] inputBM;
		float[] outputBM;
		float[] input;

		bool modelPrepared;

		void Start()
		{
			UISetup();


			// for Rendering 
			blendMaterial = new Material(Shader.Find("Ailia/DewarpnetShader"));
			mainTexId = Shader.PropertyToID("_MainTex");
			uvTexId = Shader.PropertyToID("_uvTex");
			blendFlagId = Shader.PropertyToID("blendFlag");
			mainVFlipId = Shader.PropertyToID("mainVFlip");
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

			channelFirstKernel = inputDataProcessingShader.FindKernel("ChannelFirst");
			channelFirstUpsideDownKernel = inputDataProcessingShader.FindKernel("ChannelFirstUpsideDown");
			resizeKernel = dewarpnetDataProcessingShader.FindKernel("WCtoBM_Resize");
			bmOutputToUVTextureKernel = dewarpnetDataProcessingShader.FindKernel("bmOutputToUVTexture");

			AiliaInit();
		}

		void AiliaInit()
		{
			// Create Ailia
			ailiaModelWC = new AiliaModel();
			ailiaModelBM = new AiliaModel();
			CreateDewarpNet(ailiaModelWC, ailiaModelBM, gpu_mode);

			// Load sample image
			AiliaImageSource = gameObject.GetComponent<AiliaImageSource>();
			if (AiliaImageSource == null)
			{
				AiliaImageSource = gameObject.AddComponent<AiliaImageSource>();
			}
			AiliaImageSource.CreateSource("file://" + Application.dataPath + "/AXIP/AILIA-MODELS/ImageDeformation/SampleImage/dewarpnet_test.png");

		}

		void UISetup()
		{
			Debug.Assert(UICanvas != null, "UICanvas is null");

			label_text = UICanvas.transform.Find("LabelText").GetComponent<Text>();
			mode_text = UICanvas.transform.Find("ModeLabel").GetComponent<Text>();
			raw_image = UICanvas.transform.Find("RawImage").GetComponent<RawImage>();
			raw_image.gameObject.SetActive(false);

			mode_text.text = "ailia Dewarpnet\nSpace key down to original image";
		}

		void Update()
		{
			if (!AiliaImageSource.IsPrepared || !modelPrepared)
			{
				return;
			}

			if (outputWC == null)
			{
				float rawImageRatio = rawImageSize.x / rawImageSize.y;
				float ratio = AiliaImageSource.Width / (float)AiliaImageSource.Height;
				raw_image.rectTransform.sizeDelta = new Vector2(ratio / rawImageRatio * rawImageSize.x, rawImageSize.y);

				SetShape();

				// texture & buffer allocate
				resultRenderTexture = new RenderTexture(OutputWidth, OutputHeight, 0, RenderTextureFormat.ARGBFloat);
				resultRenderTexture.enableRandomWrite = true;
				resultRenderTexture.Create();

				originalTexture = AiliaImageSource.GetTexture(AiliaImageUtil.Crop.No);
				AiliaImageSource.Resize(InputWidth, InputHeight);
			}

			if (oneshot)
			{
				oneshot = false;

				// Make input data
				long start_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
				var inputTexture = AiliaImageSource.GetTexture(new Rect(0, 0, InputWidth, InputHeight));
				InputDataPocessing(inputTexture, input, true);
				long end_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

				// Predict
				long start_time2 = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
				bool result = ailiaModelWC.Predict(outputWC, input);

				MiddleDataPocessing(outputWC, inputBM);
				result = ailiaModelBM.Predict(outputBM, inputBM);
				// convert result to image
				OutputDataProcessing(outputBM, resultRenderTexture);

				long end_time2 = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

				if (label_text != null)
				{
					label_text.text = ((end_time - start_time) + (end_time2 - start_time2)).ToString() + "ms\n" + ailiaModelWC.EnvironmentName();
				}

				// for viewer
				raw_image.texture = originalTexture;
				blendMaterial.SetTexture(mainTexId, originalTexture);
				blendMaterial.SetTexture(uvTexId, resultRenderTexture);

				blendMaterial.SetFloat(mainVFlipId, 1);

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
		void CreateDewarpNet(AiliaModel ailiaModelWC, AiliaModel ailiaModelBM, bool gpu_mode = true)
		{
			string asset_path = Application.temporaryCachePath;
			string serverFolderName = "dewarpnet";

			if (gpu_mode)
			{
				// call before OpenFile
				ailiaModelWC.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
				ailiaModelBM.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
			}

			AiliaDownload ailia_download = new AiliaDownload();
			ailia_download.DownloaderProgressPanel = UICanvas.transform.Find("DownloaderProgressPanel").gameObject;
			var urlList = new List<ModelDownloadURL>();
			urlList.Add(new ModelDownloadURL() { folder_path = serverFolderName, file_name = "wc_model.onnx.prototxt" });
			urlList.Add(new ModelDownloadURL() { folder_path = serverFolderName, file_name = "wc_model.onnx" });
			urlList.Add(new ModelDownloadURL() { folder_path = serverFolderName, file_name = "bm_model.onnx.prototxt" });
			urlList.Add(new ModelDownloadURL() { folder_path = serverFolderName, file_name = "bm_model.onnx" });

			StartCoroutine(ailia_download.DownloadWithProgressFromURL(urlList, () =>
			{
				modelPrepared = ailiaModelWC.OpenFile(asset_path + "/wc_model.onnx.prototxt", asset_path + "/wc_model.onnx") &
								ailiaModelBM.OpenFile(asset_path + "/bm_model.onnx.prototxt", asset_path + "/bm_model.onnx");
			}));
		}

		void SetShape()
		{
			var shape = ailiaModelWC.GetInputShape();
			InputWidth = (int)shape.x;
			InputHeight = (int)shape.y;
			InputChannel = (int)shape.z;
			input = new float[InputWidth * InputHeight * InputChannel];

			shape = ailiaModelWC.GetOutputShape();
			outputWC = new float[shape.x * shape.y * shape.z];

			shape = ailiaModelBM.GetInputShape();
			inputBM = new float[shape.x * shape.y * shape.z];

			shape = ailiaModelBM.GetOutputShape();
			OutputWidth = (int)shape.x;
			OutputHeight = (int)shape.y;
			OutputChannel = (int)shape.z;
			outputBM = new float[OutputWidth * OutputHeight * OutputChannel];
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

		ComputeBuffer[] middleCBuffer = new ComputeBuffer[2];
		void MiddleDataPocessing(float[] wcOutput, float[] bmInput)
		{
			if (middleCBuffer[0] == null || middleCBuffer[0].count != wcOutput.Length)
			{
				if (middleCBuffer[0] != null) middleCBuffer[0].Release();
				middleCBuffer[0] = new ComputeBuffer(wcOutput.Length, sizeof(float));
			}
			if (middleCBuffer[1] == null || middleCBuffer[1].count != bmInput.Length)
			{
				if (middleCBuffer[1] != null) middleCBuffer[1].Release();
				middleCBuffer[1] = new ComputeBuffer(bmInput.Length, sizeof(float));
			}

			middleCBuffer[0].SetData(wcOutput);
			dewarpnetDataProcessingShader.SetBuffer(resizeKernel, computeShaderInputBufferId, middleCBuffer[0]);
			dewarpnetDataProcessingShader.SetBuffer(resizeKernel, computeShaderResultId, middleCBuffer[1]);
			dewarpnetDataProcessingShader.Dispatch(resizeKernel, 4, 4, 3);
			middleCBuffer[1].GetData(bmInput);
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
			dewarpnetDataProcessingShader.SetBuffer(bmOutputToUVTextureKernel, computeShaderInputBufferId, outputCbuffer);
			dewarpnetDataProcessingShader.SetTexture(bmOutputToUVTextureKernel, computeShaderResultTextureId, resultTexture);

			dewarpnetDataProcessingShader.Dispatch(bmOutputToUVTextureKernel, 4, 4, 1);
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
			ailiaModelWC.Close();
			ailiaModelBM.Close();
			if (inputCBuffer != null) inputCBuffer.Release();
			if (middleCBuffer[0] != null) middleCBuffer[0].Release();
			if (middleCBuffer[1] != null) middleCBuffer[1].Release();
			if (outputCbuffer != null) outputCbuffer.Release();
		}
	}
}
