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
		private RawImage rawImage = null;
		private string currentImagePath = "";

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
					modelPrepared = llm.Open(asset_path + "/gemma-3-4b-it-Q4_K_M.gguf", 2048);
					if (modelPrepared){
						modelPrepared = llm.OpenMultimodalProjector(asset_path + "/gemma-3-4b-it-GGUF_mmproj-model-f16.gguf");
					}
				}
				if (modelPrepared == false){
					Debug.Log("ailiaModel.OpenFile failed");
				}
				else
				{
					// Check VLM capabilities
					bool vision_support = false;
					bool audio_support = false;
					if (llm.GetMultimodalCapabilities(ref vision_support, ref audio_support))
					{
						Debug.Log("Vision support: " + vision_support + ", Audio support: " + audio_support);
					}
					else
					{
						Debug.Log("Failed to get multimodal capabilities");
					}
				}

				StartCoroutine(LoadImage());
				SetSystemPrompt();
			}));
		}

		private void SetSystemPrompt(){
			messages.Clear();
			AiliaLLMMultimodalChatMessage message = new AiliaLLMMultimodalChatMessage();
			message.role = "system";
			message.content = "画像についてできるだけ簡潔に説明してください";
			message.media_data = null;
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

					// Save image to temporary file for file_path approach
					currentImagePath = Application.temporaryCachePath + "/sample_image.jpg";
					byte[] jpgData = currentImage.EncodeToJPG();
					System.IO.File.WriteAllBytes(currentImagePath, jpgData);

					Debug.Log("Image saved to: " + currentImagePath);
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
				message.media_data = null;
				messages.Add(message);
			}
		}

		public void Submit(){
			if (!modelPrepared) {
				Debug.Log("Model not prepared");
				return;
			}
			if (done == false) {
				Debug.Log("Generation in progress");
				return;
			}

			string query_text = input_field.text;
			Debug.Log("Submit called with query: " + query_text);

			// Don't process empty queries
			if (string.IsNullOrEmpty(query_text))
			{
				Debug.Log("Empty query, skipping");
				return;
			}

			if (string.IsNullOrEmpty(currentImagePath)){
				label_text.text = "Image not loaded yet";
				return;
			}

			if (llm.ContextFull()){
				messages = new List<AiliaLLMMultimodalChatMessage>();
				SetSystemPrompt();
			}

			// Create user message with image (following working sample)
			AiliaLLMMultimodalChatMessage message = new AiliaLLMMultimodalChatMessage();
			message.role = "user";
			message.content = query_text + " <__media__>"; // Add media tag
			message.media_data = new List<AiliaLLMMediaData>();

			// Add image using file_path approach (like working sample)
			AiliaLLMMediaData imageData = new AiliaLLMMediaData();
			imageData.media_type = "image";
			imageData.file_path = currentImagePath;
			imageData.data = null; // Use file_path instead of data
			imageData.width = 0;
			imageData.height = 0;
			message.media_data.Add(imageData);

			messages.Add(message);
			input_field.text = "";

			Debug.Log("Total messages: " + messages.Count);
			for (int i = 0; i < messages.Count; i++)
			{
				int mediaCount = (messages[i].media_data != null) ? messages[i].media_data.Count : 0;
				Debug.Log("Message " + i + " - Role: " + messages[i].role + ", Content: " + messages[i].content + ", Media count: " + mediaCount);
			}

			generate_text = "";
			bool success = llm.SetMultimodalPrompt(messages);
			if (!success) {
				Debug.Log("Failed to set multimodal prompt - Context full: " + llm.ContextFull());
				label_text.text = "Failed to set prompt. Please check console for details.";
				return;
			}

			Debug.Log("SetMultimodalPrompt succeeded, starting generation");
			done = false;
		}
	}
}