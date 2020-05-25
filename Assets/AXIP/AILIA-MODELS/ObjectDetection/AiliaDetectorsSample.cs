/* AILIA Unity Plugin Detector Sample */
/* Copyright 2018-2019 AXELL CORPORATION */

using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Runtime.InteropServices;

using UnityEngine;
using UnityEngine.UI;

namespace ailiaSDK {
	public class AiliaDetectorsSample : AiliaRenderer {
		[SerializeField, HideInInspector]
		private AiliaModelsConst.AiliaModelTypes ailiaModelType = AiliaModelsConst.AiliaModelTypes.yolov3_tiny;
		[SerializeField, HideInInspector]
		private GameObject UICanvas = null;
		//Settings
		public bool gpu_mode = false;
		public int camera_id = 0;

		//Result
		public RawImage raw_image = null;
		public Text label_text = null;
		public Text mode_text = null;

		//Preview
		private Texture2D preview_texture = null;

		//AILIA
		private AiliaDetectorModel ailia_detector = new AiliaDetectorModel();

		private AiliaCamera ailia_camera = new AiliaCamera();
		private AiliaDownload ailia_download = new AiliaDownload();

		// AILIA open file
		private bool FileOpened = false;

		// Compute parameter
		float threshold = 0.2f;
		float iou = 0.25f;

		private void CreateAiliaDetector(AiliaModelsConst.AiliaModelTypes modelType)
		{
			string asset_path = Application.temporaryCachePath;
			uint category_n = 0;
			var urlList = new List<ModelDownloadURL>();
			switch (modelType)
			{
				case AiliaModelsConst.AiliaModelTypes.yolov1_tiny:
					mode_text.text = "ailia Detector";
					threshold = 0.2f;
					iou = 0.45f;
					category_n = 20;
					if (gpu_mode)
					{
						ailia_detector.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
					}
					ailia_detector.Settings(
						AiliaFormat.AILIA_NETWORK_IMAGE_FORMAT_RGB,
						AiliaFormat.AILIA_NETWORK_IMAGE_CHANNEL_FIRST,
						AiliaFormat.AILIA_NETWORK_IMAGE_RANGE_UNSIGNED_FP32,
						AiliaDetector.AILIA_DETECTOR_ALGORITHM_YOLOV1,
						category_n,
						AiliaDetector.AILIA_DETECTOR_FLAG_NORMAL
					);

					urlList.Add(new ModelDownloadURL() { folder_path = "yolov1-tiny", file_name = "yolov1-tiny.prototxt" });
					urlList.Add(new ModelDownloadURL() { folder_path = "yolov1-tiny", file_name = "yolov1-tiny.caffemodel" });

					StartCoroutine(ailia_download.DownloadWithProgressFromURL(urlList, () =>
					{
						FileOpened = ailia_detector.OpenFile(asset_path + "/yolov1-tiny.prototxt", asset_path + "/yolov1-tiny.caffemodel");
					}));
					break;
				case AiliaModelsConst.AiliaModelTypes.yolov3_tiny:
					mode_text.text = "ailia Detector";
					threshold = 0.4f;
					iou = 0.45f;
					category_n = 80;
					if (gpu_mode)
					{
						ailia_detector.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
					}
					ailia_detector.Settings(
						AiliaFormat.AILIA_NETWORK_IMAGE_FORMAT_RGB, 
						AiliaFormat.AILIA_NETWORK_IMAGE_CHANNEL_FIRST, 
						AiliaFormat.AILIA_NETWORK_IMAGE_RANGE_UNSIGNED_FP32,
						AiliaDetector.AILIA_DETECTOR_ALGORITHM_YOLOV3,
						category_n, 
						AiliaDetector.AILIA_DETECTOR_FLAG_NORMAL
					);

					urlList.Add(new ModelDownloadURL() { folder_path = "yolov3-tiny", file_name = "yolov3-tiny.opt.onnx.prototxt" });
					urlList.Add(new ModelDownloadURL() { folder_path = "yolov3-tiny", file_name = "yolov3-tiny.opt.onnx" });

					StartCoroutine(ailia_download.DownloadWithProgressFromURL(urlList, () =>
					{
						FileOpened = ailia_detector.OpenFile(asset_path + "/yolov3-tiny.opt.onnx.prototxt", asset_path + "/yolov3-tiny.opt.onnx");
					}));

					break;
				case AiliaModelsConst.AiliaModelTypes.yolov3_face:
					mode_text.text = "ailia FaceDetector";
					//Face Detection
					threshold = 0.2f;
					iou = 0.45f;
					category_n = 1;
					if (gpu_mode)
					{
						ailia_detector.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
					}
					ailia_detector.Settings(
						AiliaFormat.AILIA_NETWORK_IMAGE_FORMAT_RGB,
						AiliaFormat.AILIA_NETWORK_IMAGE_CHANNEL_FIRST,
						AiliaFormat.AILIA_NETWORK_IMAGE_RANGE_UNSIGNED_FP32,
						AiliaDetector.AILIA_DETECTOR_ALGORITHM_YOLOV3, 
						category_n,
						AiliaDetector.AILIA_DETECTOR_FLAG_NORMAL
					);

					urlList.Add(new ModelDownloadURL() { folder_path = "yolov3-face", file_name = "yolov3-face.opt.onnx.prototxt" });
					urlList.Add(new ModelDownloadURL() { folder_path = "yolov3-face", file_name = "yolov3-face.opt.onnx" });

					StartCoroutine(ailia_download.DownloadWithProgressFromURL(urlList, () =>
					{
						FileOpened = ailia_detector.OpenFile(asset_path + "/yolov3-face.opt.onnx.prototxt", asset_path + "/yolov3-face.opt.onnx");
					}));

					break;
				default:
					Debug.Log("Others ailia models are working in progress.");
					break;
			}
		}


