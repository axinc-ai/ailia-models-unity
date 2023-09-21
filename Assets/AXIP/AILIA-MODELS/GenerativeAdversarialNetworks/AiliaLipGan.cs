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
	public class AiliaLipGan
	{
		private const int DETECTION_WIDTH = 96;
		private const int DETECTION_HEIGHT = 96;
		private const int SAMPLE_RATE=16000;
		private const int FFT_N=800;
		private const int HOP_N=200; // 0.0125ms (HOP_N / SAMPLE_RATE)
		private const int WIN_N=800;
		private const float POWER=1;
		private const int MELS = 80;
		private const float F_MIN=55;
		private const float F_MAX=7600;
		private const int MEL_FRAMES = 27;
		
		private float [] m_melspectrogram = null;
		private int m_frame_len = 0;

		// Audio utils
		private void Preemphasis(float [] data){
			float before = 0.0f;
			for (int i = 0; i < data.Length; i++){
				float new_before = data[i];
				data[i] = data[i] - 0.97f * before;
				before = new_before;
			}
		}

		private float AmpToDb(float x, float min_level){
			return 20 * Mathf.Log(Mathf.Max(min_level, x)) / Mathf.Log(10);
		}

		private float Normalize(float s, float max_abs_value, float min_level_db){
			return Mathf.Clamp((2 * max_abs_value) * ((s - min_level_db) / (-min_level_db)) - max_abs_value, -max_abs_value, max_abs_value);
		}

		// Get melspectrum using ailia.audio
		private void MelSpectrum(float [] samples,int clip_samples,int clip_channels,int clip_frequency, bool debug){
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
			int resample_samples = (int)((long)clip_samples * SAMPLE_RATE / clip_frequency);
			if(debug){
				Debug.Log("samplingRate:"+clip_frequency);
				Debug.Log("samples:"+clip_samples);
				Debug.Log("resample_samples:"+resample_samples);
			}
			float[] data = new float[resample_samples];
			int status=AiliaAudio.ailiaAudioResample(data, data_s, SAMPLE_RATE, data.Length, clip_frequency, data_s.Length);
			if(status!=0){
				Debug.Log("ailiaAudioResample failed %d\n"+status);
				return;
			}

			// Preemphasis
			if(debug){
				Debug.Log("Input ["+data[ 0] + " " + data[ 1] + " " + data[ 2] + "]");
			}
			Preemphasis(data);
			if(debug){
				Debug.Log("Preemphasis ["+data[ 0] + " " + data[ 1] + " " + data[ 2] + "]");
			}

			// Get melspectrum
			int len = data.Length;
			int frame_len=0;
			status = AiliaAudio.ailiaAudioGetFrameLen(ref frame_len, len, FFT_N, HOP_N, AiliaAudio.AILIA_AUDIO_STFT_CENTER_ENABLE);
			if(status!=0){
				Debug.Log("ailiaAudioGetFrameLen failed %d\n"+status);
				return;
			}

			if (debug){
				Debug.Log("frame_len:"+frame_len);
			}

			float [] melspectrogram = new float [MELS*frame_len];
			status=AiliaAudio.ailiaAudioGetMelSpectrogram(melspectrogram, data, len, SAMPLE_RATE, FFT_N, HOP_N, WIN_N, AiliaAudio.AILIA_AUDIO_WIN_TYPE_HANN, 
				frame_len, AiliaAudio.AILIA_AUDIO_STFT_CENTER_ENABLE, POWER, AiliaAudio.AILIA_AUDIO_FFT_NORMALIZE_NONE ,F_MIN, F_MAX, MELS, 
				AiliaAudio.AILIA_AUDIO_MEL_NORMALIZE_ENABLE,AiliaAudio.AILIA_AUDIO_MEL_SCALE_FORMULA_SLANYE);
			if(status!=0){
				Debug.Log("ailiaAudioGetMelSpectrogram failed "+status);
				return;
			}

			if(debug){
				Debug.Log("Melspectrogram ("+MELS+","+frame_len+")");
				Debug.Log("["+melspectrogram[ 0] + " " + melspectrogram[ 1] + " " + melspectrogram[ 2] + " ... " + melspectrogram[frame_len-3] + " " + melspectrogram[frame_len-2] + " " + melspectrogram[frame_len-1] + "]");
				Debug.Log("["+melspectrogram[frame_len] + " " + melspectrogram[frame_len+1] + " " + melspectrogram[frame_len+2] + " ... " + melspectrogram[frame_len+frame_len-3] + " " + melspectrogram[frame_len+frame_len-2] + " " + melspectrogram[frame_len+frame_len-1] + "]");
			}

			float min_level_db = -100;
			float ref_level_db = 20;
			float max_abs_value = 4.0f;
			float min_level = Mathf.Exp(min_level_db / 20 * Mathf.Log(10));
			for (int i = 0; i < melspectrogram.Length; i++){
				float x = melspectrogram[i];
				float s = AmpToDb(x, min_level) - ref_level_db;
				s = Normalize(s, max_abs_value, min_level_db);
				melspectrogram[i] = s;
			}

			if(debug){
				Debug.Log("Normalized ("+MELS+","+frame_len+")");
				Debug.Log("["+melspectrogram[ 0] + " " + melspectrogram[ 1] + " " + melspectrogram[ 2] + " ... " + melspectrogram[frame_len-3] + " " + melspectrogram[frame_len-2] + " " + melspectrogram[frame_len-1] + "]");
				Debug.Log("["+melspectrogram[frame_len] + " " + melspectrogram[frame_len+1] + " " + melspectrogram[frame_len+2] + " ... " + melspectrogram[frame_len+frame_len-3] + " " + melspectrogram[frame_len+frame_len-2] + " " + melspectrogram[frame_len+frame_len-1] + "]");
			}

			m_melspectrogram = melspectrogram;
			m_frame_len = frame_len;
		}

		// Melspectrum
		public void SetAudio(AudioClip clip, bool debug = false)
		{
			// Get pcm from audio clip
			float[] samples = new float[clip.samples * clip.channels];
			clip.GetData(samples, 0);   //float range

			// Get mel spectrum using ailia.audio
			MelSpectrum(samples,clip.samples,clip.channels,clip.frequency, debug);
		}

		// Crop input data
		private float [] CropInputFace(int fx, int fy, int fw, int fh, Color32[] camera, int tex_width, int tex_height){
			float[] data = new float[DETECTION_WIDTH * DETECTION_HEIGHT * 6];
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
						data[(y * w + x) * 6 + 0] = 0;
						data[(y * w + x) * 6 + 1] = 0;
						data[(y * w + x) * 6 + 2] = 0;

						data[(y * w + x) * 6 + 3] = 0;
						data[(y * w + x) * 6 + 4] = 0;
						data[(y * w + x) * 6 + 5] = 0;
						continue;
					}

					// img
					data[(y * w + x) * 6 + 0] = (float)((camera[(tex_height - 1 - y2) * tex_width + x2].r) / 255.0);
					data[(y * w + x) * 6 + 1] = (float)((camera[(tex_height - 1 - y2) * tex_width + x2].g) / 255.0);
					data[(y * w + x) * 6 + 2] = (float)((camera[(tex_height - 1 - y2) * tex_width + x2].b) / 255.0);

					// img masked
					if (y >= h / 2){
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
			return data;
		}

		byte Blend(byte dst, float src, float alpha){
			float v = (float)dst * (1 - alpha) + src * alpha * 255;
			v = Mathf.Min(Mathf.Max(v, 0), 255);
			return (byte)v;
		}

		// Output
		private void Composite(int fx, int fy, int fw, int fh, Color32[] result, int tex_width, int tex_height, float [] output){
			for (int y = 0; y < fh; y++)
			{
				for (int x = 0; x < fw; x++)
				{
					int x2 = x * DETECTION_WIDTH / fw;
					int y2 = y * DETECTION_HEIGHT / fh;
					int x3 = fx - fw / 2 + x;
					int y3 = fy - fh / 2 + y;
					if (x3 >= 0 && x3 < tex_width && y3 >= 0 && y3 < tex_height){
						int rx = 16;
						int ry = 16;
						float dx = Mathf.Min(Mathf.Min(x2, DETECTION_WIDTH - 1 - x2) / rx, 1);
						float dy = Mathf.Min(Mathf.Min(y2, DETECTION_HEIGHT - 1 - y2) / ry, 1);
						float alpha = Mathf.Min(dx, dy);
						int d_adr = (tex_height - 1 - y3) * tex_width + x3;
						result[d_adr].r = Blend(result[d_adr].r, output[(y2 * DETECTION_WIDTH + x2) * 3 + 0], alpha);
						result[d_adr].g = Blend(result[d_adr].g, output[(y2 * DETECTION_WIDTH + x2) * 3 + 1], alpha);
						result[d_adr].b = Blend(result[d_adr].b, output[(y2 * DETECTION_WIDTH + x2) * 3 + 2], alpha);
					}
				}
			}
		}

		// Update is called once per frame
		public Color32[] GenerateImage(AiliaModel ailia_model, Color32[] camera, int tex_width, int tex_height, List<AiliaBlazeface.FaceInfo> result_detections, float audio_time, bool debug=false)
		{
			// create base images
			Color32[] result = new Color32 [camera.Length];
			for (int i = 0; i < result.Length; i++){
				result[i] = camera[i];
			}
			
			for (int i = 0; i < result_detections.Count; i++)
			{
				//extract roi
				AiliaBlazeface.FaceInfo face = result_detections[i];
				int fw = (int)(face.width * tex_width);
				int fh = (int)(face.height * tex_height);
				int fx = (int)(face.center.x * tex_width);
				int fy = (int)(face.center.y * tex_height);
				int w = DETECTION_WIDTH;
				int h = DETECTION_HEIGHT;

				//extract data
				float [] data = CropInputFace(fx, fy, fw, fh, camera, tex_width, tex_height);

				//debug display data
				if(debug){
					for (int y = 0; y < h; y++)
					{
						for (int x = 0; x < w; x++)
						{
							result[tex_width*(tex_height-1-y)+x].r = (byte)((data[(y * w + x) * 6 + 0] ) * 255.0);
							result[tex_width*(tex_height-1-y)+x].g = (byte)((data[(y * w + x) * 6 + 1] ) * 255.0);
							result[tex_width*(tex_height-1-y)+x].b = (byte)((data[(y * w + x) * 6 + 2] ) * 255.0);

							result[tex_width*(tex_height-1-y)+x+w].r = (byte)((data[(y * w + x) * 6 + 3] ) * 255.0);
							result[tex_width*(tex_height-1-y)+x+w].g = (byte)((data[(y * w + x) * 6 + 4] ) * 255.0);
							result[tex_width*(tex_height-1-y)+x+w].b = (byte)((data[(y * w + x) * 6 + 5] ) * 255.0);
						}
					}
				}

				// mel = (1, 80, 27, 1) // 27 frames mel, channel last
				float steps = 1.0f / (1.0f * HOP_N / SAMPLE_RATE);
				float [] mels = new float [1 * MELS * MEL_FRAMES * 1];
				//Debug.Log("mel from " + (int)(steps * audio_time));
				for (int m = 0; m < MELS; m++){
					for (int j = 0; j < MEL_FRAMES; j++){
						int t = j + (int)(steps * audio_time);
						if (t > m_frame_len){
							t = m_frame_len - 1;
						}
						mels[m * MEL_FRAMES + j] = m_melspectrogram[m * m_frame_len + t];
					}
				}
				//Debug.Log("data " + data[0]+ " " + data[1]+ " " + data[2]+ " " + data[3]+ " " + data[4]+ " " + data[5]);
				//Debug.Log("mels " + mels[0]+ " " + mels[1]);

				//compute
				float [] output = new float [w * h * 3];
				uint[] input_blobs = ailia_model.GetInputBlobList();
				if (input_blobs != null)
				{

					Ailia.AILIAShape input_shape = new Ailia.AILIAShape();
					input_shape.x=6;
					input_shape.y=(uint)DETECTION_WIDTH;
					input_shape.z=(uint)DETECTION_HEIGHT;
					input_shape.w=1;
					input_shape.dim=4;

					bool success = ailia_model.SetInputBlobShape(input_shape, (int)input_blobs[1]);
					if (success == false){
						Debug.Log("SetInputBlobShape failed");
					}

					input_shape.x=1;
					input_shape.y=(uint)MEL_FRAMES;
					input_shape.z=(uint)MELS;
					input_shape.w=1;
					input_shape.dim=4;

					success = ailia_model.SetInputBlobShape(input_shape, (int)input_blobs[0]);
					if (success == false){
						Debug.Log("SetInputBlobShape failed");
					}

					success = ailia_model.SetInputBlobData(data, (int)input_blobs[1]);
					if (!success)
					{
						Debug.Log("Can not SetInputBlobData");
					}
					success = ailia_model.SetInputBlobData(mels, (int)input_blobs[0]);
					if (!success)
					{
						Debug.Log("Can not SetInputBlobData");
					}

					ailia_model.Update();

					uint[] output_blobs = ailia_model.GetOutputBlobList();
					if (output_blobs != null && output_blobs.Length >= 1)
					{
						success = ailia_model.GetBlobData(output, (int)output_blobs[0]);
						if (!success)
						{
							Debug.Log("Can not GetBlobData");
						}
					}
				}

				//display
				if(debug){
					for (int y = 0; y < h; y++)
					{
						for (int x = 0; x < w; x++)
						{
							result[tex_width*(tex_height-1-y)+x+w*2].r = (byte)((output[(y * w + x) * 3 + 0] ) * 255.0);
							result[tex_width*(tex_height-1-y)+x+w*2].g = (byte)((output[(y * w + x) * 3 + 1] ) * 255.0);
							result[tex_width*(tex_height-1-y)+x+w*2].b = (byte)((output[(y * w + x) * 3 + 2] ) * 255.0);
						}
					}
				}

				//Write output
				Composite(fx, fy, fw, fh, result, tex_width, tex_height, output);
			}

			return result;
		}
	}
}