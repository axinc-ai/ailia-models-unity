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

public class AiliaDownload
{
	private GameObject _DownloaderProgressPanel = null;
	// public bool downloadCompleted = false;
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
		}
		if (www.downloadHandler.data.Length == 0)
		{
			Debug.Log(file_name + " not found");
		}
		return www.downloadHandler.data;
	}

	public IEnumerator DownloadWithProgressFromURL(string folder_path, string file_name, Action OnCompleted)
	{
		string toPath = Application.temporaryCachePath + "/" + file_name;

		if (System.IO.File.Exists(toPath) == true)
		{
			FileInfo fileInfo = new System.IO.FileInfo(toPath);
			if (fileInfo.Length != 0)
			{
				Debug.Log("Already exists : " + toPath + " " + fileInfo.Length);
				// downloadCompleted = true;
				OnCompleted();
				yield break; ;
			}
		}

		Debug.Log("Download model to " + toPath);

		string url = "https://storage.googleapis.com/ailia-models/" + folder_path + "/" + file_name;
		DownloaderProgressPanel.SetActive(true);
		ProgressImage.fillAmount = 0.0f;
		using (var www = UnityWebRequest.Get(url))
		{
			www.SendWebRequest();
			while (true)
			{
				if (www.isDone)
				{
					File.WriteAllBytes(toPath, www.downloadHandler.data);
					// downloadCompleted = true;
					DownloaderProgressPanel.SetActive(false);
					OnCompleted();
					yield break;
				}
				yield return null;
				Debug.Log(www.downloadProgress);
				ProgressImage.fillAmount = www.downloadProgress;
				var val = www.downloadProgress * 100;
				var val_str = Math.Ceiling(val).ToString();
				ProgressText.text = val_str + "%";
			}
		}
	}
}
