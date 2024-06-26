﻿/* AILIA Unity Plugin RVC Sample */
/* Copyright 2023 AXELL CORPORATION */

using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

using ailia;
using ailiaAudio;

namespace ailiaSDK
{
	public class AiliaRvc
	{
		// Model input parameter
		private const int BATCH_SIZE = 1;
		private const int FEAT_SIZE_V1 = 256;
		private const int FEAT_SIZE_V2 = 768;
		private const int RND_SIZE = 192;
		private const int T_PAD = 48000; // 16khz domain samples
		private const int HUBERT_SAMPLE_RATE = 16000;

		// Target sampling rate
		private int OUTPUT_SAMPLE_RATE = 40000;
		private int T_PAD_TGT = 120000; // T_PAD * (40000hz / 16000hz)

		// Model
		private AiliaModel hubert_model = new AiliaModel();
		private AiliaModel vc_model = new AiliaModel();
		private AiliaRvcCrepe f0_model = new AiliaRvcCrepe();

		// Debug
		private bool debug = false;

		// Mode
		private bool f0_mode = false;
		private int f0_up_key = 0;
		private int rvc_version = 1;
		private int feat_size = FEAT_SIZE_V1;

		// State
		private class RvcState{
		public float[] hubert_input;
		public float[] hubert_padding_mask;
		public float[] output;
		};

		private RvcState async_state = null;
		private long rvc_time = 0;
		private long f0_time = 0;
		private Task async_task = null;

		private int async_processing_state = STATE_EMPTY;
		private const int STATE_EMPTY = 0;
		private const int STATE_PROCESSING = 1;
		private const int STATE_FINISH = 2;

		private RvcState GetRvcState(AudioClip clip){
			// Source
			if (!_validate(clip)){
				return null;
			}
			RvcState state = new RvcState();
			float [] pcm = new float[clip.samples * clip.channels];
			clip.GetData(pcm, 0);

			// Pre Process
			state.hubert_input = new float[GetSamples(clip)];
			state.hubert_padding_mask = new float[state.hubert_input.Length]; // zeros
			Resample(state.hubert_input, state.hubert_padding_mask, clip.samples, clip.channels, clip.frequency, pcm);

			return state;
		}

		// Constructer
		public AiliaRvc(){
		}

		// Open model from onnx file
		public bool OpenFile(string hubert_stream, string hubert_weight, string vc_stream, string vc_weight, int version, bool gpu_mode){
			Close();
			if (gpu_mode)
			{
				hubert_model.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
				vc_model.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
			}

			uint memory_mode = Ailia.AILIA_MEMORY_REDUCE_CONSTANT | Ailia.AILIA_MEMORY_REDUCE_CONSTANT_WITH_INPUT_INITIALIZER | Ailia.AILIA_MEMORY_REUSE_INTERSTAGE;
			rvc_version = version;
			if (rvc_version != 2){
				hubert_model.SetMemoryMode(memory_mode);
				feat_size = FEAT_SIZE_V1;
			}else{
				feat_size = FEAT_SIZE_V2;
			}
			vc_model.SetMemoryMode(memory_mode);

			bool status = hubert_model.OpenFile(hubert_stream, hubert_weight);
			if (!status){
				return status;
			}
			status = vc_model.OpenFile(vc_stream, vc_weight);
			if (!status){
				return status;
			}
			f0_mode = false;
			return status;
		}

		public bool OpenFileF0(string f0_stream, string f0_weight, bool f0_gpu_mode){
			bool status = f0_model.OpenFile(f0_stream, f0_weight, f0_gpu_mode);
			f0_mode = true;
			return status;
		}

		// Close model
		public void Close(){
			AsyncDestroy();
			hubert_model.Close();
			vc_model.Close();
			f0_model.Close();
		}

		// Set f0
		public void SetF0UpKeys(int up_key){
			f0_up_key = up_key;
		}

		// Set target sampling rate
		public void SetTargetSmaplingRate(int hz){
			OUTPUT_SAMPLE_RATE = hz;
			T_PAD_TGT = (int)((long)T_PAD * hz / HUBERT_SAMPLE_RATE);
			//Debug.Log("Output Sample Rate " + hz+" T_PAD_TGT " + T_PAD_TGT);
		}

		// Get backend environment name
		public string EnvironmentName(){
			if (f0_mode){
				return "rvc env : "+hubert_model.EnvironmentName()+"\nf0 env : "+f0_model.EnvironmentName();
			}
			return hubert_model.EnvironmentName();
		}

		private bool _validate(AudioClip clip){
			if (clip.channels != 1){
				Debug.Log("channel must be 1");
				return false;
			}
			if (clip.samples <= 0){
				Debug.Log("samples must be greater than 1");
				return false;
			}
			return true;
		}

		// Voice convert (sync API)
		public AudioClip Process(AudioClip clip)
		{
			RvcState state = GetRvcState(clip);
			if (state == null){
				return null;
			}
			ProcessCore(state);
			AudioClip newClip = AudioClip.Create("Segment", state.output.Length, 1, OUTPUT_SAMPLE_RATE, false);
			newClip.SetData(state.output, 0);
			return newClip;
		}

		// Voice convert (async API)

