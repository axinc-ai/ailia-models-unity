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
			sentence_transformer_japanese
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

		void Start()
		{
			UISetup();

			// for Processing
			AiliaInit();
		}

		void AiliaInit()
		{
			// Create Ailia
			ailiaModel = CreateAiliaNet(modelType, gpu_mode);
		}

		void UISetup()
		{
			Debug.Assert (UICanvas != null, "UICanvas is null");

			label_text = UICanvas.transform.Find("LabelText").GetComponent<Text>();
			mode_text = UICanvas.transform.Find("ModeLabel").GetComponent<Text>();

			mode_text.text = "ailia Image Segmentation\nSpace key down to original image";
		}

		void Update()
		{
			if (!modelPrepared)
			{
				return;
			}

			long start_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
			float [] embedding = textEmbedding.Embedding("こんにちは。", ailiaModel, ailiaTokenizer);
			long end_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

			if (label_text != null)
			{
				label_text.text = (end_time - start_time).ToString() + "ms\n" + ailiaModel.EnvironmentName();
			}

			oneshot = false;
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
			urlList.Add(new ModelDownloadURL() { folder_path = "sentence-transformers-japanese", file_name = "paraphrase-multilingual-mpnet-base-v2.onnx.prototxt" });
			urlList.Add(new ModelDownloadURL() { folder_path = "sentence-transformers-japanese", file_name = "paraphrase-multilingual-mpnet-base-v2.onnx" });
			urlList.Add(new ModelDownloadURL() { folder_path = "sentence-transformers-japanese", file_name = "sentencepiece.bpe.model" });

			StartCoroutine(ailia_download.DownloadWithProgressFromURL(urlList, () =>
			{
				modelPrepared = ailiaModel.OpenFile(asset_path + "/" + "paraphrase-multilingual-mpnet-base-v2.onnx.prototxt", asset_path + "/" + "paraphrase-multilingual-mpnet-base-v2.onnx");
				if (modelPrepared){
					modelPrepared = ailiaTokenizer.Open(asset_path + "/sentencepiece.bpe.model");
					if (modelPrepared){
						modelPrepared = ailiaTokenizer.Create(AiliaTokenizer.AILIA_TOKENIZER_TYPE_XLM_ROBERTA, AiliaTokenizer.AILIA_TOKENIZER_FLAG_NONE);
					}
				}
			}));

			return ailiaModel;
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