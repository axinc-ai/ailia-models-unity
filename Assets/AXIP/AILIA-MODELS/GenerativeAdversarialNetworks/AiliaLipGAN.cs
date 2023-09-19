/* AILIA Unity Plugin LipGAN Sample */
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
	public class AiliaLipGAN
	{
		public const int NUM_KEYPOINTS = 468;
		public const int DETECTION_WIDTH = 192;
		public const int DETECTION_HEIGHT = 192;

		private const float DSCALE = 1.5f;

		public struct FaceMeshInfo
		{
			public float width;
			public float height;
			public Vector2[] keypoints;
			public Vector2 center;
			public float theta;
		}

		void Resample(){
			// Step Loop
			int targetSampleRate = 16000;
			float [] conf = new float[pcm.Length];
			int steps = sequence * sampleRate / targetSampleRate;
			int s;
			for (s = 0; s < pcm.Length - steps; s+=steps){
				// Resampling to targetSampleRate
				for (int i = 0; i < input.Length; i++){
					int i2 = i * sampleRate / targetSampleRate;
					if (s + i2 < pcm.Length){
						input[i] = pcm[s + i2];
					}else{
						input[i] = 0;
					}
				}
				sr[0] = targetSampleRate;
			}

			//public static extern int ailiaAudioResample(float[] dst, float[] src, int dst_sample_rate, int dst_n, int src_sample_rate, int src_n);
		}

		// Get melspectrum using ailia.audio
		float [] MelSpectrum(float [] samples,int clip_samples,int clip_channels,int clip_frequency){
			// Convert stereo to mono
			float[] data = new float[clip_samples];
			for (int i = 0; i < data.Length; ++i)
			{
				if(clip_channels==1){
					data[i]=samples[i];
				}else{
					data[i]=(samples[i*2+0]+samples[i*2+1])/2;
				}
			}

			// Get melspectrum
			int len = data.Length;
			const int SAMPLE_RATE=44100;
			const int FFT_N=2048;
			const int HOP_N=1024;
			const int WIN_N=2048;
			const float POWER=2;
			const float F_MIN=0.0f;
			float f_max=44100/2;

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

			return melspectrogram;
		}

		// Melspectrum
		public void SetAudio(AudioClip clip)
		{
			// Resample to 16kHz

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
				int fw = (int)(face.width * tex_width * DSCALE);
				int fh = (int)(face.height * tex_height * DSCALE);
				int fx = (int)(face.center.x * tex_width);
				int fy = (int)(face.center.y * tex_height);
				const int RIGHT_EYE=0;
				const int LEFT_EYE=1;
				float theta_x = (float)(face.keypoints[LEFT_EYE].x - face.keypoints[RIGHT_EYE].x);
				float theta_y = (float)(face.keypoints[LEFT_EYE].y - face.keypoints[RIGHT_EYE].y);
				float theta = (float)System.Math.Atan2(theta_y,theta_x);

				//extract data
				float[] data = new float[DETECTION_WIDTH * DETECTION_HEIGHT * 3];
				int w = DETECTION_WIDTH;
				int h = DETECTION_HEIGHT;
				float scale = 1.0f * fw / w;
				float ss=(float)System.Math.Sin(-theta);
				float cs=(float)System.Math.Cos(-theta);
				for (int y = 0; y < h; y++)
				{
					for (int x = 0; x < w; x++)
					{
						int ox = (x - w/2);
						int oy = (y - h/2);
						int x2 = (int)((ox *  cs + oy * ss) * scale + fx);
						int y2 = (int)((ox * -ss + oy * cs) * scale + fy);
						if (x2 < 0 || y2 < 0 || x2 >= tex_width || y2 >= tex_height)
						{
							data[(y * w + x) + 0 * w * h] = 0;
							data[(y * w + x) + 1 * w * h] = 0;
							data[(y * w + x) + 2 * w * h] = 0;
							continue;
						}
						data[(y * w + x) + 0 * w * h] = (float)((camera[(tex_height - 1 - y2) * tex_width + x2].r) / 127.5 - 1.0);
						data[(y * w + x) + 1 * w * h] = (float)((camera[(tex_height - 1 - y2) * tex_width + x2].g) / 127.5 - 1.0);
						data[(y * w + x) + 2 * w * h] = (float)((camera[(tex_height - 1 - y2) * tex_width + x2].b) / 127.5 - 1.0);
					}
				}

				//debug display data
				if(debug){
					for (int y = 0; y < h; y++)
					{
						for (int x = 0; x < w; x++)
						{
							camera[tex_width*(tex_height-1-y)+x].r = (byte)((data[y * w + x + 0 * w * h] + 1.0) * 127.5);
							camera[tex_width*(tex_height-1-y)+x].g = (byte)((data[y * w + x + 1 * w * h] + 1.0) * 127.5);
							camera[tex_width*(tex_height-1-y)+x].b = (byte)((data[y * w + x + 2 * w * h] + 1.0) * 127.5);
						}
					}
				}

				//compute
				float [] output = new float [NUM_KEYPOINTS * 3];
				bool success = ailia_model.Predict(output,data);
				if (!success)
				{
					Debug.Log("Can not Predict");
				}

				//object
				FaceMeshInfo facemesh_info = new FaceMeshInfo();
				facemesh_info.center = face.center;
				facemesh_info.theta = theta;
				facemesh_info.width = face.width * DSCALE;
				facemesh_info.height = face.height * DSCALE;
				facemesh_info.keypoints = new Vector2[NUM_KEYPOINTS];
				for(int j=0;j<NUM_KEYPOINTS;j++){
					facemesh_info.keypoints[j]=new Vector2(output[j*3+0],output[j*3+1]);
				}

				//display
				if(debug){
					for(int j=0;j<NUM_KEYPOINTS;j++){
						int x = (int)(output[j*3+0]);
						int y = (int)(output[j*3+1]);
						if(x>=0 && y>=0 && x<=w && y<=h){
							camera[tex_width*(tex_height-1-y)+x].r = 0;
							camera[tex_width*(tex_height-1-y)+x].g = 255;
							camera[tex_width*(tex_height-1-y)+x].b = 0;
						}
					}
				}

				result.Add(facemesh_info);
			}

			return result;
		}

	}
}