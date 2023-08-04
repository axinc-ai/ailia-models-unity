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
		private AiliaModel clipModel;

		// Sampler
		private AiliaDiffusionDdim ddim = new AiliaDiffusionDdim();

		// Parameters
		private int CondInputBatch;
		private int CondInputWidth;
		private int CondInputHeight;
		private int CondInputChannel;
		private int SequenceWidth;
		private int SequenceHeight;
		private int DiffusionOutputWidth;
		private int DiffusionOutputHeight;
		private int DiffusionOutputChannel;
		private int AeOutputWidth;
		private int AeOutputHeight;
		private int AeOutputChannel;

		// Buffers
		private float[] ae_output;
		private float [] diffusion_img;
		private float [] context;

		// Profile
		private float profile_pre;
		private float profile_diffusion;
		private float profile_ae;
		private float profile_post;
		private string profile_text;

		// Tokens
		private static float [] empty_tokens = {
				49406, 49407, 49407, 49407, 49407, 49407, 49407, 49407, 49407, 49407,
				49407, 49407, 49407, 49407, 49407, 49407, 49407, 49407, 49407, 49407,
				49407, 49407, 49407, 49407, 49407, 49407, 49407, 49407, 49407, 49407,
				49407, 49407, 49407, 49407, 49407, 49407, 49407, 49407, 49407, 49407,
				49407, 49407, 49407, 49407, 49407, 49407, 49407, 49407, 49407, 49407,
				49407, 49407, 49407, 49407, 49407, 49407, 49407, 49407, 49407, 49407,
				49407, 49407, 49407, 49407, 49407, 49407, 49407, 49407, 49407, 49407,
				49407, 49407, 49407, 49407, 49407, 49407, 49407};

		private static float [] prompt_tokens = {
				49406,   320,  8853,   539,   550, 18376,  6765,   320,  4558, 49407,
				49407, 49407, 49407, 49407, 49407, 49407, 49407, 49407, 49407, 49407,
				49407, 49407, 49407, 49407, 49407, 49407, 49407, 49407, 49407, 49407,
				49407, 49407, 49407, 49407, 49407, 49407, 49407, 49407, 49407, 49407,
				49407, 49407, 49407, 49407, 49407, 49407, 49407, 49407, 49407, 49407,
				49407, 49407, 49407, 49407, 49407, 49407, 49407, 49407, 49407, 49407,
				49407, 49407, 49407, 49407, 49407, 49407, 49407, 49407, 49407, 49407,
				49407, 49407, 49407, 49407, 49407, 49407, 49407};

		public bool Open(
			string diffusion_emb_model_path, string diffusion_emb_weight_path,
			string diffusion_mid_model_path, string diffusion_mid_weight_path,
			string diffusion_out_model_path, string diffusion_out_weight_path,
			string ae_model_path, string ae_weight_path,
			string clip_model_path, string clip_weight_path,		
			bool gpu_mode)
		{
			string asset_path = Application.temporaryCachePath;

			diffusionEmbModel = new AiliaModel();
			diffusionMidModel = new AiliaModel();
			diffusionOutModel = new AiliaModel();
			aeModel = new AiliaModel();
			clipModel = new AiliaModel();

			if (gpu_mode)
			{
				// call before OpenFile
				diffusionEmbModel.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
				diffusionMidModel.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
				diffusionOutModel.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
				aeModel.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
				clipModel.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
			}

			uint memory_mode = Ailia.AILIA_MEMORY_REDUCE_CONSTANT | Ailia.AILIA_MEMORY_REDUCE_CONSTANT_WITH_INPUT_INITIALIZER | Ailia.AILIA_MEMORY_REUSE_INTERSTAGE;
			diffusionEmbModel.SetMemoryMode(memory_mode);
			diffusionMidModel.SetMemoryMode(memory_mode);
			diffusionOutModel.SetMemoryMode(memory_mode);
			aeModel.SetMemoryMode(memory_mode);
			// clip requres internal state

			bool modelPrepared = false;
			modelPrepared = diffusionEmbModel.OpenFile(diffusion_emb_model_path, diffusion_emb_weight_path);
			if (modelPrepared == true){
				modelPrepared = diffusionMidModel.OpenFile(diffusion_mid_model_path, diffusion_mid_weight_path);
				if (modelPrepared == true){
					modelPrepared = diffusionOutModel.OpenFile(diffusion_out_model_path, diffusion_out_weight_path);
					if (modelPrepared == true){
						modelPrepared = aeModel.OpenFile(ae_model_path, ae_weight_path);
						if (modelPrepared == true){
							modelPrepared = clipModel.OpenFile(clip_model_path, clip_weight_path);
						}
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
			clipModel.Close();
		}

		private void AllocateBuffer(){
			CondInputBatch = 2; // cond and non cond
			CondInputWidth = 64;
			CondInputHeight = 64;
			CondInputChannel = 4;

			SequenceWidth = 768;
			SequenceHeight = 77;

			AeOutputWidth = 512;
			AeOutputHeight = 512;
			AeOutputChannel = 3;

			ae_output = new float[AeOutputWidth * AeOutputHeight * AeOutputChannel];
		}

		private float [] Tokenize(float [] tokens){
			const int CLIP_SEQUENCE_SIZE = 77;
			const int CLIP_CONTEXT_SIZE = 768;

			Ailia.AILIAShape shape=new Ailia.AILIAShape();
			shape.x = CLIP_SEQUENCE_SIZE;
			shape.y = 1;
			shape.z = 1;
			shape.w = 1;
			shape.dim = 2;

			float [] z = new float [CLIP_SEQUENCE_SIZE * CLIP_CONTEXT_SIZE];

			clipModel.SetInputBlobShape(shape, (int)clipModel.GetInputBlobList()[0]);
			clipModel.SetInputBlobData(diffusion_img , (int)clipModel.GetInputBlobList()[0]);
			bool result = clipModel.Update();
			if (result == false){
				Debug.Log("CLIP failed.");
			}
			clipModel.GetBlobData(z , (int)clipModel.FindBlobIndexByName("/ln_final/Add_1_output_0"));

			Debug.Log("Clip embedding "+z[0]+" "+z[1]);

			return z;
		}

		private void SetInputShape(){
			Ailia.AILIAShape shape=new Ailia.AILIAShape();
			shape.x = (uint)CondInputWidth;
			shape.y = (uint)CondInputHeight;
			shape.z = (uint)CondInputChannel;
			shape.w = 1;
			shape.dim = 4;
			aeModel.SetInputBlobShape(shape, (int)clipModel.GetInputBlobList()[0]);
		}

		public Color32[] Predict(int step, int ddim_num_steps)
		{
			// Initial diffusion image
			if (step == 0){
				AllocateBuffer();
				SetInputShape();

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

			// Clip Embedding
			long start_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
			if (step == 0){
				float [] uc = Tokenize(empty_tokens);
				float [] c = Tokenize(prompt_tokens);
				context = new float [SequenceWidth * SequenceHeight * CondInputBatch];
				for (int i = 0; i < SequenceWidth * SequenceHeight; i++){
					context[0 * SequenceWidth * SequenceHeight + i] = c[i];
					context[1 * SequenceWidth * SequenceHeight + i] = uc[i];
				}
			}
			long end_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

			// Make output image
			Color32[] outputImage;
			outputImage = new Color32[AeOutputWidth * AeOutputHeight];

			// Diffusion Loop
			float ddim_eta = 1.0f;
			AiliaDiffusionDdim.DdimParameters parameters = ddim.MakeDdimParameters(ddim_num_steps, ddim_eta, AiliaDiffusionAlphasComprod.alphas_cumprod_super_resolution);
			if (ddim_num_steps != parameters.ddim_timesteps.Count){
				return outputImage;
			}
			long start_time3 = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
			if (step < ddim_num_steps){
				int index = ddim_num_steps - 1 - step;
				float[] diffusion_output = DiffusionInfer(diffusion_img, step);
				ddim.DdimSampling(diffusion_img, diffusion_output, parameters, index);
			}
			long end_time3 = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

			// AutoEncoder
			long start_time4 = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
			aeModel.SetInputBlobData(diffusion_img , (int)aeModel.GetInputBlobList()[0]);
			result = aeModel.Update();
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

		private float [] DiffusionInfer(float [] diffusion_img, int step){
			// diffusion emb
			float [] timestamp = new float[CondInputBatch];

			Dictionary<string,AiliaTensor> input_tensors = new Dictionary<string,AiliaTensor>();
			AiliaTensor x_tensor = new AiliaTensor((uint)CondInputWidth, (uint)CondInputHeight, (uint)CondInputChannel, (uint)CondInputBatch, 4, diffusion_img);
			AiliaTensor timestamps_tensor = new AiliaTensor((uint)CondInputBatch, 1, 1, 1, 1, timestamp);
			AiliaTensor context_tensor = new AiliaTensor((uint)SequenceWidth, (uint)SequenceHeight, (uint)CondInputBatch, 1, 3, context);

			for (int i = 0; i < CondInputBatch; i++){
				timestamp[i] = step;
			}

			input_tensors.Add("x", x_tensor);
			input_tensors.Add("timesteps", timestamps_tensor);
			input_tensors.Add("context", context_tensor);

			List<AiliaTensor> output_tensors = Infer(diffusionEmbModel, input_tensors);
			
			// diffusion mid
			input_tensors = new Dictionary<string,AiliaTensor>();
			input_tensors.Add("h", output_tensors[0]);
			input_tensors.Add("emb", output_tensors[1]);
			input_tensors.Add("context", context_tensor);
			input_tensors.Add("h6", output_tensors[8]);
			input_tensors.Add("h7", output_tensors[9]);
			input_tensors.Add("h8", output_tensors[10]);
			input_tensors.Add("h9", output_tensors[11]);
			input_tensors.Add("h10", output_tensors[12]);
			input_tensors.Add("h11", output_tensors[13]);

			List<AiliaTensor> output_tensors_mid = Infer(diffusionMidModel, input_tensors);

			// diffusin out
			input_tensors = new Dictionary<string,AiliaTensor>();
			input_tensors.Add("h", output_tensors_mid[0]);
			input_tensors.Add("emb", output_tensors[1]);
			input_tensors.Add("context", context_tensor);
			input_tensors.Add("h0", output_tensors[2]);
			input_tensors.Add("h1", output_tensors[3]);
			input_tensors.Add("h2", output_tensors[4]);
			input_tensors.Add("h3", output_tensors[5]);
			input_tensors.Add("h4", output_tensors[6]);
			input_tensors.Add("h5", output_tensors[7]);

			List<AiliaTensor> output_tensors_out = Infer(diffusionOutModel, input_tensors);

			float[] diffusion_output = output_tensors_out[0].data;

			float unconditional_guidance_scale = 7.5f;
			for (int i = 0; i < CondInputWidth * CondInputHeight * CondInputChannel; i++){
				float e_t = diffusion_output[i];
				float e_t_uncond = diffusion_output[CondInputWidth * CondInputHeight * CondInputChannel + i];
				e_t = e_t_uncond + unconditional_guidance_scale * (e_t - e_t_uncond);
				diffusion_output[i] = e_t;
				diffusion_output[CondInputWidth * CondInputHeight * CondInputChannel + i] = e_t;
			}

			return diffusion_output;
		}

		public string GetProfile(){
			return profile_text;
		}

		private void OutputDataProcessing(float[] outputData, Color32[] pixelBuffer)
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
		private List<AiliaTensor> Infer(AiliaModel model, Dictionary<string,AiliaTensor> input_tensors){
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