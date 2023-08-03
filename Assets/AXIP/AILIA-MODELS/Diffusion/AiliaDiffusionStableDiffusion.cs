/* AILIA Unity Plugin Diffusion Sample */
/* Copyright 2023 AXELL CORPORATION */

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ailiaSDK
{
	public class AiliaDiffusionStableDiffusion
	{
		//AILIA
		private AiliaModel diffusionEmbModel;
		private AiliaModel diffusionMidModel;
		private AiliaModel diffusionOutModel;
		private AiliaModel aeModel;

		// Sampler
		private AiliaDiffusionDdim ddim = new AiliaDiffusionDdim();

		// Parameters
		private int CondInputBatch;
		private int CondInputWidth;
		private int CondInputHeight;
		private int CondInputChannel;
		private int SequenceWidth;
		private int SequenzeHeight;
		private int DiffusionOutputWidth;
		private int DiffusionOutputHeight;
		private int DiffusionOutputChannel;
		private int AeOutputWidth;
		private int AeOutputHeight;
		private int AeOutputChannel;

		// Buffers
		private float[] cond_input;
		private float[] ae_output;
		private float [] diffusion_img;

		// Profile
		private float profile_pre;
		private float profile_diffusion;
		private float profile_ae;
		private float profile_post;
		private string profile_text;

		public bool Open(
			string diffusion_emb_model_path, string diffusion_emb_weight_path,
			string diffusion_mid_model_path, string diffusion_mid_weight_path,
			string diffusion_out_model_path, string diffusion_out_weight_path,
			string ae_model_path, string ae_weight_path, bool gpu_mode)
		{
			string asset_path = Application.temporaryCachePath;

			diffusionEmbModel = new AiliaModel();
			diffusionMidModel = new AiliaModel();
			diffusionOutModel = new AiliaModel();
			aeModel = new AiliaModel();

			if (gpu_mode)
			{
				// call before OpenFile
				diffusionEmbModel.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
				diffusionMidModel.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
				diffusionOutModel.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
				aeModel.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
			}

			uint memory_mode = Ailia.AILIA_MEMORY_REDUCE_CONSTANT | Ailia.AILIA_MEMORY_REDUCE_CONSTANT_WITH_INPUT_INITIALIZER | Ailia.AILIA_MEMORY_REUSE_INTERSTAGE;
			diffusionEmbModel.SetMemoryMode(memory_mode);
			diffusionMidModel.SetMemoryMode(memory_mode);
			diffusionOutModel.SetMemoryMode(memory_mode);
			aeModel.SetMemoryMode(memory_mode);

			bool modelPrepared = false;
			modelPrepared = diffusionEmbModel.OpenFile(diffusion_emb_model_path, diffusion_emb_weight_path);
			if (modelPrepared == true){
				modelPrepared = diffusionMidModel.OpenFile(diffusion_mid_model_path, diffusion_mid_weight_path);
				if (modelPrepared == true){
					modelPrepared = diffusionOutModel.OpenFile(diffusion_out_model_path, diffusion_out_weight_path);
					if (modelPrepared == true){
						modelPrepared = aeModel.OpenFile(ae_model_path, ae_weight_path);
					}
				}
			}

			return modelPrepared;
		}

		public void Close(){
			diffusionEmbModel.Close();
			diffusionMidModel.Close();
			diffusionOutModel.Close();
			aeModel.Close();
		}

		private void AllocateBuffer(){
			CondInputBatch = 2;
			CondInputWidth = 64;
			CondInputHeight = 64;
			CondInputChannel = 4;

			SequenceWidth = 768;
			SequenzeHeight = 77;

			AeOutputWidth = 512;
			AeOutputHeight = 512;
			AeOutputChannel = 3;
			
			cond_input = new float[CondInputWidth * CondInputHeight * CondInputChannel];
			ae_output = new float[AeOutputWidth * AeOutputHeight * AeOutputChannel];
		}

		public Color32[] Predict(int step, int ddim_num_steps)
		{
			// Initial diffusion image
			if (step == 0){
				AllocateBuffer();

				diffusion_img = new float[CondInputBatch * CondInputWidth * CondInputHeight * CondInputChannel];
				for (int i = 0; i < CondInputBatch * CondInputWidth * CondInputHeight * CondInputChannel; i++){
					diffusion_img[i] = ddim.randn();
				}

				profile_pre = 0.0f;
				profile_diffusion = 0.0f;
				profile_ae = 0.0f;
				profile_post = 0.0f;
				profile_text = "";
			}

			// Condition
			bool result = false;

			long start_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
			long end_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

			// diffusion mid

			// diffusion out

			// autoencoder

			// Make output image
			Color32[] outputImage;
			outputImage = new Color32[AeOutputWidth * AeOutputHeight];


			// Diffusion context (noise 3dim + cond 3dim)
			float [] diffusion_ctx = new float[CondInputWidth * CondInputHeight * 6];
			for (int i = 0; i < CondInputWidth * CondInputHeight * 3; i++){
				diffusion_ctx[CondInputWidth * CondInputHeight * 3 + i] = cond_input[i];
			}

			// Diffusion Loop
			float ddim_eta = 1.0f;
			AiliaDiffusionDdim.DdimParameters parameters = ddim.MakeDdimParameters(ddim_num_steps, ddim_eta, AiliaDiffusionAlphasComprod.alphas_cumprod_super_resolution);
			if (ddim_num_steps != parameters.ddim_timesteps.Count){
				return outputImage;
			}
			long start_time3 = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
			if (step < ddim_num_steps){
				int index = ddim_num_steps - 1 - step;

				// diffusion emb
				float [] timestamp = new float[CondInputBatch];
				float [] context = new float [SequenceWidth * SequenzeHeight * CondInputBatch]; // clip embedding

				Dictionary<string,AiliaTensor> input_tensors = new Dictionary<string,AiliaTensor>();
				AiliaTensor x_tensor = new AiliaTensor((uint)CondInputWidth, (uint)CondInputHeight, (uint)CondInputChannel, (uint)CondInputBatch, 4, diffusion_img);
				AiliaTensor timestamps_tensor = new AiliaTensor((uint)CondInputBatch, 1, 1, 1, 1, timestamp);
				AiliaTensor context_tensor = new AiliaTensor((uint)SequenceWidth, (uint)SequenzeHeight, (uint)CondInputBatch, 1, 3, context);
				input_tensors.Add("x", x_tensor);
				input_tensors.Add("timestamps", timestamps_tensor);
				input_tensors.Add("context", context_tensor);

				List<AiliaTensor> output_tensors = Infer(diffusionEmbModel, input_tensors);
				AiliaTensor feature_tensor = output_tensors[0];
				
				// diffusion mid
				input_tensors = new Dictionary<string,AiliaTensor>();
				//input_tensors.Add("x", output_tensors[0]);

				// diffusin out

				/*
				float [] t = new float[1];
				t[0] = parameters.ddim_timesteps[index];
				List<float []> inputs = new List<float []>();
				for (int i = 0; i < CondInputWidth * CondInputHeight * 3; i++){
					diffusion_ctx[i] = diffusion_img[i];
				}
				inputs.Add(diffusion_ctx);
				inputs.Add(t);
				List<float []> results = Forward(diffusionModel, inputs);
				float [] diffusion_output = results[0];
				ddim.DdimSampling(diffusion_img, diffusion_output, parameters, index);
				*/
			}

			long end_time3 = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

			// AutoEncoder
			long start_time4 = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
			aeModel.SetInputBlobData(diffusion_img , (int)aeModel.GetInputBlobList()[0]);
			//result = aeModel.Update();
			aeModel.GetBlobData(ae_output , (int)aeModel.GetOutputBlobList()[0]);
			long end_time4 = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

			// convert result to image
			long start_time5 = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
			OutputDataProcessing(ae_output, outputImage);
			long end_time5 = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

			// profile
			if (step == 0){
				profile_pre += (end_time - start_time);
			}
			profile_diffusion += (end_time3 - start_time3);
			if (step == 0){
				profile_ae += (end_time4 - start_time4);
				profile_post += (end_time5 - start_time5);
			}

			string text = "Step " + (step + 1) + "/" + (parameters.ddim_timesteps.Count) +"\n";
			text += "Size " + AeOutputWidth.ToString() + "x" + AeOutputHeight.ToString() +"\n";
			text += "Pre " + profile_pre.ToString() + " ms\n";
			text += "Diffusion " + profile_diffusion.ToString() + " ms\n";
			text += "AE " + profile_ae.ToString() + " ms\n";
			text += "Post " + profile_post.ToString() + " ms\n";
			text += aeModel.EnvironmentName();
			profile_text = text;
			
			return outputImage;
		}

		public string GetProfile(){
			return profile_text;
		}

		void OutputDataProcessing(float[] outputData, Color32[] pixelBuffer)
		{
			Debug.Log("outputData" + outputData.Length);
			Debug.Log("pixelBuffer" + pixelBuffer.Length);

			for (int i = 0; i < pixelBuffer.Length; i++)
			{
				pixelBuffer[i].r = (byte)Mathf.Clamp((outputData[i + 0 * pixelBuffer.Length] + 1.0f) / 2.0f * 255, 0, 255);
				pixelBuffer[i].g = (byte)Mathf.Clamp((outputData[i + 1 * pixelBuffer.Length] + 1.0f) / 2.0f * 255, 0, 255);
				pixelBuffer[i].b = (byte)Mathf.Clamp((outputData[i + 2 * pixelBuffer.Length] + 1.0f) / 2.0f * 255, 0, 255);
				pixelBuffer[i].a = 255;
			}
		}

		private class AiliaTensor{
			public Ailia.AILIAShape shape;
			public float [] data;

			public AiliaTensor(uint x, uint y, uint z, uint w, uint dim, float [] data = null){
				Ailia.AILIAShape shape=new Ailia.AILIAShape();
				shape.x = x;
				shape.y = y;
				shape.z = z;
				shape.w = w;
				shape.dim = dim;

				this.shape = shape;
				if (data == null){
					this.data = new float[shape.x * shape.y * shape.z * shape.w];
				}else{
					this.data = data;
				}
			}
		};

		// Infer
		List<AiliaTensor> Infer(AiliaModel model, Dictionary<string,AiliaTensor> input_tensors){
			// Set input
			foreach(KeyValuePair<string, AiliaTensor> dicItem in input_tensors) {
				Debug.Log("Input "+dicItem.Key+" Shape "+dicItem.Value.shape.w+" "+dicItem.Value.shape.z+" "+dicItem.Value.shape.y+" "+dicItem.Value.shape.x+" dim "+dicItem.Value.shape.dim);
				model.SetInputBlobShape(dicItem.Value.shape,model.FindBlobIndexByName(dicItem.Key));
				model.SetInputBlobData(dicItem.Value.data,model.FindBlobIndexByName(dicItem.Key));
			}

			// Infer
			model.Update();

			// Get output
			uint[] output_blob_idx = model.GetOutputBlobList();
			List<AiliaTensor> output_tensors = new List<AiliaTensor>();
			for (int i = 0; i < output_blob_idx.Length; i++){
				Ailia.AILIAShape output_shape=model.GetBlobShape((int)output_blob_idx[i]);
				Debug.Log("Output "+i+" Shape "+output_shape.w+" "+output_shape.z+" "+output_shape.y+" "+output_shape.x+" dim "+output_shape.dim);
				AiliaTensor output_tensor = new AiliaTensor(output_shape.x, output_shape.y, output_shape.z, output_shape.w, output_shape.dim, null);       
				model.GetBlobData(output_tensor.data, (int)output_blob_idx[i]);

				output_tensors.Add(output_tensor);
			}

			return output_tensors;
		}
	}
}