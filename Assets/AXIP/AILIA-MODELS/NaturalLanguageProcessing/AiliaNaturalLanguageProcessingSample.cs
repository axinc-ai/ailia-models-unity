/* AILIA Unity Plugin Natural Language Processing Sample */
/* Copyright 2023 - 2024 AXELL CORPORATION */

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Runtime.InteropServices;

using ailia;
using ailiaTokenizer;
using ailiaSpeech;

namespace ailiaSDK
{
	public class AiliaNaturalLanguageProcessingSample : MonoBehaviour
	{
		// Model list
		public enum NaturalLanguageProcessingSampleModels
		{
			sentence_transformer_japanese,
			multilingual_e5,
			fugumt_en_ja,
			fugumt_ja_en
		}

		// UI
		[SerializeField]
		public InputField input_field;

		// Settings
		public NaturalLanguageProcessingSampleModels modelType = NaturalLanguageProcessingSampleModels.sentence_transformer_japanese;
		public bool gpu_mode = false;
		public GameObject UICanvas = null;

		// Result
		Text label_text = null;
		Text mode_text = null;

		// AILIA
		private AiliaModel ailiaModel = null;
		private AiliaTokenizerModel ailiaTokenizer = null;
		private AiliaNaturalLanguageProcessingTextEmbedding textEmbedding = new AiliaNaturalLanguageProcessingTextEmbedding();
		private AiliaSpeechTranslateModel ailia_speech_translate = new AiliaSpeechTranslateModel();

		bool modelPrepared = false;
		bool modelAllocated = false;

		// Chunk
		public TextAsset database = null;
		private string[] chunk_text = null;
		private List<float[]> chunk_embedding;
		private int chunk_cnt = 0;
		private string env_name = "";

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

			if (modelType == NaturalLanguageProcessingSampleModels.sentence_transformer_japanese){
				urlList.Add(new ModelDownloadURL() { folder_path = "sentence-transformers-japanese", file_name = "paraphrase-multilingual-mpnet-base-v2.onnx.prototxt" });
				urlList.Add(new ModelDownloadURL() { folder_path = "sentence-transformers-japanese", file_name = "paraphrase-multilingual-mpnet-base-v2.onnx" });
				urlList.Add(new ModelDownloadURL() { folder_path = "sentence-transformers-japanese", file_name = "sentencepiece.bpe.model" });
			}
			if (modelType == NaturalLanguageProcessingSampleModels.multilingual_e5){
				urlList.Add(new ModelDownloadURL() { folder_path = "multilingual-e5", file_name = "multilingual-e5-base.onnx.prototxt" });
				urlList.Add(new ModelDownloadURL() { folder_path = "multilingual-e5", file_name = "multilingual-e5-base.onnx" });
				urlList.Add(new ModelDownloadURL() { folder_path = "multilingual-e5", file_name = "sentencepiece.bpe.model" });
			}
			if (modelType == NaturalLanguageProcessingSampleModels.fugumt_en_ja){
				urlList.Add(new ModelDownloadURL() { folder_path = "fugumt-en-ja", file_name = "seq2seq-lm-with-past.onnx", local_name = "fugumt_en_ja_seq2seq-lm-with-past.onnx" });
				urlList.Add(new ModelDownloadURL() { folder_path = "fugumt-en-ja", file_name = "source.spm", local_name = "fugumt_en_ja_source.spm" });
				urlList.Add(new ModelDownloadURL() { folder_path = "fugumt-en-ja", file_name = "target.spm", local_name = "fugumt_en_ja_target.spm" });
			}
			if (modelType == NaturalLanguageProcessingSampleModels.fugumt_ja_en){
				urlList.Add(new ModelDownloadURL() { folder_path = "fugumt-ja-en", file_name = "encoder_model.onnx", local_name = "fugumt_ja_en_encoder_model.onnx" });
				urlList.Add(new ModelDownloadURL() { folder_path = "fugumt-ja-en", file_name = "decoder_model.onnx", local_name = "fugumt_ja_en_decoder_model.onnx" });
				urlList.Add(new ModelDownloadURL() { folder_path = "fugumt-ja-en", file_name = "source.spm", local_name = "fugumt_ja_en_source.spm"});
				urlList.Add(new ModelDownloadURL() { folder_path = "fugumt-ja-en", file_name = "target.spm", local_name = "fugumt_ja_en_target.spm"});
			}

