/* AILIA Unity Plugin Large Language Model Sample */
/* Copyright 2025 AXELL CORPORATION */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

using ailia;
using ailiaLLM;

namespace ailiaSDK
{
	public class AiliaVisionLanguageModelSample : MonoBehaviour
	{
		// Model list
		public enum VisionLanguageModelSampleModels
		{
			gemma3_4b,
		}

		// UI
		[SerializeField]
		public InputField input_field;

		// Settings
		public VisionLanguageModelSampleModels modelType = VisionLanguageModelSampleModels.gemma3_4b;
		public bool gpu_mode = false;
		public GameObject UICanvas = null;

		// Result
		Text label_text = null;
		Text mode_text = null;

		// AILIA
		private AiliaLLMModel llm = null;
		private List<AiliaLLMMultimodalChatMessage> messages = new List<AiliaLLMMultimodalChatMessage>(); // Chat History
		private Texture2D currentImage = null;
		private byte[] currentImageData = null;
		private RawImage rawImage = null;

		bool modelPrepared = false;
		bool modelAllocated = false;
		bool done = true;
		string generate_text = "";

		void Start()
		{
			AiliaLicense.CheckAndDownloadLicense();
			UISetup();

			// for Processing
			AiliaInit();
		}

		void UISetup()
		{
			Debug.Assert (UICanvas != null, "UICanvas is null");

			label_text = UICanvas.transform.Find("LabelText").GetComponent<Text>();
			mode_text = UICanvas.transform.Find("ModeLabel").GetComponent<Text>();

			mode_text.text = "ailia Vision Language Model Sample";
			label_text.text = "Loading image...";

			rawImage = UICanvas.transform.Find("RawImage").GetComponent<RawImage>();
			rawImage.gameObject.SetActive(true);
		}

		void AiliaInit()
		{
			// Create Ailia
			CreateAiliaNet(modelType, gpu_mode);
		}

		// Download models and Create ailiaModel
		void CreateAiliaNet(VisionLanguageModelSampleModels modelType, bool gpu_mode = true)
		{
			string asset_path = Application.temporaryCachePath;

			AiliaDownload ailia_download = new AiliaDownload();
			ailia_download.DownloaderProgressPanel = UICanvas.transform.Find("DownloaderProgressPanel").gameObject;
			var urlList = new List<ModelDownloadURL>();

			if (modelType == VisionLanguageModelSampleModels.gemma3_4b){
				urlList.Add(new ModelDownloadURL() { folder_path = "gemma", file_name = "gemma-3-4b-it-Q4_K_M.gguf" });
				urlList.Add(new ModelDownloadURL() { folder_path = "gemma", file_name = "gemma-3-4b-it-GGUF_mmproj-model-f16.gguf" });
			}

			StartCoroutine(ailia_download.DownloadWithProgressFromURL(urlList, () =>
			{
				llm = new AiliaLLMModel();
				llm.Create();
				if (modelType == VisionLanguageModelSampleModels.gemma3_4b){
					modelPrepared = llm.Open(asset_path + "/gemma-3-4b-it-Q4_K_M.gguf");
					if (modelPrepared){
						modelPrepared = llm.OpenMultimodalProjector(asset_path + "/gemma-3-4b-it-GGUF_mmproj-model-f16.gguf");
					}
				}
				if (modelPrepared == false){
					Debug.Log("ailiaModel.OpenFile failed");
				}

				StartCoroutine(LoadImage());
				SetSystemPrompt();
			}));
		}

		private void SetSystemPrompt(){
			AiliaLLMMultimodalChatMessage message = new AiliaLLMMultimodalChatMessage();
			message.role = "system";
			message.content = "あなたは画像分析の専門家です。画像について詳しく説明してください。";
			message.media_data = new List<AiliaLLMMediaData>();
			messages.Add(message);
		}

		private IEnumerator LoadImage(){
			string imageUrl = "https://storage.googleapis.com/ailia-models/misc/sample_image.jpg";
			using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(imageUrl))
			{
				yield return www.SendWebRequest();

				if (www.result == UnityWebRequest.Result.Success)
				{
					currentImage = DownloadHandlerTexture.GetContent(www);
					rawImage.texture = currentImage;

					// Convert to RGBA32 format for processing
					Texture2D readableTexture = new Texture2D(currentImage.width, currentImage.height, TextureFormat.RGBA32, false);
					readableTexture.SetPixels(currentImage.GetPixels());
					readableTexture.Apply();
					currentImageData = readableTexture.GetRawTextureData();

					label_text.text = "Image loaded. Please input query about the image.";
				}
				else
				{
					Debug.Log("Failed to load image: " + www.error);
					label_text.text = "Failed to load image.";
				}
			}
		}

		void Update()
		{
			if (!modelPrepared)
			{
				return;
			}

			Generate();
		}

		void OnApplicationQuit()
		{
			DestroyAilia();
		}

		void OnDestroy()
		{
			DestroyAilia();
		}

		private void DestroyAilia()
		{
			llm.Close();
		}

		private void Generate(){
			if (done == true){
				return;
			}

			bool status = llm.Generate(ref done);
			if (status == false){
				// context size full
				done = true;
				return;
			}

			string deltaText = llm.GetDeltaText();
			generate_text = generate_text + deltaText;
			label_text.text = generate_text;

			if (done){
				AiliaLLMMultimodalChatMessage message = new AiliaLLMMultimodalChatMessage();
				message.role = "assistant";
				message.content = generate_text;
				message.media_data = new List<AiliaLLMMediaData>();
				messages.Add(message);
			}
		}

		public void Submit(){
			if (!modelPrepared) {
				return;
			}
			if (done == false) {
				return;
			}

			string query_text = input_field.text;

			if (llm.ContextFull()){
				messages = new List<AiliaLLMMultimodalChatMessage>();
				SetSystemPrompt();
			}

			AiliaLLMMultimodalChatMessage message = new AiliaLLMMultimodalChatMessage();
			message.role = "user";
			message.content = query_text;
			message.media_data = new List<AiliaLLMMediaData>();

			// Add image data if available
			if (currentImageData != null && currentImage != null){
				AiliaLLMMediaData imageData = new AiliaLLMMediaData();
				imageData.media_type = "image";
				imageData.data = currentImageData;
				imageData.width = (uint)currentImage.width;
				imageData.height = (uint)currentImage.height;
				imageData.file_path = null;
				message.media_data.Add(imageData);
			}

			messages.Add(message);

			input_field.text = "";
			generate_text = "";

			llm.SetMultimodalPrompt(messages);
			done = false;
		}
	}
}