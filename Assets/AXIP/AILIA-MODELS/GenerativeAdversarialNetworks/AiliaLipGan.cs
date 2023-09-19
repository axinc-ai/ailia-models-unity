﻿/* AILIA Unity Plugin LipGAN Sample */
/* Copyright 2023 AXELL CORPORATION */

using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Runtime.InteropServices;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace ailiaSDK
{
	public class AiliaLipGan
	{
		public const int NUM_KEYPOINTS = 468;
		public const int DETECTION_WIDTH = 96;
		public const int DETECTION_HEIGHT = 96;


		public struct LipGanInfo
		{
			public float width;
			public float height;
			public Vector2[] keypoints;
			public Vector2 center;
			public float theta;
		}

		private float [] melspectrogram = null;

		private void Preemphasis(float [] data){
			float before = 0.0f;
			for (int i = 0; i < data.Length; i++){
				float new_before = data[i];
				data[i] = data[i] - 0.97 * before;
				before = new_before;
			}

		}

		private float AmpToDb(float x, float min_level, float ref_level_db){
			return 20 * Mathf.Log(Mathf.Max(min_level, x)) / Mathf.Log(10) - ref_level_db;
		}

		private float Normalize(float s, float max_abs_value, float min_level_db){
			return Mathf.Clip((2 * max_abs_value) * ((s - min_level_db) / (-min_level_db))) - max_abs_value, -max_abs_value, max_abs_value);
		}

		// Get melspectrum using ailia.audio
		private void MelSpectrum(float [] samples,int clip_samples,int clip_channels,int clip_frequency){
			// Convert stereo to mono
			float[] data_s = new float[clip_samples];
			for (int i = 0; i < data_s.Length; ++i)
			{
				if(clip_channels==1){
					data_s[i]=samples[i];
				}else{
					data_s[i]=(samples[i*2+0]+samples[i*2+1])/2;
				}
			}

			// Resample
			int targetSampleRate = 16000;
			float[] data = new float[clip_samples * targetSampleRate / clip_frequency];
			AiliaAudio.ailiaAudioResample(data, data_s, targetSampleRate, data.Length, clip_frequency, data_s.Length);

			// Preemphasis
			Preemphasis(data);

			// Get melspectrum
			int len = data.Length;
			const int SAMPLE_RATE=16000;
			const int FFT_N=800;
			const int HOP_N=200;
			const int WIN_N=800;
			const float POWER=1;
			const int MELS = 80;
			const float F_MIN=55
			const float f_max=7600;

			int frame_len=0;
			int status = AiliaAudio.ailiaAudioGetFrameLen(ref frame_len, len, FFT_N, HOP_N, AiliaAudio.AILIA_AUDIO_STFT_CENTER_ENABLE);
			if(status!=0){
				Debug.Log("ailiaAudioGetFrameLen failed %d\n"+status);
				return null;
			}
			if(DEBUG_MODE){
				Debug.Log("samplingRate:"+clip_frequency);
				Debug.Log("samples:"+clip_samples);
				Debug.Log("frame_len:"+frame_len);
			}

			float [] melspectrogram = new float [MELS*frame_len];
			status=AiliaAudio.ailiaAudioGetMelSpectrogram(melspectrogram, data, len, SAMPLE_RATE, FFT_N, HOP_N, WIN_N, AiliaAudio.AILIA_AUDIO_WIN_TYPE_HANN, 
				frame_len, AiliaAudio.AILIA_AUDIO_STFT_CENTER_ENABLE, POWER, AiliaAudio.AILIA_AUDIO_FFT_NORMALIZE_NONE ,F_MIN, f_max, MELS, 
				AiliaAudio.AILIA_AUDIO_MEL_NORMALIZE_NONE,AiliaAudio.AILIA_AUDIO_MEL_SCALE_FORMULA_HTK);
			if(status!=0){
				Debug.Log("ailiaAudioGetMelSpectrogram failed "+status);
				return null;
			}

			float min_level_db = -100;
			float ref_level_db = 30;
			float max_abs_value = 4.0f;
			float min_level = Mathf.Exp(min_level_db / 20 * Mathf.Log(10));
			for (int i = 0; i < melspectrogram.Length; i++){
				float x = melspectrogram[i];
				float s = AmpToDb(x, min_level, ref_level_db);
				s = Normalize(s, max_abs_value, min_level_db);
				melspectrogram[i] = s;
			}

			m_melspectrogram = melspectrogram;
		}

		// Melspectrum
		public void SetAudio(AudioClip clip)
		{
			// Get pcm from audio clip
			float[] samples = new float[clip.samples * clip.channels];
			clip.GetData(samples, 0);   //float range

			// Get mel spectrum using ailia.audio
			float [] mel_spectrum=MelSpectrum(samples,clip.samples,clip.channels,clip.frequency);
			if(mel_spectrum==null){
				Debug.Log("MelSpectrum failed");
				return;
			}
		}

		// Update is called once per frame
		public List<FaceMeshInfo> Detection(AiliaModel ailia_model, Color32[] camera, int tex_width, int tex_height, List<AiliaBlazeface.FaceInfo> result_detections, bool debug=false)
		{
			List<FaceMeshInfo> result = new List<FaceMeshInfo>();
			for (int i = 0; i < result_detections.Count; i++)
			{
				//extract roi
				AiliaBlazeface.FaceInfo face = result_detections[i];
				int fw = (int)(face.width * tex_width);
				int fh = (int)(face.height * tex_height);
				int fx = (int)(face.center.x * tex_width);
				int fy = (int)(face.center.y * tex_height);

				//extract data
				float[] data = new float[DETECTION_WIDTH * DETECTION_HEIGHT * 3];
				int w = DETECTION_WIDTH;
				int h = DETECTION_HEIGHT;
				float scale = 1.0f * fw / w;
				for (int y = 0; y < h; y++)
				{
					for (int x = 0; x < w; x++)
					{
						int ox = (x - w/2);
						int oy = (y - h/2);
						int x2 = (int)((ox) * scale + fx);
						int y2 = (int)((oy) * scale + fy);
						if (x2 < 0 || y2 < 0 || x2 >= tex_width || y2 >= tex_height)
						{
							data[(y * w + x) + 0 * w * h] = 0;
							data[(y * w + x) + 1 * w * h] = 0;
							data[(y * w + x) + 2 * w * h] = 0;
							continue;
						}

						// img
						data[(y * w + x) * 6 + 0] = (float)((camera[(tex_height - 1 - y2) * tex_width + x2].r) / 255.0);
						data[(y * w + x) * 6 + 1] = (float)((camera[(tex_height - 1 - y2) * tex_width + x2].g) / 255.0);
						data[(y * w + x) * 6 + 2] = (float)((camera[(tex_height - 1 - y2) * tex_width + x2].b) / 255.0);

						// img masked
						if (y > h / 2){
							data[(y * w + x) * 6 + 3] = 0;
							data[(y * w + x) * 6 + 4] = 0;
							data[(y * w + x) * 6 + 5] = 0;
						}else{
							data[(y * w + x) * 6 + 3] = data[(y * w + x) * 6 + 0];
							data[(y * w + x) * 6 + 4] = data[(y * w + x) * 6 + 1];
							data[(y * w + x) * 6 + 5] = data[(y * w + x) * 6 + 2];
						}
					}
				}

				//debug display data
				if(debug){
					for (int y = 0; y < h; y++)
					{
						for (int x = 0; x < w; x++)
						{
							camera[tex_width*(tex_height-1-y)+x].r = (byte)((data[(y * w + x) * 6 + 0] ) * 255.0);
							camera[tex_width*(tex_height-1-y)+x].g = (byte)((data[(y * w + x) * 6 + 1] ) * 255.0);
							camera[tex_width*(tex_height-1-y)+x].b = (byte)((data[(y * w + x) * 6 + 2] ) * 255.0);
						}
					}
				}

				// mel = (1, 80, 27, 1) // 27 frames mel, channel last

				//compute
				float [] output = new float [w * h * 3];
				bool success = ailia_model.Predict(output,data);
				if (!success)
				{
					Debug.Log("Can not Predict");
				}

				//display
				if(debug){
					for (int y = 0; y < h; y++)
					{
						for (int x = 0; x < w; x++)
						{
							camera[tex_width*(tex_height-1-y)+x+tex_width].r = (byte)((output[(y * w + x) * 3 + 0] ) * 255.0);
							camera[tex_width*(tex_height-1-y)+x+tex_width].g = (byte)((output[(y * w + x) * 3 + 1] ) * 255.0);
							camera[tex_width*(tex_height-1-y)+x+tex_width].b = (byte)((output[(y * w + x) * 3 + 2] ) * 255.0);
						}
					}
				}
			}

			return result;
		}

	}
}