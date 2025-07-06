/* AILIA Unity Plugin Tracking Sample */
/* Copyright 2025 AXELL CORPORATION */

using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Runtime.InteropServices;

using UnityEngine;
using UnityEngine.UI;

using ailia;
using ailiaTracker;

namespace ailiaSDK {
	public class AiliaTrackingSample : AiliaRenderer {
		public enum TrackingModels
		{
			byte_track,
		}

		[SerializeField]
		private TrackingModels ailiaModelType = TrackingModels.byte_track;
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
		private AiliaTrackerModel ailia_tracker = new AiliaTrackerModel();

		private AiliaCamera ailia_camera = new AiliaCamera();
		private AiliaDownload ailia_download = new AiliaDownload();

		// AILIA open file
		private bool FileOpened = false;

		// Detection parameter
		float threshold = 0.1f;
		float iou = 1.0f;
		uint category_n = 80;

		// Tracking points
		private Dictionary<uint, Queue<Vector2>> boxCenters = new Dictionary<uint, Queue<Vector2>>();
		private const int MaxHistoryCount = 50;

		private void CreateAiliaDetector(TrackingModels modelType)
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
				case TrackingModels.byte_track:
					string model = "s";

					mode_text.text = "ailia Tracker";

					ailia_detector.Settings(
						AiliaFormat.AILIA_NETWORK_IMAGE_FORMAT_BGR,
						AiliaFormat.AILIA_NETWORK_IMAGE_CHANNEL_FIRST,
						AiliaFormat.AILIA_NETWORK_IMAGE_RANGE_UNSIGNED_INT8,
						AiliaDetector.AILIA_DETECTOR_ALGORITHM_YOLOX,
						category_n,
						AiliaDetector.AILIA_DETECTOR_FLAG_NORMAL
					);

					AiliaTracker.AILIATrackerSettings settings = new AiliaTracker.AILIATrackerSettings();
					settings.score_threshold = 0.1f;
					settings.nms_threshold = 0.7f;
					settings.track_threshold = 0.5f;
					settings.track_buffer = 30;
					settings.match_threshold = 0.8f;
					ailia_tracker.Create(AiliaTracker.AILIA_TRACKER_ALGORITHM_BYTE_TRACK, settings);

					urlList.Add(new ModelDownloadURL() { folder_path = "yolox", file_name = "yolox_" + model + ".opt.onnx.prototxt" });
					urlList.Add(new ModelDownloadURL() { folder_path = "yolox", file_name = "yolox_" + model + ".opt.onnx" });

					StartCoroutine(ailia_download.DownloadWithProgressFromURL(urlList, () =>
					{
						FileOpened = ailia_detector.OpenFile(asset_path + "/yolox_" + model + ".opt.onnx.prototxt", asset_path + "/yolox_" + model + ".opt.onnx");
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
			ailia_tracker.Close();
		}

		// Use this for initialization
		void Start()
		{
			AiliaLicense.CheckAndDownloadLicense();
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
			list = ailia_detector.ComputeFromImageB2T(camera, tex_width, tex_height, threshold, iou);
			List<AiliaTracker.AILIATrackerObject> list2 = ailia_tracker.Compute(list);
			long end_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

			long start_time_class = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
			foreach (AiliaTracker.AILIATrackerObject obj in list2)
			{
				DisplayTrackerResult(obj, camera, tex_width, tex_height);
			}
			long end_time_class = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

			if (label_text != null)
			{
				string env_name;
				env_name = ailia_detector.EnvironmentName();
				label_text.text = (end_time - start_time) + (end_time_class - start_time_class) + "ms\n" + env_name;
			}

			//Apply
			preview_texture.SetPixels32(camera);
			preview_texture.Apply();
		}

		private void DisplayTrackerResult(AiliaTracker.AILIATrackerObject box, Color32[] camera, int tex_width, int tex_height)
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
			color = Color.HSVToRGB(box.id / 80.0f, 1.0f, 1.0f);
			DrawRect2D(color, x1, y1, w, h, tex_width, tex_height);

			float p = (int)(box.prob * 100) / 100.0f;
			string text = "";
			text += AiliaClassifierLabel.COCO_CATEGORY[box.category];
			text += " " + p + " id:" + box.id;
			int margin = 4;
			DrawText(color, text, x1 + margin, y1 + margin, tex_width, tex_height);

			// Calculate box center
			Vector2 center = new Vector2((x1 + x2) / 2f, (y1 + y2) / 2f);

			// Maintain a history of center points for each id
			if (!boxCenters.ContainsKey(box.id))
			{
				boxCenters[box.id] = new Queue<Vector2>();
			}

			var centersQueue = boxCenters[box.id];
			centersQueue.Enqueue(center);
			if (centersQueue.Count > MaxHistoryCount)
			{
				centersQueue.Dequeue();
			}

			var centersArray = centersQueue.ToArray();
			for (int i = 0; i < centersArray.Length - 1; i++)
			{
				Vector2 start = centersArray[i];
				Vector2 end = centersArray[i + 1];
				float thickness = 1.0f;
				DrawLine(color, (int)start.x, (int)start.y, 0.0f, (int)end.x, (int)end.y, 0.0f, tex_width, tex_height, thickness);
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

