/* AILIA Unity Plugin Natural Language Processing Sample */
/* Copyright 2023 AXELL CORPORATION */

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ailiaSDK
{
	public class AiliaNaturalLanguageProcessingSample : MonoBehaviour
	{
		// Model list
		public enum NaturalLanguageProcessingSampleModels
		{
			sentence_transformer_japanese,
			multilingual_e5
		}

		// Settings
		public NaturalLanguageProcessingSampleModels modelType = NaturalLanguageProcessingSampleModels.sentence_transformer_japanese;
		public bool gpu_mode = false;
		public GameObject UICanvas = null;

		// Result
		Text label_text = null;
		Text mode_text = null;
		private bool oneshot = true;

		// AILIA
		private AiliaModel ailiaModel = null;
		private AiliaTokenizerModel ailiaTokenizer = null;
		private AiliaNaturalLanguageProcessingTextEmbedding textEmbedding = new AiliaNaturalLanguageProcessingTextEmbedding();

		bool modelPrepared = false;
		bool modelAllocated = false;

		// Chunk
		public TextAsset database = null;
		private string[] chunk_text = null;
		private List<float[]> chunk_embedding;
		private int chunk_cnt = 0;

		void Start()
		{
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

			if (modelType == NaturalLanguageProcessingSampleModels.sentence_transformer_japanese){
				urlList.Add(new ModelDownloadURL() { folder_path = "sentence-transformers-japanese", file_name = "paraphrase-multilingual-mpnet-base-v2.onnx.prototxt" });
				urlList.Add(new ModelDownloadURL() { folder_path = "sentence-transformers-japanese", file_name = "paraphrase-multilingual-mpnet-base-v2.onnx" });
				urlList.Add(new ModelDownloadURL() { folder_path = "sentence-transformers-japanese", file_name = "sentencepiece.bpe.model" });
			}
			if (modelType == NaturalLanguageProcessingSampleModels.multilingual_e5){
				urlList.Add(new ModelDownloadURL() { folder_path = "multilingual-e5", file_name = "multilingual-e5-base.onnx.prototxt" });
				urlList.Add(new ModelDownloadURL() { folder_path = "multilingual-e5", file_name = "multilingual-e5-base.onnx.onnx" });
				urlList.Add(new ModelDownloadURL() { folder_path = "multilingual-e5", file_name = "sentencepiece.bpe.model" });
			}

			StartCoroutine(ailia_download.DownloadWithProgressFromURL(urlList, () =>
			{
				if (modelType == NaturalLanguageProcessingSampleModels.sentence_transformer_japanese){
					modelPrepared = ailiaModel.OpenFile(asset_path + "/" + "paraphrase-multilingual-mpnet-base-v2.onnx.prototxt", asset_path + "/" + "paraphrase-multilingual-mpnet-base-v2.onnx");
				}
				if (modelType == NaturalLanguageProcessingSampleModels.multilingual_e5){
					modelPrepared = ailiaModel.OpenFile(asset_path + "/" + "multilingual-e5-base.onnx.prototxt", asset_path + "/" + "multilingual-e5-base.onnx");
				}
				if (modelPrepared == false){
					Debug.Log("ailiaModel.OpenFile failed");
				}
				if (modelPrepared){
					modelPrepared = ailiaTokenizer.Create(AiliaTokenizer.AILIA_TOKENIZER_TYPE_XLM_ROBERTA, AiliaTokenizer.AILIA_TOKENIZER_FLAG_NONE);
					if (modelPrepared == false){
						Debug.Log("ailiaTokenizer.Create failed");
					}
					if (modelPrepared){
						modelPrepared = ailiaTokenizer.Open(asset_path + "/sentencepiece.bpe.model");
						if (modelPrepared == false){
							Debug.Log("ailiaTokenizer.Open failed");
						}
					}
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

			string query_text = "ailia SDKとは何ですか。";
			string result = "";

			long start_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
			if (chunk_cnt < chunk_text.Length){
				chunk_embedding.Add(textEmbedding.Embedding(chunk_text[chunk_cnt], ailiaModel, ailiaTokenizer));
				result = "Embedding : "+chunk_text[chunk_cnt]+"\n";
				chunk_cnt++;
			}else{
				float [] query_embedding = textEmbedding.Embedding(query_text, ailiaModel, ailiaTokenizer);
				float max_sim = 0.0f;
				for (int i = 0; i < chunk_cnt; i++){
					float sim = textEmbedding.CosSim(query_embedding, chunk_embedding[i]);
					Debug.Log(""+ chunk_text[i]+"/"+sim);
					if (sim > max_sim){
						max_sim = sim;
						result = chunk_text[i];
					}
				}
				result = "Query : "+query_text+"\nTarget : "+result+" ("+max_sim+")\n";
			}
			long end_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

			if (label_text != null)
			{
				label_text.text = result+(end_time - start_time).ToString() + "ms\n" + ailiaModel.EnvironmentName();
			}

			oneshot = false;
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
			if (ailiaModel != null){
				ailiaModel.Close();
			}
			if (ailiaTokenizer != null){
				ailiaTokenizer.Close();
			}
		}


	}
}