/**
* \~japanese
* @file
* @brief AILIA Unity Plugin Native Interface
* @copyright 2018-2021 AXELL Corporation
* @date July 28, 2021
*
* \~english
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
        * 
        * \~english
        * Object category number (0 to category_count-1)
        */
        public UInt32 category;
        /**
        * \~japanese
        * 推定確率
        * 
        * \~english
        * Estimated Probability
        */
        public float prob;
        /**
        * \~japanese
        * 左上X位置(1で画像横幅)
        * 
        * \~english
        * Top-left X position (image width at 1)
        */
        public float x;
        /**
        * \~japanese
        * 左上Y位置(1で画像高さ)
        * 
        * \~english
        * Top-left Y position (image height at 1)
        */
        public float y;
        /**
        * \~japanese
        * 幅(1で画像横幅、負数は取らない)
        * 
        * \~english
        * Width (1 for image width, negative numbers are not taken)
        */
        public float w;
        /**
        * \~japanese
        * 高さ(1で画像高さ、負数は取らない)
        * 
        * \~english
        * Height (image height with 1, negative numbers are not taken)
        */
        public float h;
    }

    /**
    * \~japanese
    * YOLOV1
    * 
    * \~english
    * YOLOV1
    */
    public const Int32  AILIA_DETECTOR_ALGORITHM_YOLOV1 = (0);
    /**
    * \~japanese
    * YOLOV2
    * 
    * \~english
    * YOLOV2
    */
    public const Int32  AILIA_DETECTOR_ALGORITHM_YOLOV2 = (1);
    /**
    * \~japanese
    * YOLOV3
    * 
    * \~english
    * YOLOV3
    */
    public const Int32  AILIA_DETECTOR_ALGORITHM_YOLOV3 = (2);
    /**
    * \~japanese
    * YOLOV4
    * 
    * \~english
    * YOLOV4
    */
    public const Int32  AILIA_DETECTOR_ALGORITHM_YOLOV4 = (3);
    /**
    * \~japanese
    * YOLOX
    * 
    * \~english
    * YOLOX
    */
    public const Int32  AILIA_DETECTOR_ALGORITHM_YOLOX  = (4);

    /**
    * \~japanese
    * SSD
    * 
    * \~english
    * SSD
    */
    public const Int32  AILIA_DETECTOR_ALGORITHM_SSD    = (8);

    /**
    * \~japanese
    * オプションなし
    * 
    * \~english
    * No options
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
    * 
    *  \~japanese
    * @brief   Create a detector object.
    * @param detector          Detector object pointer
    * @param net               Network object pointer
    * @param format            Image format of the network (AILIA_NETWORK_IMAGE_FORMAT_*)
    * @param channel           Image channel of the network (AILIA_NETWORK_IMAGE_CHANNEL_*)
    * @param range             Image range of the network (AILIA_NETWORK_IMAGE_RANGE_*)
    * @param algorithm         Detection algorithm (AILIA_DETECTOR_ALGORITHM_*)
    * @param caregory_count    Number of detection categories (20 for VOC, 80 for COCO, etc.)
    * @param flags             Additional option flags (AILIA_DETECTOR_FLAG_*)
    * @return
    *   Return \ref AILIA_STATUS_SUCCESS if successful, otherwise return error code.
    */
    [DllImport(Ailia.LIBRARY_NAME)]
    public static extern int ailiaCreateDetector(ref IntPtr detector,IntPtr net, UInt32 format, UInt32 channel,UInt32 range, UInt32 algorithm, UInt32 category_count, UInt32 flags);

    /**
    * \~japanese
    * @brief 検出オブジェクトを破棄します。
    * @param detector 検出オブジェクトポインタ
    * 
    * \~english
    * @brief   Destroy the detector object.
    * @param detector   Detection object pointer
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
    * 
    * \~english
    * @brief   Perform object recognition.
    * @param detector     Detection object pointer
    * @param src          Image data (32bpp)
    * @param src_stride   Byte count of one line
    * @param src_width    Image width
    * @param src_height   Image height
    * @param src_format   Image format (AILIA_IMAGE_FORMAT_*)
    * @param threshold    Detection threshold (e.g. 0.1f) (the smaller the threshold, the more likely it is to be detected and the more detections will be made)
    * @param iou          Duplicate exclusion threshold (e.g. 0.45f) (the smaller the threshold is, the less duplicates are allowed and the fewer the number of detections)
    * @return
    *   If successful, return \ref AILIA_STATUS_SUCCESS, otherwise return error code.
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
    * 
    * \~english
    * @brief   Get the number of recognition results.
    * @param detector    Detection object pointer
    * @param obj_count   Number of objects
    * @return
    *   If successful, return \ref AILIA_STATUS_SUCCESS, otherwise return error code.
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
    *   ailiaPredict() を一度も実行していない場合は \ref AILIA_STATUS_INVALID_STATE が返ります。
    *   認識結果は確率順でソートされます。
    * 
    * \~english
    * @brief Get the recognition result.
    * @param detector Detection object pointer
    * @param obj object information
    * @param obj_idx object index
    * @param version AILIA_DETECTOR_OBJECT_VERSION
    * @return
    * Returns \ref AILIA_STATUS_SUCCESS if successful, otherwise returns error code.
    * @details.
    *   If ailiaPredict() has never been executed, then return * \ref AILIA_STATUS_INVALID_STATE.
    *   Recognition results are sorted in order of probability.
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
    * 
    * \~english
    * @brief   Set anchors or biases information for YoloV2 etc.
    * @param detector        Detection object pointer
    * @param anchors         Dimensions of anchors (possible shape, height and width of detection box)
    * @param anchors_count   number of anchors (half of the array size of anchors)
    * @return
    *   Return \ref AILIA_STATUS_SUCCESS if successful, otherwise return error code.
    * @details
    *   YoloV2, for example, tries multiple forms of the default detection box at the same time. This data describes the plural form of the box.
    *   anchors is stored in the form {x,y,x,y...} The anchors are stored in the form {x,y,x,y...}.
    *   If anchors_count is 5, then anchors is a 10-dimensional array.
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
    * 
    * \~english
    * @brief   Specifies the input image size to the model in YoloV3.
    * @param detector       Detection object pointer
    * @param input_width    Input image width for the model
    * @param input_height   Input image height of the model
    * @return
    *   Returns \ref AILIA_STATUS_SUCCESS if successful, otherwise returns an error code.
    * @details
    *   YoloV3 allows a single model to correspond to any input resolution. (with a multiple limit of 32)
    *   Execute this API if you want to specify the input image size for a model, e.g. to reduce computation time.
    *   This API must be executed between ailiaCreateDetector() and ailiaDetectorCompute().
    *   If this API is not executed, the default value of 416x416 is used.
    *   If executed outside of YOLOv3, it returns \ref AILIA_STATUS_INVALID_STATE.
    */
    [DllImport(Ailia.LIBRARY_NAME)]
    public static extern int ailiaDetectorSetInputShape(IntPtr detector, UInt32 input_width, UInt32 input_height);
}
