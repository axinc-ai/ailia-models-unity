/* AILIA Unity Plugin Detector Sample */
/* Copyright 2018-2019 AXELL CORPORATION */

using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

using ailia;

namespace ailiaSDK {

	public class Yolov11SegSample : AiliaRenderer {
		[SerializeField]
		private GameObject UICanvas = null;

		//Settings
		[SerializeField]
		private bool gpu_mode = false;
		[SerializeField]
		private int camera_id = 0;

		[SerializeField]
		private Texture2D demoTexture = null;

		//Result
		RawImage raw_image = null;
		Text label_text = null;
		Text mode_text = null;

		//Preview
		private Texture2D preview_texture = null;

		//AILIA
		private Yolov11Seg yolov11seg = null;

		private AiliaCamera ailia_camera = new AiliaCamera();
		private AiliaDownload ailia_download = new AiliaDownload();

		private void CreateAiliaDetector()
		{
			string asset_path = Application.temporaryCachePath;
			var urlList = new List<ModelDownloadURL>();

			yolov11seg = new Yolov11Seg(AiliaClassifierLabel.COCO_CATEGORY, gpu_mode);
			string base_url = "";

			mode_text.text = "ailia yolov11_seg Detector";

			urlList.Add(new ModelDownloadURL() { folder_path = "yolov11-seg", file_name = "yolo11n-seg.onnx.prototxt" });
			urlList.Add(new ModelDownloadURL() { folder_path = "yolov11-seg", file_name = "yolo11n-seg.onnx" });

			StartCoroutine(ailia_download.DownloadWithProgressFromURL(urlList, () =>
			{
				yolov11seg.Open(asset_path + "/yolo11n-seg.onnx.prototxt", asset_path + "/yolo11n-seg.onnx");
			}));
		}

		private void DestroyAiliaDetector()
		{
			yolov11seg.Close();
		}

		// Use this for initialization
		void Start()
		{
			SetUIProperties();
			CreateAiliaDetector();
			ailia_camera.CreateCamera(camera_id);
		}

		// Update is called once per frame
		void Update()
		{
			if (!ailia_camera.IsEnable())
			{
				return;
			}

			//Clear result
			Clear();

			int tex_width, tex_height;
    		Color32[] camera;
			if (demoTexture == null)
			{
				//Get camera image
				tex_width = ailia_camera.GetWidth();
				tex_height = ailia_camera.GetHeight();
				camera = ailia_camera.GetPixels32();
			}
			else
			{
				//Get demo image
				tex_width = demoTexture.width;
				tex_height = demoTexture.height;
				camera = (Color32[])demoTexture.GetPixels32().Clone();
			}

			if (preview_texture == null)
			{
				preview_texture = new Texture2D(tex_width, tex_height);
				raw_image.texture = preview_texture;
			}

			//Detection
			long start_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

			List<AILIADetectorObjectEX> list = yolov11seg.Predict(camera, tex_width, tex_height);

			long end_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

			long start_time_class = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
			foreach (AILIADetectorObjectEX obj in list)
			{
				DisplayDetectedResult(obj, ref camera, tex_width, tex_height);
			}
			long end_time_class = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

			if (label_text != null)
			{
				string env_name = yolov11seg.EnvironmentName();
				label_text.text = (end_time - start_time) + (end_time_class - start_time_class) + "ms\n" + env_name;
			}

			//Apply
			preview_texture.SetPixels32(camera);
			preview_texture.Apply();
		}

		private void DisplayDetectedResult(AILIADetectorObjectEX box, ref Color32[] camera, int tex_width, int tex_height)
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
			int category_n = yolov11seg.GetCategoryCount();
			color = Color.HSVToRGB((float)box.category / category_n, 1.0f, 1.0f);
			DrawRect2D(color, x1, y1, w, h, tex_width, tex_height);

			// Draw segmentation mask
			if (box.mask != null)
			{
				int maskWidth = box.mask.w;
				int maskHeight = box.mask.h;
				float[] maskData = box.mask.data;

				Color maskColor = new Color(color.r, color.g, color.b, 0.5f);

				for (int y = y1; y < y2; y++)
				{
					float normalizedY = (float)(y - y1) / h;

					for (int x = x1; x < x2; x++)
					{
						float normalizedX = (float)(x - x1) / w;

						// Calculate mask index with proper interpolation
						float maskX = normalizedX * maskWidth;
						float maskY = normalizedY * maskHeight;

						// Bilinear interpolation
						int maskX1 = Mathf.FloorToInt(maskX);
						int maskY1 = Mathf.FloorToInt(maskY);
						int maskX2 = Mathf.Min(maskX1 + 1, maskWidth - 1);
						int maskY2 = Mathf.Min(maskY1 + 1, maskHeight - 1);

						float wx = maskX - maskX1;
						float wy = maskY - maskY1;

						float v1 = maskData[maskY1 * maskWidth + maskX1];
						float v2 = maskData[maskY1 * maskWidth + maskX2];
						float v3 = maskData[maskY2 * maskWidth + maskX1];
						float v4 = maskData[maskY2 * maskWidth + maskX2];

						float maskValue = v1 * (1 - wx) * (1 - wy) + v2 * wx * (1 - wy) +
										v3 * (1 - wx) * wy + v4 * wx * wy;

						if (maskValue > 0.5)
						{
							int idx = (tex_height - 1 - y) * tex_width + x;
							if (idx >= 0 && idx < camera.Length)
							{
								Color32 pixelColor = camera[idx];
								camera[idx] = Color32.Lerp(pixelColor, maskColor, 0.5f);
							}
						}
					}
				}
			}

			float p = (int)(box.prob * 100) / 100.0f;
			string text = "";
			text += yolov11seg.GetCategoryName(box.category);
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

