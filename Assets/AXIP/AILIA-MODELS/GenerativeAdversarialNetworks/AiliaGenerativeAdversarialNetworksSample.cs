/* AILIA Unity Plugin LipGAN Sample */
/* Copyright 2023 AXELL CORPORATION */

using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Runtime.InteropServices;

using UnityEngine;
using UnityEngine.UI;

namespace ailiaSDK {
	public class AiliaGenerativeAdversarialNetworksSample : AiliaRenderer {
        public enum AiliaGenerativeAdversarialNetworksModels
        {
            lipgan
        }

		[SerializeField]
		private AiliaGenerativeAdversarialNetworksModels ailiaModelType = AiliaGenerativeAdversarialNetworksModels.lipgan;
		[SerializeField]
		private GameObject UICanvas = null;
		public AudioClip audio = null;
		public AudioSource audio_source = null;
		public bool play_audio = false;
		public Texture2D image = null; 

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

		private AiliaCamera ailia_camera = new AiliaCamera();
		private AiliaDownload ailia_download = new AiliaDownload();

		// AILIA open file
		private bool FileOpened = false;

		// State
		private float audio_time = 0.0f;

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
			SetUIProperties();
			CreateAiliaDetector(ailiaModelType);
			if (image == null){
				ailia_camera.CreateCamera(camera_id);
			}
			lip_gan.SetAudio(audio, debug);
			if (play_audio){
				audio_source.PlayOneShot(audio);
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
			if(ailiaModelType==AiliaGenerativeAdversarialNetworksModels.lipgan){
				for (int i = 0; i < result_detections.Count; i++)
				{
					AiliaBlazeface.FaceInfo face = result_detections[i];
					int fw = (int)(face.width * tex_width);
					int fh = (int)(face.height * tex_height);
					int fx = (int)(face.center.x * tex_width) - fw / 2;
					int fy = (int)(face.center.y * tex_height) - fh / 2;
					DrawRect2D(Color.blue, fx, fy, fw, fh, tex_width, tex_height);

					//for (int k = 0; k < AiliaBlazeface.NUM_KEYPOINTS; k++)
					//{
					//	int x = (int)(face.keypoints[k].x * tex_width);
					//	int y = (int)(face.keypoints[k].y * tex_height);
					//	DrawRect2D(Color.blue, x, y, 1, 1, tex_width, tex_height);
					//}
				}
			}

			//Compute lipgan
			long recognition_time = 0;
			if (play_audio){
				audio_time = audio_source.time;
			}else{
				audio_time = audio_time + Time.deltaTime;
			}
			if (audio_time > audio.samples / audio.frequency){
				audio_time = audio.samples / audio.frequency;
			}
			if(ailiaModelType==AiliaGenerativeAdversarialNetworksModels.lipgan){
				//Compute
				long rec_start_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
				Color32 [] generated_image = lip_gan.GenerateImage(ailia_face_gan, camera, tex_width, tex_height, result_detections, audio_time, debug);
				long rec_end_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
				recognition_time = (rec_end_time - rec_start_time);
				camera = generated_image;
			}

			if (label_text != null)
			{
				label_text.text = detection_time + "ms + " + recognition_time + "ms\nposition " + audio_time + "\n" + ailia_face_detector.EnvironmentName();
			}

			//Apply
			preview_texture.SetPixels32(camera);
			preview_texture.Apply();
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

