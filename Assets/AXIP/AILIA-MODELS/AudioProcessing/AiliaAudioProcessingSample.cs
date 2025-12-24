/* AILIA Unity Plugin Audio Processing Sample */
/* Copyright 2023 - 2024 AXELL CORPORATION */

using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;

using UnityEngine;
using UnityEngine.UI;

using ailia;
using ailiaAudio;

namespace ailiaSDK {
	public class AiliaAudioProcessingSample : AiliaRenderer {
		//Models
		public enum AudioProcessingModels
		{
			silero_vad_v4,
			silero_vad_v6_2,
			rvc,
			rvc_with_f0
		}

		[SerializeField]
		private AudioProcessingModels ailiaModelType = AudioProcessingModels.silero_vad_v4;
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
		private AiliaDisplayAudio ailia_display_audio = new AiliaDisplayAudio();

		//AILIA open file
		private AiliaDownload ailia_download = new AiliaDownload();
		private bool FileOpened = false;
	
		//RVC
		private int rvc_version = 1;
		private bool crepe_tiny = true;
		private long rvc_time = 0;
		private long f0_time = 0;

		private void CreateAiliaNetwork(AudioProcessingModels modelType)
		{
			string asset_path = Application.temporaryCachePath;
			var urlList = new List<ModelDownloadURL>();
			switch (modelType)
			{
				case AudioProcessingModels.silero_vad_v4:
					mode_text.text = "silero_vad_v4";
	
					urlList.Add(new ModelDownloadURL() { folder_path = "silero-vad", file_name = "silero_vad.onnx.prototxt" });
					urlList.Add(new ModelDownloadURL() { folder_path = "silero-vad", file_name = "silero_vad.onnx" });

					StartCoroutine(ailia_download.DownloadWithProgressFromURL(urlList, () =>
					{
						FileOpened = ailia_vad.OpenFile(asset_path + "/silero_vad.onnx.prototxt", asset_path + "/silero_vad.onnx", gpu_mode);
					}));
					break;
				case AudioProcessingModels.silero_vad_v6_2:
					mode_text.text = "silero_vad_v6_2";
	
					urlList.Add(new ModelDownloadURL() { folder_path = "silero-vad", file_name = "silero_vad_v6_2.onnx.prototxt" });
					urlList.Add(new ModelDownloadURL() { folder_path = "silero-vad", file_name = "silero_vad_v6_2.onnx" });

					StartCoroutine(ailia_download.DownloadWithProgressFromURL(urlList, () =>
					{
						FileOpened = ailia_vad.OpenFile(asset_path + "/silero_vad_v6_2.onnx.prototxt", asset_path + "/silero_vad_v6_2.onnx", gpu_mode);
					}));
					break;
				case AudioProcessingModels.rvc:
					mode_text.text = "silero_vad_v4 + rvc";

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
					mode_text.text = "silero_vad_v4 + rvc + f0";

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
				default:
					Debug.Log("Others ailia models are working in progress.");
					break;
			}
		}

		private void DestroyAiliaNetwork()
		{
			ailia_vad.Close();
			ailia_rvc.Close();
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

			VadAndRvcUpdate(waveData, channels, frequency);
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

