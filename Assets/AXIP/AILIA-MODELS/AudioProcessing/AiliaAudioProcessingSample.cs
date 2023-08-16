/* AILIA Unity Plugin Audio Processing Sample */
/* Copyright 2023 AXELL CORPORATION */

using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Runtime.InteropServices;

using UnityEngine;
using UnityEngine.UI;

namespace ailiaSDK {
	public class AiliaAudioProcessingSample : AiliaRenderer {
		//Models
		public enum AudioProcessingModels
		{
			silero_vad,
			rvc,
			rvc_with_f0
		}

		[SerializeField]
		private AudioProcessingModels ailiaModelType = AudioProcessingModels.silero_vad;
		[SerializeField]
		private GameObject UICanvas = null;

		//Settings
		[SerializeField]
		private bool gpu_mode = false;
		[SerializeField]
		private bool f0_gpu_mode = false;
		[SerializeField]
		private bool mic_mode = false;
		[SerializeField]
		private bool async_mode = false;	// Async Processing

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
		private long rvc_time = 0;
		private long f0_time = 0;

		//Preview
		private Texture2D wave_texture = null;

		//AILIA
		private AiliaSileroVad ailia_vad = new AiliaSileroVad();
		private AiliaRvc ailia_rvc = new AiliaRvc();
		private AiliaMicrophone ailia_mic = new AiliaMicrophone();
		private AiliaSplitAudio ailia_split = new AiliaSplitAudio();

		//AILIA open file
		private AiliaDownload ailia_download = new AiliaDownload();
		private bool FileOpened = false;

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
							FileOpened = ailia_rvc.OpenFile(asset_path + "/hubert_base.onnx.prototxt", asset_path + "/hubert_base.onnx", asset_path + "/AISO-HOWATTO.onnx.prototxt", asset_path + "/AISO-HOWATTO.onnx", gpu_mode);
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
					urlList.Add(new ModelDownloadURL() { folder_path = "rvc", file_name = "crepe.onnx.prototxt" });
					urlList.Add(new ModelDownloadURL() { folder_path = "rvc", file_name = "crepe.onnx" });

