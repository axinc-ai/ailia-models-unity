/* AILIA Unity Plugin Foundation Sample */
/* Copyright 2018-2023 AXELL CORPORATION */

using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Runtime.InteropServices;

using UnityEngine;
using UnityEngine.UI;

using ailia;

namespace ailiaSDK {
	public class AiliaFoundationSample : AiliaRenderer {
		public enum FoundationModels
		{
			detic
		}

		[SerializeField]
		private FoundationModels ailiaModelType = FoundationModels.detic;
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
		private AiliaModel ailia_model = new AiliaModel();
		private AiliaCamera ailia_camera = new AiliaCamera();
		private AiliaDownload ailia_download = new AiliaDownload();

		//Max resolution (you can set larger value)
		private int IMAGE_MAX_WIDTH = 360;

		//AILIA open file
		private bool FileOpened = false;

		private void CreateAiliaFoundation(FoundationModels modelType)
		{
			string asset_path = Application.temporaryCachePath;
			var urlList = new List<ModelDownloadURL>();
			// Detic do not work on FP16. So we can use cpu mode.
			/*
			if (gpu_mode)
			{
				ailia_model.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
			}
			*/
			switch (modelType)
			{
				case FoundationModels.detic:
					urlList.Add(new ModelDownloadURL() { folder_path = "detic", file_name = "Detic_C2_SwinB_896_4x_IN-21K+COCO_lvis_op16.onnx.prototxt" });
					urlList.Add(new ModelDownloadURL() { folder_path = "detic", file_name = "Detic_C2_SwinB_896_4x_IN-21K+COCO_lvis_op16.onnx" });

					StartCoroutine(ailia_download.DownloadWithProgressFromURL(urlList, () =>
					{
						FileOpened = ailia_model.OpenFile(asset_path + "/Detic_C2_SwinB_896_4x_IN-21K+COCO_lvis_op16.onnx.prototxt", asset_path + "/Detic_C2_SwinB_896_4x_IN-21K+COCO_lvis_op16.onnx");
					}));

					break;
				default:
					Debug.Log("Others ailia models are working in progress.");
					break;
			}
		}


		private void DestroyAiliaFoundation()
		{
			ailia_model.Close();
		}

		// Use this for initialization
		void Start()
		{
			AiliaLicense.CheckAndDownloadLicense();
			SetUIProperties();
			CreateAiliaFoundation(ailiaModelType);
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
			RunDetection(camera, tex_width, tex_height);
			long end_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond; ;

			long start_time_fill = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
			ApplyResult();
			long end_time_fill = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

			if (label_text != null)
			{
				label_text.text = (end_time - start_time) + " + " + (end_time_fill - start_time_fill) + "ms\n" + ailia_model.EnvironmentName();
			}

			//Apply
			preview_texture.SetPixels32(camera);
			preview_texture.Apply();
		}


		private float result_time;
		private Color32[] result_image = null;
		private List<AiliaDetector.AILIADetectorObject> result_list = null;

