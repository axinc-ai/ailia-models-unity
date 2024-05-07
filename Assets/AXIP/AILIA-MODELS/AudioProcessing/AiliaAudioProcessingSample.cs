/* AILIA Unity Plugin Audio Processing Sample */
/* Copyright 2023 AXELL CORPORATION */

using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Runtime.InteropServices;

using UnityEngine;
using UnityEngine.UI;

using ailia;
using ailiaAudio;
using ailiaSpeech;

namespace ailiaSDK {
	public class AiliaAudioProcessingSample : AiliaRenderer {
		//Models
		public enum AudioProcessingModels
		{
			silero_vad,
			rvc,
			rvc_with_f0,
			whisper_tiny,
			whisper_small,
			whisper_medium,
		}

		[SerializeField]
		private AudioProcessingModels ailiaModelType = AudioProcessingModels.silero_vad;
		[SerializeField]
		private GameObject UICanvas = null;

		//Settings
		[SerializeField]
		private bool gpu_mode = false;
		[SerializeField]
		private bool mic_mode = false;
		[SerializeField]
		private bool rvc_f0_gpu_mode = false;
		[SerializeField]
		private bool rvc_async_mode = false;	// Async Processing
		[SerializeField]
		private bool whisper_live_transcribe = true;

		//Input Audio Clip
		public AudioClip audio_clip = null;

		//Output VAD Audio Clip
		private List<AudioClip> vad_audio_clip = new List<AudioClip>();
		private List<AudioClip> vad_audio_clip_play_list = new List<AudioClip>();

		//Output Audio Source
		public AudioSource audio_source = null;

		//Result
		private RawImage raw_image = null;
		private Text label_text = null;
		private Text mode_text = null;

		//AILIA
		private AiliaSileroVad ailia_vad = new AiliaSileroVad();
		private AiliaRvc ailia_rvc = new AiliaRvc();
		private AiliaMicrophone ailia_mic = new AiliaMicrophone();
		private AiliaSplitAudio ailia_split = new AiliaSplitAudio();
		private AiliaSpeechModel ailia_speech = new AiliaSpeechModel();
		private AiliaDisplayAudio ailia_display_audio = new AiliaDisplayAudio();

		//AILIA open file
		private AiliaDownload ailia_download = new AiliaDownload();
		private bool FileOpened = false;
	
		//RVC
		private int rvc_version = 1;
		private bool crepe_tiny = true;
		private long rvc_time = 0;
		private long f0_time = 0;

		//Whisper
		string content_text = "";

