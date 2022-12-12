/* AILIA Unity Plugin Hand Recognizer Sample */
/* Copyright 2022 AXELL CORPORATION */

using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Runtime.InteropServices;

using UnityEngine;
using UnityEngine.UI;

namespace ailiaSDK {
	public class AiliaHandRecognizerSample : AiliaRenderer {
		// Choose model
		public enum HandRecognizerModels
		{
				blazehand
		}

		[SerializeField]
		private HandRecognizerModels ailiaModelType = HandRecognizerModels.blazehand;
		[SerializeField]
		private GameObject UICanvas = null;

		//Settings
		[SerializeField]
		private bool gpu_mode = false;
		[SerializeField]
		private int camera_id = 0;
		

		//Result
		RawImage raw_image = null;
		Text label_text = null;
		Text mode_text = null;

		//Preview
		private Texture2D preview_texture = null;

		//AILIA
		private AiliaModel ailia_hand_detection = new AiliaModel();
		private AiliaModel ailia_hand_landmark = new AiliaModel();

		private AiliaBlazepalm blaze_palm = new AiliaBlazepalm();
		private AiliaBlazehand blaze_hand = new AiliaBlazehand();

		private AiliaCamera ailia_camera = new AiliaCamera();
		private AiliaDownload ailia_download = new AiliaDownload();

		// AILIA open file
		private bool FileOpened = false;

		private void CreateAiliaRecognizer(HandRecognizerModels modelType)
		{
			string asset_path = Application.temporaryCachePath;
			var urlList = new List<ModelDownloadURL>();
			if (gpu_mode)
			{
				ailia_hand_detection.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
				ailia_hand_landmark.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
			}
			switch (modelType)
			{		
				case HandRecognizerModels.blazehand:
					mode_text.text = "ailia hand Recognizer";

					urlList.Add(new ModelDownloadURL() { folder_path = "blazepalm", file_name = "blazepalm.onnx.prototxt" });
					urlList.Add(new ModelDownloadURL() { folder_path = "blazepalm", file_name = "blazepalm.onnx" });
					urlList.Add(new ModelDownloadURL() { folder_path = "blazehand", file_name = "blazehand.onnx.prototxt" });
					urlList.Add(new ModelDownloadURL() { folder_path = "blazehand", file_name = "blazehand.onnx" });

					StartCoroutine(ailia_download.DownloadWithProgressFromURL(urlList, () =>
					{
						FileOpened = ailia_hand_detection.OpenFile(asset_path + "/blazepalm.onnx.prototxt", asset_path + "/blazepalm.onnx");
						FileOpened = ailia_hand_landmark.OpenFile(asset_path + "/blazehand.onnx.prototxt", asset_path + "/blazehand.onnx");
					}));

					break;

				default:
					Debug.Log("Others ailia models are working in progress.");
					break;
			}
		}


		private void DestroyAiliaDetector()
		{
			ailia_hand_detection.Close();
			ailia_hand_landmark.Close();
		}

		// Use this for initialization
		void Start()
		{
			SetUIProperties();
			CreateAiliaRecognizer(ailiaModelType);
			ailia_camera.CreateCamera(camera_id);
		}

