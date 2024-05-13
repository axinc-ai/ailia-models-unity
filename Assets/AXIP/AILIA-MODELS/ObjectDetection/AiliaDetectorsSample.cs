/* AILIA Unity Plugin Detector Sample */
/* Copyright 2018-2019 AXELL CORPORATION */

using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Runtime.InteropServices;

using UnityEngine;
using UnityEngine.UI;

using ailia;

namespace ailiaSDK {
	public class AiliaDetectorsSample : AiliaRenderer {
		public enum DetectorModels
		{
			yolov1_tiny,
			yolov1_face,
			yolov2,
			yolov2_tiny,
			yolov3,
			yolov3_tiny,
			yolov3_face,
			yolov3_hand,
			yolov4,
			yolov4_tiny,
			mobilenet_ssd,
			yolox_nano,
			yolox_tiny,
			yolox_s,
			yolox_tiny_nnapi,
			yolox_s_nnapi,
		}

		[SerializeField]
		private DetectorModels ailiaModelType = DetectorModels.yolov3_tiny;
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
		private AiliaDetectorModel ailia_detector = new AiliaDetectorModel();
		private AiliaTFLiteYoloxSample ailia_tflite = new AiliaTFLiteYoloxSample();

		private AiliaCamera ailia_camera = new AiliaCamera();
		private AiliaDownload ailia_download = new AiliaDownload();

		// Pretrained model array for mobilenet_ssd, default value is mb2-ssd-lite
		[SerializeField, HideInInspector]
		private string pretrainedModel = "mb2-ssd-lite";

		// AILIA open file
		private bool FileOpened = false;

		// Compute parameter
		float threshold = 0.2f;
		float iou = 0.25f;
		string[] classifierLabel;
		uint category_n = 1;

