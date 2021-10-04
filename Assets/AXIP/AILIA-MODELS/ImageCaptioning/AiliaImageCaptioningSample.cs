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
	public class AiliaImageCaptioningSample : AiliaRenderer
	{
		[SerializeField]
		private GameObject UICanvas = null;

		//Settings
		[SerializeField]
		private bool gpu_mode = true;
		[SerializeField]
		private bool is_english = false;
		[SerializeField]
		private int camera_id = 0;

		//Output buffer
		public RawImage raw_image = null;
		public Text mode_text = null;
		public Text label_text = null;

		//Preview texture
		private Texture2D preview_texture = null;

		//ailia Instance
		private AiliaCamera ailia_camera = new AiliaCamera();
		private AiliaDownload ailia_download = new AiliaDownload();

		private AiliaCaptioning ailiaCaptioning;

		// AILIA open file
		private bool FileOpened = false;

		private void CreateAilia()
		{
			string asset_path = Application.temporaryCachePath;
			var urlList = new List<ModelDownloadURL>();

			ailiaCaptioning = new AiliaCaptioning(ailia_download);
			StartCoroutine(ailiaCaptioning.Initialize(gpu_mode, (success) => FileOpened = success));
		}

		private void DestroyAilia()
		{
			ailiaCaptioning.Dispose();
		}

		void Start()
		{
			mode_text.text = "ailia Image captioning";
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
			long start_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
			ailiaCaptioning.PreprocessTexture(ailia_camera.GetTexture2D());
			string caption = ailiaCaptioning.InferCaptionFromFrame();
			long end_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond; ;

			//Display prediction time
			if (label_text != null)
			{
				label_text.text = (end_time - start_time) + "ms\n" + caption;
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