/* AILIA Unity Plugin Download Sample */
/* Copyright 2018-2019 AXELL CORPORATION */

using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Runtime.InteropServices;

using UnityEngine;
using UnityEngine.UI;

public class AiliaDownload {
	//Copy for Android
	public void CopyModelToTemporaryCachePath (string file_name)
	{
	#if UNITY_ANDROID && !UNITY_EDITOR
		string prefix="";
	#else
		string prefix="file://";
	#endif

		string path = prefix+Application.streamingAssetsPath + "/" + file_name;
		WWW www = new WWW(path);
		while (!www.isDone)
		{
			// NOP.
		}
		string toPath = Application.temporaryCachePath + "/" + file_name;
		File.WriteAllBytes(toPath, www.bytes);
	}

	//Download to memory for Android
	public byte[] DownloadModel (string file_name)
	{
	#if UNITY_ANDROID && !UNITY_EDITOR
		string prefix="";
	#else
		string prefix="file://";
	#endif

		string path = prefix + file_name;
		WWW www = new WWW(path);
		while (!www.isDone)
		{
			// NOP.
		}
		if(www.bytes.Length == 0){
			Debug.Log(file_name+" not found");
		}
		return www.bytes;
	}
}
