/* AILIA Unity Plugin Download Sample */
/* Copyright 2018-2019 AXELL CORPORATION */

using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Runtime.InteropServices;

using UnityEngine;
using UnityEngine.UI;

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

		WWW www = new WWW(url);
		while (!www.isDone)
		{
			// NOP.
		}
		File.WriteAllBytes(toPath, www.bytes);
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
		WWW www = new WWW(path);
		while (!www.isDone)
		{
			// NOP.
		}
		if (www.bytes.Length == 0)
		{
			Debug.Log(file_name + " not found");
		}
		return www.bytes;
	}
}
