/* AILIA Unity Plugin GAN Sample */
/* Copyright 2023 AXELL CORPORATION */

using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Runtime.InteropServices;

using UnityEngine;
using UnityEngine.UI;

using ailia;

namespace ailiaSDK {
	public class AiliaGenerativeAdversarialNetworksSample : AiliaRenderer {
		public enum AiliaGenerativeAdversarialNetworksModels
		{
			lipgan,
			gfpgan
		}

		[SerializeField]
		private AiliaGenerativeAdversarialNetworksModels ailiaModelType = AiliaGenerativeAdversarialNetworksModels.lipgan;
		[SerializeField]
		private GameObject UICanvas = null;
		public AudioClip audio = null;
		public AudioSource audio_source = null;
		public bool play_audio = false;
		public Texture2D image_lipgan = null; 
		public Texture2D image_gfpgan = null; 
		private Texture2D image = null;

		//Settings
		[SerializeField]
		private bool gpu_mode = false;
		[SerializeField]
		private int camera_id = 0;
		[SerializeField]
		private bool debug = false;

		//Result
		RawImage raw_image = null;
		Text label_text = null;
		Text mode_text = null;

		//Preview
		private Texture2D preview_texture = null;

		//AILIA
		private AiliaModel ailia_face_detector = new AiliaModel();
		private AiliaModel ailia_face_gan = new AiliaModel();

		private AiliaBlazeface blaze_face = new AiliaBlazeface();
		private AiliaLipGan lip_gan = new AiliaLipGan();
		private AiliaGfpGan gfp_gan = new AiliaGfpGan();

		private AiliaCamera ailia_camera = new AiliaCamera();
		private AiliaDownload ailia_download = new AiliaDownload();

		// AILIA open file
		private bool FileOpened = false;

		// State
		private float audio_time = 0.0f;
		private bool one_shot = true;

		private void CreateAiliaDetector(AiliaGenerativeAdversarialNetworksModels modelType)
		{
			string asset_path = Application.temporaryCachePath;
			var urlList = new List<ModelDownloadURL>();
			if (gpu_mode)
			{
				ailia_face_detector.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
				ailia_face_gan.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
			}
			switch (modelType)
			{		
				case AiliaGenerativeAdversarialNetworksModels.lipgan:
					mode_text.text = "ailia LipGAN";

					urlList.Add(new ModelDownloadURL() { folder_path = "blazeface", file_name = "blazeface.onnx.prototxt" });
					urlList.Add(new ModelDownloadURL() { folder_path = "blazeface", file_name = "blazeface.onnx" });
					urlList.Add(new ModelDownloadURL() { folder_path = "lipgan", file_name = "lipgan.onnx.prototxt" });
					urlList.Add(new ModelDownloadURL() { folder_path = "lipgan", file_name = "lipgan.onnx" });

					StartCoroutine(ailia_download.DownloadWithProgressFromURL(urlList, () =>
					{
						FileOpened = ailia_face_detector.OpenFile(asset_path + "/blazeface.onnx.prototxt", asset_path + "/blazeface.onnx");
						if (FileOpened){
							FileOpened = ailia_face_gan.OpenFile(asset_path + "/lipgan.onnx.prototxt", asset_path + "/lipgan.onnx");
						}
					}));

					break;

				case AiliaGenerativeAdversarialNetworksModels.gfpgan:
					mode_text.text = "ailia GFPGAN\nSpace key down to original image";

					urlList.Add(new ModelDownloadURL() { folder_path = "blazeface", file_name = "blazeface.onnx.prototxt" });
					urlList.Add(new ModelDownloadURL() { folder_path = "blazeface", file_name = "blazeface.onnx" });
					urlList.Add(new ModelDownloadURL() { folder_path = "gfpgan", file_name = "GFPGANv1.4.onnx.prototxt" });
					urlList.Add(new ModelDownloadURL() { folder_path = "gfpgan", file_name = "GFPGANv1.4.onnx" });

					StartCoroutine(ailia_download.DownloadWithProgressFromURL(urlList, () =>
					{
						FileOpened = ailia_face_detector.OpenFile(asset_path + "/blazeface.onnx.prototxt", asset_path + "/blazeface.onnx");
						if (FileOpened){
							FileOpened = ailia_face_gan.OpenFile(asset_path + "/GFPGANv1.4.onnx.prototxt", asset_path + "/GFPGANv1.4.onnx");
						}
					}));

					break;

				default:
					Debug.Log("Others ailia models are working in progress.");
					break;
			}
		}


		private void DestroyAiliaDetector()
		{
			ailia_face_detector.Close();
			ailia_face_gan.Close();
		}

		// Use this for initialization
		void Start()
		{
			AiliaLicense.CheckAndDownloadLicense();

			if(ailiaModelType==AiliaGenerativeAdversarialNetworksModels.lipgan){
				image = image_lipgan;
			}
			if(ailiaModelType==AiliaGenerativeAdversarialNetworksModels.gfpgan){
				image = image_gfpgan;
			}

			SetUIProperties();
			CreateAiliaDetector(ailiaModelType);
			if (image == null){
				ailia_camera.CreateCamera(camera_id);
			}
			lip_gan.SetAudio(audio, debug);
			if (play_audio){
				audio_source.clip = audio;
				audio_source.Play();
			}
		}

