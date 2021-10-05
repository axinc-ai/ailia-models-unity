/* AILIA Unity Plugin Download Sample */
/* Copyright 2018-2019 AXELL CORPORATION */

using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Runtime.InteropServices;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

namespace ailiaSDK
{
	public class AiliaDownload
	{
		private GameObject _DownloaderProgressPanel = null;
		public GameObject DownloaderProgressPanel
		{
			get { return _DownloaderProgressPanel; }
			set { _DownloaderProgressPanel = value; }
		}
		private Image _ProgressImage = null;
		private Image ProgressImage
		{
			get
			{
				if (_ProgressImage == null)
				{
					if (_DownloaderProgressPanel == null) return null;
					var panel = _DownloaderProgressPanel.transform.Find("Image");
					if (panel == null) return null;

					_ProgressImage = panel.gameObject.GetComponent<Image>();
				}
				return _ProgressImage;
			}
		}
		private Text _ProgressText = null;
		private Text ProgressText
		{
			get
			{
				if (_ProgressText == null)
				{
					if (_DownloaderProgressPanel == null) return null;
					var panel = _DownloaderProgressPanel.transform.Find("Text");
					if (panel == null) return null;

					_ProgressText = panel.gameObject.GetComponent<Text>();
				}
				return _ProgressText;
			}
		}
		private Text _ContentsText = null;
		private Text ContentsText
		{
			get
			{
				if (_ContentsText == null)
				{
					if (_DownloaderProgressPanel == null) return null;
					var panel = _DownloaderProgressPanel.transform.Find("Contents/ContentsText");
					if (panel == null) return null;

					_ContentsText = panel.gameObject.GetComponent<Text>();
				}
				return _ContentsText;
			}
		}
		private Button _CloseButton = null;
		private Button CloseButton
		{
			get
			{
				if (_CloseButton == null)
				{
					if (_DownloaderProgressPanel == null) return null;
					var panel = _DownloaderProgressPanel.transform.Find("CloseButton");
					if (panel == null) return null;

					_CloseButton = panel.gameObject.GetComponent<Button>();
				}
				return _CloseButton;
			}
		}
		private const int ContentLineCount = 7;

		public void DownloadModelFromUrl(string folder_path, string file_name)
		{
			string toPath = Application.temporaryCachePath + "/" + file_name;

			if (System.IO.File.Exists(toPath) == true)
			{
				FileInfo fileInfo = new System.IO.FileInfo(toPath);
				if (fileInfo.Length != 0)
				{
					Debug.Log("Already exists : " + toPath + " " + fileInfo.Length);
					return;
				}
			}

			Debug.Log("Download model to " + toPath);

			string url = "https://storage.googleapis.com/ailia-models/" + folder_path + "/" + file_name;

			UnityWebRequest www = UnityWebRequest.Get(url);
			www.SendWebRequest();
			while (!www.isDone)
			{
				// NOP.
				// Error
				if (www.isHttpError || www.isNetworkError)
				{
					Debug.LogError(www.error);
					return;
				}
			}
			File.WriteAllBytes(toPath, www.downloadHandler.data);
		}


		//Download to memory for Android
		public byte[] DownloadModel(string file_name)
		{
#if UNITY_ANDROID && !UNITY_EDITOR
		string prefix="";
#else
			string prefix = "file://";
#endif

			string path = prefix + file_name;
			UnityWebRequest www = UnityWebRequest.Get(path);
			www.SendWebRequest();
			while (!www.isDone)
			{
				// NOP.
				// Error
				if (www.isHttpError || www.isNetworkError)
				{
					Debug.LogError(www.error);
					break;
				}
			}
			if (www.downloadHandler.data.Length == 0)
			{
				Debug.Log(file_name + " not found");
			}
			return www.downloadHandler.data;
		}

		public IEnumerator DownloadWithProgressFromURL(List<ModelDownloadURL> urlList, Action OnCompleted)
		{
			if (urlList.Count == 0) yield break;

			var count = urlList.Count;
			var progress = 0.0f;
			var content = "";
			ProgressImage.fillAmount = 0.0f;
			ContentsText.text = content;

			foreach (var downloadUrl in urlList)
			{
				string toPath = Application.temporaryCachePath + "/" + downloadUrl.file_name;

				if (System.IO.File.Exists(toPath) == true)
				{
					FileInfo fileInfo = new System.IO.FileInfo(toPath);
					if (fileInfo.Length != 0)
					{
						var tex = "Already exists : " + toPath + " " + fileInfo.Length;
						content += (tex + "\n");
						if (ContentsText.cachedTextGenerator.lineCount > 9)
						{
							content = content.Substring(content.IndexOf('\n') + 1);
						}
						ContentsText.text = content;
						Debug.Log(tex);
						continue;
					}
				}

				var download_text = "Download model to " + toPath;
				Debug.Log(download_text);

				string url = "https://storage.googleapis.com/ailia-models/" + downloadUrl.folder_path + "/" + downloadUrl.file_name;
				DownloaderProgressPanel.SetActive(true);
				using (var www = UnityWebRequest.Get(url))
				{
					www.SendWebRequest();
					while (true)
					{
						// Error
						if (www.isHttpError || www.isNetworkError)
						{
							Debug.LogError($"Error fetching '{url}': {www.error}");
							content += "<color=red>" + www.error + "</color>" + "\n";
							if (ContentsText.cachedTextGenerator.lineCount > ContentLineCount)
							{
								content = content.Substring(content.IndexOf('\n') + 1);
							}
							ContentsText.text = content;

							CloseButton.onClick.AddListener(() =>
							{
								DownloaderProgressPanel.SetActive(false);
							});
							yield break;
						}
						// Download is done
						if (www.isDone)
						{
							File.WriteAllBytes(toPath, www.downloadHandler.data);
							content += download_text + "\n";
							if (ContentsText.cachedTextGenerator.lineCount > ContentLineCount)
							{
								content = content.Substring(content.IndexOf('\n') + 1);
							}
							ContentsText.text = content;
							break;
						}

						yield return null;
						// Update UI Texts
						progress = www.downloadProgress;
						ProgressImage.fillAmount = progress;

						var val = progress * 100;
						var val_str = Math.Ceiling(val).ToString();
						ProgressText.text = val_str + "%";

						ulong size = 0;
						var header = www.GetResponseHeader("Content-Length");
						if (header != null)
						{
							ulong.TryParse(header, out size);
						}
						if (ContentsText.cachedTextGenerator.lineCount > ContentLineCount)
						{
							content = content.Substring(content.IndexOf('\n') + 1);
						}
						ContentsText.text = content + download_text + " (" + www.downloadedBytes.ToString() + "/" + size.ToString() + ")";
					}
				}
			}
			DownloaderProgressPanel.SetActive(false);
			OnCompleted();
			yield break;
		}
	}
	public class ModelDownloadURL
	{
		public string folder_path;
		public string file_name;
	}

}