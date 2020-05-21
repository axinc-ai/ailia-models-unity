/* AILIA Unity Plugin Native Interface */
/* Copyright 2018 AXELL CORPORATION */

using UnityEngine;
using System.Collections;
using System.IO;
using System;
using System.Runtime.InteropServices;

namespace ailiaSDK
{
	public class AiliaFeatureExtractor
	{
		/****************************************************************
		* 特徴抽出API
		**/

		public const UInt32 AILIA_FEATURE_EXTRACTOR_DISTANCE_L2NORM = (0);  /* L2ノルム */

		/**
		*  特徴抽出オブジェクトを作成します。
		*    引数:
		*      fextractor - 特徴抽出オブジェクトポインタ
		*      net        - ネットワークオブジェクトポインタ
		*      format     - ネットワークの画像フォーマット （AILIA_NETWORK_IMAGE_FORMAT_*）
		*      channel    - ネットワークの画像チャンネル (AILIA_NETWORK_IMAGE_CHANNEL_*)
		*      range      - ネットワークの画像レンジ （AILIA_NETWORK_IMAGE_RANGE_*）
		*      layer_name - 特徴に対応したレイヤーの名称 (VGG16の場合はfc1,　NULLで最終レイヤー)
		*    返値:
		*      成功した場合はAILIA_STATUS_SUCCESS、そうでなければエラーコードを返す。
		*/
		[DllImport(Ailia.LIBRARY_NAME, EntryPoint = "ailiaCreateFeatureExtractor", CharSet = CharSet.Ansi)]
		public static extern int ailiaCreateFeatureExtractor(ref IntPtr fextractor, IntPtr net, UInt32 format, UInt32 channel, UInt32 range, string layer_name);

		/**
		*  特徴抽出クオブジェクトを破棄します。
		*    引数:
		*      fextractor - 特徴抽出オブジェクトポインタ
		*/
		[DllImport(Ailia.LIBRARY_NAME)]
		public static extern void ailiaDestroyFeatureExtractor(IntPtr fextractor);


		/**
		*  特徴の抽出を行います。
		*    引数:
		*      fextractor                  - 特徴抽出オブジェクトポインタ
		*      dst                         - 特徴の格納先ポインタ（numeric型）
		*      dst_size                    - dstのサイズ(byte)
		*      src                         - 画像データ
		*      src_stride                  - 1ラインのバイト数
		*      src_width                   - 画像幅
		*      src_height                  - 画像高さ
		*      src_format                  - AILIA_UTILITY_IMAGE_FORMAT_*
		*    返値:
		*      成功した場合はAILIA_STATUS_SUCCESS、そうでなければエラーコードを返す。
		*/
		[DllImport(Ailia.LIBRARY_NAME)]
		public static extern int ailiaFeatureExtractorCompute(IntPtr fextractor, IntPtr dst, UInt32 dst_size, IntPtr src, UInt32 src_stride, UInt32 src_width, UInt32 src_height, UInt32 src_format);

		/**
		*  特徴間の距離を計算します。
		*    引数:
		*      fextractor                  - 特徴抽出オブジェクトポインタ
		*      distance                    - 特徴間距離
		*      distance_type               - 特徴間距離の種別
		*      feature1                    - 特徴の格納先ポインタ（numeric型）
		*      feature1_size               - dstのサイズ(byte)
		*      feature2                    - 特徴の格納先ポインタ（numeric型）
		*      feature2_size               - dstのサイズ(byte)
		*    返値:
		*      成功した場合はAILIA_STATUS_SUCCESS、そうでなければエラーコードを返す。
		*/
		[DllImport(Ailia.LIBRARY_NAME)]
		public static extern int ailiaFeatureExtractorMatch(IntPtr fextractor, ref float distance, UInt32 distace_type, IntPtr feature1, UInt32 feature1_size, IntPtr feature2, UInt32 feature2_size);
	}
}