		private void CreateAiliaNetwork(AudioProcessingModels modelType)
		{
			string asset_path = Application.temporaryCachePath;
			var urlList = new List<ModelDownloadURL>();
			switch (modelType)
			{
				case AudioProcessingModels.silero_vad:
					mode_text.text = "silero_vad";
	
					urlList.Add(new ModelDownloadURL() { folder_path = "silero-vad", file_name = "silero_vad.onnx.prototxt" });
					urlList.Add(new ModelDownloadURL() { folder_path = "silero-vad", file_name = "silero_vad.onnx" });

					StartCoroutine(ailia_download.DownloadWithProgressFromURL(urlList, () =>
					{
						FileOpened = ailia_vad.OpenFile(asset_path + "/silero_vad.onnx.prototxt", asset_path + "/silero_vad.onnx", gpu_mode);
					}));
					break;
				case AudioProcessingModels.rvc:
					mode_text.text = "silero_vad + rvc";

					urlList.Add(new ModelDownloadURL() { folder_path = "silero-vad", file_name = "silero_vad.onnx.prototxt" });
					urlList.Add(new ModelDownloadURL() { folder_path = "silero-vad", file_name = "silero_vad.onnx" });
					urlList.Add(new ModelDownloadURL() { folder_path = "rvc", file_name = "hubert_base.onnx.prototxt" });
					urlList.Add(new ModelDownloadURL() { folder_path = "rvc", file_name = "hubert_base.onnx" });
					urlList.Add(new ModelDownloadURL() { folder_path = "rvc", file_name = "AISO-HOWATTO.onnx.prototxt" });
					urlList.Add(new ModelDownloadURL() { folder_path = "rvc", file_name = "AISO-HOWATTO.onnx" });

					StartCoroutine(ailia_download.DownloadWithProgressFromURL(urlList, () =>
					{
						FileOpened = ailia_vad.OpenFile(asset_path + "/silero_vad.onnx.prototxt", asset_path + "/silero_vad.onnx", gpu_mode);
						if (FileOpened == true){
							FileOpened = ailia_rvc.OpenFile(asset_path + "/hubert_base.onnx.prototxt", asset_path + "/hubert_base.onnx", asset_path + "/AISO-HOWATTO.onnx.prototxt", asset_path + "/AISO-HOWATTO.onnx", rvc_version, gpu_mode);
						}
					}));
					break;
				case AudioProcessingModels.rvc_with_f0:
					mode_text.text = "silero_vad + rvc + f0";

					urlList.Add(new ModelDownloadURL() { folder_path = "silero-vad", file_name = "silero_vad.onnx.prototxt" });
					urlList.Add(new ModelDownloadURL() { folder_path = "silero-vad", file_name = "silero_vad.onnx" });
					urlList.Add(new ModelDownloadURL() { folder_path = "rvc", file_name = "hubert_base.onnx.prototxt" });
					urlList.Add(new ModelDownloadURL() { folder_path = "rvc", file_name = "hubert_base.onnx" });
					urlList.Add(new ModelDownloadURL() { folder_path = "rvc", file_name = "AISO-HOWATTO.onnx.prototxt" });
					urlList.Add(new ModelDownloadURL() { folder_path = "rvc", file_name = "AISO-HOWATTO.onnx" });
					if (crepe_tiny){
						urlList.Add(new ModelDownloadURL() { folder_path = "rvc", file_name = "crepe_tiny.onnx.prototxt" });
						urlList.Add(new ModelDownloadURL() { folder_path = "rvc", file_name = "crepe_tiny.onnx" });
					}else{
						urlList.Add(new ModelDownloadURL() { folder_path = "rvc", file_name = "crepe.onnx.prototxt" });
						urlList.Add(new ModelDownloadURL() { folder_path = "rvc", file_name = "crepe.onnx" });
					}

					StartCoroutine(ailia_download.DownloadWithProgressFromURL(urlList, () =>
					{
						FileOpened = ailia_vad.OpenFile(asset_path + "/silero_vad.onnx.prototxt", asset_path + "/silero_vad.onnx", gpu_mode);
						if (FileOpened == true){
							FileOpened = ailia_rvc.OpenFile(asset_path + "/hubert_base.onnx.prototxt", asset_path + "/hubert_base.onnx", Application.streamingAssetsPath + "/rvc_f0.onnx.prototxt", Application.streamingAssetsPath + "/rvc_f0.onnx", rvc_version, gpu_mode);
							if (FileOpened == true){
								if (crepe_tiny){
									FileOpened = ailia_rvc.OpenFileF0(asset_path + "/crepe_tiny.onnx.prototxt", asset_path + "/crepe_tiny.onnx", rvc_f0_gpu_mode);
								}else{
									FileOpened = ailia_rvc.OpenFileF0(asset_path + "/crepe.onnx.prototxt", asset_path + "/crepe.onnx", rvc_f0_gpu_mode);
								}
							}else{
								Debug.LogError("Please put rvc_f0.onnx and rvc_f0.onnx.prototxt to streaming assets path.");
							}
							ailia_rvc.SetF0UpKeys(11);
							ailia_rvc.SetTargetSmaplingRate(48000);
						}
					}));
					break;
				case AudioProcessingModels.whisper_tiny:
				case AudioProcessingModels.whisper_small:
				case AudioProcessingModels.whisper_medium:
					mode_text.text = "whisper";

					string encoder_path = "";
					string decoder_path = "";
					string vad_path = "silero_vad.onnx";

					int task = AiliaSpeech.AILIA_SPEECH_TASK_TRANSCRIBE; //AiliaSpeech.AILIA_SPEECH_TASK_TRANSLATE;
					int flag = AiliaSpeech.AILIA_SPEECH_FLAG_NONE;
					if (whisper_live_transcribe){
						flag = AiliaSpeech.AILIA_SPEECH_FLAG_LIVE;
					}
					int memory_mode = Ailia.AILIA_MEMORY_REDUCE_CONSTANT | Ailia.AILIA_MEMORY_REDUCE_CONSTANT_WITH_INPUT_INITIALIZER | Ailia.AILIA_MEMORY_REUSE_INTERSTAGE;
					int env_id = GetEnvId(gpu_mode);
					int api_model_type = 0;
					if (ailiaModelType == AudioProcessingModels.whisper_tiny){
						api_model_type = AiliaSpeech.AILIA_SPEECH_MODEL_TYPE_WHISPER_MULTILINGUAL_TINY;
						encoder_path = "encoder_tiny.opt3.onnx";
						decoder_path = "decoder_tiny_fix_kv_cache.opt3.onnx";
					}
					if (ailiaModelType == AudioProcessingModels.whisper_small){
						api_model_type = AiliaSpeech.AILIA_SPEECH_MODEL_TYPE_WHISPER_MULTILINGUAL_SMALL;
						encoder_path = "encoder_small.opt3.onnx";
						decoder_path = "decoder_small_fix_kv_cache.opt3.onnx";
					}
					if (ailiaModelType == AudioProcessingModels.whisper_medium){
						api_model_type = AiliaSpeech.AILIA_SPEECH_MODEL_TYPE_WHISPER_MULTILINGUAL_MEDIUM;
						encoder_path = "encoder_medium.opt3.onnx";
						decoder_path = "decoder_medium_fix_kv_cache.opt3.onnx";
					}
					urlList.Add(new ModelDownloadURL() { folder_path = "silero-vad", file_name = vad_path });
					urlList.Add(new ModelDownloadURL() { folder_path = "whisper", file_name = encoder_path });
					urlList.Add(new ModelDownloadURL() { folder_path = "whisper", file_name = decoder_path });
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

		private int GetEnvId(bool gpu_mode){
			string env_name = "auto";
			int env_id = Ailia.AILIA_ENVIRONMENT_ID_AUTO;
			if (gpu_mode) {
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
		
		private void DestroyAiliaNetwork()
		{
			ailia_vad.Close();
			ailia_rvc.Close();
			ailia_speech.Close();
		}		

		// Use this for initialization
		void Start()
		{
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

			if (ailiaModelType == AudioProcessingModels.whisper_tiny || ailiaModelType == AudioProcessingModels.whisper_small || ailiaModelType == AudioProcessingModels.whisper_medium){
				WhisperUpdate(waveData, channels, frequency);
			} else {
				VadAndRvcUpdate(waveData, channels, frequency);
			}
		}

		void WhisperUpdate(float[] waveData, uint channels, uint frequency){
			// Preview
			ailia_display_audio.DisplayPcm(waveData, null, channels);

			// Error handle
			if (ailia_speech.IsError()){
				return;
			}

			// Get result
			WhisperDisplayIntermediateResult();
			WhisperGetResult();

			// Transcribe request
			bool complete = ailia_mic.IsComplete();
			ailia_speech.Transcribe(waveData, frequency, channels, complete);
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

		void VadAndRvcUpdate(float[] waveData, uint channels, uint frequency){
			// VAD
			long start_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
			AiliaSileroVad.VadResult vad_result = ailia_vad.VAD(waveData, (int)channels, (int)frequency);
			long end_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
			if (label_text != null)
			{
				if (ailiaModelType == AudioProcessingModels.rvc){
					label_text.text = "vad time : " + (end_time - start_time) + "ms\nrvc time : " + rvc_time + "ms\n" + ailia_rvc.EnvironmentName();
				}else{
					if (ailiaModelType == AudioProcessingModels.rvc_with_f0){
						label_text.text = "vad time : " + (end_time - start_time) + "ms\nrvc time : " + rvc_time + "ms\nf0 time : " + f0_time + "ms\n" + ailia_rvc.EnvironmentName();
					}else{
						label_text.text = "vad time : " + (end_time - start_time) + "ms\n" + ailia_vad.EnvironmentName();
					}
				}
				if (rvc_async_mode && ailia_rvc.AsyncProcessing()){
					label_text.text += "\nrvc async processing\n";
				}
			}

			// Split
			ailia_split.Split(vad_result);
			if (rvc_async_mode){
				RvcGetResultAsync();
				if (ailia_split.GetAudioClipCount() > 0){
					if (!ailia_rvc.AsyncProcessing()){
						AudioClip clip = ailia_split.PopAudioClip();
						RvcPushSplitAudioAsync(clip);
					}
				}
			}else{
				if (ailia_split.GetAudioClipCount() > 0){
					AudioClip clip = ailia_split.PopAudioClip();
					RvcPushSplitAudio(clip);
				}
			}

			// Play
			if (!audio_source.isPlaying && vad_audio_clip_play_list.Count > 0){
				AudioClip clip = vad_audio_clip_play_list[0];
				vad_audio_clip_play_list.RemoveAt(0);
				audio_source.PlayOneShot(clip);
			}
			
			// Preview
			ailia_display_audio.DisplayVad(vad_result, vad_audio_clip, channels);
		}

		private void RvcPushSplitAudio(AudioClip clip)
		{
			if (ailiaModelType == AudioProcessingModels.rvc || ailiaModelType == AudioProcessingModels.rvc_with_f0){
				clip = ailia_rvc.Process(clip);
				rvc_time = ailia_rvc.GetProfile();
			}
			vad_audio_clip.Add(clip);
			vad_audio_clip_play_list.Add(clip);
		}

		private void RvcPushSplitAudioAsync(AudioClip clip)
		{
			if (ailiaModelType == AudioProcessingModels.rvc || ailiaModelType == AudioProcessingModels.rvc_with_f0){
				ailia_rvc.AsyncProcess(clip);
			}else{
				vad_audio_clip.Add(clip);
				vad_audio_clip_play_list.Add(clip);
			}
		}

		private void RvcGetResultAsync(){
			if (ailia_rvc.AsyncResultExist()){
				AudioClip clip = ailia_rvc.AsyncGetResult();
				vad_audio_clip.Add(clip);
				vad_audio_clip_play_list.Add(clip);
				rvc_time = ailia_rvc.GetProfile();
				f0_time = ailia_rvc.GetProfileF0();
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

