/* AILIA Unity Plugin Detector Model Class */
/* Copyright 2019 AXELL CORPORATION */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Runtime.InteropServices;

public class AiliaDetectorModel : AiliaModel
{
	private IntPtr ailia_detector = IntPtr.Zero;

	uint format = AiliaFormat.AILIA_NETWORK_IMAGE_FORMAT_RGB;
	uint channel = AiliaFormat.AILIA_NETWORK_IMAGE_CHANNEL_FIRST;
	uint range = AiliaFormat.AILIA_NETWORK_IMAGE_RANGE_SIGNED_FP32;
	uint algorithm = AiliaDetector.AILIA_DETECTOR_ALGORITHM_YOLOV1;
	uint category_n = 1;
	uint flag = AiliaDetector.AILIA_DETECTOR_FLAG_NORMAL;

	//モデルの設定を行う
	public bool Settings(uint set_format, uint set_channel, uint set_range, uint set_algorithm, uint set_category_n, uint set_flag)
	{
		format = set_format;
		channel = set_channel;
		range = set_range;
		algorithm = set_algorithm;
		category_n = set_category_n;
		flag = set_flag;
		return true;
	}

	//YoloV2などのためにアンカーズ（anchors又はbiases）の情報を設定する
	public bool Anchors(float[] anchors)
	{
		UInt32 anchors_count = (UInt32)(anchors.Length / 2);
		if (ailia_detector == IntPtr.Zero)
		{
			if (logging)
			{
				Debug.Log("ailia_detector must be opened");
			}
			return false;
		}
		int status = AiliaDetector.ailiaDetectorSetAnchors(ailia_detector, anchors, anchors_count);
		if (status != Ailia.AILIA_STATUS_SUCCESS)
		{
			if (logging)
			{
				Debug.Log("ailiaDetectorSetAnchors failed " + status);
			}
			return false;
		}
		return true;
	}

	//YoloV3の入力形状を設定する
	public bool SetInputShape(uint x, uint y)
	{
		if (ailia_detector == IntPtr.Zero)
		{
			if (logging)
			{
				Debug.Log("ailia_detector must be opened");
			}
			return false;
		}
		int status = AiliaDetector.ailiaDetectorSetInputShape(ailia_detector, x, y);
		if (status != Ailia.AILIA_STATUS_SUCCESS)
		{
			if (logging)
			{
				Debug.Log("ailiaDetectorSetInputShape failed " + status);
			}
			return false;
		}
		return true;
	}

	//ファイルから開く
	public override bool OpenFile(string prototxt, string model_path)
	{
		Close();
		bool status = base.OpenFile(prototxt, model_path);
		if (!status)
		{
			if (logging)
			{
				Debug.Log("ailiaModelOpenFile failed");
			}
			return false;
		}
		return OpenDetector();
	}

	//コールバックから開く
	public override bool OpenEx(Ailia.ailiaFileCallback callback, IntPtr arg1, IntPtr arg2)
	{
		Close();
		bool status = base.OpenEx(callback, arg1, arg2);
		if (!status)
		{
			if (logging)
			{
				Debug.Log("ailiaModelOpenEx failed");
			}
			return false;
		}
		return OpenDetector();
	}

	//メモリから開く
	public override bool OpenMem(byte[] prototxt_buf, byte[] model_buf)
	{
		Close();
		bool status = base.OpenMem(prototxt_buf, model_buf);
		if (!status)
		{
			if (logging)
			{
				Debug.Log("ailiaModelOpenMem failed");
			}
			return false;
		}
		return OpenDetector();
	}

	private bool OpenDetector()
	{
		int status = AiliaDetector.ailiaCreateDetector(ref ailia_detector, ailia, format, channel, range, algorithm, category_n, flag);
		if (status != Ailia.AILIA_STATUS_SUCCESS)
		{
			if (logging)
			{
				Debug.Log("ailiaCreateDetector failed " + status);
			}
			Close();
			return false;
		}
		return true;
	}

	//画像から推論する
	public List<AiliaDetector.AILIADetectorObject> ComputeFromImage(Color32[] camera, int tex_width, int tex_height, float threshold, float iou)
	{
		return ComputeFromImageWithFormat(camera, tex_width, tex_height, threshold, iou, AiliaFormat.AILIA_IMAGE_FORMAT_RGBA);
	}

	//画像から推論する（上下反転）
	public List<AiliaDetector.AILIADetectorObject> ComputeFromImageB2T(Color32[] camera, int tex_width, int tex_height, float threshold, float iou)
	{
		return ComputeFromImageWithFormat(camera, tex_width, tex_height, threshold, iou, AiliaFormat.AILIA_IMAGE_FORMAT_RGBA_B2T);
	}

	private List<AiliaDetector.AILIADetectorObject> ComputeFromImageWithFormat(Color32[] camera, int tex_width, int tex_height, float threshold, float iou, uint format)
	{
		if (ailia_detector == IntPtr.Zero)
		{
			return null;
		}

		//バッファの固定
		GCHandle preview_handle = GCHandle.Alloc(camera, GCHandleType.Pinned);
		IntPtr preview_buf_ptr = preview_handle.AddrOfPinnedObject();

		//画像認識を行ってカテゴリを表示
		int status = AiliaDetector.ailiaDetectorCompute(ailia_detector, preview_buf_ptr, (UInt32)tex_width * 4, (UInt32)tex_width, (UInt32)tex_height, format, threshold, iou);
		if (status != Ailia.AILIA_STATUS_SUCCESS)
		{
			if (logging)
			{
				Debug.Log("ailiaDetectorCompute failed " + status);
			}
			return null;
		}

		//推論結果を表示
		List<AiliaDetector.AILIADetectorObject> result_list = new List<AiliaDetector.AILIADetectorObject>();
		uint count = 0;
		AiliaDetector.ailiaDetectorGetObjectCount(ailia_detector, ref count);
		for (uint i = 0; i < count; i++)
		{
			AiliaDetector.AILIADetectorObject detector_obj = new AiliaDetector.AILIADetectorObject();
			status = AiliaDetector.ailiaDetectorGetObject(ailia_detector, detector_obj, (uint)i, AiliaClassifier.AILIA_CLASSIFIER_CLASS_VERSION);
			if (status != Ailia.AILIA_STATUS_SUCCESS)
			{
				if (logging)
				{
					Debug.Log("ailiaDetectorGetObject failed " + status);
				}
				break;
			}
			result_list.Add(detector_obj);
		}

		//バッファの開放
		preview_handle.Free();

		return result_list;
	}

	//開放する
	public override void Close()
	{
		if (ailia_detector != IntPtr.Zero)
		{
			AiliaDetector.ailiaDestroyDetector(ailia_detector);
			ailia_detector = IntPtr.Zero;
		}
		base.Close();
	}
}
