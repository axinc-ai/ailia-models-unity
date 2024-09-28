/* AILIA Unity Plugin Large Language Model Sample */
/* Copyright 2024 AXELL CORPORATION */

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using ailia;
using ailiaLLM;

namespace ailiaSDK
{
	public class AiliaLargeLanguageModelSample : MonoBehaviour
	{
		// Model list
		public enum LargeLanguageModelSampleModels
		{
			gemma2_2b,
		}

		// UI
		[SerializeField]
		public InputField input_field;

		// Settings
		public LargeLanguageModelSampleModels modelType = LargeLanguageModelSampleModels.gemma2_2b;
		public bool gpu_mode = false;
		public GameObject UICanvas = null;

		// Result
		Text label_text = null;
		Text mode_text = null;

		// AILIA
		private AiliaLLMModel llm = null;
		private List<AiliaLLMChatMessage> messages = new List<AiliaLLMChatMessage>(); // Chat History

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

			mode_text.text = "ailia Large Language Model Sample";
			label_text.text = "Please input query.";

			UICanvas.transform.Find("RawImage").GetComponent<RawImage>().gameObject.SetActive(false);
		}

		void AiliaInit()
		{
			// Create Ailia
			CreateAiliaNet(modelType, gpu_mode);
		}

		// Download models and Create ailiaModel
		void CreateAiliaNet(LargeLanguageModelSampleModels modelType, bool gpu_mode = true)
		{
			string asset_path = Application.temporaryCachePath;

			AiliaDownload ailia_download = new AiliaDownload();
			ailia_download.DownloaderProgressPanel = UICanvas.transform.Find("DownloaderProgressPanel").gameObject;
			var urlList = new List<ModelDownloadURL>();

			if (modelType == LargeLanguageModelSampleModels.gemma2_2b){
				urlList.Add(new ModelDownloadURL() { folder_path = "gemma", file_name = "gemma-2-2b-it-Q4_K_M.gguf" });
			}

			StartCoroutine(ailia_download.DownloadWithProgressFromURL(urlList, () =>
			{
				llm = new AiliaLLMModel();
				llm.Create();
				modelPrepared = llm.Open(asset_path + "/gemma-2-2b-it-Q4_K_M.gguf");
				if (modelPrepared == false){
					Debug.Log("ailiaModel.OpenFile failed");
				}

				// System Prompt
				AiliaLLMChatMessage message = new AiliaLLMChatMessage();
				message.role = "system";
				message.content = "あなたは可愛いくまのキャラクターです。発言の語尾に「くま」をつけてください。";
				messages.Add(message);
			}));
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
			llm.Generate(ref done);
			string deltaText = llm.GetDeltaText();
			generate_text = generate_text + deltaText;
			label_text.text = generate_text;

			//Debug.Log(generate_text);

			if (done){
				AiliaLLMChatMessage message = new AiliaLLMChatMessage();
				message.role = "assistant";
				message.content = generate_text;
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

			AiliaLLMChatMessage message = new AiliaLLMChatMessage();
			message.role = "user";
			message.content = query_text;
			messages.Add(message);

			input_field.text = "";
			generate_text = "";

			llm.SetPrompt(messages);
			done = false;
		}
	}
}