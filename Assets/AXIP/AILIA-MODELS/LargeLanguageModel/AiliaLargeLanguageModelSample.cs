/* AILIA Unity Plugin Large Language Model Sample */
/* Copyright 2024 AXELL CORPORATION */

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
		public LargeLanguageModelSampleModels gemma2_2b = LargeLanguageModelSampleModels.gemma2_2b;
		public bool gpu_mode = false;
		public GameObject UICanvas = null;

		// Result
		Text label_text = null;
		Text mode_text = null;

		// AILIA
		private AiliaLLMModel ailiaLLMModel = null;
    	private List<AiliaLLMChatMessage> messages = new List<AiliaLLMChatMessage>();

		bool modelPrepared = false;
		bool modelAllocated = false;

		// Chunk
		public TextAsset database = null;
		private string[] chunk_text = null;
		private List<float[]> chunk_embedding;
		private int chunk_cnt = 0;

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

			mode_text.text = "ailia Natural Processing Sample";

			UICanvas.transform.Find("RawImage").GetComponent<RawImage>().gameObject.SetActive(false);
		}

		void AiliaInit()
		{
			// Create Ailia
			ailiaModel = CreateAiliaNet(modelType, gpu_mode);

			// Embedding
			chunk_text = database.text.Split('\n');
			chunk_embedding = new List<float[]>();
		}


		// Download models and Create ailiaModel
		AiliaModel CreateAiliaNet(NaturalLanguageProcessingSampleModels modelType, bool gpu_mode = true)
		{
			string asset_path = Application.temporaryCachePath;

			ailiaModel = new AiliaModel();
			if (gpu_mode)
			{
				// call before OpenFile
				ailiaModel.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
			}

			ailiaTokenizer = new AiliaTokenizerModel();

			AiliaDownload ailia_download = new AiliaDownload();
			ailia_download.DownloaderProgressPanel = UICanvas.transform.Find("DownloaderProgressPanel").gameObject;
			var urlList = new List<ModelDownloadURL>();

			if (modelType == LargeLanguageModelSampleModels.gemma2_2b){
				urlList.Add(new ModelDownloadURL() { folder_path = "gemma2", file_name = "gemma-2-2b-it-Q4_K_M.gguf" });
			}

			StartCoroutine(ailia_download.DownloadWithProgressFromURL(urlList, () =>
			{


        llm.Create();
        llm.Open(Application.streamingAssetsPath + "/gemma-2-2b-it-Q4_K_M.gguf");

        AiliaLLMChatMessage message = new AiliaLLMChatMessage();
        message.role = "system";
        message.content = "語尾に「だわん」をつけてください。";
        messages.Add(message);
				if (modelPrepared == false){
					Debug.Log("ailiaModel.OpenFile failed");
				}
			}));

			return ailiaModel;
		}

		void Update()
		{
			if (!modelPrepared)
			{
				return;
			}

			string result = "";
			if (modelType == NaturalLanguageProcessingSampleModels.sentence_transformer_japanese || modelType == NaturalLanguageProcessingSampleModels.multilingual_e5){
				if (chunk_cnt < chunk_text.Length){
					long start_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
					chunk_embedding.Add(textEmbedding.Embedding(chunk_text[chunk_cnt], ailiaModel, ailiaTokenizer));
					result = "Embedding : "+chunk_text[chunk_cnt]+"\n";
					chunk_cnt++;
					long end_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
					if (label_text != null)
					{
						label_text.text = result+(end_time - start_time).ToString() + "ms\n" + ailiaModel.EnvironmentName();
					}
				}
			}
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

		public void Submit(){
			if (!modelPrepared)
			{
				return;
			}

			string query_text = input_field.text;
			string result = "";


			        AiliaLLMChatMessage message = new AiliaLLMChatMessage();
        message.role = "user";
        message.content = inputFiled.text;
        messages.Add(message);
        inputFiled.text = "";

        llm.SetPrompt(messages);
        bool done = false;
        string text = "";
        while (true){
            llm.Generate(ref done);
            if (done == true){
                break;
            }
            string deltaText = llm.GetDeltaText();
            text = text + deltaText;
            textField.text = text;
        }
        textField.text = text;

        message = new AiliaLLMChatMessage();
        message.role = "assistant";
        message.content = text;
        messages.Add(message);


			if (modelType == NaturalLanguageProcessingSampleModels.fugumt_en_ja || modelType == NaturalLanguageProcessingSampleModels.fugumt_ja_en){
				result = ailia_speech_translate.Translate(query_text);
				label_text.text = result;
			}

			if (modelType == NaturalLanguageProcessingSampleModels.sentence_transformer_japanese || modelType == NaturalLanguageProcessingSampleModels.multilingual_e5){
				long start_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
				if (chunk_cnt >= chunk_text.Length){
					float [] query_embedding = textEmbedding.Embedding(query_text, ailiaModel, ailiaTokenizer);
					float max_sim = 0.0f;
					for (int i = 0; i < chunk_cnt; i++){
						float sim = textEmbedding.CosSimilarity(query_embedding, chunk_embedding[i]);
						Debug.Log(""+ chunk_text[i]+"/"+sim);
						if (sim > max_sim){
							max_sim = sim;
							result = chunk_text[i];
						}
					}
					result = "Query : "+query_text+"\nResult : "+result+" ("+max_sim+")\n";
				}
				long end_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
				if (label_text != null)
				{
					label_text.text = result+(end_time - start_time).ToString() + "ms\n" + ailiaModel.EnvironmentName();
				}
			}
		}
	}
}