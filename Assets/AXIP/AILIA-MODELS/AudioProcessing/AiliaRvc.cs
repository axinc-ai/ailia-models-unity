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

		// Model
		private AiliaModel hubert_model = new AiliaModel();
		private AiliaModel vc_model = new AiliaModel();

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

			// Hubert
			float[] hubert_input = new float[GetSamples(clip)];
			float[] hubert_padding_mask = new float[hubert_input.Length]; // zeros

			float[] hubert_output = new float[BATCH_SIZE];

			Resample(hubert_input, hubert_padding_mask, clip);

			// Create Inference Buffer
			List<float[]> hubert_inputs = new List<float[]>();
			hubert_inputs.Add(hubert_input);
			hubert_inputs.Add(hubert_padding_mask);
			
			List<float[]> hubert_outputs = new List<float[]>();
			hubert_outputs.Add(hubert_output);

			// Hubert Inference
			bool status = Forward(hubert_model, hubert_inputs, hubert_outputs, true);
			if (status == false){
				Debug.Log("Forward failed");
				return null;
			}

			// Interpolate
			float [] feats = Interpolate(hubert_output);

			// VC
			int len = feats.Length / FEAT_SIZE;

			float [] p_len = new float[1];
			float [] sid = new float[1];
			float [] rnd = new float[RND_SIZE * len];

			float[] vc_output = new float[BATCH_SIZE];

			p_len[0] = len;
			sid[0] = 0;
			Randn(rnd);

			// VC Inference
			List<float[]> vc_inputs = new List<float[]>();
			vc_inputs.Add(feats);
			vc_inputs.Add(p_len);
			vc_inputs.Add(sid);
			vc_inputs.Add(rnd);
				
			List<float[]> vc_outputs = new List<float[]>();
			vc_outputs.Add(vc_output);

			Forward(vc_model, vc_inputs, vc_outputs, false);

			// Prevent overflow
			Clip(vc_output);

			// Create new Audio Clip
			AudioClip newClip = AudioClip.Create("Segment", vc_output.Length, 1, 40000, false);
			newClip.SetData(vc_output, 0);
			return newClip;
		}

		// Resampler
		private void Resample(float [] hubert_input, float [] hubert_padding_mask, AudioClip clip){
			float [] pcm = new float[clip.samples * clip.channels];
			clip.GetData(pcm, 0);
			int sampleRate = clip.frequency;
			int targetSampleRate = 16000;
			float [] conf = new float[pcm.Length];

			// Resampling to targetSampleRate
			for (int i = 0; i < hubert_input.Length; i++){
				int i2 = i * sampleRate / targetSampleRate;
				if (i2 < pcm.Length){
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
			return clip.samples * targetSampleRate / sampleRate;
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
		private bool Forward(AiliaModel ailia_model, List<float[]> inputs, List<float[]> outputs, bool hubert){
			bool success;
			
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
						sequence_shape.x=(uint)inputs[i].Length / FEAT_SIZE / (uint)BATCH_SIZE;
						sequence_shape.y=FEAT_SIZE;
						sequence_shape.z=(uint)BATCH_SIZE;
						sequence_shape.w=1;
						sequence_shape.dim=3;
					}
				}

				success = ailia_model.SetInputBlobShape(sequence_shape, (int)input_blob_idx);
				if (success == false){
					Debug.Log("SetInputBlobShape failed");
					return false;
				}
				success = ailia_model.SetInputBlobData(inputs[i], (int)input_blob_idx);
				if (success == false){
					Debug.Log("SetInputBlobData failed");
					return false;
				}
			}

			// Inference
			success = ailia_model.Update();
			if (success == false) {
				Debug.Log("Update failed");
				return false;
			}

			// Get outpu blob shape and get output blob data
			uint[] output_blobs = ailia_model.GetOutputBlobList();

			for (int i = 0; i < outputs.Count; i++){
				uint output_blob_idx = output_blobs[i];
				
				Ailia.AILIAShape output_blob_shape = ailia_model.GetBlobShape((int)output_blob_idx);
				success = ailia_model.GetBlobData(outputs[i], (int)output_blob_idx);
				if (success == false){
					Debug.Log("GetBlobData failed");
					return false;
				}
			}

			return true;
		}
	}
}