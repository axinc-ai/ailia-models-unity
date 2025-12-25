/* AILIA Unity Plugin Speech To Text Sample */
/* Copyright 2023 - 2025 AXELL CORPORATION */

using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;

using UnityEngine;
using UnityEngine.UI;

using ailia;
using ailiaAudio;
using ailiaSpeech;

namespace ailiaSDK {
	public class AiliaSpeechToTextSample : AiliaRenderer {
		//Models
		public enum AiliaSpeechToTextModels
		{
			whisper_tiny,
			whisper_small,
			whisper_medium,
			whisper_turbo,
			sensevoice_small
		}

		[SerializeField]
		private AiliaSpeechToTextModels ailiaModelType = AiliaSpeechToTextModels.whisper_small;
		[SerializeField]
		private GameObject UICanvas = null;

		//Settings
		[SerializeField]
		private bool gpu_mode = false;
		[SerializeField]
		private bool mic_mode = false;
		[SerializeField]
		private bool whisper_live_transcribe = true;
		[SerializeField]
		private bool isRecording = true;

		//Input Audio Clip
		public AudioClip audio_clip = null;

		//Result
		private RawImage raw_image = null;
		private Text label_text = null;
		private Text mode_text = null;

		//AILIA
		private AiliaMicrophone ailia_mic = new AiliaMicrophone();
		private AiliaSpeechModel ailia_speech = new AiliaSpeechModel();
		private AiliaDisplayAudio ailia_display_audio = new AiliaDisplayAudio();

		//AILIA open file
		private AiliaDownload ailia_download = new AiliaDownload();
		private bool FileOpened = false;
	
		//Whisper
		string content_text = "";

		enum Mode
		{
			START_WAIT,
			RECORDING,
			TRANSCRIBING,
			COMPLETE,
		}
		private Mode mode = Mode.START_WAIT;

		private void CreateAiliaNetwork(AiliaSpeechToTextModels modelType)
		{
			string asset_path = Application.temporaryCachePath;
			var urlList = new List<ModelDownloadURL>();
			switch (modelType)
			{
				case AiliaSpeechToTextModels.whisper_tiny:
				case AiliaSpeechToTextModels.whisper_small:
				case AiliaSpeechToTextModels.whisper_medium:
				case AiliaSpeechToTextModels.whisper_turbo:
				case AiliaSpeechToTextModels.sensevoice_small:
					mode_text.text = "whisper";

					string encoder_path = "";
					string decoder_path = "";
					string pb_path = "";
					string vad_path = "silero_vad.onnx"; // v4

					int task = AiliaSpeech.AILIA_SPEECH_TASK_TRANSCRIBE; //AiliaSpeech.AILIA_SPEECH_TASK_TRANSLATE;
					int flag = AiliaSpeech.AILIA_SPEECH_FLAG_NONE;
					if (whisper_live_transcribe){
						flag = AiliaSpeech.AILIA_SPEECH_FLAG_LIVE;
					}
					int memory_mode = Ailia.AILIA_MEMORY_REDUCE_CONSTANT | Ailia.AILIA_MEMORY_REDUCE_CONSTANT_WITH_INPUT_INITIALIZER | Ailia.AILIA_MEMORY_REUSE_INTERSTAGE;
					int env_id = ailia_speech.GetEnvironmentId(gpu_mode);
					int api_model_type = 0;
					string remote_path = "";
					if (ailiaModelType == AiliaSpeechToTextModels.whisper_tiny){
						api_model_type = AiliaSpeech.AILIA_SPEECH_MODEL_TYPE_WHISPER_MULTILINGUAL_TINY;
						encoder_path = "encoder_tiny.opt3.onnx";
						decoder_path = "decoder_tiny_fix_kv_cache.opt3.onnx";
						remote_path = "whisper";
					}
					if (ailiaModelType == AiliaSpeechToTextModels.whisper_small){
						api_model_type = AiliaSpeech.AILIA_SPEECH_MODEL_TYPE_WHISPER_MULTILINGUAL_SMALL;
						encoder_path = "encoder_small.opt3.onnx";
						decoder_path = "decoder_small_fix_kv_cache.opt3.onnx";
						remote_path = "whisper";
					}
					if (ailiaModelType == AiliaSpeechToTextModels.whisper_medium){
						api_model_type = AiliaSpeech.AILIA_SPEECH_MODEL_TYPE_WHISPER_MULTILINGUAL_MEDIUM;
						encoder_path = "encoder_medium.opt3.onnx";
						decoder_path = "decoder_medium_fix_kv_cache.opt3.onnx";
						remote_path = "whisper";
					}
					if (ailiaModelType == AiliaSpeechToTextModels.whisper_turbo){
						api_model_type = AiliaSpeech.AILIA_SPEECH_MODEL_TYPE_WHISPER_MULTILINGUAL_LARGE_V3;
						encoder_path = "encoder_turbo.onnx";
						pb_path = "encoder_turbo_weights.pb";
						decoder_path = "decoder_turbo_fix_kv_cache.onnx";
						remote_path = "whisper";
					}
					if (ailiaModelType == AiliaSpeechToTextModels.sensevoice_small){
						api_model_type = AiliaSpeech.AILIA_SPEECH_MODEL_TYPE_SENSEVOICE_SMALL;
						encoder_path = "sensevoice_small.onnx";
						decoder_path = "sensevoice_small.model";
						remote_path = "sensevoice";
						vad_path = "silero_vad_v6_2.onnx";
					}
					urlList.Add(new ModelDownloadURL() { folder_path = "silero-vad", file_name = vad_path });
					urlList.Add(new ModelDownloadURL() { folder_path = remote_path, file_name = encoder_path });
					urlList.Add(new ModelDownloadURL() { folder_path = remote_path, file_name = decoder_path });
					if (pb_path != ""){
						urlList.Add(new ModelDownloadURL() { folder_path = remote_path, file_name = pb_path });
					}
					bool virtual_memory_enable = false;
					string language = "auto"; // ja
					StartCoroutine(ailia_download.DownloadWithProgressFromURL(urlList, () =>
					{
						FileOpened = ailia_speech.Open(asset_path + "/" + encoder_path, asset_path + "/" + decoder_path, env_id, memory_mode, api_model_type, task, flag, language);
						if (FileOpened) {
							FileOpened = ailia_speech.OpenVad(asset_path + "/" + vad_path, AiliaSpeech.AILIA_SPEECH_VAD_TYPE_SILERO);
						}
					}));

					break;
				default:
					Debug.Log("Others ailia models are working in progress.");
					break;
			}
		}