		// Update is called once per frame
		void Update()
		{
			if (!ailia_camera.IsEnable())
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
			int tex_width = ailia_camera.GetWidth();
			int tex_height = ailia_camera.GetHeight();
			if (preview_texture == null)
			{
				preview_texture = new Texture2D(tex_width, tex_height);
				raw_image.texture = preview_texture;
			}
			Color32[] camera = ailia_camera.GetPixels32();
			Debug.Log(camera.Length);

			//Blazehand
			// Detection
			long detection_start_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
			List<AiliaBlazepalm.HandInfo> result_detections = blaze_palm.Detection(ailia_hand_detection, camera, tex_width, tex_height);
			long detection_end_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
			long detection_time = (detection_end_time - detection_start_time);

			// Landmark
			long landmark_start_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
			List<AiliaBlazehand.LandmarkInfo> result_landmark = blaze_hand.Detection(ailia_hand_landmark, camera, tex_width, tex_height, result_detections);
			long landmark_end_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
			long landmark_time = (landmark_end_time - landmark_start_time);

			// Draw result
			if(ailiaModelType==HandRecognizerModels.blazehand){
				// Blazepalm
				for (int i = 0; i < result_detections.Count; i++)
				{
					AiliaBlazepalm.HandInfo hand = result_detections[i];
					int fw = (int)(hand.width * tex_width);
					int fh = (int)(hand.height * tex_height);
					int fx = (int)(hand.center.x * tex_width) - fw / 2;
					int fy = (int)(hand.center.y * tex_height) - fh / 2;
					DrawRect2D(Color.blue, fx, fy, fw, fh, tex_width, tex_height);

					for (int k = 0; k < AiliaBlazepalm.NUM_KEYPOINTS; k++)
					{
						int x = (int)(hand.keypoints[k].x * tex_width);
						int y = (int)(hand.keypoints[k].y * tex_height);
						DrawRect2D(Color.blue, x, y, 1, 1, tex_width, tex_height);
					}

					
				}
				// Blazehand
				for(int i = 0; i < result_detections.Count; i++)
				{
					AiliaBlazehand.LandmarkInfo hand = result_landmark[i];
					int fw = (int)(hand.width * tex_width);
					int fh = (int)(hand.height * tex_height);
					int fx = (int)(hand.center.x * tex_width) - fw / 2;
					int fy = (int)(hand.center.y * tex_height) - fh / 2;
					DrawAffine2D(Color.green, fx, fy, fw, fh, tex_width, tex_height, hand.theta);

					float scale = 1.0f * fw / AiliaBlazehand.DETECTION_WIDTH;

					float ss=(float)System.Math.Sin(hand.theta);
					float cs=(float)System.Math.Cos(hand.theta);

					for (int k = 0; k < AiliaBlazehand.NUM_KEYPOINTS; k++)
					{
						int x = (int)(hand.center.x * tex_width  + ((hand.keypoints[k].x - AiliaBlazehand.DETECTION_WIDTH/2) * cs + (hand.keypoints[k].y - AiliaBlazehand.DETECTION_HEIGHT/2) * -ss)* scale);
						int y = (int)(hand.center.y * tex_height + ((hand.keypoints[k].x - AiliaBlazehand.DETECTION_WIDTH/2) * ss + (hand.keypoints[k].y - AiliaBlazehand.DETECTION_HEIGHT/2) *  cs)* scale);
						DrawRect2D(Color.red, x, y, 1, 1, tex_width, tex_height);
					}
					for (int k = 0; k < 21; k++)
					{
						int x0 = (int)(hand.center.x * tex_width  + ((hand.keypoints[AiliaBlazehand.HAND_CONNECTIONS[k,0]].x - AiliaBlazehand.DETECTION_WIDTH/2) * cs + (hand.keypoints[AiliaBlazehand.HAND_CONNECTIONS[k,0]].y - AiliaBlazehand.DETECTION_HEIGHT/2) * -ss)* scale);
						int y0 = (int)(hand.center.y * tex_height + ((hand.keypoints[AiliaBlazehand.HAND_CONNECTIONS[k,0]].x - AiliaBlazehand.DETECTION_WIDTH/2) * ss + (hand.keypoints[AiliaBlazehand.HAND_CONNECTIONS[k,0]].y - AiliaBlazehand.DETECTION_HEIGHT/2) *  cs)* scale);

						int x1 = (int)(hand.center.x * tex_width  + ((hand.keypoints[AiliaBlazehand.HAND_CONNECTIONS[k,1]].x - AiliaBlazehand.DETECTION_WIDTH/2) * cs + (hand.keypoints[AiliaBlazehand.HAND_CONNECTIONS[k,1]].y - AiliaBlazehand.DETECTION_HEIGHT/2) * -ss)* scale);
						int y1 = (int)(hand.center.y * tex_height + ((hand.keypoints[AiliaBlazehand.HAND_CONNECTIONS[k,1]].x - AiliaBlazehand.DETECTION_WIDTH/2) * ss + (hand.keypoints[AiliaBlazehand.HAND_CONNECTIONS[k,1]].y - AiliaBlazehand.DETECTION_HEIGHT/2) *  cs)* scale);
						DrawLine(Color.green, x0, y0, 0, x1, y1, 0, tex_width, tex_height);

					}
				}
			}

				

			if (label_text != null)
			{
				if(ailiaModelType==HandRecognizerModels.blazehand){
					label_text.text = detection_time + "ms + " + landmark_time + "ms\n" + ailia_hand_detection.EnvironmentName();
				}
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