/* AILIA Unity Plugin Diffusion Sample */
/* Copyright 2023 AXELL CORPORATION */

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ailiaSDK
{
	public class AiliaDiffusionSample : MonoBehaviour
	{
		public enum DiffusionModels
		{
			Inpainting
		}

		//Settings
		public DiffusionModels diffusionModels = DiffusionModels.Inpainting;
		public bool gpu_mode = false;
		public GameObject UICanvas = null;
		public bool oneshot = true;

		//Result
		RawImage raw_image = null;
		Text label_text = null;
		Text mode_text = null;

		//AILIA
		private AiliaModel diffusionModel;
		private AiliaModel aeModel;
		private AiliaModel condModel;

		// Input source
		public AiliaImageSource AiliaImageSource;
		public AiliaImageSource AiliaImageSourceMask;
		public AiliaImageSource AiliaImageSourceMaskResize;

		// Sampler
		private AiliaDiffusionDdim ddim = new AiliaDiffusionDdim();

		// shader
		int CondInputWidth;
		int CondInputHeight;
		int CondInputChannel;
		int CondOutputWidth;
		int CondOutputHeight;
		int CondOutputChannel;
		int DiffusionOutputWidth;
		int DiffusionOutputHeight;
		int DiffusionOutputChannel;
		int AeOutputWidth;
		int AeOutputHeight;
		int AeOutputChannel;

		Texture2D resultTexture2D;
		Texture2D originalTexture;
		Vector2 rawImageSize;
		float[] cond_output;
		float[] cond_input;
		float[] cond_mask;
		float[] cond_mask_resize;
		float[] ae_output;

		bool modelPrepared;

		void Start()
		{
			UISetup();

			rawImageSize = raw_image.rectTransform.sizeDelta;

			AiliaInit();
		}

		void AiliaInit()
		{
			// Create Ailia
			CreateAiliaNet(diffusionModels, gpu_mode);
			// Load sample image
			LoadImage(diffusionModels);
		}

		void UISetup()
		{
			Debug.Assert(UICanvas != null, "UICanvas is null");

			label_text = UICanvas.transform.Find("LabelText").GetComponent<Text>();
			mode_text = UICanvas.transform.Find("ModeLabel").GetComponent<Text>();
			raw_image = UICanvas.transform.Find("RawImage").GetComponent<RawImage>();
			raw_image.gameObject.SetActive(false);

			mode_text.text = "ailia Diffusion\nSpace key down to original image";
		}

		private void AllocateBuffer(){
			float rawImageRatio = rawImageSize.x / rawImageSize.y;
			float ratio = AiliaImageSource.Width / (float)AiliaImageSource.Height;
			raw_image.rectTransform.sizeDelta = new Vector2(ratio / rawImageRatio * rawImageSize.x, rawImageSize.y);

			SetShape(diffusionModels);

			// texture & buffer allocate
			resultTexture2D = new Texture2D(AeOutputWidth, AeOutputHeight, TextureFormat.RGBA32, false);

			AiliaImageSource.Resize(CondInputWidth, CondInputHeight);
			AiliaImageSourceMask.Resize(CondInputWidth, CondInputHeight);
			AiliaImageSourceMaskResize.Resize(CondOutputWidth, CondOutputHeight);

			cond_input = new float[CondInputWidth * CondInputHeight * CondInputChannel];
			cond_mask = new float[CondInputWidth * CondInputHeight];
			cond_output = new float[CondOutputWidth * CondOutputHeight * CondOutputChannel];

			cond_mask_resize = new float[CondOutputWidth * CondOutputHeight];
 
			ae_output = new float[AeOutputWidth * AeOutputHeight * AeOutputChannel];
		}

		Color32[] Predict(Color32[] inputImage, Color32[] inputMask, Color32[] inputMaskResize, float [] diffusion_img, int step)
		{
			// Make output image
			Color32[] outputImage;
			outputImage = new Color32[AeOutputWidth * AeOutputHeight];

			// Make input data
			long start_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
			if (step == 0){
				InputDataImage(diffusionModels, inputImage, cond_input);
				InputDataMask(diffusionModels, inputMask, cond_mask);
				InputDataMask(diffusionModels, inputMaskResize, cond_mask_resize);
				InputDataPreprocess(cond_input, cond_mask, cond_mask_resize);
			}
			long end_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

			// Condition
			bool result = false;
			long start_time2 = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
			if (step == 0){
				result = condModel.Predict(cond_output, cond_input);
			}
			long end_time2 = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

			// Diffusion context (noise 3dim + cond 3dim + resized mask 1dim)
			float [] diffusion_ctx = new float[CondOutputWidth * CondOutputHeight * 7];
			for (int i = 0; i < CondOutputWidth * CondOutputHeight * 3; i++){
				diffusion_ctx[CondOutputWidth * CondOutputHeight * 3 + i] = cond_output[i];
			}
			for (int i = 0; i < CondOutputWidth * CondOutputHeight; i++){
				diffusion_ctx[CondOutputWidth * CondOutputHeight * 6 + i] = cond_mask_resize[i];
			}

			// Diffusion Loop
			float ddim_eta = 0.0f;
			AiliaDiffusionDdim.DdimParameters parameters = ddim.MakeDdimParameters(ddim_num_steps, ddim_eta);
			if (ddim_num_steps != parameters.ddim_timesteps.Count){
				return outputImage;
			}
			long start_time3 = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
			if (step < ddim_num_steps){
				int index = ddim_num_steps - 1 - step;

				float [] t = new float[1];
				t[0] = parameters.ddim_timesteps[index];
				List<float []> inputs = new List<float []>();
				for (int i = 0; i < CondOutputWidth * CondOutputHeight * 3; i++){
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
			OutputDataProcessing(diffusionModels, ae_output, cond_input, cond_mask, outputImage);
			long end_time5 = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

			if (label_text != null)
			{
				string text = "Step " + (step + 1) + "/" + (parameters.ddim_timesteps.Count) +"\n";
				text += "Size " + CondOutputWidth.ToString() + "x" + CondOutputHeight.ToString() +"\n";
				text += "Pre " + (end_time - start_time).ToString() + " ms\n";
				text += "Cond " + (end_time2 - start_time2).ToString() + "ms\n";
				text += "Diffusion " + (end_time3 - start_time3).ToString() + " ms\n";
				text += "AE " + (end_time4 - start_time4).ToString() + " ms\n";
				text += "Post " + (end_time5 - start_time5).ToString() + " ms\n";
				text += condModel.EnvironmentName();
				label_text.text = text;
			}

			return outputImage;
		}

		private int ddim_num_steps = 5;
		private int step = 0;
		private float [] diffusion_img;
		private const float SLEEP_TIME = 1.0f;
		private float sleep = SLEEP_TIME;

		void Update()
		{
			if (!AiliaImageSource.IsPrepared || !AiliaImageSourceMask.IsPrepared || !AiliaImageSourceMaskResize.IsPrepared || !modelPrepared)
			{
				Debug.Log("Waiting prepare "+AiliaImageSource.IsPrepared+","+AiliaImageSourceMask.IsPrepared+","+AiliaImageSourceMaskResize.IsPrepared+","+modelPrepared);
				return;
			}

			Debug.Log("Prepare success");

			if (cond_output == null)
			{
				AllocateBuffer();
			}

			if (sleep > 0){
				sleep = sleep - Time.deltaTime;
				if (sleep < 0.0f){
					sleep = 0.0f;
				}
			}

			if (oneshot && sleep == 0.0f)
			{
				Rect rect = new Rect(0, 0, CondInputWidth, CondInputHeight);
				Color32[] inputImage = null;
				Color32[] inputMask = null;
				Color32[] inputMaskResize = null;
				inputImage = AiliaImageSource.GetPixels32(rect, true);
				inputMask = AiliaImageSourceMask.GetPixels32(rect, true);
				inputMaskResize = AiliaImageSourceMaskResize.GetPixels32(rect, true);

				// Initial diffusion image
				if (step == 0){
					diffusion_img = new float[CondOutputWidth * CondOutputHeight * 3];
					for (int i = 0; i < CondOutputWidth * CondOutputHeight * 3; i++){
						diffusion_img[i] = ddim.randn();
					}
				}

				// Diffusion loop
				Color32[] outputImage = Predict(inputImage, inputMask, inputMaskResize, diffusion_img, step);
				step++;
				if (step >= ddim_num_steps){
					oneshot = false;
				}

				// T2B (Model) to B2T (Unity)
				VerticalFlip(CondInputWidth, CondInputHeight, inputImage);
				VerticalFlip(AeOutputWidth, AeOutputHeight, outputImage);

				// for viewer
				originalTexture = new Texture2D(CondInputWidth, CondInputHeight, TextureFormat.RGBA32, false);
				originalTexture.SetPixels32(inputImage);
				originalTexture.Apply();

				resultTexture2D = new Texture2D(AeOutputWidth, AeOutputHeight, TextureFormat.RGBA32, false);
				resultTexture2D.SetPixels32(outputImage);
				resultTexture2D.Apply();

				raw_image.texture = resultTexture2D;
				raw_image.gameObject.SetActive(true);

				// sleep
				sleep = SLEEP_TIME;
			}

			// When space key down, draw original image
			if (Input.GetKey(KeyCode.Space))
			{
				raw_image.texture = originalTexture;
			}
			else
			{
				raw_image.texture = resultTexture2D;
			}
		}

		// Download models and Create ailiaModel
		void CreateAiliaNet(DiffusionModels modelType, bool gpu_mode = true)
		{
			string asset_path = Application.temporaryCachePath;

			var urlList = new List<ModelDownloadURL>();

			switch (modelType)
			{
				case DiffusionModels.Inpainting:
					string serverFolderName = "latent-diffusion-inpainting";
					urlList.Add(new ModelDownloadURL() { folder_path = serverFolderName, file_name = "cond_stage_model.onnx.prototxt" });
					urlList.Add(new ModelDownloadURL() { folder_path = serverFolderName, file_name = "cond_stage_model.onnx" });
					urlList.Add(new ModelDownloadURL() { folder_path = serverFolderName, file_name = "diffusion_model.onnx.prototxt" });
					urlList.Add(new ModelDownloadURL() { folder_path = serverFolderName, file_name = "diffusion_model.onnx" });
					urlList.Add(new ModelDownloadURL() { folder_path = serverFolderName, file_name = "autoencoder.onnx.prototxt" });
					urlList.Add(new ModelDownloadURL() { folder_path = serverFolderName, file_name = "autoencoder.onnx" });
					break;
			}

			diffusionModel = new AiliaModel();
			aeModel = new AiliaModel();
			condModel = new AiliaModel();

			if (gpu_mode)
			{
				// call before OpenFile
				diffusionModel.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
				aeModel.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
				condModel.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
			}

			uint memory_mode = Ailia.AILIA_MEMORY_REDUCE_CONSTANT | Ailia.AILIA_MEMORY_REDUCE_CONSTANT_WITH_INPUT_INITIALIZER | Ailia.AILIA_MEMORY_REUSE_INTERSTAGE;
			diffusionModel.SetMemoryMode(memory_mode);
			aeModel.SetMemoryMode(memory_mode);
			condModel.SetMemoryMode(memory_mode);

			AiliaDownload ailia_download = new AiliaDownload();
			ailia_download.DownloaderProgressPanel = UICanvas.transform.Find("DownloaderProgressPanel").gameObject;

			StartCoroutine(ailia_download.DownloadWithProgressFromURL(urlList, () =>
			{
				modelPrepared = diffusionModel.OpenFile(asset_path + "/" + "diffusion_model.onnx.prototxt", asset_path + "/" + "diffusion_model.onnx");
				if (modelPrepared == true){
					modelPrepared = aeModel.OpenFile(asset_path + "/" + "autoencoder.onnx.prototxt", asset_path + "/" + "autoencoder.onnx");
					if (modelPrepared == true){
						modelPrepared = condModel.OpenFile(asset_path + "/" + "cond_stage_model.onnx.prototxt", asset_path + "/" + "cond_stage_model.onnx");
					}
				}
			}));
		}

		void SetShape(DiffusionModels diffusionModels)
		{
			Ailia.AILIAShape shape = null;

			switch (diffusionModels)
			{
				case DiffusionModels.Inpainting:
					// Set input image shape
					shape = new Ailia.AILIAShape();
					shape.x = (uint)AiliaImageSource.Width;
					shape.y = (uint)AiliaImageSource.Height;
					shape.z = 3;
					shape.w = 1;
					shape.dim = 4;
					condModel.SetInputShape(shape);
					CondInputWidth = AiliaImageSource.Width;
					CondInputHeight = AiliaImageSource.Height;
					CondInputChannel = 3;

					// Get output image shape
					shape = condModel.GetOutputShape();
					CondOutputWidth = (int)shape.x;
					CondOutputHeight = (int)shape.y;
					CondOutputChannel = (int)shape.z;

					// Set input image shape
					shape.x = (uint)CondOutputWidth;
					shape.y = (uint)CondOutputHeight;
					shape.z = 7; // noise + cond + mask
					shape.w = 1;
					shape.dim = 4;
					diffusionModel.SetInputShape(shape);

					// Get output image shape
					shape = condModel.GetOutputShape();
					DiffusionOutputWidth = (int)shape.x;
					DiffusionOutputHeight = (int)shape.y;
					DiffusionOutputChannel = (int)shape.z;

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
					break;
			}
		}

		void LoadImage(DiffusionModels diffusionModels)
		{
			switch (diffusionModels)
			{
				case DiffusionModels.Inpainting:
					AiliaImageSource.CreateSource("file://" + Application.dataPath + "/AXIP/AILIA-MODELS/Diffusion/SampleImage/inpainting.png");
					AiliaImageSourceMask.CreateSource("file://" + Application.dataPath + "/AXIP/AILIA-MODELS/Diffusion/SampleImage/inpainting_mask.png");
					AiliaImageSourceMaskResize.CreateSource("file://" + Application.dataPath + "/AXIP/AILIA-MODELS/Diffusion/SampleImage/inpainting_mask.png");
					break;
			}
		}

		void InputDataImage(DiffusionModels diffusionModels, Color32[] inputImage, float[] processedInputBuffer)
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

		void InputDataMask(DiffusionModels diffusionModels, Color32[] inputImage, float[] processedInputBuffer)
		{
			float weight = 1f / 255f;
			float bias = 0;

			// Inpainting : Channel First, RGB, /255f

			for (int i = 0; i < inputImage.Length; i++)
			{
				float v = (inputImage[i].g) * weight + bias;
				if ( v < 0.5f ){
					v = 0.0f;
				}else{
					v = 1.0f;
				}
				processedInputBuffer[i] = v;
			}
		}

		void InputDataPreprocess(float[] image, float [] mask, float [] mask_resize)
		{
			for (int i = 0; i < image.Length/3; i++)
			{
				for (int rgb = 0; rgb < 3; rgb++){
					image[i + image.Length/3 * rgb] = image[i + image.Length/3 * rgb] * (1.0f - mask[i]);
				}
			}

			for (int i = 0; i < image.Length; i++)
			{
				image[i] = image[i] * 2 - 1;
			}

			for (int i = 0; i < mask.Length; i++)
			{
				mask[i] = mask[i] * 2 - 1;
			}

			for (int i = 0; i < mask_resize.Length; i++)
			{
				mask_resize[i] = mask_resize[i] * 2 - 1;
			}
		}

		void OutputDataProcessing(DiffusionModels diffusionModels, float[] outputData, float[] imgData, float[] maskData, Color32[] pixelBuffer)
		{
			Debug.Log("outputData" + outputData.Length);
			Debug.Log("imgData" + imgData.Length);
			Debug.Log("maskData" + maskData.Length);
			Debug.Log("pixelBuffer" + pixelBuffer.Length);

			for (int i = 0; i < outputData.Length; i++)
			{
				float mask = maskData[i % maskData.Length]; // one channel
				float img = imgData[i];
				float predicted_image = outputData[i];
				float inpainted = (1 - mask) * img + mask * predicted_image;
				outputData[i] = inpainted;
			}

			for (int i = 0; i < pixelBuffer.Length; i++)
			{
				pixelBuffer[i].r = (byte)Mathf.Clamp((outputData[i + 0 * pixelBuffer.Length] + 1.0f) / 2.0f * 255, 0, 255);
				pixelBuffer[i].g = (byte)Mathf.Clamp((outputData[i + 1 * pixelBuffer.Length] + 1.0f) / 2.0f * 255, 0, 255);
				pixelBuffer[i].b = (byte)Mathf.Clamp((outputData[i + 2 * pixelBuffer.Length] + 1.0f) / 2.0f * 255, 0, 255);
				pixelBuffer[i].a = 255;
			}
		}

		void VerticalFlip(int width, int height, Color32[] image){
			for (int y = 0; y < height / 2; y++){
				for (int x = 0; x < width; x++){
					Color32 temp = image[y * width + x];
					image[y * width + x] = image[(height - 1 - y) * width + x];
					image[(height - 1 - y) * width + x] = temp;
				}
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

		void OnApplicationQuit()
		{
			DestroyAiliaNetwork();
		}

		void OnDestroy()
		{
			DestroyAiliaNetwork();
		}

		private void DestroyAiliaNetwork()
		{
			diffusionModel.Close();
			aeModel.Close();
			condModel.Close();
		}
	}
}