		private bool RunDetection(Color32[] camera, int tex_width, int tex_height)
		{
			//メモリの制約から最大解像度を抑制
			int resize_width = tex_width;
			int resize_height = tex_height;
			if (resize_width > IMAGE_MAX_WIDTH){
				resize_height = resize_height * IMAGE_MAX_WIDTH / resize_width;
				resize_width = IMAGE_MAX_WIDTH;
			}

			//リサイズ
			uint[] input_blobs = ailia_model.GetInputBlobList();

			Ailia.AILIAShape input_shape = new Ailia.AILIAShape();
			int align = 8;
			input_shape.x = (uint)(((resize_width+align-1)/align)*align);
			input_shape.y = (uint)(((resize_height+align-1)/align)*align);
			input_shape.z = 3;
			input_shape.w = 1;
			input_shape.dim = 4;

			Ailia.AILIAShape hw_shape = new Ailia.AILIAShape();
			hw_shape.x = 2;
			hw_shape.y = 1;
			hw_shape.z = 1;
			hw_shape.w = 1;
			hw_shape.dim = 1;

			float[] data = new float[input_shape.x * input_shape.y * input_shape.z * input_shape.w];

			float[] hw = new float[hw_shape.x * hw_shape.y * hw_shape.z * hw_shape.w];
			hw[0] = input_shape.y;	// h
			hw[1] = input_shape.x;	// w

			int w = (int)input_shape.x;
			int h = (int)input_shape.y;
			float scale = 1.0f * tex_height / h;
			if (tex_width > tex_height && input_shape.x == input_shape.y)
			{
				scale = 1.0f * tex_width / w;
			}

			for (int y = 0; y < h; y++)
			{
				for (int x = 0; x < w; x++)
				{
					int y2 = (int)(1.0 * y * scale);
					int x2 = (int)(1.0 * x * scale);
					if (y2 >= tex_height)
					{
						continue;
					}
					if (x2 >= tex_width)
					{
						continue;
					}

					data[((h - 1 - y) * w + x) + 0 * w * h] = (float)(camera[y2 * tex_width + x2].r);
					data[((h - 1 - y) * w + x) + 1 * w * h] = (float)(camera[y2 * tex_width + x2].g);
					data[((h - 1 - y) * w + x) + 2 * w * h] = (float)(camera[y2 * tex_width + x2].b);
				}
			}

			//セグメンテーション
			long start_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
			if (input_blobs != null)
			{
				bool success = ailia_model.SetInputBlobShape(input_shape, (int)input_blobs[0]);
				if (!success)
				{
					Debug.Log("Can not SetInputBlobShape");
					return false;
				}
				success = ailia_model.SetInputBlobShape(hw_shape, (int)input_blobs[1]);
				if (!success)
				{
					Debug.Log("Can not SetInputBlobShape");
					return false;
				}
				success = ailia_model.SetInputBlobData(data, (int)input_blobs[0]);
				if (!success)
				{
					Debug.Log("Can not SetInputBlobData");
					return false;
				}
				success = ailia_model.SetInputBlobData(hw, (int)input_blobs[1]);
				if (!success)
				{
					Debug.Log("Can not SetInputBlobData");
					return false;
				}

				success = ailia_model.Update();
				if (!success)
				{
					Debug.Log("Can not Update");
					Debug.Log(ailia_model.GetErrorDetail());
					return false;
				}

				uint[] output_blobs = ailia_model.GetOutputBlobList();
				if (output_blobs != null && output_blobs.Length >= 4)
				{
					Ailia.AILIAShape box_shape = ailia_model.GetBlobShape((int)output_blobs[0]);
					Ailia.AILIAShape score_shape = ailia_model.GetBlobShape((int)output_blobs[1]);
					Ailia.AILIAShape class_shape = ailia_model.GetBlobShape((int)output_blobs[2]);
					Ailia.AILIAShape mask_shape = ailia_model.GetBlobShape((int)output_blobs[3]);

					if (box_shape != null && score_shape != null && class_shape != null && mask_shape != null)
					{
						if (box_shape.z >= 1){
							float[] box_data = new float[box_shape.x * box_shape.y * box_shape.z * box_shape.w];
							float[] score_data = new float[score_shape.x * score_shape.y * score_shape.z * score_shape.w];
							float[] class_data= new float[class_shape.x * class_shape.y * class_shape.z * class_shape.w];
							float[] mask_data = new float[mask_shape.x * mask_shape.y * mask_shape.z * mask_shape.w];

							if (ailia_model.GetBlobData(box_data, (int)output_blobs[0]) &&
								ailia_model.GetBlobData(score_data, (int)output_blobs[1]) &&
								ailia_model.GetBlobData(class_data, (int)output_blobs[2]) &&
								ailia_model.GetBlobData(mask_data, (int)output_blobs[3]))
							{
								FillDetic(camera, tex_width, tex_height, mask_data, mask_shape);

								result_list = new List<AiliaDetector.AILIADetectorObject>();
								for (int c = 0; c < box_shape.y; c++){
									AiliaDetector.AILIADetectorObject box = new AiliaDetector.AILIADetectorObject();
									box.x = (box_data[c*4 + 0] * scale / tex_width);
									box.y = (box_data[c*4 + 1] * scale / tex_height);
									box.w = (box_data[c*4 + 2] * scale / tex_width) - box.x;
									box.h = (box_data[c*4 + 3] * scale / tex_height) - box.y;
									box.category = (uint)class_data[c];
									box.prob = score_data[c];
									result_list.Add(box);
								}
							}
						}
					}
				}
			}

			long end_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond; ;

			result_time = (end_time - start_time);
			result_image = camera;

			return true;
		}

