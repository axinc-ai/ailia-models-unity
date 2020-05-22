/* AILIA Unity Plugin Classifier Model Class */
/* Copyright 2019 AXELL CORPORATION */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace ailiaSDK
{
	public class AiliaClassifierModel : AiliaModel
	{
		private IntPtr ailia_classifier = IntPtr.Zero;

		private uint format = AiliaFormat.AILIA_NETWORK_IMAGE_FORMAT_BGR;
		private uint channel = AiliaFormat.AILIA_NETWORK_IMAGE_CHANNEL_FIRST;
		private uint range = AiliaFormat.AILIA_NETWORK_IMAGE_RANGE_SIGNED_INT8;

		//モデル設定
		public bool Settings(uint set_format, uint set_channel, uint set_range)
		{
			format = set_format;
			channel = set_channel;
			range = set_range;
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
			return OpenClassifier();
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
			return OpenClassifier();
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
			return OpenClassifier();
		}

		private bool OpenClassifier()
		{
			int status = AiliaClassifier.ailiaCreateClassifier(ref ailia_classifier, ailia, format, channel, range);
			if (status != Ailia.AILIA_STATUS_SUCCESS)
			{
				if (logging)
				{
					Debug.Log("ailiaCreateClassifier failed " + status);
				}
				Close();
				return false;
			}
			return true;
		}

		//画像から推論する
		public List<AiliaClassifier.AILIAClassifierClass> ComputeFromImage(Color32[] camera, int tex_width, int tex_height, uint max_class_count)
		{
			return ComputeFromImageWithFormat(camera, tex_width, tex_height, max_class_count, AiliaFormat.AILIA_IMAGE_FORMAT_RGBA);
		}

		//画像から推論する（上下反転）
		public List<AiliaClassifier.AILIAClassifierClass> ComputeFromImageB2T(Color32[] camera, int tex_width, int tex_height, uint max_class_count)
		{
			return ComputeFromImageWithFormat(camera, tex_width, tex_height, max_class_count, AiliaFormat.AILIA_IMAGE_FORMAT_RGBA_B2T);
		}

		private List<AiliaClassifier.AILIAClassifierClass> ComputeFromImageWithFormat(Color32[] camera, int tex_width, int tex_height, uint max_class_count, uint format)
		{
			if (ailia_classifier == IntPtr.Zero)
			{
				return null;
			}

			//バッファの固定
			GCHandle preview_handle = GCHandle.Alloc(camera, GCHandleType.Pinned);
			IntPtr preview_buf_ptr = preview_handle.AddrOfPinnedObject();

			//画像認識を行ってカテゴリを表示
			//Unityのカメラ画像は上下反転しているのでAILIA_IMAGE_FORMAT_RGBA_B2Tを指定
			int status = AiliaClassifier.ailiaClassifierCompute(ailia_classifier, preview_buf_ptr, (UInt32)tex_width * 4, (UInt32)tex_width, (UInt32)tex_height, format, max_class_count);
			if (status != Ailia.AILIA_STATUS_SUCCESS)
			{
				if (logging)
				{
					Debug.Log("ailiaClassifierCompute failed " + status);
				}
				return null;
			}

			//推論結果を表示
			List<AiliaClassifier.AILIAClassifierClass> result_list = new List<AiliaClassifier.AILIAClassifierClass>();
			for (int i = 0; i < max_class_count; i++)
			{
				AiliaClassifier.AILIAClassifierClass classifier_obj = new AiliaClassifier.AILIAClassifierClass();
				status = AiliaClassifier.ailiaClassifierGetClass(ailia_classifier, classifier_obj, (uint)i, AiliaClassifier.AILIA_CLASSIFIER_CLASS_VERSION);
				if (status != Ailia.AILIA_STATUS_SUCCESS)
				{
					if (logging)
					{
						Debug.Log("ailiaClassifierGetClass failed" + status);
					}
					break;
				}
				result_list.Add(classifier_obj);
			}

			//バッファの開放
			preview_handle.Free();

			return result_list;
		}

		//開放する
		public override void Close()
		{
			if (ailia_classifier != IntPtr.Zero)
			{
				AiliaClassifier.ailiaDestroyClassifier(ailia_classifier);
				ailia_classifier = IntPtr.Zero;
			}
			base.Close();
		}
	}
}