					StartCoroutine(ailia_download.DownloadWithProgressFromURL(urlList, () =>
					{
						FileOpened = ailia_vad.OpenFile(asset_path + "/silero_vad.onnx.prototxt", asset_path + "/silero_vad.onnx", gpu_mode);
						if (FileOpened == true){
							FileOpened = ailia_rvc.OpenFile(asset_path + "/hubert_base.onnx.prototxt", asset_path + "/hubert_base.onnx", Application.streamingAssetsPath + "/rvc_f0.onnx.prototxt", Application.streamingAssetsPath + "/rvc_f0.onnx", gpu_mode);
							if (FileOpened == true){
								FileOpened = ailia_rvc.OpenFileF0(asset_path + "/crepe.onnx.prototxt", asset_path + "/crepe.onnx", f0_gpu_mode);
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

		// Display pcm and vad confidence value
		private float[] displayWaveData = null;
		private float[] displayConfData = null;

		private void DisplayPreviewPcm(Color32 [] colors, float [] waveData, float [] conf, uint channels){
			int steps = 10;
			int w = wave_texture.width;
			int h = wave_texture.height / 4;
			int original_h = wave_texture.height;
			int buf_w = w * steps;
			int offset_y = 3 * h;

			if (displayWaveData == null){
				displayWaveData = new float[buf_w];
				displayConfData = new float[buf_w];
			}

			int add_data_n = (int)(waveData.Length / channels);
			int reuse_data_n = buf_w - add_data_n;
			for (int i = 0; i < reuse_data_n; i++){
				displayWaveData[i] = displayWaveData[i + (buf_w - reuse_data_n)];
				displayConfData[i] = displayConfData[i + (buf_w - reuse_data_n)];
			}
			for (int i = reuse_data_n; i < buf_w; i++){
				if (i >= 0){
					displayWaveData[i] = waveData[(i - reuse_data_n) * channels];
					displayConfData[i] = conf[(i - reuse_data_n) * channels];
				}
			}

			for (int x = 0; x < w ; x++){
				for (int y = 0; y < original_h ; y++){
					colors[y*w+x] = new Color32(0,0,0,255);
				}
				int y3 = (int)(displayConfData[x * steps] * h);
				if (y3 >= 0 && y3 < h){
					for (int y = 0; y < y3 ; y++){
						colors[(y+offset_y)*w+x] = new Color32(255,0,0,255);
					}
				}
				int y2 = (int)(displayWaveData[x * steps] * h / 2 + h / 2);
				if (y2 >= 0 && y2 < h){
					colors[(y2+offset_y)*w+x] = new Color32(0,255,0,255);
				}
			}
		}

		// Display splitted audio clip
		private void DisplayVadAudioClip(Color32 [] colors, AudioClip clip, int i, int count){
			float [] buf = new float[clip.channels * clip.samples];
			clip.GetData(buf, 0);

			int w = wave_texture.width;
			int div = count;
			if (div < 3){
				div = 3;
			}
			int h = (wave_texture.height - wave_texture.height/4) / div;
			int steps = clip.samples / w;
			if (steps < 1){
				steps = 1;
			}
			int offset_y = 3 * wave_texture.height/4 - (i + 1) * h;
			int buf_w = w * steps;

			for (int x = 0; x < w ; x++){
				int y2 = (int)(buf[x * steps] * h / 2 + h / 2);
				if (y2 >= 0 && y2 < h){
					colors[(offset_y + y2)*w+x] = new Color32(0,255,0,255);
				}
			}
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
			int tex_width = 480;
			int tex_height = 480;
			if (wave_texture == null)
			{
				wave_texture = new Texture2D(tex_width, tex_height);
				raw_image.texture = wave_texture;
			}
			
			// Get Mic Input
			float[] waveData = null;
			uint channels = 1;
			uint frequency = 1;
			waveData = ailia_mic.GetPcm(ref channels, ref frequency);

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
				if (async_mode && ailia_rvc.AsyncProcessing()){
					label_text.text += "\nrvc async processing\n";
				}
			}

			// Split
			ailia_split.Split(vad_result);
			if (async_mode){
				GetResultAsync();
				if (ailia_split.GetAudioClipCount() > 0){
					if (!ailia_rvc.AsyncProcessing()){
						AudioClip clip = ailia_split.PopAudioClip();
						PushSplitAudioAsync(clip);
					}
				}
			}else{
				if (ailia_split.GetAudioClipCount() > 0){
					AudioClip clip = ailia_split.PopAudioClip();
					PushSplitAudio(clip);
				}
			}

			// Play
			if (!audio_source.isPlaying && vad_audio_clip_play_list.Count > 0){
				AudioClip clip = vad_audio_clip_play_list[0];
				vad_audio_clip_play_list.RemoveAt(0);
				audio_source.PlayOneShot(clip);
			}
			
			// Preview
			Color32 [] colors = wave_texture.GetPixels32();
			DisplayPreviewPcm(colors, vad_result.pcm, vad_result.conf, channels);
			for (int i = 0; i < vad_audio_clip.Count; i++){
				DisplayVadAudioClip(colors, vad_audio_clip[i], i, vad_audio_clip.Count);
			}
			wave_texture.SetPixels32(colors);
			wave_texture.Apply();
		}

		private void PushSplitAudio(AudioClip clip)
		{
			if (ailiaModelType == AudioProcessingModels.rvc || ailiaModelType == AudioProcessingModels.rvc_with_f0){
				clip = ailia_rvc.Process(clip);
				rvc_time = ailia_rvc.GetProfile();
			}
			vad_audio_clip.Add(clip);
			vad_audio_clip_play_list.Add(clip);
		}

		private void PushSplitAudioAsync(AudioClip clip)
		{
			if (ailiaModelType == AudioProcessingModels.rvc || ailiaModelType == AudioProcessingModels.rvc_with_f0){
				ailia_rvc.AsyncProcess(clip);
			}else{
				vad_audio_clip.Add(clip);
				vad_audio_clip_play_list.Add(clip);
			}
		}

		private void GetResultAsync(){
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