		private void FillDetic(Color32[] camera, int tex_width, int tex_height, float[] output_data, Ailia.AILIAShape output_shape)
		{
			float scale2 = (float)(1.0 * output_shape.y / tex_height);
			if (tex_width > tex_height && output_shape.x == output_shape.y)
			{
				scale2 = (float)(1.0 * output_shape.x / tex_width);
			}

			int category_n = (int)output_shape.z;

			for (int py = 0; py < tex_height; py++)
			{
				for (int px = 0; px < tex_width; px++)
				{
					int y = (int)(py * scale2);
					int x = (int)(px * scale2);
					if (y >= output_shape.y || x >= output_shape.x)
					{
						continue;
					}
					int ir = (int)(camera[(tex_height - 1 - py) * tex_width + px].r);
					int ig = (int)(camera[(tex_height - 1 - py) * tex_width + px].g);
					int ib = (int)(camera[(tex_height - 1 - py) * tex_width + px].b);
					int r2=0;
					int g2=0;
					int b2=0;
					for (int ci = 0; ci < category_n; ci++){
						float p = output_data[ci * output_shape.x * output_shape.y + y * output_shape.x + x];
						if (p > 0.5f){
							int r = (int)(p * ((ci%8+1)%2) * 255);
							int g = (int)(p * ((ci%8+1)/2%2) * 255);
							int b = (int)(p * ((ci%8+1)/4%2) * 255);
							r2 = r2 + r;
							g2 = g2 + g;
							b2 = b2 + b;
						}
					}
					r2 = Math.Min(255, Math.Max(0, r2));
					g2 = Math.Min(255, Math.Max(0, g2));
					b2 = Math.Min(255, Math.Max(0, b2));
					Color32 c = new Color32();
					float a = 0.5f;
					c.a = 255;
					c.r = (byte)(r2 * a + ir * (1.0 - a));
					c.g = (byte)(g2 * a + ig * (1.0 - a));
					c.b = (byte)(b2 * a + ib * (1.0 - a));
					camera[(tex_height - 1 - py) * tex_width + px] = c;
				}
			}
		}

		private void ApplyResult()
		{
			//画像反映
			if (result_image != null && preview_texture != null){
				if (preview_texture.width * preview_texture.height == result_image.Length){
					preview_texture.SetPixels32(result_image);
					preview_texture.Apply();
				}
			}

			int tex_width = ailia_camera.GetWidth();
			int tex_height = ailia_camera.GetHeight();

			if (result_list != null){
				foreach (AiliaDetector.AILIADetectorObject obj in result_list)
				{
					ObjectClassifier(obj, tex_width, tex_height);
				}
			}
		}

		private void ObjectClassifier(AiliaDetector.AILIADetectorObject box, int tex_width, int tex_height)
		{
			//ピクセル座標への変換
			int x = (int)(box.x * tex_width);
			int y = (int)(box.y * tex_height);

			int w = (int)(box.w * tex_width);
			int h = (int)(box.h * tex_height);

			if (w <= 0 || h <= 0)
			{
				return;
			}

			Color color = Color.white;
			color = Color.HSVToRGB(box.category % 20 / 20.0f, 1.0f, 1.0f);
			DrawRect2D(color, x, y, w, h, tex_width, tex_height);

			float p = (int)(box.prob * 100) / 100.0f;
			string text = "";
			text += AiliaFoundationLabel.LVIS_CATEGORY[box.category];
			text += " " + p;
			int margin = 4;
			DrawText(color, text, x + margin, y + margin, tex_width, tex_height);
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
			DestroyAiliaFoundation();
			ailia_camera.DestroyCamera();
		}

		void OnDestroy()
		{
			DestroyAiliaFoundation();
			ailia_camera.DestroyCamera();
		}
	}
}