		private void CreateAiliaDetector(DetectorModels modelType)
		{
			string asset_path = Application.temporaryCachePath;
			var urlList = new List<ModelDownloadURL>();
			if (gpu_mode)
			{
				ailia_detector.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
			}
			string base_url = "";
			switch (modelType)
			{
				case DetectorModels.yolov1_tiny:
					mode_text.text = "ailia yolov1-tiny Detector";
					classifierLabel = AiliaClassifierLabel.VOC_CATEGORY;
					threshold = 0.2f;
					iou = 0.45f;
					category_n = 20;

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
				case DetectorModels.yolov1_face:
					mode_text.text = "ailia yolov1-face FaceDetector";
					classifierLabel = new string[] { "face" };
					threshold = 0.2f;
					iou = 0.45f;
					category_n = 1;

					ailia_detector.Settings(
						AiliaFormat.AILIA_NETWORK_IMAGE_FORMAT_RGB,
						AiliaFormat.AILIA_NETWORK_IMAGE_CHANNEL_FIRST,
						AiliaFormat.AILIA_NETWORK_IMAGE_RANGE_UNSIGNED_FP32,
						AiliaDetector.AILIA_DETECTOR_ALGORITHM_YOLOV1,
						category_n,
						AiliaDetector.AILIA_DETECTOR_FLAG_NORMAL
					);

					urlList.Add(new ModelDownloadURL() { folder_path = "yolov1-face", file_name = "yolov1-face.prototxt" });
					urlList.Add(new ModelDownloadURL() { folder_path = "yolov1-face", file_name = "yolov1-face.caffemodel" });

					StartCoroutine(ailia_download.DownloadWithProgressFromURL(urlList, () =>
					{
						FileOpened = ailia_detector.OpenFile(asset_path + "/yolov1-face.prototxt", asset_path + "/yolov1-face.caffemodel");
					}));

					break;
				case DetectorModels.yolov2:
					mode_text.text = "ailia yolov2 Detector";
					classifierLabel = AiliaClassifierLabel.COCO_CATEGORY;
					threshold = 0.2f;
					iou = 0.45f;
					category_n = 80;

					ailia_detector.Settings(
						AiliaFormat.AILIA_NETWORK_IMAGE_FORMAT_RGB,
						AiliaFormat.AILIA_NETWORK_IMAGE_CHANNEL_FIRST,
						AiliaFormat.AILIA_NETWORK_IMAGE_RANGE_SIGNED_FP32,
						AiliaDetector.AILIA_DETECTOR_ALGORITHM_YOLOV2,
						category_n,
						AiliaDetector.AILIA_DETECTOR_FLAG_NORMAL
					);

					urlList.Add(new ModelDownloadURL() { folder_path = "yolov2", file_name = "yolov2.onnx.prototxt" });
					urlList.Add(new ModelDownloadURL() { folder_path = "yolov2", file_name = "yolov2.onnx" });

					StartCoroutine(ailia_download.DownloadWithProgressFromURL(urlList, () =>
					{
						FileOpened = ailia_detector.OpenFile(asset_path + "/yolov2.onnx.prototxt", asset_path + "/yolov2.onnx");
						ailia_detector.Anchors(AiliaClassifierLabel.COCO_ANCHORS);
					}));

					break;
				case DetectorModels.yolov2_tiny:
					mode_text.text = "ailia yolov2-tiny Detector";
					classifierLabel = AiliaClassifierLabel.COCO_CATEGORY;
					threshold = 0.2f;
					iou = 0.45f;
					category_n = 80;

					ailia_detector.Settings(
						AiliaFormat.AILIA_NETWORK_IMAGE_FORMAT_RGB,
						AiliaFormat.AILIA_NETWORK_IMAGE_CHANNEL_FIRST,
						AiliaFormat.AILIA_NETWORK_IMAGE_RANGE_SIGNED_FP32,
						AiliaDetector.AILIA_DETECTOR_ALGORITHM_YOLOV2,
						category_n,
						AiliaDetector.AILIA_DETECTOR_FLAG_NORMAL
					);

					urlList.Add(new ModelDownloadURL() { folder_path = "yolov2-tiny", file_name = "yolov2-tiny-coco.onnx.prototxt" });
					urlList.Add(new ModelDownloadURL() { folder_path = "yolov2-tiny", file_name = "yolov2-tiny-coco.onnx" });

					StartCoroutine(ailia_download.DownloadWithProgressFromURL(urlList, () =>
					{
						FileOpened = ailia_detector.OpenFile(asset_path + "/yolov2-tiny-coco.onnx.prototxt", asset_path + "/yolov2-tiny-coco.onnx");
						ailia_detector.Anchors(AiliaClassifierLabel.COCO_ANCHORS);
					}));

					break;
				case DetectorModels.yolov3:
					mode_text.text = "ailia yolov3 Detector";
					classifierLabel = AiliaClassifierLabel.COCO_CATEGORY;
					threshold = 0.4f;
					iou = 0.45f;
					category_n = 80;

					ailia_detector.Settings(
						AiliaFormat.AILIA_NETWORK_IMAGE_FORMAT_RGB,
						AiliaFormat.AILIA_NETWORK_IMAGE_CHANNEL_FIRST,
						AiliaFormat.AILIA_NETWORK_IMAGE_RANGE_UNSIGNED_FP32,
						AiliaDetector.AILIA_DETECTOR_ALGORITHM_YOLOV3,
						category_n,
						AiliaDetector.AILIA_DETECTOR_FLAG_NORMAL
					);

					urlList.Add(new ModelDownloadURL() { folder_path = "yolov3", file_name = "yolov3.opt2.onnx.prototxt" });
					urlList.Add(new ModelDownloadURL() { folder_path = "yolov3", file_name = "yolov3.opt2.onnx" });

					StartCoroutine(ailia_download.DownloadWithProgressFromURL(urlList, () =>
					{
						FileOpened = ailia_detector.OpenFile(asset_path + "/yolov3.opt2.onnx.prototxt", asset_path + "/yolov3.opt2.onnx");
					}));

					break;
				case DetectorModels.yolov3_tiny:
					mode_text.text = "ailia yolov3-tiny Detector";
					classifierLabel = AiliaClassifierLabel.COCO_CATEGORY;
					threshold = 0.4f;
					iou = 0.45f;
					category_n = 80;

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
				case DetectorModels.yolov3_face:
					mode_text.text = "ailia yolov3-face FaceDetector";
					classifierLabel = new string[] { "face" };
					//Face Detection
					threshold = 0.2f;
					iou = 0.45f;
					category_n = 1;

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
				case DetectorModels.yolov3_hand:
					mode_text.text = "ailia yolov3-hand Detector";
					classifierLabel = new string[] { "hand" };
					threshold = 0.4f;
					iou = 0.45f;
					category_n = 1;

					ailia_detector.Settings(
						AiliaFormat.AILIA_NETWORK_IMAGE_FORMAT_RGB,
						AiliaFormat.AILIA_NETWORK_IMAGE_CHANNEL_FIRST,
						AiliaFormat.AILIA_NETWORK_IMAGE_RANGE_UNSIGNED_FP32,
						AiliaDetector.AILIA_DETECTOR_ALGORITHM_YOLOV3,
						category_n,
						AiliaDetector.AILIA_DETECTOR_FLAG_NORMAL
					);

					urlList.Add(new ModelDownloadURL() { folder_path = "yolov3-hand", file_name = "yolov3-hand.opt.onnx.prototxt" });
					urlList.Add(new ModelDownloadURL() { folder_path = "yolov3-hand", file_name = "yolov3-hand.opt.onnx" });

					StartCoroutine(ailia_download.DownloadWithProgressFromURL(urlList, () =>
					{
						FileOpened = ailia_detector.OpenFile(asset_path + "/yolov3-hand.opt.onnx.prototxt", asset_path + "/yolov3-hand.opt.onnx");
					}));

					break;

				case DetectorModels.yolov4:
					mode_text.text = "ailia yolov4 Detector";
					classifierLabel = AiliaClassifierLabel.COCO_CATEGORY;
					threshold = 0.4f;
					iou = 0.45f;
					category_n = 80;

					ailia_detector.Settings(
						AiliaFormat.AILIA_NETWORK_IMAGE_FORMAT_RGB,
						AiliaFormat.AILIA_NETWORK_IMAGE_CHANNEL_FIRST,
						AiliaFormat.AILIA_NETWORK_IMAGE_RANGE_UNSIGNED_FP32,
						AiliaDetector.AILIA_DETECTOR_ALGORITHM_YOLOV4,
						category_n,
						AiliaDetector.AILIA_DETECTOR_FLAG_NORMAL
					);

					urlList.Add(new ModelDownloadURL() { folder_path = "yolov4", file_name = "yolov4.onnx.prototxt" });
					urlList.Add(new ModelDownloadURL() { folder_path = "yolov4", file_name = "yolov4.onnx" });

					StartCoroutine(ailia_download.DownloadWithProgressFromURL(urlList, () =>
					{
						FileOpened = ailia_detector.OpenFile(asset_path + "/yolov4.onnx.prototxt", asset_path + "/yolov4.onnx");
					}));

					break;
				
				case DetectorModels.yolov4_tiny:
					mode_text.text = "ailia yolov4-tiny Detector";
					classifierLabel = AiliaClassifierLabel.COCO_CATEGORY;
					threshold = 0.25f;
					iou = 0.45f;
					category_n = 80;

					ailia_detector.Settings(
						AiliaFormat.AILIA_NETWORK_IMAGE_FORMAT_RGB,
						AiliaFormat.AILIA_NETWORK_IMAGE_CHANNEL_FIRST,
						AiliaFormat.AILIA_NETWORK_IMAGE_RANGE_UNSIGNED_FP32,
						AiliaDetector.AILIA_DETECTOR_ALGORITHM_YOLOV4,
						category_n,
						AiliaDetector.AILIA_DETECTOR_FLAG_NORMAL
					);

					urlList.Add(new ModelDownloadURL() { folder_path = "yolov4-tiny", file_name = "yolov4-tiny.onnx.prototxt" });
					urlList.Add(new ModelDownloadURL() { folder_path = "yolov4-tiny", file_name = "yolov4-tiny.onnx" });

					StartCoroutine(ailia_download.DownloadWithProgressFromURL(urlList, () =>
					{
						FileOpened = ailia_detector.OpenFile(asset_path + "/yolov4-tiny.onnx.prototxt", asset_path + "/yolov4-tiny.onnx");
					}));

					break;

				case DetectorModels.yolox_nano:
				case DetectorModels.yolox_tiny:
				case DetectorModels.yolox_s:
					string model="";
					if(modelType==DetectorModels.yolox_nano){
						model="nano";
					}
					if(modelType==DetectorModels.yolox_tiny){
						model="tiny";
					}
					if(modelType==DetectorModels.yolox_s){
						model="s";
					}

					mode_text.text = "ailia yolox "+model+" Detector";
					classifierLabel = AiliaClassifierLabel.COCO_CATEGORY;
					threshold = 0.25f;
					iou = 0.45f;
					category_n = 80;

					ailia_detector.Settings(
						AiliaFormat.AILIA_NETWORK_IMAGE_FORMAT_BGR,
						AiliaFormat.AILIA_NETWORK_IMAGE_CHANNEL_FIRST,
						AiliaFormat.AILIA_NETWORK_IMAGE_RANGE_UNSIGNED_INT8,
						AiliaDetector.AILIA_DETECTOR_ALGORITHM_YOLOX,
						category_n,
						AiliaDetector.AILIA_DETECTOR_FLAG_NORMAL
					);

					urlList.Add(new ModelDownloadURL() { folder_path = "yolox", file_name = "yolox_"+model+".opt.onnx.prototxt" });
					urlList.Add(new ModelDownloadURL() { folder_path = "yolox", file_name = "yolox_"+model+".opt.onnx" });

					StartCoroutine(ailia_download.DownloadWithProgressFromURL(urlList, () =>
					{
						FileOpened = ailia_detector.OpenFile(asset_path + "/yolox_"+model+".opt.onnx.prototxt", asset_path + "/yolox_"+model+".opt.onnx");
					}));

					break;

				case DetectorModels.yolox_tiny_nnapi:
				case DetectorModels.yolox_s_nnapi:
					if (modelType == DetectorModels.yolox_tiny_nnapi){
						mode_text.text = "ailia yolox tiny NNAPI Detector";
					}else{
						mode_text.text = "ailia yolox s NNAPI Detector";
					}
					classifierLabel = AiliaClassifierLabel.COCO_CATEGORY;
					threshold = 0.25f;
					iou = 0.45f;
					category_n = 80;

					if (modelType == DetectorModels.yolox_tiny_nnapi){
						model = "yolox_tiny_full_integer_quant.opt.tflite";
					}else{
						model = "yolox_s_full_integer_quant.opt.tflite";
					}

					urlList.Add(new ModelDownloadURL() { folder_path = "yolox", file_name = model});
					base_url = "https://storage.googleapis.com/ailia-models-tflite/";
					StartCoroutine(ailia_download.DownloadWithProgressFromURL(urlList, () =>
					{
						FileOpened = ailia_tflite.CreateAiliaTFLite(asset_path + "/" + model);
					}, base_url));
					break;

				case DetectorModels.mobilenet_ssd:
					mode_text.text = "ailia mobilenet_ssd Detector. Pretraine model : " + pretrainedModel;
					classifierLabel = AiliaClassifierLabel.VOC_CATEGORY;
					threshold = 0.4f;
					iou = 0.45f;
					category_n = 80;

					ailia_detector.Settings(
						AiliaFormat.AILIA_NETWORK_IMAGE_FORMAT_RGB,
						AiliaFormat.AILIA_NETWORK_IMAGE_CHANNEL_FIRST,
						AiliaFormat.AILIA_NETWORK_IMAGE_RANGE_UNSIGNED_FP32,
						AiliaDetector.AILIA_DETECTOR_ALGORITHM_SSD,
						category_n,
						AiliaDetector.AILIA_DETECTOR_FLAG_NORMAL
					);

					var model_path = pretrainedModel + ".onnx.prototxt";
					var weight_path = pretrainedModel + ".onnx";
					urlList.Add(new ModelDownloadURL() { folder_path = "mobilenet_ssd", file_name = model_path });
					urlList.Add(new ModelDownloadURL() { folder_path = "mobilenet_ssd", file_name = weight_path });

					StartCoroutine(ailia_download.DownloadWithProgressFromURL(urlList, () =>
					{
						FileOpened = ailia_detector.OpenFile(asset_path + "/" + model_path, asset_path + "/" + weight_path);
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
			ailia_tflite.DestroyAiliaTFLite();
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

			//Detection
			long start_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond; ;
			List<AiliaDetector.AILIADetectorObject> list = new List<AiliaDetector.AILIADetectorObject>();
			if (ailiaModelType == DetectorModels.yolox_tiny_nnapi || ailiaModelType == DetectorModels.yolox_s_nnapi){
				list = ailia_tflite.ComputeFromImageB2T(camera, tex_width, tex_height, threshold, iou);
			}else{
				list = ailia_detector.ComputeFromImageB2T(camera, tex_width, tex_height, threshold, iou);
			}
			long end_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond; ;

			long start_time_class = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
			foreach (AiliaDetector.AILIADetectorObject obj in list)
			{
				DisplayDetectedResult(obj, camera, tex_width, tex_height);
			}
			long end_time_class = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

			if (label_text != null)
			{
				string env_name;
				if (ailiaModelType == DetectorModels.yolox_tiny_nnapi || ailiaModelType == DetectorModels.yolox_s_nnapi){
					env_name = ailia_tflite.GetDeviceName();
				}else{
					env_name = ailia_detector.EnvironmentName();
				}
				label_text.text = (end_time - start_time) + (end_time_class - start_time_class) + "ms\n" + env_name;
			}

			//Apply
			preview_texture.SetPixels32(camera);
			preview_texture.Apply();
		}

		private void DisplayDetectedResult(AiliaDetector.AILIADetectorObject box, Color32[] camera, int tex_width, int tex_height)
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
			color = Color.HSVToRGB((float)box.category / category_n, 1.0f, 1.0f);
			DrawRect2D(color, x1, y1, w, h, tex_width, tex_height);

			float p = (int)(box.prob * 100) / 100.0f;
			string text = "";
			text += classifierLabel[box.category];
			text += " " + p;
			int margin = 4;
			DrawText(color, text, x1 + margin, y1 + margin, tex_width, tex_height);
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

