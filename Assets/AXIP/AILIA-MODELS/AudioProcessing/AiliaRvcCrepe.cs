/* AILIA Unity Plugin RVC Sample */
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
	public class AiliaRvcCrepe
	{
		private bool unit_test = false;
		AiliaModel f0_model = new AiliaModel();

		// Constructer
		public AiliaRvcCrepe(){
		}

		// Open model from onnx file
		public bool OpenFile(string f0_stream, string f0_weight, bool gpu_mode){
			Close();
			if (gpu_mode){
				f0_model.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
			}
			uint memory_mode = Ailia.AILIA_MEMORY_REDUCE_CONSTANT | Ailia.AILIA_MEMORY_REDUCE_CONSTANT_WITH_INPUT_INITIALIZER | Ailia.AILIA_MEMORY_REUSE_INTERSTAGE;
			f0_model.SetMemoryMode(memory_mode);
			bool status = f0_model.OpenFile(f0_stream, f0_weight);
			if (unit_test){
				UnitTest();
			}
			return status;
		}

		// Close model
		public void Close(){
			f0_model.Close();
		}

		// Filter
		private float [] Mean(float [] f0, int window_size){
			float [] mean = new float[f0.Length];
			for (int i = 0; i < f0.Length; i++){
				float sum = 0.0f;
				for (int j = 0; j < window_size; j++){
					int k = i + j - window_size/2;
					if (k < 0){
						k = 0;
					}
					if (k > f0.Length - 1){
						k = f0.Length - 1;
					}
					sum += f0[k];
				}
				mean[i] = sum / window_size;
			}
			return mean;
		}

		private float ConbertToFrequency(float [] probabilities, int t){
			// decoder.argmax
			int PITCH_BINS = 360;
			float max_prob = 0.0f;
			int max_i = 0;
			for (int i = 0; i < PITCH_BINS; i++){
				if (max_prob < probabilities[t * PITCH_BINS + i]){
					max_prob = probabilities[t * PITCH_BINS + i];
					max_i = i;
				}
			}

			float CENTS_PER_BIN = 20;
			int bins = max_i;
			float cents = CENTS_PER_BIN * bins + 1997.3794084376191f;
			//cents = dither(cents); // add noise
			float frequency = Mathf.Pow(10 * 2, (cents / 1200));
			return frequency;
		}

		// Crepe Pitch Detection
		private float [] Crepe(float [] x, int f0_len){
			float [] f0 = new float[f0_len];
			float [] pd = new float[f0_len];

			int sample_rate = 16000;
			int hop_length = sample_rate / 100;
			int WINDOW_SIZE = 1024;
			int total_frames = 1 + x.Length / hop_length;
			
			for (int i = 0; i < total_frames; i++){
				// create frame
				int pad = WINDOW_SIZE / 2;
				int start = i * hop_length - pad;
				float [] input = new float[WINDOW_SIZE];
				float sum = 0;
				for (int j = 0; j < WINDOW_SIZE; j++){
					int k = start + j;
					float v = 0;
					if (k >= 0 && k < x.Length){
						v = x[k];
					}
					input[j] = v;
					sum+=v;
				}

				// mean center and scale
				float mean = sum / WINDOW_SIZE;
				float norm = 0;
				for (int j = 0; j < WINDOW_SIZE; j++){
					input[j] = input[j] - mean;
					norm += input[j] * input[j];
				}
				norm = Mathf.Sqrt(norm);
				if (norm < (float)(1e-10)){
					norm = (float)(1e-10);
				}
				for (int j = 0; j < WINDOW_SIZE; j++){
					input[j] = input[j] / norm;
				}

				// infer
				Ailia.AILIAShape sequence_shape = new Ailia.AILIAShape();
				sequence_shape.x=(uint)WINDOW_SIZE;
				sequence_shape.y=1;
				sequence_shape.z=1;
				sequence_shape.w=1;
				sequence_shape.dim=2;
				uint[] input_blobs = f0_model.GetInputBlobList();
				f0_model.SetInputBlobShape(sequence_shape, (int)input_blobs[0]);
				uint[] output_blobs = f0_model.GetOutputBlobList();
				Ailia.AILIAShape output_blob_shape = f0_model.GetBlobShape((int)output_blobs[0]);
				float [] probabilities = new float[output_blob_shape.x * output_blob_shape.y * output_blob_shape.z * output_blob_shape.w];
				f0_model.Predict(probabilities, input);

				//Convert probabilities to F0 and periodicity
				for (int t = 0; t < output_blob_shape.y; t++){
					float frequency = ConbertToFrequency(probabilities, t);
					if (t + i * hop_length < f0.Length){
						f0[t + i * hop_length] = frequency;
					}
				}

			}

			// filter
			//pd = torchcrepe.filter.median(pd, 3)
			f0 = Mean(f0, 3);
			//for (int i = 0; i < f0.Length; i++){
			//	if (pd[i] < 0.1);
			//		f0[i] = 0:
			//	}
			//}

			return f0;
		}

		// Pitch Detection : pcm is 16khz
		public void GetF0(float [] f0_coarse, float [] f0bak, float [] pcm, int f0_up_key){
			// Infer
			float [] f0 = Crepe(pcm, f0_coarse.Length);

			// Pitch shift
			float f0_min = 50;
			float f0_max = 1100;
			float f0_mel_min = 1127 * Mathf.Log(1 + f0_min / 700);
			float f0_mel_max = 1127 * Mathf.Log(1 + f0_max / 700);

			for (int i = 0; i < f0.Length; i++){
				f0[i] = f0[i] * Mathf.Pow(2, f0_up_key / 12.0f);
				f0bak[i] = f0[i];
			}

			// Calc quantized pitch
			for (int i = 0; i < f0.Length; i++){
				float f0_mel = 1127 * Mathf.Log(1 + f0[i] / 700);
				if (f0_mel > 0){
					f0_mel = (f0_mel - f0_mel_min) * 254 / (f0_mel_max - f0_mel_min) + 1;
					f0_mel = Mathf.Round(f0_mel);
				}
				if (f0_mel <= 1){
					f0_mel = 1;
				}
				if (f0_mel > 255){
					f0_mel = 255;
				}
				f0_coarse[i] = f0_mel;
			}
		}

		// UnitTest
		public void UnitTest(){
			float [] pcm = new float[2048];
			int len = 128;
			float [] f0 = Crepe(pcm, len);
		}
	}
}