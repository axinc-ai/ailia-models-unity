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
	public class AiliaRvc
	{
		// Model input parameter
		private const int BATCH_SIZE = 1;
		private const int FEAT_SIZE = 256;
		private const int RND_SIZE = 192;
		private const int T_PAD = 48000;
		private const int T_PAD_TGT = 120000;
		
		// Model
		private AiliaModel hubert_model = new AiliaModel();
		private AiliaModel vc_model = new AiliaModel();

		// Debug
		private bool debug = false;

		// Constructer
		public AiliaRvc(){
		}

		// Open model from onnx file
		public bool OpenFile(string hubert_stream, string hubert_weight, string vc_stream, string vc_weight, bool gpu_mode){
			Close();
			if (gpu_mode)
			{
				hubert_model.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
				vc_model.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
			}
			bool status = hubert_model.OpenFile(hubert_stream, hubert_weight);
			if (!status){
				return status;
			}
			return vc_model.OpenFile(vc_stream, vc_weight);
		}

		// Close model
		public void Close(){
			hubert_model.Close();
			vc_model.Close();
		}

		// Get backend environment name
		public string EnvironmentName(){
			return hubert_model.EnvironmentName();
		}

		// Voice convert
		public AudioClip Process(AudioClip clip)
		{
			// Get PCM
			if (clip.channels != 1){
				Debug.Log("channel must be 1");
				return null;
			}
			if (clip.samples <= 0){
				Debug.Log("samples must be greater than 1");
				return null;
			}

			// Hubert
			float[] hubert_input = new float[GetSamples(clip)];
			float[] hubert_padding_mask = new float[hubert_input.Length]; // zeros

			Resample(hubert_input, hubert_padding_mask, clip);

			// Create Inference Buffer
			List<float[]> hubert_inputs = new List<float[]>();
			hubert_inputs.Add(hubert_input);
			hubert_inputs.Add(hubert_padding_mask);
			
			// Hubert Inference
			List<float[]> hubert_outputs = Forward(hubert_model, hubert_inputs, true);
			float [] hubert_output = hubert_outputs[0];

			// Interpolate
			float [] feats = Interpolate(hubert_output);

			// VC
			int len = feats.Length / FEAT_SIZE;

			float [] p_len = new float[1];
			float [] sid = new float[1];
			float [] rnd = new float[RND_SIZE * len];

			p_len[0] = len;
			sid[0] = 0;
			Randn(rnd);

			// VC Inference
			List<float[]> vc_inputs = new List<float[]>();
			vc_inputs.Add(feats);
			vc_inputs.Add(p_len);
			vc_inputs.Add(sid);
			vc_inputs.Add(rnd);
				
			List<float[]> vc_outputs = Forward(vc_model, vc_inputs, false);
			float [] vc_output = vc_outputs[0];

			// Prevent overflow
			Clip(vc_output);

			// Trim
			float[] trm = new float[vc_output.Length - T_PAD_TGT*2];
			for (int i = 0; i < trm.Length; i++){
				trm[i] = vc_output[i + T_PAD_TGT];
			}

			// Create new Audio Clip
			AudioClip newClip = AudioClip.Create("Segment", trm.Length, 1, 40000, false);
			newClip.SetData(trm, 0);
			return newClip;
		}

		// Resampler
		private void Resample(float [] hubert_input, float [] hubert_padding_mask, AudioClip clip){
			// Get PCM
			float [] pcm = new float[clip.samples * clip.channels];
			clip.GetData(pcm, 0);
			int sampleRate = clip.frequency;
			int targetSampleRate = 16000;

			// Resampling to targetSampleRate
			for (int i = 0; i < hubert_input.Length; i++){
				float rate = 1.0f * sampleRate / targetSampleRate;
				int i2 = (int)((i - T_PAD) * rate);
				if (i2 < 0){
					i2 = -i2;// reflection
				}
				if (i2 >= pcm.Length){
					i2 = pcm.Length- (i2 - pcm.Length);
				}
				if (i2 >= 0 && i2 < pcm.Length){
					hubert_input[i] = pcm[i2];
				}else{
					hubert_input[i] = 0;
				}
				hubert_padding_mask[i] = 0;
			}
		}

		private int GetSamples(AudioClip clip){
			int sampleRate = clip.frequency;
			int targetSampleRate = 16000;
			float rate = 1.0f * targetSampleRate / sampleRate;
			return (int)(clip.samples * rate) + T_PAD * 2;
		}

		// Interpolate
		private float [] Interpolate(float [] hubert_output){
			float [] feats = new float [hubert_output.Length * 2];
			for (int i = 0; i < hubert_output.Length / FEAT_SIZE; i++){
				for (int j = 0; j < FEAT_SIZE; j++){
					feats[(i * 2 + 0) * FEAT_SIZE + j] = hubert_output[i * FEAT_SIZE + j];
					feats[(i * 2 + 1) * FEAT_SIZE + j] = hubert_output[i * FEAT_SIZE + j];
				}
			}
			return feats;
		}

		// Random
		private void Randn(float [] rnd){
			System.Random random = new System.Random(); 
			for (int i = 0; i < rnd.Length; i++){
				// randn
				float X = (float)random.NextDouble(); 
				float Y = (float)random.NextDouble(); 
				float Z1 =(float)(Math.Sqrt(-2.0 * Math.Log(X)) * Math.Cos(2.0 * Math.PI * Y)); 
				float Z2 =(float)( Math.Sqrt(-2.0 * Math.Log(X)) * Math.Sin(2.0 * Math.PI * Y));
				rnd[i] = Z1 * 0.66666f;
			}

		}

		// CLIP
		private void Clip(float[] vc_output){
			float max_value = 0;
			for (int i = 0; i < vc_output.Length; i++){
				if (max_value < Mathf.Abs(vc_output[i])){
					max_value = Mathf.Abs(vc_output[i]);
				}
			}
			max_value = max_value * 0.99f;
			if (max_value > 1){
				for (int i = 0; i < vc_output.Length; i++){
					vc_output[i] = vc_output[i] / max_value;
				}
			}
		}

		// Infer one frame using ailia SDK
		private List<float[]> Forward(AiliaModel ailia_model, List<float[]> inputs, bool hubert){
			bool success;
			List<float[]> outputs = new List<float[]>();
			
			// Set input blob shape and set input blob data
			uint[] input_blobs = ailia_model.GetInputBlobList();

			for (int i = 0; i < inputs.Count; i++){
				uint input_blob_idx = input_blobs[i];

				Ailia.AILIAShape sequence_shape = new Ailia.AILIAShape();
				if (hubert){
					sequence_shape.x=(uint)inputs[i].Length / (uint)BATCH_SIZE;
					sequence_shape.y=(uint)BATCH_SIZE;
					sequence_shape.z=1;
					sequence_shape.w=1;
					sequence_shape.dim=2;
				}else{
					if ( i == 0 ){
						sequence_shape.x=FEAT_SIZE;
						sequence_shape.y=(uint)inputs[i].Length / FEAT_SIZE / (uint)BATCH_SIZE;
						sequence_shape.z=(uint)BATCH_SIZE;
						sequence_shape.w=1;
						sequence_shape.dim=3;
					}
					if ( i == 1 || i == 2){
						sequence_shape.x=1;
						sequence_shape.y=1;
						sequence_shape.z=1;
						sequence_shape.w=1;
						sequence_shape.dim=1;
					}
					if ( i == 3){
						sequence_shape.x=(uint)inputs[i].Length / RND_SIZE / (uint)BATCH_SIZE;
						sequence_shape.y=RND_SIZE;
						sequence_shape.z=(uint)BATCH_SIZE;
						sequence_shape.w=1;
						sequence_shape.dim=3;
					}
				}

				if (debug){
					Debug.Log("Input "+i+" Shape "+sequence_shape.w+","+sequence_shape.z+","+sequence_shape.y+","+sequence_shape.x+" dim "+sequence_shape.dim);
				}

				success = ailia_model.SetInputBlobShape(sequence_shape, (int)input_blob_idx);
				if (success == false){
					Debug.Log("SetInputBlobShape failed");
					return null;
				}
				success = ailia_model.SetInputBlobData(inputs[i], (int)input_blob_idx);
				if (success == false){
					Debug.Log("SetInputBlobData failed");
					return null;
				}
			}

			// Inference
			success = ailia_model.Update();
			if (success == false) {
				Debug.Log("Update failed");
				return null;
			}

			// Get outpu blob shape and get output blob data
			uint[] output_blobs = ailia_model.GetOutputBlobList();
			for (int i = 0; i < output_blobs.Length; i++){
				uint output_blob_idx = output_blobs[i];
				
				Ailia.AILIAShape output_blob_shape = ailia_model.GetBlobShape((int)output_blob_idx);
				if (debug){
					Debug.Log("Output "+i+" Shape "+output_blob_shape.w+","+output_blob_shape.z+","+output_blob_shape.y+","+output_blob_shape.x+" dim "+output_blob_shape.dim);
				}

				float [] output = new float[output_blob_shape.x * output_blob_shape.y * output_blob_shape.z * output_blob_shape.w];

				success = ailia_model.GetBlobData(output, (int)output_blob_idx);
				if (success == false){
					Debug.Log("GetBlobData failed");
					return null;
				}

				outputs.Add(output);
			}

			return outputs;
		}
	}
}