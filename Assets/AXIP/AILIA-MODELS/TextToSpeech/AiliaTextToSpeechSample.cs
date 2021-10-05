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
	public class AiliaTextToSpeechSample : AiliaRenderer
	{
		[SerializeField]
		private GameObject UICanvas = null;

		//Settings
		[SerializeField]
		private bool gpu_mode = true;

		//Output buffer
		public Text mode_text = null;
		public Text label_text = null;
		public InputField inputField;
		public GameObject inferenceButton;
		public GameObject spinner;

		//ailia Instance
		private AiliaDownload ailia_download = new AiliaDownload();

		private AiliaTts ailiaTts;
		private AiliaTextToSpeechThreadedJob ttsJob;

		// AILIA open file
		private bool FileOpened = false;

		private void CreateAilia()
		{
			string asset_path = Application.temporaryCachePath;
			var urlList = new List<ModelDownloadURL>();

			ailiaTts = new AiliaTts(ailia_download);
			StartCoroutine(ailiaTts.Initialize(gpu_mode, (success) =>
				{
					FileOpened = success;

					if (success)
					{
						inputField.gameObject.SetActive(true);
						inferenceButton.SetActive(true);
					}
				}
			));
		}

		private void DestroyAilia()
		{
			ailiaTts.Dispose();
		}

		void Start()
		{
			mode_text.text = "ailia Text to speech";
			SetUIProperties();
			CreateAilia();
		}

		private void Update()
		{
			if (spinner.activeSelf)
			{
				spinner.transform.localRotation = Quaternion.Euler(spinner.transform.localRotation.eulerAngles + 0.75f * Vector3.back);
			}
		}

		public void RunSpeechInference()
		{
			if (ttsJob == null)
			{
				StartCoroutine(RunSpeechInferenceCoroutine());
			}
		}

		private IEnumerator RunSpeechInferenceCoroutine()
		{
			spinner.SetActive(true);
			ttsJob = new AiliaTextToSpeechThreadedJob(ailiaTts, inputField.text);
			ttsJob.Start();
			yield return ttsJob.WaitFor();
			spinner.SetActive(false);
			PlaySamples(ttsJob.samples);
			ttsJob = null;
		}

		private void PlaySamples(float[] samples)
		{
			AudioClip clip = AudioClip.Create("output", samples.Length, 1, 22050, false);
			AudioSource source;

			try
			{
				source = gameObject.GetComponent<AudioSource>();

				if (source == null)
				{
					source = gameObject.AddComponent<AudioSource>();
				}
			}
			catch
			{
				source = gameObject.AddComponent<AudioSource>();
			}
			source.Stop();
			source.volume = 0.3f;
			clip.SetData(samples, 0);
			source.clip = clip;
			source.Play();
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

			label_text = UICanvas.transform.Find("LabelText").gameObject.GetComponent<Text>();
			mode_text = UICanvas.transform.Find("ModeLabel").gameObject.GetComponent<Text>();
		}

		void OnApplicationQuit()
		{
			DestroyAilia();
		}

		void OnDestroy()
		{
			DestroyAilia();
		}
	}
}