		// Update is called once per frame
		void Update()
		{
			if (!ailia_camera.IsEnable() && image == null)
			{
				return;
			}
			if (!FileOpened)
			{
				return;
			}

			//Clear result
			Clear();

			//Get camera image
			int tex_width = 0;
			int tex_height = 0;
			Color32[] camera = null;

			//Get image
			if (image == null){
				tex_width = ailia_camera.GetWidth();
				tex_height = ailia_camera.GetHeight();
				camera = ailia_camera.GetPixels32();
			}
			if (image != null){
				tex_width = image.width;
				tex_height = image.height;
				camera = image.GetPixels32();
			}

			//Create output image
			if (preview_texture == null)
			{
				preview_texture = new Texture2D(tex_width, tex_height);
				raw_image.texture = preview_texture;
			}

			//BlazeFace
			long start_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
			List<AiliaBlazeface.FaceInfo> result_detections = blaze_face.Detection(ailia_face_detector, camera, tex_width, tex_height);
			long end_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
			long detection_time = (end_time - start_time);

			//Draw result
			bool display_blazeface_result = true;
			if(display_blazeface_result){
				for (int i = 0; i < result_detections.Count; i++)
				{
					AiliaBlazeface.FaceInfo face = result_detections[i];
					int fw = (int)(face.width * tex_width);
					int fh = (int)(face.height * tex_height);
					int fx = (int)(face.center.x * tex_width) - fw / 2;
					int fy = (int)(face.center.y * tex_height) - fh / 2;
					if (debug){
						DrawRect2D(Color.blue, fx, fy, fw, fh, tex_width, tex_height);

						for (int k = 0; k < AiliaBlazeface.NUM_KEYPOINTS; k++)
						{
							int x = (int)(face.keypoints[k].x * tex_width);
							int y = (int)(face.keypoints[k].y * tex_height);
							DrawRect2D(Color.blue, x, y, 1, 1, tex_width, tex_height);
						}
					}
				}
			}

			if(ailiaModelType==AiliaGenerativeAdversarialNetworksModels.lipgan){
				//TimeStep
				long gan_time = 0;
				if (play_audio){
					audio_time = (float)audio_source.timeSamples / audio_source.clip.frequency;
				}else{
					audio_time = audio_time + Time.deltaTime;
				}
				if (audio_time > audio.samples / audio.frequency){
					audio_time = audio.samples / audio.frequency;
				}

				//Compute
				long rec_start_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
				Color32 [] generated_image = lip_gan.GenerateImage(ailia_face_gan, camera, tex_width, tex_height, result_detections, audio_time, debug);
				long rec_end_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
				gan_time = (rec_end_time - rec_start_time);
				camera = generated_image;

				//Inference Time
				if (label_text != null)	{
					label_text.text = detection_time + "ms + " + gan_time + "ms\nposition " + audio_time + "\n" + ailia_face_detector.EnvironmentName();
				}

				//Apply
				preview_texture.SetPixels32(camera);
				preview_texture.Apply();
			}
			if(ailiaModelType==AiliaGenerativeAdversarialNetworksModels.gfpgan && one_shot){
				//Compute
				long gan_time = 0;
				long rec_start_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
				Color32 [] generated_image = gfp_gan.GenerateImage(ailia_face_gan, camera, tex_width, tex_height, result_detections, debug);
				long rec_end_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
				gan_time = (rec_end_time - rec_start_time);
				camera = generated_image;
			
				//Inference Time
				if (label_text != null){
					label_text.text = detection_time + "ms + " + gan_time + "ms\n" + ailia_face_detector.EnvironmentName();
				}

				//Apply
				if (one_shot){
					preview_texture.SetPixels32(camera);
					preview_texture.Apply();
				}

				if (image != null){
					one_shot = false;
				}
			}

			// When space key down, draw original image
			if (Input.GetKey(KeyCode.Space))
			{
				raw_image.texture = image;
			}
			else
			{
				raw_image.texture = preview_texture;
			}
		}

		void SetUIProperties()
		{
			if (UICanvas == null) return;
			// Set up UI for AiliaDownloader
			var downloaderProgressPanel = UICanvas.transform.Find("DownloaderProgressPanel");
			ailia_download.DownloaderProgressPanel = downloaderProgressPanel.gameObject;
			// Set up lines
			line_panel = UICanvas.transform.Find("LinePanel").gameObject;
			lines = UICanvas.transform.Find("LinePanel/Lines").gameObject;
			line = UICanvas.transform.Find("LinePanel/Lines/Line").gameObject;
			text_panel = UICanvas.transform.Find("TextPanel").gameObject;
			text_base = UICanvas.transform.Find("TextPanel/TextHolder").gameObject;

			raw_image = UICanvas.transform.Find("RawImage").gameObject.GetComponent<RawImage>();
			label_text = UICanvas.transform.Find("LabelText").gameObject.GetComponent<Text>();
			mode_text = UICanvas.transform.Find("ModeLabel").gameObject.GetComponent<Text>();
		}

		void OnApplicationQuit()
		{
			DestroyAiliaDetector();
			ailia_camera.DestroyCamera();
		}

		void OnDestroy()
		{
			DestroyAiliaDetector();
			ailia_camera.DestroyCamera();
		}
	}
}

