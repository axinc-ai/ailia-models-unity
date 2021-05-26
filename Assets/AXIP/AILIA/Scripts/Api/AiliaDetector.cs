/* AILIA Unity Plugin Native Interface */
/* Copyright 2018 AXELL CORPORATION */

using UnityEngine;
using System.Collections;
using System.IO;
using System;
using System.Runtime.InteropServices;

namespace ailiaSDK
{
	public class AiliaDetector
	{
		/****************************************************************
		* 物体情報
		**/

		public const Int32 AILIA_DETECTOR_OBJECT_VERSION = (1);

		[StructLayout(LayoutKind.Sequential)]
		public class AILIADetectorObject
		{
			public UInt32 category; // オブジェクトカテゴリ番号(0～category_count-1)
			public float prob;      // 推定確率
			public float x;         // 左上X位置(1で画像横幅)
			public float y;         // 左上Y位置(1で画像高さ)
			public float w;         // 幅(1で画像横幅、負数は取らない)
			public float h;         // 高さ(1で画像高さ、負数は取らない)
		}

		public const UInt32 AILIA_DETECTOR_ALGORITHM_YOLOV1 = (0);  //YOLOV1
		public const UInt32 AILIA_DETECTOR_ALGORITHM_YOLOV2 = (1);  //YOLOV2
		public const UInt32 AILIA_DETECTOR_ALGORITHM_YOLOV3 = (2);  //YOLOV3
		public const UInt32 AILIA_DETECTOR_ALGORITHM_YOLOV4 = (3);  //YOLOV4

		public const UInt32 AILIA_DETECTOR_ALGORITHM_SSD = (8); //SSD

		public const UInt32 AILIA_DETECTOR_FLAG_NORMAL = (0); //オプションなし

		/****************************************************************
		* 物体認識
		**/

		/**
		*  検出オブジェクトを作成します。
		*    引数:
		*      detector       - 検出オブジェクトポインタ
		*      net            - ネットワークオブジェクトポインタ
		*      format         - ネットワークの画像フォーマット （AILIA_NETWORK_IMAGE_FORMAT_*）
		*      channel        - ネットワークの画像チャンネル (AILIA_NETWORK_IMAGE_CHANNEL_*)
		*      range          - ネットワークの画像レンジ （AILIA_NETWORK_IMAGE_RANGE_*）
		*      algorithm      - AILIA_DETECTOR_ALGORITHM_*
		*      caregory_count - 認識カテゴリ数(20等)
		*?     flags          - AILIA_DETECTOR_FLAG_*
		*    返値:
		*      成功した場合はAILIA_STATUS_SUCCESS、そうでなければエラーコードを返す。
		*/
		[DllImport(Ailia.LIBRARY_NAME)]
		public static extern int ailiaCreateDetector(ref IntPtr detector, IntPtr net, UInt32 format, UInt32 channel, UInt32 range, UInt32 algorithm, UInt32 category_count, UInt32 flags);

		/**
		*  検出オブジェクトを破棄します。
		*    引数:
		*      detector - 検出オブジェクトポインタ
		*/
		[DllImport(Ailia.LIBRARY_NAME)]
		public static extern void ailiaDestroyDetector(IntPtr detector);

		/**
		*  物体認識を行います。
		*    引数:
		*      detector                    - 検出オブジェクトポインタ
		*      src                         - 画像データ
		*      src_stride                  - 1ラインのバイト数
		*      src_width                   - 画像幅
		*      src_height                  - 画像高さ
		*      src_format                  - AILIA_UTILITY_IMAGE_FORMAT_*
		*      threshold                   - 認識しきい値(0.1f等)(小さいほど認識が甘くなり認識数増加)
		*      iou                         - 重複除外しきい値(0.45f等)(小さいほど重複を許容せず認識数減少)
		*    返値:
		*      成功した場合はAILIA_STATUS_SUCCESS、そうでなければエラーコードを返す。
		*/
		[DllImport(Ailia.LIBRARY_NAME)]
		public static extern int ailiaDetectorCompute(IntPtr detector, IntPtr src, UInt32 src_stride, UInt32 src_width, UInt32 src_height, UInt32 src_format, float threshold, float iou);

		/**
		*  認識結果の数を取得します。
		*    引数:
		*      detector   - 検出オブジェクトポインタ
		*      obj_count  - オブジェクト数
		*    返値:
		*      成功した場合はAILIA_STATUS_SUCCESS、そうでなければエラーコードを返す。
		*/
		[DllImport(Ailia.LIBRARY_NAME)]
		public static extern int ailiaDetectorGetObjectCount(IntPtr detector, ref UInt32 obj_count);

		/**
		*  認識結果を取得します。
		*    引数:
		*      detector   - 検出オブジェクトポインタ
		*      obj        - オブジェクト情報
		*      obj_idx    - オブジェクトインデックス
		*      version    - AILIA_DETECTOR_OBJECT_VERSION
		*    返値:
		*      成功した場合はAILIA_STATUS_SUCCESS、そうでなければエラーコードを返す。
		*    解説:
		*      ailiaPredictを一度も実行していない場合はAILIA_STATUS_INVALID_STATEが返ります。
		*      認識結果は確率順でソートされます。
		*/
		[DllImport(Ailia.LIBRARY_NAME)]
		public static extern int ailiaDetectorGetObject(IntPtr detector, [In, Out] AILIADetectorObject obj, UInt32 obj_idx, UInt32 version);

		/**
		*  YoloV2などのためにアンカーズ（anchors又はbiases）の情報を設定します。
		*    引数:
		*      detector	      - 検出オブジェクトポインタ
		*      anchors        - アンカーズの寸法（検出ボックスの可能な形、高さと広さ）
		*      anchors_count  - アンカーズの数（anchorsの配列サイズの半分）
		*    返値:
		*      成功した場合はAILIA_STATUS_SUCCESS、そうでなければエラーコードを返す。
		*    解説:
		*      YoloV2などは既定の複数な形の検出ボックスを同時に試しています。このデータはそのボックスの複数な形の情報を記述します。
		*      anchorsには{x,y,x,y...}の形式で格納します。
		*      anchors_countが5の場合、anchorsは10次元の配列になります。
		*/
		[DllImport(Ailia.LIBRARY_NAME)]
		public static extern int ailiaDetectorSetAnchors(IntPtr detector, float[] anchors, UInt32 anchors_count);

		/**
		*  YoloV3でのモデルへの入力画像サイズを指定します。
		*    引数:
		*      detector	      - 検出オブジェクトポインタ
		*      input_width    - モデルの入力画像幅
		*      input_height   - モデルの入力画像高さ
		*    返値:
		*      成功した場合はAILIA_STATUS_SUCCESS、そうでなければエラーコードを返す。
		*    解説:
		*      YoloV3では単一のモデルが任意の入力解像度に対応します。(32 の倍数制限あり)
		*      計算量の削減等でモデルへの入力画像サイズを指定する場合この API を実行してください。
		*      ailiaCreateDetector() と ailiaDetectorCompute() の間に実行する必要があります。
		*      この API を実行しない場合、デフォルトの 416x416 を利用します。
		*      YOLOv3 以外で実行した場合、AILIA_STATUS_INVALID_STATE を返します。 
		*/
		[DllImport(Ailia.LIBRARY_NAME)]
		public static extern int ailiaDetectorSetInputShape(IntPtr detector, UInt32 input_width, UInt32 input_height);
	}
}