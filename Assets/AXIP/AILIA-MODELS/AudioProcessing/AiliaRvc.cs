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
	public class AiliaRVC
	{
		// Model config
		private const int NUM_INPUTS = 4;
		private const int NUM_OUTPUTS = 3;

		// Model input parameter
		private const int batch = 1;
		private const int sequence = 1536;

		// Model
		private AiliaModel hubert = new AiliaModel();
		private AiliaModel vc = new AiliaModel();

		// Constructer
		public AiliaRVC(){
			ResetState();
		}

		// Open model from onnx file
		public bool OpenFile(string hubert_stream, string hubert_weight, string vc_stream, string vc_weight, bool gpu_mode){
			Close();
			if (gpu_mode)
			{
				hubert.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
				vc.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
			}
			bool status = hubert.OpenFile(hubert_stream, hubert_weight);
			if (!status){
				return status;
			}
			return vc.OpenFile(vc_stream, vc_weight);
		}

		// Close model
		public void Close(){
			hubert.Close();
			vc.Close();
		}

		// Get backend environment name
		public string EnvironmentName(){
			return hubert.EnvironmentName();
		}

		// Voice convert
		public AudioClip Process(AudioClip clip)
		{
			// Get PCM
			if (clip.channels != 1){
				Debug.Log("channel must be 1");
				return null;
			}
			float pcm[] = new float[clip.samples * clip.channels];
			clip.GetData(pcm, 0);

			//hubert.run(None, {'source': feats, 'padding_mask': padding_mask})
			//vc.run(None, {'phone': feats, 'phone_lengths': p_len, 'ds': sid, 'rnd': rnd})

			float[] input = new float[clip.samples * targetSampleRate / sampleRate];
			float[] padding_mask = new float[input.Length]; // zeros

			// Create Inference Buffer
			List<float[]> inputs = new List<float[]>();
			inputs.Add(input);
			inputs.Add(padding_mask);

			float[] output = new float[batch];
				
			List<float[]> outputs = new List<float[]>();
			outputs.Add(output);

			// Hubert
			int sampleRate = clip.frequency;
			int targetSampleRate = 16000;
			float [] conf = new float[pcm.Length];
			int s;
			// Resampling to targetSampleRate
			for (int i = 0; i < input.Length; i++){
				int i2 = i * sampleRate / targetSampleRate;
				if (s + i2 < pcm.Length){
					input[i] = pcm[s + i2];
				}else{
					input[i] = 0;
				}
				padding_mask[i] = 0;
			}

			// Inference
			bool status = Forward(inputs, outputs, true);
			if (status == false){
				Debug.Log("Forward failed");
				return null;
			}

			// VC

			// Create new Audio Clip



		}

		// Infer one frame using ailia SDK
		private bool Forward(List<float[]> inputs, List<float[]> outputs, bool hubert){
			bool success;
			
			// Set input blob shape and set input blob data
			uint[] input_blobs = ailia_model.GetInputBlobList();

			for (int i = 0; i < NUM_INPUTS; i++){
				uint input_blob_idx = input_blobs[i];

				Ailia.AILIAShape sequence_shape = new Ailia.AILIAShape();
				if (hubert){
					sequence_shape.x=(uint)inputs[i].Length / (uint)batch;
					sequence_shape.y=(uint)batch;
					sequence_shape.z=1;
					sequence_shape.w=1;
					sequence_shape.dim=2;
				}else{
					if ( i == 0 ){
						sequence_shape.x=(uint)inputs[i].Length / (uint)batch;
						sequence_shape.y=(uint)batch;
						sequence_shape.z=1;
						sequence_shape.w=1;
						sequence_shape.dim=2;
					}
					if ( i == 1 ){
						sequence_shape.x=(uint)inputs[i].Length;
						sequence_shape.y=1;
						sequence_shape.z=1;
						sequence_shape.w=1;
						sequence_shape.dim=1;
					}
					if ( i == 2 || i == 3){
						sequence_shape.x=(uint)inputs[i].Length / (uint)batch / 2;
						sequence_shape.y=(uint)batch;
						sequence_shape.z=2;
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

			for (int i = 0; i < NUM_OUTPUTS; i++){
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