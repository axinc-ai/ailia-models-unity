/* AILIA Unity Plugin Classifier Sample */
/* Copyright 2018-2019 AXELL CORPORATION */

using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Runtime.InteropServices;

using UnityEngine;
using UnityEngine.UI;

namespace ailiaSDK
{
	public class AiliaImageClassificationSample : AiliaRenderer
	{
		[SerializeField]
		private AiliaModelsConst.AiliaModelTypes ailiaModelType = AiliaModelsConst.AiliaModelTypes.resnet50;
		[SerializeField]
		private GameObject UICanvas = null;

		//Settings
		public bool gpu_mode = true;
		public bool is_english = false;
		public int camera_id = 0;

		//Output buffer
		public RawImage raw_image = null;
		public Text mode_text = null;
		public Text label_text = null;

		//Preview texture
		private Texture2D preview_texture = null;

		//ailia Instance
		private AiliaClassifierModel ailia_classifier_model = new AiliaClassifierModel();

		private AiliaCamera ailia_camera = new AiliaCamera();
		private AiliaDownload ailia_download = new AiliaDownload();

		// AILIA open file
		private bool FileOpened = false;

		private void CreateAilia()
		{
			string asset_path = Application.temporaryCachePath;
			var urlList = new List<ModelDownloadURL>();
			if (gpu_mode)
			{
				ailia_classifier_model.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
			}

			switch (ailiaModelType)
			{
				case AiliaModelsConst.AiliaModelTypes.resnet50:
					ailia_classifier_model.Settings(
						AiliaFormat.AILIA_NETWORK_IMAGE_FORMAT_BGR,
						AiliaFormat.AILIA_NETWORK_IMAGE_CHANNEL_FIRST,
						AiliaFormat.AILIA_NETWORK_IMAGE_RANGE_SIGNED_INT8
					);

					urlList.Add(new ModelDownloadURL() { folder_path = "resnet50", file_name = "resnet50.opt.onnx.prototxt" });
					urlList.Add(new ModelDownloadURL() { folder_path = "resnet50", file_name = "resnet50.opt.onnx" });

					StartCoroutine(ailia_download.DownloadWithProgressFromURL(urlList, () =>
					{
						FileOpened = ailia_classifier_model.OpenFile(asset_path + "/resnet50.opt.onnx.prototxt", asset_path + "/resnet50.opt.onnx");
					}));
					break;
			}
		}

		private void DestroyAilia()
		{
			ailia_classifier_model.Close();
		}

		void Start()
		{
			mode_text.text = "ailia Classifier";
			SetUIProperties();
			CreateAilia();
			ailia_camera.CreateCamera(camera_id);
		}

		void Update()
		{
			if (!ailia_camera.IsEnable() || !FileOpened)
			{
				return;
			}

			//Clear label
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

			//Classify
			long start_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond; ;
			uint display_n = 5;
			List<AiliaClassifier.AILIAClassifierClass> result_list = ailia_classifier_model.ComputeFromImageB2T(camera, tex_width, tex_height, display_n);
			long end_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond; ;

			//Display prediction time
			if (label_text != null)
			{
				label_text.text = (end_time - start_time) + "ms\n" + ailia_classifier_model.EnvironmentName();
			}

			//Detection result
			int y = 0;
			foreach (AiliaClassifier.AILIAClassifierClass classifier_obj in result_list)
			{
				string result = "";
				if (is_english)
				{
					result = AiliaClassifierLabel.IMAGENET_CATEGORY[classifier_obj.category] + " " + (int)(classifier_obj.prob * 100) / 100.0f;
				}
				else
				{
					result = AiliaClassifierLabel.IMAGENET_CATEGORY_JP[classifier_obj.category] + " " + (int)(classifier_obj.prob * 100) / 100.0f;
				}

				int margin = 4;
				Color32 color = Color.HSVToRGB(classifier_obj.category / 1000.0f, 1.0f, 1.0f);
				DrawText(color, result, margin, margin + y, tex_width, tex_height);
				y += tex_height / 12;
			}

			//Apply image
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
			DestroyAilia();
			ailia_camera.DestroyCamera();
		}

		void OnDestroy()
		{
			DestroyAilia();
			ailia_camera.DestroyCamera();
		}
	}
}