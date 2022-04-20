/* AILIA Unity Plugin Detector Sample */
/* Copyright 2021 AXELL CORPORATION */

using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Runtime.InteropServices;

using UnityEngine;
using UnityEngine.UI;

namespace ailiaSDK {
	public class AiliaFaceDetectorsSample : AiliaRenderer {
        public enum FaceDetectorModels
        {
            blazeface,
            facemesh
        }

		[SerializeField]
		private FaceDetectorModels ailiaModelType = FaceDetectorModels.blazeface;
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
		private AiliaModel ailia_face_detector = new AiliaModel();
		private AiliaModel ailia_face_recognizer = new AiliaModel();

		private AiliaBlazefaceSample blaze_face = new AiliaBlazefaceSample();
		private AiliaFaceMeshSample face_mesh = new AiliaFaceMeshSample();

		private AiliaCamera ailia_camera = new AiliaCamera();
		private AiliaDownload ailia_download = new AiliaDownload();

		// AILIA open file
		private bool FileOpened = false;

		private void CreateAiliaDetector(FaceDetectorModels modelType)
		{
			string asset_path = Application.temporaryCachePath;
			var urlList = new List<ModelDownloadURL>();
			if (gpu_mode)
			{
				ailia_face_detector.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
				ailia_face_recognizer.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
			}
			switch (modelType)
			{		
				case FaceDetectorModels.blazeface:
					mode_text.text = "ailia face Detector";

					urlList.Add(new ModelDownloadURL() { folder_path = "blazeface", file_name = "blazeface.onnx.prototxt" });
					urlList.Add(new ModelDownloadURL() { folder_path = "blazeface", file_name = "blazeface.onnx" });

					StartCoroutine(ailia_download.DownloadWithProgressFromURL(urlList, () =>
					{
						FileOpened = ailia_face_detector.OpenFile(asset_path + "/blazeface.onnx.prototxt", asset_path + "/blazeface.onnx");
					}));

					break;

				case FaceDetectorModels.facemesh:
					mode_text.text = "ailia face Recognizer";

					urlList.Add(new ModelDownloadURL() { folder_path = "blazeface", file_name = "blazeface.onnx.prototxt" });
					urlList.Add(new ModelDownloadURL() { folder_path = "blazeface", file_name = "blazeface.onnx" });
					urlList.Add(new ModelDownloadURL() { folder_path = "facemesh", file_name = "facemesh.onnx.prototxt" });
					urlList.Add(new ModelDownloadURL() { folder_path = "facemesh", file_name = "facemesh.onnx" });

					StartCoroutine(ailia_download.DownloadWithProgressFromURL(urlList, () =>
					{
						FileOpened = ailia_face_detector.OpenFile(asset_path + "/blazeface.onnx.prototxt", asset_path + "/blazeface.onnx");
						FileOpened = ailia_face_recognizer.OpenFile(asset_path + "/facemesh.onnx.prototxt", asset_path + "/facemesh.onnx");
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
			ailia_face_recognizer.Close();
		}

		// Use this for initialization
		void Start()
		{
			SetUIProperties();
			CreateAiliaDetector(ailiaModelType);
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

			//BlazeFace
			long start_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
			List<AiliaBlazefaceSample.FaceInfo> result_detections = blaze_face.Detection(ailia_face_detector, camera, tex_width, tex_height);
			long end_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

			//Draw result
			for (int i = 0; i < result_detections.Count; i++)
			{
				AiliaBlazefaceSample.FaceInfo face = result_detections[i];
				int fw = (int)(face.width * tex_width);
				int fh = (int)(face.height * tex_height);
				int fx = (int)(face.center.x * tex_width) - fw / 2;
				int fy = (int)(face.center.y * tex_height) - fh / 2;
				//Debug.Log("==>> " + fx + ", " + fy + ", " + fw + ", " + fh);
				DrawRect2D(Color.blue, fx, fy, fw, fh, tex_width, tex_height);

				/*
				Vector2 from = new Vector2((fx + fx + fw) * 0.5f, (fy + fy + fh * 0.7f) * 0.5f);
				Vector2 ahead = ((face.keypoints[0] + face.keypoints[1]) * 0.5f + face.keypoints[2]) * 0.5f;
				ahead.x *= tex_width;
				ahead.y *= tex_height;
				const float length = 3;
				ahead = ahead + (ahead - from) * length;
				Vector2 offset = new Vector2(face.keypoints[2].x * tex_width - from.x, face.keypoints[2].y * tex_height - from.y);
				from += offset;
				ahead += offset;
				DrawLine(Color.red, (int)from.x, (int)from.y, 0, (int)ahead.x, (int)ahead.y, 0, tex_width, tex_height);//, 1.5f);
				*/

				for (int k = 0; k < AiliaBlazefaceSample.NUM_KEYPOINTS; k++)
				{
					int x = (int)(face.keypoints[k].x * tex_width);
					int y = (int)(face.keypoints[k].y * tex_height);
					DrawRect2D(Color.green, x, y, 1, 1, tex_width, tex_height);
				}
			}

			//Estimate ROI



			if (label_text != null)
			{
				label_text.text = (end_time - start_time) + "ms\n" + ailia_face_detector.EnvironmentName();
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

