/* AILIA Unity Plugin Microphone Sample */
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
	public class AiliaMicrophone
	{
		private string targetDevice = "";

		private string m_DeviceName = "";
		private AudioClip m_AudioClip;
		private int m_LastAudioPos;
		private int input_pointer = 0;
		private bool m_mic_mode = false;

		public void InitializeMic(bool mic_mode, AudioClip audio_clip){
			if (m_AudioClip != null){
				return;
			}

			m_mic_mode = mic_mode;

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

		public void DestroyMic(){
			if (m_AudioClip == null){
				return;
			}
			if (m_mic_mode == true){
				Microphone.End(targetDevice);
				AudioClip.Destroy(m_AudioClip);
			}
			m_AudioClip = null;
		}

		// Get Pcm
		public float [] GetPcm(ref uint channels, ref uint frequency){
			channels = (uint)m_AudioClip.channels;
			frequency = (uint)m_AudioClip.frequency;

			int input_step = (int)(Time.deltaTime * frequency);
			if (input_step < 1){
				input_step = 1;
			}
			float [] waveData = GetPcmCore(input_step);
			if (m_mic_mode == true){
				input_pointer += (int)(waveData.Length / channels);
			}
			return waveData;
		}

		private float [] GetPcmCore(int input_step){
			float [] waveData;
			waveData = new float[0];
			if (m_mic_mode == false){
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
			if (m_mic_mode == true){
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

		public bool IsComplete(){
			if (m_mic_mode == true){
				return false;
			}
			return (input_pointer >= m_AudioClip.samples);
		}
	}
}