		public void AsyncProcess(AudioClip clip){
			if (async_processing_state == STATE_PROCESSING){
				return;
			}
			async_state = GetRvcState(clip);
			async_processing_state = STATE_PROCESSING;
			async_task = Task.Run(
				() => {
					ProcessCore(async_state);
					async_processing_state = STATE_FINISH;
				}
			);
		}

		public bool AsyncProcessing(){
			return (async_processing_state == STATE_PROCESSING);
		}

		public bool AsyncResultExist(){
			return (async_processing_state == STATE_FINISH);
		}

		public AudioClip AsyncGetResult(){
			AudioClip newClip = AudioClip.Create("Segment", async_state.output.Length, 1, OUTPUT_SAMPLE_RATE, false);
			newClip.SetData(async_state.output, 0);
			async_processing_state = STATE_EMPTY;
			return newClip;
		}

		private void AsyncDestroy(){
			if (async_task == null){
				return;
			}
			async_task.Wait();
		}

		// RVC core
		private void ProcessCore(RvcState state)
		{
			long start_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

			// Create Inference Buffer
			List<float[]> hubert_inputs = new List<float[]>();
			hubert_inputs.Add(state.hubert_input);
			hubert_inputs.Add(state.hubert_padding_mask);
			
			// Hubert Inference
			List<float[]> hubert_outputs = Forward(hubert_model, hubert_inputs, true);
			float [] hubert_output = hubert_outputs[0];

			// Interpolate
			float [] feats = Interpolate(hubert_output);

			// VC
			int len = feats.Length / feat_size;

			float [] p_len = new float[1];
			float [] sid = new float[1];
			float [] rnd = new float[RND_SIZE * len];

			p_len[0] = len;
			sid[0] = 0;
			Randn(rnd);

			// Pitch
			float [] pitch = new float[len];
			float [] pitch_f = new float[len];
			if (f0_mode){
				long start_time2 = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
				f0_model.GetF0(pitch, pitch_f, state.hubert_input, f0_up_key);
				long end_time2 = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
				f0_time = end_time2 - start_time2;
			}else{
				f0_time = 0;
			}

			// VC Inference
			List<float[]> vc_inputs = new List<float[]>();
			vc_inputs.Add(feats);
			vc_inputs.Add(p_len);
			if (f0_mode){
				vc_inputs.Add(pitch);
				vc_inputs.Add(pitch_f);
			}
			vc_inputs.Add(sid);
			vc_inputs.Add(rnd);
				
			List<float[]> vc_outputs = Forward(vc_model, vc_inputs, false);
			float [] vc_output = vc_outputs[0];

			// Prevent overflow
			Clip(vc_output);

			// Trim
			if (vc_output.Length <= T_PAD_TGT *2){
				Debug.LogError("model output is too small (" + vc_output.Length + ")");
			}
			float[] trm = new float[vc_output.Length - T_PAD_TGT*2];
			for (int i = 0; i < trm.Length; i++){
				trm[i] = vc_output[i + T_PAD_TGT];
			}
			state.output = trm;

			long end_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
			rvc_time = end_time - start_time;
		}

		public long GetProfile(){
			return rvc_time - f0_time;
		}

		public long GetProfileF0(){
			return f0_time;
		}

		// Resampler
		private void Resample(float [] hubert_input, float [] hubert_padding_mask, int samples, int channels, int frequency, float [] pcm){
			// Get PCM
			int sampleRate = frequency;
			int targetSampleRate = HUBERT_SAMPLE_RATE;

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
			int targetSampleRate = HUBERT_SAMPLE_RATE;
			float rate = 1.0f * targetSampleRate / sampleRate;
			return (int)(clip.samples * rate) + T_PAD * 2;
		}

		// Interpolate
		private float [] Interpolate(float [] hubert_output){
			float [] feats = new float [hubert_output.Length * 2];
			for (int i = 0; i < hubert_output.Length / feat_size; i++){
				for (int j = 0; j < feat_size; j++){
					feats[(i * 2 + 0) * feat_size + j] = hubert_output[i * feat_size + j];
					feats[(i * 2 + 1) * feat_size + j] = hubert_output[i * feat_size + j];
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
						sequence_shape.x=(uint)feat_size;
						sequence_shape.y=(uint)inputs[i].Length / (uint)feat_size / (uint)BATCH_SIZE;
						sequence_shape.z=(uint)BATCH_SIZE;
						sequence_shape.w=1;
						sequence_shape.dim=3;
					}
					if ( i == 1){
						sequence_shape.x=1;
						sequence_shape.y=1;
						sequence_shape.z=1;
						sequence_shape.w=1;
						sequence_shape.dim=1;
					}
					if (f0_mode) {
						if ( i == 2 || i == 3 ){
							// picth and pitchf
							sequence_shape.x=(uint)inputs[i].Length / (uint)BATCH_SIZE;
							sequence_shape.y=(uint)BATCH_SIZE;
							sequence_shape.z=1;
							sequence_shape.w=1;
							sequence_shape.dim=2;
						}
					}
					if ((!f0_mode && i == 2) || (f0_mode && i == 4)){
						sequence_shape.x=1;
						sequence_shape.y=1;
						sequence_shape.z=1;
						sequence_shape.w=1;
						sequence_shape.dim=1;
					}
					if ((!f0_mode && i == 3) || (f0_mode && i == 5)){
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
				
				if (hubert && rvc_version == 2){
					output_blob_idx = (uint)ailia_model.FindBlobIndexByName("/encoder/Slice_5_output_0");
				}

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