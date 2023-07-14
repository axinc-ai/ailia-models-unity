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

		public AiliaSileroVad(){
			ResetState();
		}

		public void ResetState(){
			input = new float[batch * sequence];
			sr = new float[1];
			h = new float[2 * batch * 64];
			c = new float[2 * batch * 64];
		}

		public List<float> VAD(AiliaModel ailia_model, float [] pcm, int nSamples, int sampleRate)
		{
			if (sampleRate != 16000 && sampleRate != 8000){
				Debug.Log("Sample rate must be 16000 or 8000");
				return null;
			}
			List<float> conf = new List<float>();
			for (int s = 0; s < nSamples; s+=sequence){
				for (int i = 0; i < input.Length; i++){
					if (s + i < nSamples){
						input[i] = pcm[s + i];
					}else{
						input[i] = 0;
					}
				}
				sr[0] = sampleRate;

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

				bool status = Forward(ailia_model, inputs, outputs);
				if (status == false){
					Debug.Log("Forward failed");
					return null;
				}

				conf.Add(output[0]);
			}

			return conf;

			/*
			uint[] input_blobs = ailia_model.GetInputBlobList();
			if (input_blobs != null)
			{
				bool success = ailia_model.SetInputBlobData(data, (int)input_blobs[0]);
				if (!success)
				{
					Debug.Log("Can not SetInputBlobData");
				}

				long start_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
				ailia_model.Update();

				List<FaceInfo> detections = null;

				uint[] output_blobs = ailia_model.GetOutputBlobList();
				if (output_blobs != null && output_blobs.Length >= 2)
				{
					Ailia.AILIAShape box_shape = ailia_model.GetBlobShape((int)output_blobs[0]);
					Ailia.AILIAShape score_shape = ailia_model.GetBlobShape((int)output_blobs[1]);

					if (box_shape != null && score_shape != null)
					{
						float[] box_data = new float[box_shape.x * box_shape.y * box_shape.z * box_shape.w];
						float[] score_data = new float[score_shape.x * score_shape.y * score_shape.z * score_shape.w];

						if (ailia_model.GetBlobData(box_data, (int)output_blobs[0]) &&
								ailia_model.GetBlobData(score_data, (int)output_blobs[1]))
						{
							float aspect = (float)tex_width / tex_height;
							detections = PostProcess(box_data, box_shape, score_data, score_shape, w, h, aspect);
						}
					}
				}

				if (detections != null)
				{
					detections = WeightedNonMaxSuppression(detections);
					//Debug.Log("Num faces: " + detections.Count);
				}
				return detections;
			}
			*/
			return null;
		}

		private bool Forward(AiliaModel ailia_model, List<float[]> inputs, List<float[]> outputs){
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