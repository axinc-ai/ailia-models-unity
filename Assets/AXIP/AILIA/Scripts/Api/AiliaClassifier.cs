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

public class AiliaClassifier
{
    /****************************************************************
    * 識別情報
    **/

    public const Int32  AILIA_CLASSIFIER_CLASS_VERSION = (1);

    [StructLayout(LayoutKind.Sequential)]
    public class AILIAClassifierClass {
        /**
        * \~japanese
        * オブジェクトカテゴリ番号
        */
        public UInt32 category;
        /**
        * \~japanese
        * 推定確率
        */
        public float prob;
    }

    /****************************************************************
    * 識別API
    **/

    /**
    * \~japanese
    * @brief 識別オブジェクトを作成します。
    * @param classifier 識別オブジェクトポインタへのポインタ
    * @param net        ネットワークオブジェクトポインタ
    * @param format     ネットワークの画像フォーマット （AILIA_NETWORK_IMAGE_FORMAT_*）
    * @param channel    ネットワークの画像チャンネル (AILIA_NETWORK_IMAGE_CHANNEL_*)
    * @param range      ネットワークの画像レンジ （AILIA_NETWORK_IMAGE_RANGE_*）
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    * @details
    *   識別オブジェクトを作成します。
    */
    [DllImport(Ailia.LIBRARY_NAME)]
    public static extern int ailiaCreateClassifier(ref IntPtr classifier,IntPtr net, UInt32 format, UInt32 channel, UInt32 range);

    /**
    * \~japanese
    * @brief 識別オブジェクトを破棄します。
    * @param classifier 識別オブジェクトポインタ
    */
    [DllImport(Ailia.LIBRARY_NAME)]
    public static extern void ailiaDestroyClassifier(IntPtr classifier);

    /**
    * \~japanese
    * @brief 物体識別を行います。
    * @param classifier                  識別オブジェクトポインタ
    * @param src                         画像データ
    * @param src_stride                  1ラインのバイト数
    * @param src_width                   画像幅
    * @param src_height                  画像高さ
    * @param src_format                  画像のフォーマット(AILIA_IMAGE_FORMAT_*)
    * @param max_class_count             認識結果の最大個数
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    */
    [DllImport(Ailia.LIBRARY_NAME)]
    public static extern int ailiaClassifierCompute(IntPtr classifier, IntPtr src, UInt32 src_stride, UInt32 src_width, UInt32 src_height, UInt32 src_format, UInt32 max_class_count);

    /**
    * \~japanese
    * @brief 認識結果の数を取得します。
    * @param classifier 識別オブジェクトポインタ
    * @param cls_count  クラス数
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    */
    [DllImport(Ailia.LIBRARY_NAME)]
    public static extern int ailiaClassifierGetClassCount(IntPtr classifier, ref UInt32 cls_count);

    /**
    * \~japanese
    * @brief 認識結果を取得します。
    * @param classifier 識別オブジェクトポインタ
    * @param cls        クラス情報
    * @param cls_idx    クラスインデックス
    * @param version    AILIA_CLASSIFIER_CLASS_VERSION
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    * @details
    *    ailiaPredict() を一度も実行していない場合は \ref AILIA_STATUS_INVALID_STATE が返ります。
    *   認識結果は確率順でソートされます。
    */
    [DllImport(Ailia.LIBRARY_NAME)]
    public static extern int ailiaClassifierGetClass(IntPtr classifier, [In,Out] AILIAClassifierClass obj, UInt32 cls_idx, UInt32 version);
}