		private void DestroyAiliaNetwork()
		{
			ailia_speech.Close();
		}		

		// Use this for initialization
		void Start()
		{
			AiliaLicense.CheckAndDownloadLicense();
			ailia_mic.InitializeMic(mic_mode, audio_clip);
			SetUIProperties();
			CreateAiliaNetwork(ailiaModelType);
		}

		// Update is called once per frame
		void Update()
		{
			if (!FileOpened)
			{
				return;
			}

			//Clear result
			Clear();

			//Get camera image
			raw_image.texture = ailia_display_audio.Create();

			
			// Get Mic Input
			float[] waveData = null;
			uint channels = 1;
			uint frequency = 1;
			waveData = ailia_mic.GetPcm(ref channels, ref frequency);

			WhisperUpdate(waveData, channels, frequency);
		}

		void WhisperUpdate(float[] waveData, uint channels, uint frequency){
			// Preview
			ailia_display_audio.DisplayPcm(waveData, null, channels, isRecording);

			// Error handle
			if (ailia_speech.IsError()){
				return;
			}

			// Get result
			WhisperDisplayIntermediateResult();
			WhisperGetResult();

			// Transcribe request
			if (waveData.Length > 0){
				switch (mode)
				{
					case Mode.START_WAIT:
						if (isRecording) mode = Mode.RECORDING;
						break;
					case Mode.RECORDING:
						ailia_speech.Transcribe(waveData, frequency, channels, false);
						if (!isRecording)
						{
							mode = Mode.TRANSCRIBING;
						}
						break;
					case Mode.TRANSCRIBING:
						ailia_speech.Transcribe(waveData, frequency, channels, true);
						mode = Mode.COMPLETE;
						break;
					case Mode.COMPLETE:
						if (ailia_speech.IsCompleted())
						{
							ailia_speech.ResetTranscribeState();
							mode = Mode.START_WAIT;
						}
						break;
				}
			}
		}

		private void WhisperDisplayIntermediateResult(){
			string intermediateText = ailia_speech.GetIntermediateText();
			if (content_text != "" || intermediateText != ""){
				if (intermediateText != ""){
					label_text.text = "[processing] " + intermediateText + "\n" + content_text;
				}else{
					label_text.text = content_text;
				}
			} else{
				if (ailia_speech.IsProcessing()){
					label_text.text = "[processing]";
				}
			}
		}

		private void WhisperGetResult(){
			List<string> results = ailia_speech.GetResults();
			for (uint idx = 0; idx < results.Count; idx++){
				string text = results[(int)idx];
				string display_text = text + "\n";
				content_text = display_text + content_text;
			}
		}

		void SetUIProperties()
		{
			if (UICanvas == null) return;
			// Set up UI for AiliaDownloader
			var downloaderProgressPanel = UICanvas.transform.Find("DownloaderProgressPanel");
			ailia_download.DownloaderProgressPanel = downloaderProgressPanel.gameObject;
			// Set up lines
			line_panel = UICanvas.transform.Find("LinePanel").gameObject;
			lines = UICanvas.transform.Find("LinePanel/Lines").gameObject;
			line = UICanvas.transform.Find("LinePanel/Lines/Line").gameObject;
			text_panel = UICanvas.transform.Find("TextPanel").gameObject;
			text_base = UICanvas.transform.Find("TextPanel/TextHolder").gameObject;

			raw_image = UICanvas.transform.Find("RawImage").gameObject.GetComponent<RawImage>();
			label_text = UICanvas.transform.Find("LabelText").gameObject.GetComponent<Text>();
			mode_text = UICanvas.transform.Find("ModeLabel").gameObject.GetComponent<Text>();
		}

		void OnApplicationQuit()
		{
			ailia_mic.DestroyMic();
			DestroyAiliaNetwork();
		}

		void OnDestroy()
		{
			ailia_mic.DestroyMic();
			DestroyAiliaNetwork();
		}
	}
}