		private void DestroyAiliaDetector()
		{
			ailia_detector.Close();
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
				ailia_detector.Close();
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

			//Detection
			long start_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond; ;
			List<AiliaDetector.AILIADetectorObject> list = ailia_detector.ComputeFromImageB2T(camera, tex_width, tex_height, threshold, iou);
			long end_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond; ;

			long start_time_class = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
			foreach (AiliaDetector.AILIADetectorObject obj in list)
			{
				Classifier(ailiaModelType, obj, camera, tex_width, tex_height);
			}
			long end_time_class = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

			if (label_text != null)
			{
				label_text.text = (end_time - start_time) + (end_time_class - start_time_class) + "ms\n" + ailia_detector.EnvironmentName();
			}

			//Apply
			preview_texture.SetPixels32(camera);
			preview_texture.Apply();
		}

		private void Classifier(AiliaModelsConst.AiliaModelTypes modelType, AiliaDetector.AILIADetectorObject box, Color32[] camera, int tex_width, int tex_height)
		{
			switch (modelType)
			{
				case AiliaModelsConst.AiliaModelTypes.yolov1_tiny:
					ObjClassifierYolov1Tiny(box, camera, tex_width, tex_height);
					break;
				case AiliaModelsConst.AiliaModelTypes.yolov3_tiny:
					ObjClassifier(box, camera, tex_width, tex_height);
					break;
				case AiliaModelsConst.AiliaModelTypes.yolov3_face:
					FaceClassifier(box, camera, tex_width, tex_height);
					break;
				default:
					break;
			}
		}

		private void ObjClassifierYolov1Tiny(AiliaDetector.AILIADetectorObject box, Color32[] camera, int tex_width, int tex_height)
		{
			//Convert to pixel domain
			int x1 = (int)(box.x * tex_width);
			int y1 = (int)(box.y * tex_height);
			int x2 = (int)((box.x + box.w) * tex_width);
			int y2 = (int)((box.y + box.h) * tex_height);

			int w = (x2 - x1);
			int h = (y2 - y1);

			if (w <= 0 || h <= 0)
			{
				return;
			}

			Color color = Color.white;
			color = Color.HSVToRGB(box.category / 20.0f, 1.0f, 1.0f);
			DrawRect2D(color, x1, y1, w, h, tex_width, tex_height);

			float p = (int)(box.prob * 100) / 100.0f;
			string text = "";
			text += AiliaClassifierLabel.VOC_CATEGORY[box.category];
			text += " " + p;
			int margin = 4;
			DrawText(color, text, x1 + margin, y1 + margin, tex_width, tex_height);
		}

		private void ObjClassifier(AiliaDetector.AILIADetectorObject box, Color32[] camera, int tex_width, int tex_height)
		{
			//Convert to pixel domain
			int x1 = (int)(box.x * tex_width);
			int y1 = (int)(box.y * tex_height);
			int x2 = (int)((box.x + box.w) * tex_width);
			int y2 = (int)((box.y + box.h) * tex_height);

			int w = (x2 - x1);
			int h = (y2 - y1);

			if (w <= 0 || h <= 0)
			{
				return;
			}

			Color color = Color.white;
			color = Color.HSVToRGB(box.category / 80.0f, 1.0f, 1.0f);
			DrawRect2D(color, x1, y1, w, h, tex_width, tex_height);

			float p = (int)(box.prob * 100) / 100.0f;
			string text = "";
			text += AiliaClassifierLabel.COCO_CATEGORY[box.category];
			text += " " + p;
			int margin = 4;
			DrawText(color, text, x1 + margin, y1 + margin, tex_width, tex_height);
		}

		private void FaceClassifier(AiliaDetector.AILIADetectorObject box, Color32[] camera, int tex_width, int tex_height)
		{
			//Convert to pixel position
			int x1 = (int)(box.x * tex_width);
			int y1 = (int)(box.y * tex_height);
			int x2 = (int)((box.x + box.w) * tex_width);
			int y2 = (int)((box.y + box.h) * tex_height);

			int w = (x2 - x1);
			int h = (y2 - y1);

			float expand = 1.4f;
			x1 -= (int)(w * expand - w) / 2;
			y1 -= (int)(h * expand - h) / 2;
			w = (int)(w * expand);
			h = (int)(h * expand);

			if (w <= 0 || h <= 0)
			{
				return;
			}

			//Draw Box
			Color color = Color.white;
			color = Color.HSVToRGB(box.category / 7.0f, 1.0f, 1.0f);
			DrawRect2D(color, x1, y1, w, h, tex_width, tex_height);
			string text = "";
			text += "face: " + (int)(box.prob * 100) / 100.0f;

			int margin = 4;
			DrawText(color, text, x1 + margin, y1 + margin, tex_width, tex_height);
		}

		void SetUIProperties()
		{
			if (UICanvas == null) return;
			// Set up UI for AiliaDownloader
			var downloaderProgressPanel = UICanvas.transform.Find("DownloaderProgressPanel");
			ailia_download.DownloaderProgressPanel = downloaderProgressPanel.gameObject;
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

