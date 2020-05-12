/* AILIA Unity Plugin Feature Extractor Model Class */
/* Copyright 2019 AXELL CORPORATION */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Runtime.InteropServices;

public class AiliaFeatureExtractorModel : AiliaModel
{
	private IntPtr ailia_feature_extractor = IntPtr.Zero;

	private uint format = AiliaFormat.AILIA_NETWORK_IMAGE_FORMAT_BGR;
	private uint channel = AiliaFormat.AILIA_NETWORK_IMAGE_CHANNEL_FIRST;
	private uint range = AiliaFormat.AILIA_NETWORK_IMAGE_RANGE_SIGNED_INT8;
	private string layer_name = "";
	private uint distace_type = AiliaFeatureExtractor.AILIA_FEATURE_EXTRACTOR_DISTANCE_L2NORM;

	//モデル設定
	public bool Settings(uint set_format, uint set_channel, uint set_range, uint set_distance_type, string set_layer_name)
	{
		format = set_format;
		channel = set_channel;
		range = set_range;
		distace_type = set_distance_type;
		layer_name = set_layer_name;
		return true;
	}

	//ファイルから開く
	public override bool OpenFile(string prototxt, string model_path)
	{
		Close();
		bool status = base.OpenFile(prototxt, model_path);
		if (status == false)
		{
			if (logging)
			{
				Debug.Log("ailiaModelOpenFile failed");
			}
			return false;
		}
		return OpenFeatureExtractor();
	}

	//コールバックから開く
	public override bool OpenEx(Ailia.ailiaFileCallback callback, IntPtr arg1, IntPtr arg2)
	{
		Close();
		bool status = base.OpenEx(callback, arg1, arg2);
		if (status == false)
		{
			if (logging)
			{
				Debug.Log("ailiaModelOpenEx failed");
			}
			return false;
		}
		return OpenFeatureExtractor();
	}

	//メモリから開く
	public override bool OpenMem(byte[] prototxt_buf, byte[] model_buf)
	{
		Close();
		bool status = base.OpenMem(prototxt_buf, model_buf);
		if (status == false)
		{
			if (logging)
			{
				Debug.Log("ailiaModelOpenMem failed");
			}
			return false;
		}
		return OpenFeatureExtractor();
	}

	private bool OpenFeatureExtractor()
	{
		int status = AiliaFeatureExtractor.ailiaCreateFeatureExtractor(ref ailia_feature_extractor, ailia, format, channel, range, layer_name);
		if (status != Ailia.AILIA_STATUS_SUCCESS)
		{
			if (logging)
			{
				Debug.Log("ailiaCreateFeatureExtractor failed " + status);
			}
			Close();
			return false;
		}
		return true;
	}

	//画像から特徴量を取得する
	public float[] ComputeFromImage(Color32[] camera, int tex_width, int tex_height)
	{
		return ComputeFromImageWithFormat(camera, tex_width, tex_height, AiliaFormat.AILIA_IMAGE_FORMAT_RGBA);
	}

	//画像から特徴量を取得する（上下反転）
	public float[] ComputeFromImageB2T(Color32[] camera, int tex_width, int tex_height)
	{
		return ComputeFromImageWithFormat(camera, tex_width, tex_height, AiliaFormat.AILIA_IMAGE_FORMAT_RGBA_B2T);
	}

	private float[] ComputeFromImageWithFormat(Color32[] camera, int tex_width, int tex_height, uint format)
	{
		if (ailia_feature_extractor == IntPtr.Zero)
		{
			return null;
		}

		//特徴量のサイズを取得
		Ailia.AILIAShape shape = base.GetBlobShape(base.FindBlobIndexByName(layer_name));
		if (shape == null)
		{
			if (logging)
			{
				Debug.Log("GetBlobShape failed");
			}
			return null;
		}

		//出力先の確保
		float[] output_buf = new float[shape.w * shape.z * shape.y * shape.x];
		GCHandle output_handle = GCHandle.Alloc(output_buf, GCHandleType.Pinned);
		IntPtr output_buf_ptr = output_handle.AddrOfPinnedObject();

		//バッファの固定
		GCHandle preview_handle = GCHandle.Alloc(camera, GCHandleType.Pinned);
		IntPtr preview_buf_ptr = preview_handle.AddrOfPinnedObject();

		//特徴量取得
		int status = AiliaFeatureExtractor.ailiaFeatureExtractorCompute(ailia_feature_extractor, output_buf_ptr, (UInt32)output_buf.Length * 4, preview_buf_ptr, (UInt32)tex_width * 4, (UInt32)tex_width, (UInt32)tex_height, format);
		if (status != Ailia.AILIA_STATUS_SUCCESS)
		{
			if (logging)
			{
				Debug.Log("ailiaFeatureExtractorCompute failed " + status);
			}
			return null;
		}

		//バッファの開放
		preview_handle.Free();
		output_handle.Free();

		return output_buf;
	}

	//特徴量同士の距離を計算する
	public float Match(float[] feature1, float[] feature2)
	{
		if (feature1 == null || feature2 == null)
		{
			if (logging)
			{
				Debug.Log("input feature is empty");
			}
			return float.NaN;
		}

		float distance = 0;

		GCHandle feature1_handle = GCHandle.Alloc(feature1, GCHandleType.Pinned);
		IntPtr feature1_buf_ptr = feature1_handle.AddrOfPinnedObject();

		GCHandle feature2_handle = GCHandle.Alloc(feature2, GCHandleType.Pinned);
		IntPtr feature2_buf_ptr = feature2_handle.AddrOfPinnedObject();

		int status = AiliaFeatureExtractor.ailiaFeatureExtractorMatch(ailia_feature_extractor, ref distance, distace_type, feature1_buf_ptr, (uint)feature1.Length * 4, feature2_buf_ptr, (uint)feature2.Length * 4);
		if (status != Ailia.AILIA_STATUS_SUCCESS)
		{
			if (logging)
			{
				Debug.Log("ailiaFeatureExtractorMatch failed " + status);
			}
			return float.NaN;
		}

		feature1_handle.Free();
		feature2_handle.Free();
		return distance;
	}

	//開放する
	public override void Close()
	{
		if (ailia_feature_extractor != IntPtr.Zero)
		{
			AiliaFeatureExtractor.ailiaDestroyFeatureExtractor(ailia_feature_extractor);
			ailia_feature_extractor = IntPtr.Zero;
		}
		base.Close();
	}
}
