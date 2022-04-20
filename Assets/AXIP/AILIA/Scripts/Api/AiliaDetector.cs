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

public class AiliaDetector
{
    /****************************************************************
    * 物体情報
    **/

    public const Int32  AILIA_DETECTOR_OBJECT_VERSION = (1);

    [StructLayout(LayoutKind.Sequential)]
    public class AILIADetectorObject {
        /**
        * \~japanese
        * オブジェクトカテゴリ番号(0～category_count-1)
        */
        public UInt32 category;
        /**
        * \~japanese
        * 推定確率
        */
        public float prob;
        /**
        * \~japanese
        * 左上X位置(1で画像横幅)
        */
        public float x;
        /**
        * \~japanese
        * 左上Y位置(1で画像高さ)
        */
        public float y;
        /**
        * \~japanese
        * 幅(1で画像横幅、負数は取らない)
        */
        public float w;
        /**
        * \~japanese
        * 高さ(1で画像高さ、負数は取らない)
        */
        public float h;
    }

    /**
    * \~japanese
    * YOLOV1
    */
    public const Int32  AILIA_DETECTOR_ALGORITHM_YOLOV1 = (0);
    /**
    * \~japanese
    * YOLOV2
    */
    public const Int32  AILIA_DETECTOR_ALGORITHM_YOLOV2 = (1);
    /**
    * \~japanese
    * YOLOV3
    */
    public const Int32  AILIA_DETECTOR_ALGORITHM_YOLOV3 = (2);
    /**
    * \~japanese
    * YOLOV4
    */
    public const Int32  AILIA_DETECTOR_ALGORITHM_YOLOV4 = (3);
    /**
    * \~japanese
    * YOLOX
    */
    public const Int32  AILIA_DETECTOR_ALGORITHM_YOLOX  = (4);

    /**
    * \~japanese
    * SSD
    */
    public const Int32  AILIA_DETECTOR_ALGORITHM_SSD    = (8);

    /**
    * \~japanese
    * オプションなし
    */
    public const Int32  AILIA_DETECTOR_FLAG_NORMAL      = (0);

    /****************************************************************
    * 物体認識
    **/

    /**
    * \~japanese
    * @brief 検出オブジェクトを作成します。
    * @param detector       検出オブジェクトポインタ
    * @param net            ネットワークオブジェクトポインタ
    * @param format         ネットワークの画像フォーマット (AILIA_NETWORK_IMAGE_FORMAT_*)
    * @param channel        ネットワークの画像チャンネル (AILIA_NETWORK_IMAGE_CHANNEL_*)
    * @param range          ネットワークの画像レンジ (AILIA_NETWORK_IMAGE_RANGE_*)
    * @param algorithm      検出アルゴリズム(AILIA_DETECTOR_ALGORITHM_*)
    * @param caregory_count 検出カテゴリ数(VOCの場合は20、COCOの場合は80、などを指定)
    * @param flags          追加オプションフラグ(AILIA_DETECTOR_FLAG_*)
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    */
    [DllImport(Ailia.LIBRARY_NAME)]
    public static extern int ailiaCreateDetector(ref IntPtr detector,IntPtr net, UInt32 format, UInt32 channel,UInt32 range, UInt32 algorithm, UInt32 category_count, UInt32 flags);

    /**
    * \~japanese
    * @brief 検出オブジェクトを破棄します。
    * @param detector 検出オブジェクトポインタ
    */
    [DllImport(Ailia.LIBRARY_NAME)]
    public static extern void ailiaDestroyDetector(IntPtr detector);

    /**
    * \~japanese
    * @brief 物体認識を行います。
    * @param detector                    検出オブジェクトポインタ
    * @param src                         画像データ(32bpp)
    * @param src_stride                  1ラインのバイト数
    * @param src_width                   画像幅
    * @param src_height                  画像高さ
    * @param src_format                  画像フォーマット(AILIA_IMAGE_FORMAT_*)
    * @param threshold                   検出しきい値(0.1f等)(小さいほど検出されやすくなり、検出数増加)
    * @param iou                         重複除外しきい値(0.45f等)(小さいほど重複を許容せず検出数減少)
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    */
    [DllImport(Ailia.LIBRARY_NAME)]
    public static extern int ailiaDetectorCompute(IntPtr detector, IntPtr src, UInt32 src_stride, UInt32 src_width, UInt32 src_height, UInt32 src_format, float threshold, float iou);

    /**
    * \~japanese
    * @brief 認識結果の数を取得します。
    * @param detector   検出オブジェクトポインタ
    * @param obj_count  オブジェクト数
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    */
    [DllImport(Ailia.LIBRARY_NAME)]
    public static extern int ailiaDetectorGetObjectCount(IntPtr detector, ref UInt32 obj_count);

    /**
    * \~japanese
    * @brief 認識結果を取得します。
    * @param detector   検出オブジェクトポインタ
    * @param obj        オブジェクト情報
    * @param obj_idx    オブジェクトインデックス
    * @param version    AILIA_DETECTOR_OBJECT_VERSION
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    * @details
    *    ailiaPredict() を一度も実行していない場合は \ref AILIA_STATUS_INVALID_STATE が返ります。
    *   認識結果は確率順でソートされます。
    */
    [DllImport(Ailia.LIBRARY_NAME)]
    public static extern int ailiaDetectorGetObject(IntPtr detector, [In,Out] AILIADetectorObject obj, UInt32 obj_idx, UInt32 version);

    /**
    * \~japanese
    * @brief YoloV2などのためにアンカーズ (anchors又はbiases) の情報を設定します。
    * @param detector       検出オブジェクトポインタ
    * @param anchors        アンカーズの寸法 (検出ボックスの可能な形、高さと広さ)
    * @param anchors_count  アンカーズの数 (anchorsの配列サイズの半分)
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    * @details
    *   YoloV2などは既定の複数な形の検出ボックスを同時に試しています。このデータはそのボックスの複数な形の情報を記述します。
    *   anchorsには{x,y,x,y...}の形式で格納します。
    *   anchors_countが5の場合、anchorsは10次元の配列になります。
    */
    [DllImport(Ailia.LIBRARY_NAME)]
    public static extern int ailiaDetectorSetAnchors(IntPtr detector, float [] anchors, UInt32 anchors_count);

    /**
    * \~japanese
    * @brief YoloV3でのモデルへの入力画像サイズを指定します。
    * @param detector       検出オブジェクトポインタ
    * @param input_width    モデルの入力画像幅
    * @param input_height   モデルの入力画像高さ
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    * @details
    *   YoloV3では単一のモデルが任意の入力解像度に対応します。(32 の倍数制限あり)
    *   計算量の削減等でモデルへの入力画像サイズを指定する場合この API を実行してください。
    *    ailiaCreateDetector() と  ailiaDetectorCompute() の間に実行する必要があります。
    *   この API を実行しない場合、デフォルトの 416x416 を利用します。
    *   YOLOv3 以外で実行した場合、 \ref AILIA_STATUS_INVALID_STATE  を返します。
    */
    [DllImport(Ailia.LIBRARY_NAME)]
    public static extern int ailiaDetectorSetInputShape(IntPtr detector, UInt32 input_width, UInt32 input_height);
}
