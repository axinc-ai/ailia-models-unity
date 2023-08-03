/* AILIA Unity Plugin SileroVAD Sample */
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
	public class AiliaSplitAudio
	{
		float ACTIVE_SEC = 0.25f;	// If the pronunciation continues for this number of seconds, it is considered a valid AudioClip
		float SILENT_SEC = 0.1f;	// Split the AudioClip after this number of seconds of silence
		float THRESHOLD = 0.5f;		// VAD threshold

		const int STATE_EMPTY = 0;
		const int STATE_ACTIVE = 1;
		const int STATE_SILENT = 2;
		const int STATE_FINISH = 3;

		List<float> pcm = null;
		List<float> conf = null;
		List<AudioClip> clip = null;

		public AiliaSplitAudio(){
			Reset();
		}

		// Reset internel state
		public void Reset(){
			pcm = new List<float>();
			conf = new List<float>();
			clip = new List<AudioClip>();
		}

		// Split input audio to List of AudioClip using PCM and Confidence value
		public void Split(AiliaSileroVad.VadResult wave){
			// Push new pcm and confidence value
			for (int i = 0; i < wave.pcm.Length; i++){
				pcm.Add(wave.pcm[i]);
				conf.Add(wave.conf[i]);
			}

			// AudioClip when there is silence for a certain period of time after a certain period of sound
			int active_cnt = 0;
			int silent_cnt = 0;
			int start_i = 0;
			int end_i = 0;
			int state = STATE_EMPTY;
			for (int i = 0; i < pcm.Count; i++){
				// Silent -> Active
				if (state == STATE_EMPTY){
					if (conf[i] > THRESHOLD){
						start_i = i;
						active_cnt++;
						state = STATE_ACTIVE;
					}
					continue;
				}

				// Active -> Silent
				if (state == STATE_ACTIVE){
					if (conf[i] > THRESHOLD){
						active_cnt++;
					}else{
						if (active_cnt >= ACTIVE_SEC * wave.sampleRate){
							state = STATE_SILENT;
							silent_cnt = 0;
						}else{
							state = STATE_EMPTY;
						}
					}
					continue;
				}

				// Silent -> Active or Finish
				if (state == STATE_SILENT){
					if (conf[i] > THRESHOLD){
						state = STATE_ACTIVE;
					}else{
						silent_cnt++;
						if (silent_cnt >= SILENT_SEC * wave.sampleRate){
							state = STATE_FINISH;
							end_i = i;
							break;
						}
					}
				}
			}

			// Generate new AudioClip
			if (state == STATE_FINISH){
				int channels = 1;
				AudioClip newClip = AudioClip.Create("Segment", end_i - start_i, channels, wave.sampleRate, false);
				float [] newData = new float[end_i - start_i];
				for (int i = start_i; i < end_i; i++){
					newData[i - start_i] = pcm[i];
				}
				newClip.SetData(newData, 0);
				clip.Add(newClip);

				pcm.RemoveRange(0, end_i);
				conf.RemoveRange(0, end_i);
			}
		}

		// Get count of audio clip
		public int GetAudioClipCount(){
			return clip.Count;
		}

		// Pop older audio clip
		public AudioClip PopAudioClip(){
			if (clip.Count > 0){
				AudioClip ret = clip[0];
				clip.RemoveAt(0);
				return ret;
			}
			return null;
		}
	}
}