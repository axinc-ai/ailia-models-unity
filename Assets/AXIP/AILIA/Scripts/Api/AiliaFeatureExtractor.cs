/**
* \~japanese
* @file
* @brief AILIA Unity Plugin Native Interface
* @copyright 2018-2021 AXELL Corporation
* @date July 28, 2021
*/
using UnityEngine;
using System.Collections;
using System.IO;
using System;
using System.Runtime.InteropServices;

public class AiliaFeatureExtractor
{
    /****************************************************************
    * 特徴抽出API
    **/

    /**
    * \~japanese
    * L2ノルム
    */
    public const Int32 AILIA_FEATURE_EXTRACTOR_DISTANCE_L2NORM=(0);

    /**
    * \~japanese
    * @brief 特徴抽出オブジェクトを作成します。
    * @param fextractor 特徴抽出オブジェクトポインタ
    * @param net        ネットワークオブジェクトポインタ
    * @param format     ネットワークの画像フォーマット (AILIA_NETWORK_IMAGE_FORMAT_*)
    * @param channel    ネットワークの画像チャンネル (AILIA_NETWORK_IMAGE_CHANNEL_*)
    * @param range      ネットワークの画像レンジ (AILIA_NETWORK_IMAGE_RANGE_*)
    * @param layer_name 特徴に対応したレイヤーの名称 (VGG16の場合はfc1, nullで最終レイヤー)
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    */
    [DllImport(Ailia.LIBRARY_NAME, EntryPoint = "ailiaCreateFeatureExtractor", CharSet=CharSet.Ansi)]
    public static extern int ailiaCreateFeatureExtractor(ref IntPtr fextractor, IntPtr net, UInt32 format, UInt32 channel, UInt32 range, string layer_name);

    /**
    * \~japanese
    * @brief 特徴抽出クオブジェクトを破棄します。
    * @param fextractor 特徴抽出オブジェクトポインタ
    */
    [DllImport(Ailia.LIBRARY_NAME)]
    public static extern void ailiaDestroyFeatureExtractor(IntPtr fextractor);


    /**
    * \~japanese
    * @brief 特徴の抽出を行います。
    * @param fextractor                  特徴抽出オブジェクトポインタ
    * @param dst                         特徴の格納先ポインタ(numeric型)
    * @param dst_size                    dstのサイズ(byte)
    * @param src                         画像データ(32bpp)
    * @param src_stride                  1ラインのバイト数
    * @param src_width                   画像幅
    * @param src_height                  画像高さ
    * @param src_format                  画像フォーマット (AILIA_IMAGE_FORMAT_*)
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    */
    [DllImport(Ailia.LIBRARY_NAME)]
    public static extern int ailiaFeatureExtractorCompute(IntPtr fextractor, IntPtr dst, UInt32 dst_size, IntPtr src, UInt32 src_stride, UInt32 src_width, UInt32 src_height, UInt32 src_format);

    /**
    * \~japanese
    * @brief 特徴間の距離を計算します。
    * @param fextractor                  特徴抽出オブジェクトポインタ
    * @param distance                    特徴間距離
    * @param distance_type               特徴間距離の種別
    * @param feature1                    特徴の格納先ポインタ(numeric型)
    * @param feature1_size               dstのサイズ(byte)
    * @param feature2                    特徴の格納先ポインタ(numeric型)
    * @param feature2_size               dstのサイズ(byte)
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    */
    [DllImport(Ailia.LIBRARY_NAME)]
    public static extern int ailiaFeatureExtractorMatch(IntPtr fextractor, ref float distance, UInt32 distace_type, IntPtr feature1, UInt32 feature1_size, IntPtr feature2, UInt32 feature2_size);
}
