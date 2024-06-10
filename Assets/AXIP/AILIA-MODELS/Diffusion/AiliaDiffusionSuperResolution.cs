/* AILIA Unity Plugin Diffusion Sample */
/* Copyright 2023 AXELL CORPORATION */

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using ailia;

namespace ailiaSDK
{
	public class AiliaDiffusionSuperResolution
	{
		//AILIA
		private AiliaModel diffusionModel = new AiliaModel();
		private AiliaModel aeModel = new AiliaModel();

		// Sampler
		private AiliaDiffusionDdim ddim = new AiliaDiffusionDdim();

		// Parameters
		private int CondInputWidth;
		private int CondInputHeight;
		private int CondInputChannel;
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

		public bool Open(string diffusion_model_path, string diffusion_weight_path, string ae_model_path, string ae_weight_path, bool gpu_mode)
		{
			string asset_path = Application.temporaryCachePath;

			if (gpu_mode)
			{
				// call before OpenFile
				diffusionModel.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
				aeModel.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
			}

			uint memory_mode = Ailia.AILIA_MEMORY_REDUCE_CONSTANT | Ailia.AILIA_MEMORY_REDUCE_CONSTANT_WITH_INPUT_INITIALIZER | Ailia.AILIA_MEMORY_REUSE_INTERSTAGE;
			diffusionModel.SetMemoryMode(memory_mode);
			aeModel.SetMemoryMode(memory_mode);

			bool modelPrepared = false;
			modelPrepared = diffusionModel.OpenFile(diffusion_model_path, diffusion_weight_path);
			if (modelPrepared == true){
				modelPrepared = aeModel.OpenFile(ae_model_path, ae_weight_path);
			}

			return modelPrepared;
		}

		public void Close(){
			diffusionModel.Close();
			aeModel.Close();
		}

		private void AllocateBuffer(){
			cond_input = new float[CondInputWidth * CondInputHeight * CondInputChannel];
			ae_output = new float[AeOutputWidth * AeOutputHeight * AeOutputChannel];
		}

		public Color32[] Predict(Color32[] inputImage, int step, int ddim_num_steps)
		{
			// Initial diffusion image
			if (step == 0){
				SetShape(128, 128);
				AllocateBuffer();

				diffusion_img = new float[CondInputWidth * CondInputHeight * 3];
				for (int i = 0; i < CondInputWidth * CondInputHeight * 3; i++){
					diffusion_img[i] = ddim.randn();
				}

				profile_pre = 0.0f;
				profile_diffusion = 0.0f;
				profile_ae = 0.0f;
				profile_post = 0.0f;
				profile_text = "";
			}

			// Make output image
			Color32[] outputImage;
			outputImage = new Color32[AeOutputWidth * AeOutputHeight];

			// Make input data
			long start_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
			if (step == 0){
				InputDataImage(inputImage, cond_input);
				InputDataPreprocess(cond_input);
			}
			long end_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

			// Condition
			bool result = false;

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

		public string GetProfile(){
			return profile_text;
		}

		public void SetShape(int image_width, int image_height)
		{
			Ailia.AILIAShape shape = null;

			// Condition
			CondInputWidth = image_width;
			CondInputHeight = image_height;
			CondInputChannel = 3;

			// Set input image shape
			shape = new Ailia.AILIAShape();

			// Set input image shape
			shape.x = (uint)CondInputWidth;
			shape.y = (uint)CondInputHeight;
			shape.z = 6; // noise + cond
			shape.w = 1;
			shape.dim = 4;
			diffusionModel.SetInputShape(shape);

			// This model does not have a fixed shape until it is inferred
			// So manually set image shape

			// Get output image shape
			//shape = diffusionModel.GetOutputShape();
			DiffusionOutputWidth = (int)shape.x;
			DiffusionOutputHeight = (int)shape.y;
			DiffusionOutputChannel = 3;

			Debug.Log("diffusion output "+DiffusionOutputWidth+"/"+DiffusionOutputHeight+"/"+DiffusionOutputChannel);

			// Set input image shape
			shape.x = (uint)DiffusionOutputWidth;
			shape.y = (uint)DiffusionOutputHeight;
			shape.z = 3;
			shape.w = 1;
			shape.dim = 4;
			aeModel.SetInputShape(shape);

			// This model does not have a fixed shape until it is inferred
			// So manually set image shape

			// Get output image shape
			//shape = aeModel.GetOutputShape();
			AeOutputWidth = (int)shape.x * 4;
			AeOutputHeight = (int)shape.y * 4;
			AeOutputChannel = (int)shape.z;

			Debug.Log("ae_model output "+AeOutputWidth+"/"+AeOutputHeight+"/"+AeOutputChannel);
		}

		void InputDataImage(Color32[] inputImage, float[] processedInputBuffer)
		{
			float weight = 1f / 255f;
			float bias = 0;

			// Inpainting : Channel First, RGB, /255f

			for (int i = 0; i < inputImage.Length; i++)
			{
				processedInputBuffer[i + inputImage.Length * 0] = (inputImage[i].r) * weight + bias;
				processedInputBuffer[i + inputImage.Length * 1] = (inputImage[i].g) * weight + bias;
				processedInputBuffer[i + inputImage.Length * 2] = (inputImage[i].b) * weight + bias;
			}
		}

		void InputDataPreprocess(float[] image)
		{
			for (int i = 0; i < image.Length; i++)
			{
				image[i] = image[i] * 2 - 1;
			}
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


		// Infer one frame using ailia SDK
		private List<float[]> Forward(AiliaModel ailia_model, List<float[]> inputs){
			bool success;
			List<float[]>  outputs = new List<float[]> ();

			// Set input blob shape and set input blob data
			uint[] input_blobs = ailia_model.GetInputBlobList();

			for (int i = 0; i < inputs.Count; i++){
				uint input_blob_idx = input_blobs[i];
				Ailia.AILIAShape input_blob_shape = ailia_model.GetBlobShape((int)input_blob_idx);
				Debug.Log("Input Idx "+i+ " "+input_blob_shape.w+","+input_blob_shape.z+","+input_blob_shape.y+","+input_blob_shape.x+" dim "+input_blob_shape.dim);

				success = ailia_model.SetInputBlobData(inputs[i], (int)input_blob_idx);
				if (success == false){
					Debug.Log("SetInputBlobData failed");
					return outputs;
				}
			}

			// Inference
			success = ailia_model.Update();
			if (success == false) {
				Debug.Log("Update failed");
				return outputs;
			}

			// Get outpu blob shape and get output blob data
			uint[] output_blobs = ailia_model.GetOutputBlobList();

			for (int i = 0; i < output_blobs.Length; i++){
				uint output_blob_idx = output_blobs[i];
				
				Ailia.AILIAShape output_blob_shape = ailia_model.GetBlobShape((int)output_blob_idx);
				Debug.Log("Output Idx "+i+ " "+output_blob_shape.w+","+output_blob_shape.z+","+output_blob_shape.y+","+output_blob_shape.x+" dim "+output_blob_shape.dim);

				float [] output = new float[output_blob_shape.x * output_blob_shape.y * output_blob_shape.z * output_blob_shape.w];
				success = ailia_model.GetBlobData(output, (int)output_blob_idx);
				if (success == false){
					Debug.Log("GetBlobData failed");
					return outputs;
				}
				outputs.Add(output);
			}

			return outputs;
		}
	}
}