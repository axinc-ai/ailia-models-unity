/* AILIA Unity Plugin Display Audio Sample */
/* Copyright 2024 AXELL CORPORATION */

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
	public class AiliaDisplayAudio
	{
		// Display pcm and vad confidence value
		private float[] displayWaveData = null;
		private float[] displayConfData = null;

		//Preview
		private Texture2D wave_texture = null;

		public Texture2D Create(){
			int tex_width = 480;
			int tex_height = 480;
			if (wave_texture == null)
			{
				wave_texture = new Texture2D(tex_width, tex_height);
			}
			return wave_texture;
		}

		private void DisplayPreviewPcm(Color32 [] colors, float [] waveData, float [] conf, uint channels, Color32 color){
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
					if (conf == null){
						displayConfData[i] = 0.0f;
					}else{
						displayConfData[i] = conf[(i - reuse_data_n) * channels];
					}
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
					colors[(y2+offset_y)*w+x] = color;
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

		public void DisplayPcm(float [] waveData, float [] conf, uint channels, bool active){
			Color32 [] colors = wave_texture.GetPixels32();
			Color32 color = new Color32(0,255,0,255);
			if (active == false){
				color =  new Color32(255,0,0,255);
			}
			DisplayPreviewPcm(colors, waveData, conf, channels, color);
			wave_texture.SetPixels32(colors);
			wave_texture.Apply();
		}

		public void DisplayVad(AiliaSileroVad.VadResult vad_result, List<AudioClip> vad_audio_clip, uint channels){

			Color32 [] colors = wave_texture.GetPixels32();
			Color32 color = new Color32(0,255,0,255);
			DisplayPreviewPcm(colors, vad_result.pcm, vad_result.conf, channels, color);
			for (int i = 0; i < vad_audio_clip.Count; i++){
				DisplayVadAudioClip(colors, vad_audio_clip[i], i, vad_audio_clip.Count);
			}
			wave_texture.SetPixels32(colors);
			wave_texture.Apply();
		}

	}
}