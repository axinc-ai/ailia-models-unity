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
}
