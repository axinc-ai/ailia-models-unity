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

using ailia;

namespace ailiaSDK
{
	public class AiliaSileroVad
	{
		// Model input parameter
		private const int batch = 1;
		private int sequence = 0;
		private int context = 0;
		private int state = 0;
		private int version = 0;

		// Inference buffer
		private float[] input;
		private float[] sr;
		private float[] h;
		private float[] c;

		// Audio queue
		private float[] remain_pcm;

		// Model
		private AiliaModel ailia_model = new AiliaModel();

		// Result
		public class VadResult{
			public float [] pcm;	// input pcm
			public float [] conf;	// output confidence
			public int sampleRate;	// input sampling rate (Hz)
		};

		// Constructer
		public AiliaSileroVad(){
		}

		// Open model from onnx file
		public bool OpenFile(string stream, string weight, bool gpu_mode){
			Close();
			if (gpu_mode)
			{
				ailia_model.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
			}
			bool status = ailia_model.OpenFile(stream, weight);
			if (!status) {
				return status;
			}
			uint[] input_blobs = ailia_model.GetInputBlobList();
			if (input_blobs.Length == 4) {
				version = 4;
			} else {
				version = 6;
			}
			if (version == 4) {
				sequence = 1536;
				context = 0;
				state = 64;
			} else {
				sequence = 512;
				context = 64;
				state = 128;
			}
			ResetState();
			return status;
		}

		// Close model
		public void Close(){
			ailia_model.Close();
		}

		// Get backend environment name
		public string EnvironmentName(){
			return ailia_model.EnvironmentName();
		}

		// Reset internal state
		public void ResetState(){
			input = new float[batch * (context + sequence)];
			sr = new float[1];
			h = new float[2 * batch * state];
			if (version == 4) {
				c = new float[2 * batch * state];
			} else {
				c = new float[batch * context];
			}
			remain_pcm = new float[0];
		}

		// Feed pcm to VAD logic (channels must be 1, sampleRate is automatically resampled for VAD)
		public VadResult VAD(float [] add_pcm, int channels, int sampleRate)
		{
			if (channels != 1){
				Debug.Log("channel must be 1");
				return null;
			}

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
			if (version == 4) {
				inputs.Add(input);
				inputs.Add(sr);
				inputs.Add(h);
				inputs.Add(c);
			} else {
				inputs.Add(input);
				inputs.Add(h);
				inputs.Add(sr);
			}

			float[] output = new float[batch];
				
			List<float[]> outputs = new List<float[]>();
			if (version == 4) {
				outputs.Add(output);
				outputs.Add(h);
				outputs.Add(c);
			} else {
				outputs.Add(output);
				outputs.Add(h);
			}

			// Step Loop
			int targetSampleRate = 16000;
			float [] conf = new float[pcm.Length];
			int steps = sequence * sampleRate / targetSampleRate;
			int s;
			for (s = 0; s < pcm.Length - steps; s+=steps){
				// Resampling to targetSampleRate
				for (int i = 0; i < context; i++){
					input[i] = c[i];
				}
				for (int i = 0; i < sequence; i++){
					int i2 = i * sampleRate / targetSampleRate;
					if (s + i2 < pcm.Length){
						input[i + context] = pcm[s + i2];
					}else{
						input[i + context] = 0;
					}
				}
				for (int i = 0; i < context; i++){
					c[i] = input[sequence + i];
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
			buf.sampleRate = sampleRate;

			return buf;
		}

		// Infer one frame using ailia SDK
		private bool Forward(List<float[]> inputs, List<float[]> outputs){
			bool success;
			
			// Set input blob shape and set input blob data
			uint[] input_blobs = ailia_model.GetInputBlobList();

			for (int i = 0; i < input_blobs.Length; i++){
				uint input_blob_idx = input_blobs[i];

				Ailia.AILIAShape sequence_shape = new Ailia.AILIAShape();
				if ( i == 0 ){
					sequence_shape.x=(uint)(sequence + context);
					sequence_shape.y=(uint)batch;
					sequence_shape.z=1;
					sequence_shape.w=1;
					sequence_shape.dim=2;
				}
				if (version == 4) {
					if ( i == 1 ){
						sequence_shape.x=1;
						sequence_shape.y=1;
						sequence_shape.z=1;
						sequence_shape.w=1;
						sequence_shape.dim=1;
					}
					if ( i == 2 || i == 3){
						sequence_shape.x=(uint)state;
						sequence_shape.y=(uint)batch;
						sequence_shape.z=2;
						sequence_shape.w=1;
						sequence_shape.dim=3;
					}
				} else {
					if ( i == 1){
						sequence_shape.x=(uint)state;
						sequence_shape.y=(uint)batch;
						sequence_shape.z=2;
						sequence_shape.w=1;
						sequence_shape.dim=3;
					}
					if ( i == 2 ){
						sequence_shape.x=1;
						sequence_shape.y=1;
						sequence_shape.z=1;
						sequence_shape.w=1;
						sequence_shape.dim=1;
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

			for (int i = 0; i < output_blobs.Length; i++){
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