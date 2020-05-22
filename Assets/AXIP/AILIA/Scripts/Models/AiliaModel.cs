/* AILIA Unity Plugin Model Class */
/* Copyright 2019-2020 AXELL CORPORATION */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace ailiaSDK
{
	public class AiliaModel
	{
		protected IntPtr ailia = IntPtr.Zero;

		private int env_id = Ailia.AILIA_ENVIRONMENT_ID_AUTO;

		private string env_name = "auto";

		protected bool logging = true;

		//環境選択（簡易）
		public bool Environment(int type)
		{
			int count = GetEnvironmentCount();
			if (count == -1)
			{
				return false;
			}

			for (int i = 0; i < count; i++)
			{
				Ailia.AILIAEnvironment env = GetEnvironment(i);
				if (env == null)
				{
					return false;
				}

				if (env.type == type)
				{
					if (!SelectEnvironment(i))
					{
						return false;
					}
					if (env.backend == Ailia.AILIA_ENVIRONMENT_BACKEND_CUDA || env.backend == Ailia.AILIA_ENVIRONMENT_BACKEND_VULKAN)
					{
						return true;    //優先
					}
				}
			}
			return true;
		}

		public string EnvironmentName()
		{
			return env_name;
		}

		//環境選択（詳細）
		public int GetEnvironmentCount()
		{
			SetTemporaryCachePath();
			int count = 0;
			int status = Ailia.ailiaGetEnvironmentCount(ref count);
			if (status != Ailia.AILIA_STATUS_SUCCESS)
			{
				if (logging)
				{
					Debug.Log("ailiaGetEnvironmentCount failed " + status);
				}
				return -1;
			}
			return count;
		}

		public Ailia.AILIAEnvironment GetEnvironment(int idx)
		{
			IntPtr env_ptr = IntPtr.Zero;
			int status = Ailia.ailiaGetEnvironment(ref env_ptr, (uint)idx, Ailia.AILIA_ENVIRONMENT_VERSION);
			if (status != Ailia.AILIA_STATUS_SUCCESS)
			{
				if (logging)
				{
					Debug.Log("ailiaGetEnvironment failed " + status);
				}
				return null;
			}
			Ailia.AILIAEnvironment env = (Ailia.AILIAEnvironment)Marshal.PtrToStructure(env_ptr, typeof(Ailia.AILIAEnvironment));
			if (logging)
			{
				//Debug.Log("ENV id:"+env.id+", name:"+Marshal.PtrToStringAnsi(env.name)+", type:"+env.type);
			}
			return env;
		}

		public bool SelectEnvironment(int idx)
		{
			Ailia.AILIAEnvironment env = GetEnvironment(idx);
			if (env == null)
			{
				return false;
			}
			env_id = env.id;
			env_name = Marshal.PtrToStringAnsi(env.name);
			return true;
		}

		//AndroidのRenderScriptのパス設定
		private bool SetTemporaryCachePath()
		{
#if (UNITY_ANDROID && !UNITY_EDITOR)
		using( AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer") )
		{
			using( AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity") )
			{
				using( AndroidJavaObject cacheDir = currentActivity.Call<AndroidJavaObject>( "getCacheDir" ) )
				{
					string _CacheDir = cacheDir.Call<string>( "getCanonicalPath" );
					int status=Ailia.ailiaSetTemporaryCachePath(_CacheDir);
					if(status!=Ailia.AILIA_STATUS_SUCCESS){
						if(logging){
							Debug.Log("ailiaSetTemporaryCachePath failed "+status);
						}
						return false;
					}
				}
			}
		}
#endif
			return true;
		}

		//ファイルを開く（ファイル）
		public virtual bool OpenFile(string prototxt_path, string model_path)
		{
			Close();

			int status = Ailia.ailiaCreate(ref ailia, env_id, Ailia.AILIA_MULTITHREAD_AUTO);
			if (status != Ailia.AILIA_STATUS_SUCCESS)
			{
				if (logging)
				{
					Debug.Log("ailiaCreate failed " + status);
				}
				return false;
			}

			status = Ailia.ailiaOpenStreamFile(ailia, prototxt_path);
			if (status != Ailia.AILIA_STATUS_SUCCESS)
			{
				if (logging)
				{
					Debug.Log("ailiaOpenStreamFile failed" + status);
				}
				Close();
				return false;
			}

			status = Ailia.ailiaOpenWeightFile(ailia, model_path);
			if (status != Ailia.AILIA_STATUS_SUCCESS)
			{
				if (logging)
				{
					Debug.Log("ailiaOpenWeightFile failed" + status);
				}
				Close();
				return false;
			}

			return true;
		}

		//ファイルを開く（メモリ）
		public virtual bool OpenMem(byte[] prototxt_buf, byte[] model_buf)
		{
			Close();

			if (prototxt_buf == null || model_buf == null || prototxt_buf.Length == 0 || model_buf.Length == 0)
			{
				if (logging)
				{
					Debug.Log("input buffer is empty");
				}
				return false;
			}

			int status = Ailia.ailiaCreate(ref ailia, env_id, Ailia.AILIA_MULTITHREAD_AUTO);
			if (status != Ailia.AILIA_STATUS_SUCCESS)
			{
				if (logging)
				{
					Debug.Log("ailiaCreate failed " + status);
				}
				return false;
			}

			status = Ailia.ailiaOpenStreamMem(ailia, prototxt_buf, (uint)prototxt_buf.Length);
			if (status != Ailia.AILIA_STATUS_SUCCESS)
			{
				if (logging)
				{
					Debug.Log("ailiaOpenStreamMem failed " + status);
				}
				Close();
				return false;
			}

			status = Ailia.ailiaOpenWeightMem(ailia, model_buf, (uint)model_buf.Length);
			if (status != Ailia.AILIA_STATUS_SUCCESS)
			{
				if (logging)
				{
					Debug.Log("ailiaOpenWeightMem filed " + status);
				}
				Close();
				return false;
			}

			return true;
		}

		//ファイルを開く（コールバック）
		public virtual bool OpenEx(Ailia.ailiaFileCallback callback, IntPtr arg1, IntPtr arg2)
		{
			Close();

			int status = Ailia.ailiaCreate(ref ailia, env_id, Ailia.AILIA_MULTITHREAD_AUTO);
			if (status != Ailia.AILIA_STATUS_SUCCESS)
			{
				if (logging)
				{
					Debug.Log("ailiaCreate failed" + status);
				}
				return false;
			}

			status = Ailia.ailiaOpenStreamEx(ailia, arg1, callback, Ailia.AILIA_FILE_CALLBACK_VERSION);
			if (status != Ailia.AILIA_STATUS_SUCCESS)
			{
				if (logging)
				{
					Debug.Log("ailiaOpenStreamFileEx failed" + status);
				}
				Close();
				return false;
			}

			status = Ailia.ailiaOpenWeightEx(ailia, arg2, callback, Ailia.AILIA_FILE_CALLBACK_VERSION);
			if (status != Ailia.AILIA_STATUS_SUCCESS)
			{
				if (logging)
				{
					Debug.Log("ailiaOpenWeightFileEx failed" + status);
				}
				Close();
				return false;
			}

			return true;
		}

		//推論する
		public bool Predict(float[] output_data, float[] input_data)
		{
			if (ailia == IntPtr.Zero)
			{
				return false;
			}

			//バッファの固定
			GCHandle input_buf_handle = GCHandle.Alloc(input_data, GCHandleType.Pinned);
			IntPtr input_buf_ptr = input_buf_handle.AddrOfPinnedObject();

			GCHandle output_buf_handle = GCHandle.Alloc(output_data, GCHandleType.Pinned);
			IntPtr output_buf_ptr = output_buf_handle.AddrOfPinnedObject();

			//推論
			int status = Ailia.ailiaPredict(ailia, output_buf_ptr, (uint)(output_data.Length * 4), input_buf_ptr, (uint)(input_data.Length * 4));

			//バッファの開放
			input_buf_handle.Free();
			output_buf_handle.Free();

			if (status != Ailia.AILIA_STATUS_SUCCESS)
			{
				if (logging)
				{
					Debug.Log("ailiaPredict failed" + status);
				}
				return false;
			}
			return true;
		}

		//入力形式の取得
		public Ailia.AILIAShape GetInputShape()
		{
			Ailia.AILIAShape shape = new Ailia.AILIAShape();
			int status = Ailia.ailiaGetInputShape(ailia, shape, Ailia.AILIA_SHAPE_VERSION);
			if (status != Ailia.AILIA_STATUS_SUCCESS)
			{
				if (logging)
				{
					Debug.Log("ailiaGetInputShape failed" + status);
				}
				return null;
			}
			return shape;
		}

		//入力形式を設定
		public bool SetInputShape(Ailia.AILIAShape shape)
		{
			if (ailia == IntPtr.Zero)
			{
				return false;
			}

			int status = Ailia.ailiaSetInputShape(ailia, shape, Ailia.AILIA_SHAPE_VERSION);
			if (status != Ailia.AILIA_STATUS_SUCCESS)
			{
				if (logging)
				{
					Debug.Log("ailiaSetInputShape failed" + status);
				}
				return false;
			}
			return true;
		}

		//出力形式の取得
		public Ailia.AILIAShape GetOutputShape()
		{
			if (ailia == IntPtr.Zero)
			{
				return null;
			}
			Ailia.AILIAShape shape = new Ailia.AILIAShape();
			int status = Ailia.ailiaGetOutputShape(ailia, shape, Ailia.AILIA_SHAPE_VERSION);
			if (status != Ailia.AILIA_STATUS_SUCCESS)
			{
				if (logging)
				{
					Debug.Log("ailiaGetOutputShape failed" + status);
				}
				return null;
			}
			return shape;
		}

		//レイヤー形式の取得
		[System.Obsolete("This is an obsolete method")]
		public Ailia.AILIAShape GetBlobShape(string layer_name)
		{
			if (ailia == IntPtr.Zero)
			{
				return null;
			}
			Ailia.AILIAShape shape = new Ailia.AILIAShape();
			uint id = 0;
			int status = Ailia.ailiaFindBlobIndexByName(ailia, ref id, layer_name);
			if (status != Ailia.AILIA_STATUS_SUCCESS)
			{
				if (logging)
				{
					Debug.Log("ailiaFindBlobIndexByName failed" + status);
				}
				return null;
			}
			status = Ailia.ailiaGetBlobShape(ailia, shape, id, Ailia.AILIA_SHAPE_VERSION);
			if (status != Ailia.AILIA_STATUS_SUCCESS)
			{
				if (logging)
				{
					Debug.Log("ailiaGetBlobShape failed" + status);
				}
				return null;
			}
			return shape;
		}

		//内部データのインデックスを名前で探し取得
		public int FindBlobIndexByName(string name)
		{
			uint idx = 0;
			int status = Ailia.ailiaFindBlobIndexByName(ailia, ref idx, name);
			if (status != Ailia.AILIA_STATUS_SUCCESS)
			{
				if (logging)
				{
					Debug.Log("FindBlobIndexByName failed" + status);
				}
				return -1;
			}
			return (int)idx;
		}

		//Blobの形状を取得
		public Ailia.AILIAShape GetBlobShape(int idx)
		{
			if (ailia == IntPtr.Zero || idx < 0)
			{
				return null;
			}

			Ailia.AILIAShape shape = new Ailia.AILIAShape();
			int status = Ailia.ailiaGetBlobShape(ailia, shape, (uint)idx, Ailia.AILIA_SHAPE_VERSION);
			if (status != Ailia.AILIA_STATUS_SUCCESS)
			{
				if (logging)
				{
					Debug.Log("ailiaGetBlobShape failed" + status);
				}
				return null;
			}
			return shape;
		}

		//Blobのデータを取得
		public bool GetBlobData(float[] output_data, int idx)
		{
			if (ailia == IntPtr.Zero || idx < 0)
			{
				return false;
			}

			GCHandle output_buf_handle = GCHandle.Alloc(output_data, GCHandleType.Pinned);
			IntPtr output_buf_ptr = output_buf_handle.AddrOfPinnedObject();
			int status = Ailia.ailiaGetBlobData(ailia, output_buf_ptr, (uint)(output_data.Length * 4), (uint)idx);
			output_buf_handle.Free();

			if (status != Ailia.AILIA_STATUS_SUCCESS)
			{
				if (logging)
				{
					Debug.Log("ailiaGetBlobData failed" + status);
				}
				return false;
			}
			return true;
		}

		//Blobのデータを設定
		public bool SetInputBlobData(float[] input_data, int idx)
		{
			if (ailia == IntPtr.Zero || idx < 0)
			{
				return false;
			}

			GCHandle input_buf_handle = GCHandle.Alloc(input_data, GCHandleType.Pinned);
			IntPtr input_buf_ptr = input_buf_handle.AddrOfPinnedObject();
			int status = Ailia.ailiaSetInputBlobData(ailia, input_buf_ptr, (uint)(input_data.Length * 4), (uint)idx);
			input_buf_handle.Free();

			if (status != Ailia.AILIA_STATUS_SUCCESS)
			{
				if (logging)
				{
					Debug.Log("ailiaSetInputBlobData failed" + status);
				}
				return false;
			}
			return true;
		}

		//Blobの形式を設定
		public bool SetInputBlobShape(Ailia.AILIAShape shape, int idx)
		{
			if (ailia == IntPtr.Zero || idx < 0)
			{
				return false;
			}

			int status = Ailia.ailiaSetInputBlobShape(ailia, shape, (uint)idx, Ailia.AILIA_SHAPE_VERSION);
			if (status != Ailia.AILIA_STATUS_SUCCESS)
			{
				if (logging)
				{
					Debug.Log("ailiaSetInputBlobShape failed" + status);
				}
				return false;
			}
			return true;
		}

		//推論する
		public bool Update()
		{
			if (ailia == IntPtr.Zero)
			{
				return false;
			}
			int status = Ailia.ailiaUpdate(ailia);
			if (status != Ailia.AILIA_STATUS_SUCCESS)
			{
				if (logging)
				{
					Debug.Log("ailiaUpdate failed" + status);
				}
				return false;
			}
			return true;
		}

		//開放する
		public virtual void Close()
		{
			if (ailia != IntPtr.Zero)
			{
				Ailia.ailiaDestroy(ailia);
				ailia = IntPtr.Zero;
			}
		}
	}
}