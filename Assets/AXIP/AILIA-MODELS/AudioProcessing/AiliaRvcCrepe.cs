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
		private bool unit_test = true;
		AiliaModel f0_model = new AiliaModel();

		// Model parameter
		const int SAMPLE_RATE = 16000;
		const int WINDOW_SIZE = 1024;
		const int PITCH_BINS = 360;
		const int HOP_LENGTH = SAMPLE_RATE / 100; // 10ms

		// Constructer
		public AiliaRvcCrepe(){
		}

		// Open model from onnx file
		public bool OpenFile(string f0_stream, string f0_weight, bool gpu_mode){
			Close();
			if (gpu_mode){
				f0_model.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
			}else{
				Debug.LogWarning("With ailia SDK 1.2.15 crepe is very slow when run on cpu. Highly recommend to use your gpu. It will be improved in ailia SDK 1.2.16.");
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

		private float [] Median(float [] f0, int window_size){
			float [] median_list = new float[f0.Length];
			for (int i = 0; i < f0.Length; i++){
				float [] median = new float[window_size];
				for (int j = 0; j < window_size; j++){
					int k = i + j - window_size/2;
					if (k < 0){
						k = 0;
					}
					if (k > f0.Length - 1){
						k = f0.Length - 1;
					}
					median[j] = f0[k];
				}
				Array.Sort(median);
				median_list[i] = median[window_size / 2];
			}
			return median_list;
		}

		private void Decode(float [] probabilities, float [] f0, float [] pd, int output_blob_shape_y, int b){
			/*
			# Convert frequency range to pitch bin range
			minidx = frequency_to_bins(np.array(fmin))
			maxidx = frequency_to_bins(np.array(fmax), np.ceil)

			# Remove frequencies outside of allowable range
			probabilities[:, :minidx] = -float('inf')
			probabilities[:, maxidx:] = -float('inf')
			*/

			//DecodeArgMax(probabilities, f0, pd, output_blob_shape_y, b);
			DecodeViterbi(probabilities, f0, pd, output_blob_shape_y, b);
		}

		private void DecodeArgMax(float [] probabilities, float [] f0, float [] pd, int output_blob_shape_y, int b){
			//Convert probabilities to F0 and periodicity
			for (int t = 0; t < output_blob_shape_y; t++){
				int bins = DecodeArgMaxBin(probabilities, t);
				float periodicity = Periodicity(probabilities, t, bins);
				float frequency = ConbertToFrequency(bins);
				if (b + t < f0.Length){
					f0[b + t] = frequency;
					pd[b + t] = periodicity;
				}
			}
		}

		private int DecodeArgMaxBin(float [] probabilities, int t){
			float max_prob = 0.0f;
			int max_i = 0;
			for (int i = 0; i < PITCH_BINS; i++){
				if (max_prob < probabilities[t * PITCH_BINS + i]){
					max_prob = probabilities[t * PITCH_BINS + i];
					max_i = i;
				}
			}
			int bins = max_i;
			return bins;
		}

		private void Softmax(float [] data, int bins, int n_steps){
			for (int t = 0; t < n_steps; t++){
				float sum=0;
				for(int i = 0; i < bins; i++){
					sum += Mathf.Exp(data[i + t * bins]);
				}
				for(int i = 0; i < bins; i++){
					data[i + t * bins] = Mathf.Exp(data[i + t * bins]) / sum;
				}
			}
		}

		private float [,] Transition(){
			float [,] transition = new float [PITCH_BINS, PITCH_BINS];
			for (int y = 0; y < PITCH_BINS; y++){
				float sum = 0;
				for (int x = 0; x < PITCH_BINS; x++){
					int v = 12 - Math.Abs(x - y);
					if (v < 0){
						v = 0;
					}
					transition[y, x] = v;
					sum += v;
				}
				for (int x = 0; x < PITCH_BINS; x++){
					transition[y, x] = transition[y, x] / sum;
				}
			}
			return transition;
		}

		private void DecodeViterbi(float [] probabilities, float [] f0, float [] pd, int n_steps, int b){
			// Apply Softmax
			float [] log_prob = new float [probabilities.Length];
			for (int i = 0; i < probabilities.Length; i++){
				log_prob[i] = probabilities[i];
			}
			Softmax(log_prob, PITCH_BINS, n_steps);

			// Create cost function
			float [,] log_trans = Transition();

			// Create state
			const int n_states = PITCH_BINS;
			float [,] value = new float[n_steps, n_states];
			int [,] ptr = new int[n_steps, n_states];
			float [] log_p_init = new float[n_states];

			// Apply log
			for (int i = 0; i < n_states; i++){
				log_p_init[i] = Mathf.Log(1.0f / n_states);
			}
			for (int i = 0; i < log_prob.Length; i++){
				log_prob[i] = Mathf.Log(log_prob[i]);
			}
			for (int y = 0; y < n_states; y++){
				for (int x = 0; x < n_states; x++){
					log_trans[y, x] = Mathf.Log(log_trans[y, x]);
				}
			}
			if (unit_test){
				DumpTensor("LogProb", log_prob);
				DumpTensor("LogProbInit", log_p_init);
				DumpTensor2D("LogTrans", log_trans, n_states, n_states);
			}

			// Viterbi algorithm
			for (int i = 0; i < n_states; i++){
				value[0, i] = log_prob[0 * n_states + i] + log_p_init[i];
			}

			for (int t = 1; t < n_steps; t++){		
				// Tout[k, j] = V[t-1, k] * A[k, j] (mul to add by log plane)
				float [,] trans_out = new float [n_states, n_states];
				for (int y = 0; y < n_states; y++){
					for (int x = 0; x < n_states; x++){
						trans_out[y, x] = value[t - 1, x] + log_trans[x, y]; // Transposed matrix
					}
				}
				for (int j = 0; j < n_states; j++){
					// argmax
					int max_i = 0;
					float max_prob = Mathf.NegativeInfinity;
					for (int k = 0; k < n_states; k++){
						if (max_prob < trans_out[j, k]){
							max_prob = trans_out[j, k];
							max_i = k;
						}
					}
					ptr[t, j] = max_i;
					value[t, j] = log_prob[t * n_states + j] + trans_out[j, ptr[t, j]];
				}
			}

			// Backward final state
			int max_i2 = 0;
			float max_prob2 = Mathf.NegativeInfinity;
			for (int k = 0; k < n_states; k++){
				if (max_prob2 < value[n_steps - 1, k]){
					max_prob2 = value[n_steps - 1, k];
					max_i2 = k;
				}
			}
			Debug.Log("MaxI " + max_i2 + " MaxProb2 " + max_prob2);

			int [] state = new int[n_steps];
			state[n_steps - 1] = max_i2;
			for (int t = n_steps - 2 ; t >= 0; t--){
				state[t] = ptr[t + 1, state[t + 1]];
			}

			// Convert to f0 value
			for (int t = 0; t < n_steps; t++){
				int bins = state[t];
				float periodicity = Periodicity(probabilities, t, bins);
				float frequency = ConbertToFrequency(bins);
				if (b + t < f0.Length){
					f0[b + t] = frequency;
					pd[b + t] = periodicity;
				}
			}
		}

		private float Periodicity(float [] probablities, int t, int bins){
			return probablities[t * PITCH_BINS + bins];
		}

		private float ConbertToFrequency(int bins){
			float CENTS_PER_BIN = 20;
			float cents = CENTS_PER_BIN * bins + 1997.3794084376191f;
			//cents = dither(cents); // add noise
			float frequency = 10 * Mathf.Pow(2, (cents / 1200));

			if (unit_test){
				Debug.Log("bins "+bins+" cents "+cents+" frequency "+frequency);
			}

			return frequency;
		}

		// Normalize
		private void Normalize(float [] input){
			// mean center and scale
			float sum = 0;
			for (int j = 0; j < input.Length; j++){
				sum += input[j];
			 }
			float mean = sum / input.Length;
			float std_value = 0;
			for (int j = 0; j < input.Length; j++){
				input[j] = input[j] - mean;
				std_value += input[j] * input[j];
			}
			std_value = std_value / input.Length;
			std_value = Mathf.Sqrt(std_value);
			if (std_value < (float)(1e-10)){
				std_value = (float)(1e-10);
			}
			for (int j = 0; j < input.Length; j++){
				input[j] = input[j] / std_value;
			}
		}

		// Crepe Pitch Detection
		private float [] Crepe(float [] x, int f0_len){
			float [] f0 = new float[f0_len];
			float [] pd = new float[f0_len];

			if (unit_test){
				DumpTensor("x", x);
			}

			int total_frames = 1 + x.Length / HOP_LENGTH;
			int batch_size = 512;

			float [] input_batch = new float[batch_size * WINDOW_SIZE];
			for (int b = 0; b < total_frames; b += batch_size){
				int current_barth_size = total_frames - b;
				if (current_barth_size > batch_size){
					current_barth_size = batch_size;
				}
				for (int i = b; i < b + current_barth_size; i++){
					// create frame
					float [] input = new float[WINDOW_SIZE];
					int pad = WINDOW_SIZE / 2;
					int start = i * HOP_LENGTH - pad;
					for (int j = 0; j < WINDOW_SIZE; j++){
						int k = start + j;
						float v = 0;
						if (k < 0){ // reflection
							k = -k;
						}
						if (k >= x.Length){ // reflection
							k = x.Length - 1 - (x.Length - k);
						}
						if (k >= 0 && k < x.Length){
							v = x[k];
						}
						input[j] = v;
					}

					if (unit_test){
						DumpTensor("input", input);
					}

					Normalize(input);

					if (unit_test){
						DumpTensor("normalized input", input);
					}

					for (int j = 0; j < WINDOW_SIZE; j++){
						input_batch[j + (i - b) * WINDOW_SIZE] = input[j];
					}
				}
				if (unit_test){
					DumpTensor("input_batch", input_batch);
				}

				// infer
				// input is 1024 sample, output is 1 frequency
				// finally, calc f0 value per 10ms
				Ailia.AILIAShape sequence_shape = new Ailia.AILIAShape();
				sequence_shape.x=(uint)WINDOW_SIZE;
				sequence_shape.y=(uint)current_barth_size;
				sequence_shape.z=1;
				sequence_shape.w=1;
				sequence_shape.dim=2;
				uint[] input_blobs = f0_model.GetInputBlobList();
				bool status = f0_model.SetInputBlobShape(sequence_shape, (int)input_blobs[0]);
				if (!status){
					Debug.Log("SetInputBlobShape failed\n");
					return f0;
				}
				uint[] output_blobs = f0_model.GetOutputBlobList();
				Ailia.AILIAShape output_blob_shape = f0_model.GetBlobShape((int)output_blobs[0]);
				Debug.Log("Output blob shape " + output_blob_shape.x + " " + output_blob_shape.y + " " + output_blob_shape.z + " " + output_blob_shape.w);
				float [] probabilities = new float[output_blob_shape.x * output_blob_shape.y * output_blob_shape.z * output_blob_shape.w];
				status = f0_model.Predict(probabilities, input_batch);
				if (!status){
					Debug.Log("f0_model.Predict failed\n");
					return f0;
				}
				if (unit_test){
					DumpTensor("probabilities", probabilities);
				}

				Decode(probabilities, f0, pd, (int)output_blob_shape.y, b);
			}

			// filter
			if (unit_test){
				DumpTensor("pd", pd);
				DumpTensor("f0", f0);
			}

			pd = Median(pd, 3);
			f0 = Mean(f0, 3);

			if (unit_test){
				DumpTensor("median_pd", pd);
				DumpTensor("mean_f0", f0);
			}

			for (int i = 0; i < f0.Length; i++){
				if (pd[i] < 0.1){
					f0[i] = 0;
				}
			}

			return f0;
		}

		// Dump tensor
		private void DumpTensor(string label, float [] value){
			string result = "";
			int len = value.Length;
			if (len > 512){
				len = 512;
			}
			for (int i = 0; i < len; i++){
				result+=value[i] + " , ";
			}
			Debug.Log(label + " value : " + result);
		}

		private void DumpTensor2D(string label, float [,] value, int height, int width){
			string result = "";
			for (int y = 0; y < height; y++){
				if (y > 10){
					continue;
				}
				result+="line."+y+"\n";
				for (int x = 0; x < width; x++){
					if (x > 10){
						continue;
					}
					result+=value[y, x] + " , ";
				}
				result+="\n";
			}
			Debug.Log(label + " values :\n" + result);
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
			float [] pcm = new float[WINDOW_SIZE * 2];
			for (int i = 0; i < pcm.Length; i++){
				float hz = 1000;
				pcm[i] = Mathf.Sin(Mathf.PI * 2 * hz * i / SAMPLE_RATE);
			}
			int f0_len = 1 + pcm.Length / HOP_LENGTH;
			float [] f0 = Crepe(pcm, f0_len);
			DumpTensor("f0", f0);
#if UNITY_EDITOR
			Debug.Log("Finish unit test");
			UnityEditor.EditorApplication.isPlaying = false;
#endif
		}
	}
}