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
		public enum AudioProcessingModels
		{
			silero_vad,
		}

		[SerializeField]
		private AudioProcessingModels ailiaModelType = AudioProcessingModels.silero_vad;
		[SerializeField]
		private GameObject UICanvas = null;

		//Mic
		[SerializeField] private string m_DeviceName;
		private AudioClip m_AudioClip;
		private int m_LastAudioPos;
		private int input_pointer = 0;

		//Settings
		[SerializeField]
		private bool gpu_mode = false;
		[SerializeField]
		private bool mic_mode = false;

		//Target
		public AudioClip audio_clip = null;

		//Result
		RawImage raw_image = null;
		Text label_text = null;
		Text mode_text = null;

		//Preview
		private Texture2D wave_texture = null;

		//AILIA
		private AiliaModel ailia_model = new AiliaModel();
		private AiliaSileroVad ailia_vad = new AiliaSileroVad();

		private AiliaDownload ailia_download = new AiliaDownload();

		// AILIA open file
		private bool FileOpened = false;

		private void CreateAiliaNetwork(AudioProcessingModels modelType)
		{
			string asset_path = Application.temporaryCachePath;
			var urlList = new List<ModelDownloadURL>();
			if (gpu_mode)
			{
				ailia_model.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
			}
			switch (modelType)
			{
				case AudioProcessingModels.silero_vad:
					mode_text.text = "silero_vad";
	
					urlList.Add(new ModelDownloadURL() { folder_path = "silero-vad", file_name = "silero_vad.onnx.prototxt" });
					urlList.Add(new ModelDownloadURL() { folder_path = "silero-vad", file_name = "silero_vad.onnx" });

					StartCoroutine(ailia_download.DownloadWithProgressFromURL(urlList, () =>
					{
						FileOpened = ailia_model.OpenFile(asset_path + "/silero_vad.onnx.prototxt", asset_path + "/silero_vad.onnx");
					}));

					break;
				default:
					Debug.Log("Others ailia models are working in progress.");
					break;
			}
		}


		private void DestroyAiliaNetwork()
		{
			ailia_model.Close();
		}

		private string targetDevice = "";

		private void InitializeMic(){
			if (m_AudioClip != null){
				return;
			}

			if (mic_mode == false){
				Debug.Log("=== Audio File Input ===");
				m_AudioClip = audio_clip;
				return;
			}
			
			foreach (var device in Microphone.devices) {
				Debug.Log($"Device Name: {device}");
				if (m_DeviceName != "" && device.Contains(m_DeviceName)) {
					targetDevice = device;
				}
			}

			if (targetDevice == "" && Microphone.devices.Length >= 1){
				targetDevice = Microphone.devices[0];
			}
			
			Debug.Log($"=== Device Set: {targetDevice} ===");
			if (targetDevice == ""){
				m_AudioClip = null;
			}else{
				m_AudioClip = Microphone.Start(targetDevice, true, 10, 48000);
			}

			m_LastAudioPos = 0;
		}

		private void DestroyMic(){
			if (m_AudioClip == null){
				return;
			}
			if (mic_mode == true){
				Microphone.End(targetDevice);
				AudioClip.Destroy(m_AudioClip);
			}
			m_AudioClip = null;
		}

		// Get Pcm
		float [] GetPcm(ref uint channels, ref uint frequency){
			channels = (uint)m_AudioClip.channels;
			frequency = (uint)m_AudioClip.frequency;

			int input_step = (int)(Time.deltaTime * frequency);
			if (input_step < 1){
				input_step = 1;
			}
			float [] waveData = GetPcmCore(input_step);
			if (mic_mode == true){
				input_pointer += (int)(waveData.Length / channels);
			}
			return waveData;
		}

		float [] GetPcmCore(int input_step){
			float [] waveData;
			waveData = new float[0];
			if (mic_mode == false){
				if(input_pointer < m_AudioClip.samples){
					if (input_pointer + input_step < m_AudioClip.samples){
						waveData = new float[input_step * m_AudioClip.channels];
						m_AudioClip.GetData(waveData, input_pointer);
					}else{
						waveData = new float[(m_AudioClip.samples - input_pointer) * m_AudioClip.channels];
						m_AudioClip.GetData(waveData, input_pointer);
					}
					input_pointer = input_pointer + input_step;
				}
			}
			if (mic_mode == true){
				// from mic input
				waveData = GetUpdatedMicAudio();
			}
			return waveData;
		}

		private float[] GetUpdatedMicAudio() {
			float[] waveData = Array.Empty<float>();

			if (m_AudioClip == null){
				return waveData;
			}

			int nowAudioPos = Microphone.GetPosition(targetDevice);
			
			if (m_LastAudioPos < nowAudioPos) {
				int audioCount = nowAudioPos - m_LastAudioPos;
				waveData = new float[audioCount];
				m_AudioClip.GetData(waveData, m_LastAudioPos);
			} else if (m_LastAudioPos > nowAudioPos) {
				int audioBuffer = m_AudioClip.samples * m_AudioClip.channels;
				int audioCount = audioBuffer - m_LastAudioPos;
				
				float[] wave1 = new float[audioCount];
				m_AudioClip.GetData(wave1, m_LastAudioPos);
				
				float[] wave2 = new float[nowAudioPos];
				if (nowAudioPos != 0) {
					m_AudioClip.GetData(wave2, 0);
				}

				waveData = new float[audioCount + nowAudioPos];
				wave1.CopyTo(waveData, 0);
				wave2.CopyTo(waveData, audioCount);
			}

			m_LastAudioPos = nowAudioPos;

			return waveData;
		}

		private float[] displayWaveData = null;
		private float[] displayConfData = null;

		private void DisplayPreviewPcm(float [] waveData, float [] conf, uint channels){
			Color32 [] colors = wave_texture.GetPixels32();
			int steps = 10;
			int w = wave_texture.width;
			int h = wave_texture.height;
			int buf_w = w * steps;

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
				for (int y = 0; y < h ; y++){
					colors[y*w+x] = new Color32(0,0,0,255);
				}
				int y2 = (int)(displayWaveData[x * steps] * h / 2 + h / 2);
				if (y2 >= 0 && y2 < h){
					colors[y2*w+x] = new Color32(0,255,0,255);
				}
				int y3 = (int)(displayConfData[x * steps] * h / 2 + h / 2);
				if (y3 >= 0 && y3 < h){
					colors[y3*w+x] = new Color32(255,0,0,255);
				}
			}
			wave_texture.SetPixels32(colors);
			wave_texture.Apply();
		}

		// Use this for initialization
		void Start()
		{
			InitializeMic();
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
			Color32[] camera = new Color32[tex_width * tex_height];
			
			//Detection
			long start_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond; ;
			long end_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond; ;

			if (label_text != null)
			{
				label_text.text = (end_time - start_time) + "ms\n" + ailia_model.EnvironmentName();
			}

			// Get Mic Input
			float[] waveData = null;
			uint channels = 1;
			uint frequency = 1;
			waveData = GetPcm(ref channels, ref frequency);

			float[] conf = new float[waveData.Length];
			for (int i = 0; i < conf.Length; i++){
				conf[i] = Mathf.Sin(Mathf.PI * i / frequency);
			}

			// Preview
			DisplayPreviewPcm(waveData, conf, channels);
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
			DestroyMic();
			DestroyAiliaNetwork();
		}

		void OnDestroy()
		{
			DestroyMic();
			DestroyAiliaNetwork();
		}
	}
}

