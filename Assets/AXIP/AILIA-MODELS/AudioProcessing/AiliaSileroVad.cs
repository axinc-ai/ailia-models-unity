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
	public class AiliaSileroVad
	{
		private const int NUM_INPUTS = 4;
		private const int NUM_OUTPUTS = 3;

		private const int batch = 1;
		private const int sequence = 1536;

		private float[] input;
		private float[] sr;
		private float[] h;
		private float[] c;

		private float[] remain_pcm;
		private AiliaModel ailia_model = new AiliaModel();

		public AiliaSileroVad(){
			ResetState();
		}

		public bool OpenFile(string stream, string weight, bool gpu_mode){
			Close();
			if (gpu_mode)
			{
				ailia_model.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
			}
			return ailia_model.OpenFile(stream, weight);
		}

		public void Close(){
			ailia_model.Close();
		}

		public string EnvironmentName(){
			return ailia_model.EnvironmentName();
		}

		public void ResetState(){
			input = new float[batch * sequence];
			sr = new float[1];
			h = new float[2 * batch * 64];
			c = new float[2 * batch * 64];
			remain_pcm = new float[0];
		}

		public class VadResult{
			public float [] pcm;
			public float [] conf;
		};

		public VadResult VAD(float [] add_pcm, int sampleRate)
		{
			// New buffer
			float [] pcm = new float [remain_pcm.Length + add_pcm.Length];
			for (int i = 0; i < remain_pcm.Length; i++){
				pcm[i] = remain_pcm[i];
			}
			for (int i = 0; i < add_pcm.Length; i++){
				pcm[i + remain_pcm.Length] = add_pcm[i];
			}

			// Create Inference Buffer
			List<float[]> inputs = new List<float[]>();
			inputs.Add(input);
			inputs.Add(sr);
			inputs.Add(h);
			inputs.Add(c);

			float[] output = new float[batch];
				
			List<float[]> outputs = new List<float[]>();
			outputs.Add(output);
			outputs.Add(h);
			outputs.Add(c);

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

				// Inference
				bool status = Forward(inputs, outputs);
				if (status == false){
					Debug.Log("Forward failed");
					return null;
				}

				// Fill Confidence
				for (int i = s; i < s + steps; i++){
					conf[i] = output[0];
				}
			}

			// Save tail for next inference
			int tail = pcm.Length - s;
			remain_pcm = new float[tail];
			float [] processed = new float[s];
			for (int i = 0; i < s; i++){
				processed[i] = pcm[i];
			}
			for (int i = s; i < pcm.Length; i++){
				remain_pcm[i - s] = pcm[i];
			}

			// Return value
			VadResult buf = new VadResult();
			buf.pcm = processed;
			buf.conf = conf;

			return buf;
		}

		private bool Forward(List<float[]> inputs, List<float[]> outputs){
			bool success;
			
			uint[] input_blobs = ailia_model.GetInputBlobList();

			for (int i = 0; i < NUM_INPUTS; i++){
				uint input_blob_idx = input_blobs[i];

				Ailia.AILIAShape sequence_shape = new Ailia.AILIAShape();
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

			success = ailia_model.Update();
			if (success == false) {
				Debug.Log("Update failed");
				return false;
			}

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