			StartCoroutine(ailia_download.DownloadWithProgressFromURL(urlList, () =>
			{
				if (modelType == NaturalLanguageProcessingSampleModels.sentence_transformer_japanese){
					modelPrepared = ailiaModel.OpenFile(asset_path + "/" + "paraphrase-multilingual-mpnet-base-v2.onnx.prototxt", asset_path + "/" + "paraphrase-multilingual-mpnet-base-v2.onnx");
				}
				if (modelType == NaturalLanguageProcessingSampleModels.multilingual_e5){
					modelPrepared = ailiaModel.OpenFile(asset_path + "/" + "multilingual-e5-base.onnx.prototxt", asset_path + "/" + "multilingual-e5-base.onnx");
				}
				if (modelType == NaturalLanguageProcessingSampleModels.fugumt_en_ja || modelType == NaturalLanguageProcessingSampleModels.fugumt_ja_en){
					int env_id = GetEnvId(gpu_mode);
					int memory_mode = Ailia.AILIA_MEMORY_REDUCE_CONSTANT | Ailia.AILIA_MEMORY_REDUCE_CONSTANT_WITH_INPUT_INITIALIZER | Ailia.AILIA_MEMORY_REUSE_INTERSTAGE;
					bool status = false;
					if (modelType == NaturalLanguageProcessingSampleModels.fugumt_en_ja ){
						status = ailia_speech_translate.Open(asset_path + "/" +"fugumt_en_ja_seq2seq-lm-with-past.onnx", null, asset_path + "/" +"fugumt_en_ja_source.spm", asset_path + "/" +"fugumt_en_ja_target.spm", AiliaSpeech.AILIA_SPEECH_POST_PROCESS_TYPE_FUGUMT_EN_JA,  env_id, memory_mode);
					}
					if (modelType == NaturalLanguageProcessingSampleModels.fugumt_ja_en){
						status = ailia_speech_translate.Open(asset_path + "/" +"fugumt_ja_en_encoder_model.onnx", asset_path + "/" +"fugumt_ja_en_decoder_model.onnx", asset_path + "/" +"fugumt_ja_en_source.spm", asset_path + "/" +"fugumt_ja_en_target.spm", AiliaSpeech.AILIA_SPEECH_POST_PROCESS_TYPE_FUGUMT_JA_EN,  env_id, memory_mode);
					}
				}

				if (modelPrepared == false){
					Debug.Log("ailiaModel.OpenFile failed");
				}

				if (modelType == NaturalLanguageProcessingSampleModels.sentence_transformer_japanese || modelType == NaturalLanguageProcessingSampleModels.multilingual_e5){
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
				}
			}));

			return ailiaModel;
		}

		private int GetEnvId(bool gpu_mode){
			int env_id = Ailia.AILIA_ENVIRONMENT_ID_AUTO;
			if (gpu_mode) { // GPU
				int count = 0;
				Ailia.ailiaGetEnvironmentCount(ref count);
				for (int i = 0; i < count; i++){
					IntPtr env_ptr = IntPtr.Zero;
					Ailia.ailiaGetEnvironment(ref env_ptr, (uint)i, Ailia.AILIA_ENVIRONMENT_VERSION);
					Ailia.AILIAEnvironment env = (Ailia.AILIAEnvironment)Marshal.PtrToStructure(env_ptr, typeof(Ailia.AILIAEnvironment));

					if (env.backend == Ailia.AILIA_ENVIRONMENT_BACKEND_MPS || env.backend == Ailia.AILIA_ENVIRONMENT_BACKEND_CUDA || env.backend == Ailia.AILIA_ENVIRONMENT_BACKEND_VULKAN){
						env_id = env.id;
						env_name = Marshal.PtrToStringAnsi(env.name);
					}
				}
			} else {
				env_name = "cpu";
			}
			return env_id;
		}

		void Update()
		{
			if (!modelPrepared)
			{
				return;
			}

			string query_text = "NNAPIとは何ですか。";
			string result = "";

			if (modelType == NaturalLanguageProcessingSampleModels.fugumt_en_ja || modelType == NaturalLanguageProcessingSampleModels.fugumt_ja_en){
				result = ailia_speech_translate.Translate(query_text);
				label_text.text = result;
			}

			if (modelType == NaturalLanguageProcessingSampleModels.sentence_transformer_japanese || modelType == NaturalLanguageProcessingSampleModels.multilingual_e5){
				long start_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
				if (chunk_cnt < chunk_text.Length){
					chunk_embedding.Add(textEmbedding.Embedding(chunk_text[chunk_cnt], ailiaModel, ailiaTokenizer));
					result = "Embedding : "+chunk_text[chunk_cnt]+"\n";
					chunk_cnt++;
				}else{
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

		public void Submit(){

		}
	}
}