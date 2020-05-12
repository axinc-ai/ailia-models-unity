/* AILIA Unity Plugin Native Interface */
/* Copyright 2018 AXELL CORPORATION */

using UnityEngine;
using System.Collections;
using System.IO;
using System;
using System.Runtime.InteropServices;

public class AiliaClassifier
{
	/****************************************************************
	* 識別情報
	**/

	public const Int32 AILIA_CLASSIFIER_CLASS_VERSION = (1);

	[StructLayout(LayoutKind.Sequential)]
	public class AILIAClassifierClass
	{
		public UInt32 category; // オブジェクトカテゴリ番号
		public float prob;      // 推定確率
	}

	/****************************************************************
	* 識別API
	**/

	/**
	*  識別オブジェクトを作成します。
	*    引数:
	*      classifier - 識別オブジェクトポインタへのポインタ
	*      net        - ネットワークオブジェクトポインタ
	*      format     - ネットワークの画像フォーマット （AILIA_NETWORK_IMAGE_FORMAT_*）
	*      channel    - ネットワークの画像チャンネル (AILIA_NETWORK_IMAGE_CHANNEL_*)
	*      range      - ネットワークの画像レンジ （AILIA_NETWORK_IMAGE_RANGE_*）
	*    返値:
	*      成功した場合はAILIA_STATUS_SUCCESS、そうでなければエラーコードを返す。
	*   解説:
	*     ネットワークオブジェクトを作成します。
	*     環境を自動にした場合はCPUモードになり、BLASが利用できる場合はBLASを利用します。
	*/
	[DllImport(Ailia.LIBRARY_NAME)]
	public static extern int ailiaCreateClassifier(ref IntPtr classifier, IntPtr net, UInt32 format, UInt32 channel, UInt32 range);

	/**
	*  識別オブジェクトを破棄します。
	*    引数:
	*      classifier - 識別オブジェクトポインタ
	*/
	[DllImport(Ailia.LIBRARY_NAME)]
	public static extern void ailiaDestroyClassifier(IntPtr classifier);

	/**
	*  物体識別を行います。
	*    引数:
	*      net		                   - ネットワークオブジェクトポインタ
	*      src                         - 画像データ
	*      src_stride                  - 1ラインのバイト数
	*      src_width                   - 画像幅
	*      src_height                  - 画像高さ
	*      src_format                  - AILIA_UTILITY_IMAGE_FORMAT_*
	*      max_class_count             - 認識結果の最大個数
	*    返値:
	*      成功した場合はAILIA_STATUS_SUCCESS、そうでなければエラーコードを返す。
	*/
	[DllImport(Ailia.LIBRARY_NAME)]
	public static extern int ailiaClassifierCompute(IntPtr classifier, IntPtr src, UInt32 src_stride, UInt32 src_width, UInt32 src_height, UInt32 src_format, UInt32 max_class_count);

	/**
	*  認識結果の数を取得します。
	*    引数:
	*      net		  - ネットワークオブジェクトポインタ
	*      cls_count  - クラス数
	*    返値:
	*      成功した場合はAILIA_STATUS_SUCCESS、そうでなければエラーコードを返す。
	*/
	[DllImport(Ailia.LIBRARY_NAME)]
	public static extern int ailiaClassifierGetClassCount(IntPtr classifier, ref UInt32 cls_count);

	/**
	*  認識結果を取得します。
	*    引数:
	*      net		  - ネットワークオブジェクトポインタ
	*      cls        - クラス情報
	*      cls_idx    - クラスインデックス
	*      version    - AILIA_CLASSIFIER_CLASS_VERSION
	*    返値:
	*      成功した場合はAILIA_STATUS_SUCCESS、そうでなければエラーコードを返す。
	*    解説:
	*      ailiaPredictを一度も実行していない場合はAILIA_STATUS_INVALID_STATEが返ります。
	*      認識結果は確率順でソートされます。
	*/
	[DllImport(Ailia.LIBRARY_NAME)]
	public static extern int ailiaClassifierGetClass(IntPtr classifier, [In, Out] AILIAClassifierClass obj, UInt32 cls_idx, UInt32 version);
}
