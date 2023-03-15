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

public class AiliaPoseEstimator
{
    /* Native Binary 定義 */

    #if (UNITY_IPHONE && !UNITY_EDITOR) || (UNITY_WEBGL && !UNITY_EDITOR)
        private const String LIBRARY_NAME="__Internal";
    #else
        #if (UNITY_ANDROID && !UNITY_EDITOR)
            private const String LIBRARY_NAME="ailia";
        #else
            #if (UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX)
                private const String LIBRARY_NAME="ailia";
                //private const String LIBRARY_NAME="ailia_pose_estimate";  //for Acculus Pose
            #else
                private const String LIBRARY_NAME="ailia_pose_estimate";
    //private const String LIBRARY_NAME="ailia_pose_estimate_acculus";  //for Acculus Pose
#endif
#endif
#endif

    /****************************************************************
    * 物体情報
    **/

    /**
    * \~japanese
    *  姿勢検出
    *  
    *  \~english
    *  Posture detection
    */
    public const Int32 AILIA_POSE_ESTIMATOR_ALGORITHM_ACCULUS_POSE             = (0);
    /**
    * \~japanese
    *  顔特徴点検出
    *  
    * \~english
    * Facial Feature Point Detection
    */
    public const Int32 AILIA_POSE_ESTIMATOR_ALGORITHM_ACCULUS_FACE             = (1);
    /**
    * \~japanese
    *  上半身姿勢検出
    *  
    * \~english
    *  Upper body posture detection
    */
    public const Int32 AILIA_POSE_ESTIMATOR_ALGORITHM_ACCULUS_UP_POSE          = (2);
    /**
    * \~japanese
    *  上半身姿勢検出
    *  
    * \~english
    *  Upper body posture detection
    */
    public const Int32 AILIA_POSE_ESTIMATOR_ALGORITHM_ACCULUS_UP_POSE_FPGA     = (3);
    /**
    * \~japanese
    *  手姿勢検出
    *  
    * \~english
    *  Hand posture detection
    */
    public const Int32 AILIA_POSE_ESTIMATOR_ALGORITHM_ACCULUS_HAND             = (5);

    /**
    * \~japanese
    *  姿勢検出
    *  
    * \~english
    *  Posture detection
    */
    public const Int32 AILIA_POSE_ESTIMATOR_ALGORITHM_OPEN_POSE                = (10);
    /**
    * \~japanese
    *  姿勢検出
    *  
    * \~english
    *  Posture detection
    */
    public const Int32 AILIA_POSE_ESTIMATOR_ALGORITHM_LW_HUMAN_POSE            = (11);
    /**
    * \~japanese
    *  姿勢検出
    *  
    * \~english
    *  Posture detection
    */
    public const Int32 AILIA_POSE_ESTIMATOR_ALGORITHM_OPEN_POSE_SINGLE_SCALE   = (12);

    /* 姿勢検出 関節点 定義 */
    public const UInt32 AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_NOSE             =(0);
    public const UInt32 AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_EYE_LEFT         =(1);
    public const UInt32 AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_EYE_RIGHT        =(2);
    public const UInt32 AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_EAR_LEFT         =(3);
    public const UInt32 AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_EAR_RIGHT        =(4);
    public const UInt32 AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_SHOULDER_LEFT    =(5);
    public const UInt32 AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_SHOULDER_RIGHT   =(6);
    public const UInt32 AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_ELBOW_LEFT       =(7);
    public const UInt32 AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_ELBOW_RIGHT      =(8);
    public const UInt32 AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_WRIST_LEFT       =(9);
    public const UInt32 AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_WRIST_RIGHT      =(10);
    public const UInt32 AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_HIP_LEFT         =(11);
    public const UInt32 AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_HIP_RIGHT        =(12);
    public const UInt32 AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_KNEE_LEFT        =(13);
    public const UInt32 AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_KNEE_RIGHT       =(14);
    public const UInt32 AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_ANKLE_LEFT       =(15);
    public const UInt32 AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_ANKLE_RIGHT      =(16);
    public const UInt32 AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_SHOULDER_CENTER  =(17);
    public const UInt32 AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_BODY_CENTER      =(18);

    /* 近接上半身姿勢検出 関節点 定義 */
    public const UInt32 AILIA_POSE_ESTIMATOR_UPPOSE_KEYPOINT_NOSE               = (0);
    public const UInt32 AILIA_POSE_ESTIMATOR_UPPOSE_KEYPOINT_EYE_LEFT           = (1);
    public const UInt32 AILIA_POSE_ESTIMATOR_UPPOSE_KEYPOINT_EYE_RIGHT          = (2);
    public const UInt32 AILIA_POSE_ESTIMATOR_UPPOSE_KEYPOINT_EAR_LEFT           = (3);
    public const UInt32 AILIA_POSE_ESTIMATOR_UPPOSE_KEYPOINT_EAR_RIGHT          = (4);
    public const UInt32 AILIA_POSE_ESTIMATOR_UPPOSE_KEYPOINT_SHOULDER_LEFT      = (5);
    public const UInt32 AILIA_POSE_ESTIMATOR_UPPOSE_KEYPOINT_SHOULDER_RIGHT     = (6);
    public const UInt32 AILIA_POSE_ESTIMATOR_UPPOSE_KEYPOINT_ELBOW_LEFT         = (7);
    public const UInt32 AILIA_POSE_ESTIMATOR_UPPOSE_KEYPOINT_ELBOW_RIGHT        = (8);
    public const UInt32 AILIA_POSE_ESTIMATOR_UPPOSE_KEYPOINT_WRIST_LEFT         = (9);
    public const UInt32 AILIA_POSE_ESTIMATOR_UPPOSE_KEYPOINT_WRIST_RIGHT        = (10);
    public const UInt32 AILIA_POSE_ESTIMATOR_UPPOSE_KEYPOINT_HIP_LEFT           = (11);
    public const UInt32 AILIA_POSE_ESTIMATOR_UPPOSE_KEYPOINT_HIP_RIGHT          = (12);
    public const UInt32 AILIA_POSE_ESTIMATOR_UPPOSE_KEYPOINT_SHOULDER_CENTER    = (13);
    public const UInt32 AILIA_POSE_ESTIMATOR_UPPOSE_KEYPOINT_BODY_CENTER        = (14);

    /* 顔特徴点検出 点 定義 */
    /**
    * \~japanese
    *  個数
    *  
    * \~english
    *  Quantity
    */
    public const Int32 AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_CNT                  = (19);
    /**
    * \~japanese
    *  個数
    *  
    * \~english
    *  Quantity
    */
    public const Int32 AILIA_POSE_ESTIMATOR_FACE_KEYPOINT_CNT                  = (68);
    /**
    * \~japanese
    *  個数
    *  
    * \~english
    *  Quantity
    */
    public const Int32 AILIA_POSE_ESTIMATOR_UPPOSE_KEYPOINT_CNT                = (15);
    /**
    * \~japanese
    *  個数
    *  
    * \~english
    *  Quantity
    */
    public const Int32 AILIA_POSE_ESTIMATOR_HAND_KEYPOINT_CNT                  = (21);

    [StructLayout(LayoutKind.Sequential)]
    public struct AILIAPoseEstimatorKeypoint {
        /**
        * \~japanese
        * 入力画像内 X座標  [0.0 , 1.0)
        * 
        * \~english
        * X-coordinate in input image [0.0 , 1.0)
        */
        public float x;
        /**
        * \~japanese
        * 入力画像内 Y座標  [0.0 , 1.0)
        * 
        * \~english
        * Y-coordinate in input image [0.0 , 1.0)
        */
        public float y;
        /**
        * \~japanese
        * 姿勢検出のみ有効。体中心を座標0とした時に推定されるローカルZ座標。単位(スケール)は x と同じです。
        * 
        * \~english
        * Valid only for posture detection. Local Z coordinate estimated when body center is set as coordinate 0. Unit (scale) is the same as x.
        */
        public float z_local;
        /**
        * \~japanese
        * この点の検出信頼度。値0.0Fの場合、この点は未検出のため使用できません。
        * 
        * \~english
        * Detection confidence for this point. A value of 0.0F means that this point has not been detected and cannot be used.
        */
        public float score;
        /**
        * \~japanese
        * 通常は値0です。この点が未検出で、他の点から補間可能な場合、x,yの値を補間し、interpolated=1となります。
        * 
        * \~english
        * Normally the value is 0. If this point is undetected and can be interpolated from other points, the x,y values are interpolated and interpolated=1.
        */
        public Int32 interpolated;
    }

    /**
    * \~japanese
    *  構造体フォーマットバージョン
    *  
    *  \~english
    *  structure format version
    */
    public const Int32 AILIA_POSE_ESTIMATOR_OBJECT_POSE_VERSION = (1);

    [StructLayout(LayoutKind.Sequential)]
    public class AILIAPoseEstimatorObjectPose {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_CNT)]
        /**
        * \~japanese
        * 検出した関節点。配列インデックスが関節番号に相当します。
        * 
        * \~english
        * Detected joint points. The sequence index corresponds to the joint number.
        */
        public AILIAPoseEstimatorKeypoint[] points;
        /**
        * \~japanese
        * このオブジェクトの検出信頼度
        * 
        * \~english
        * Detection confidence for this object
        */
        public float total_score;
        /**
        * \~japanese
        * points[]の中で正常に検出された関節点の個数
        * 
        * \~english
        * Number of joint points successfully detected in points[].
        */
        public Int32 num_valid_points;
        /**
        * \~japanese
        * 時間方向に、このオブジェクトにユニークなIDです。1以上の正の値です。
        * 
        * \~english
        * Unique ID for this object in the time direction. 1 or more positive values.
        */
        public Int32 id;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        /**
        * \~japanese
        * このオブジェクトのオイラー角 yaw, pitch, roll [単位radian]。現在yawのみ対応しています。角度が検出されない場合FLT_MAXが格納されます。
        * 
        * \~english
        * Euler angles yaw, pitch, roll [unit radian] for this object. Currently only yaw is supported. If no angle is detected, FLT_MAX is stored.
        */
        public float [] angle;
    }

    /**
    * \~japanese
    *  構造体フォーマットバージョン
    *  
    *  \~english
    *  Structure-format version
    */
    public const Int32 AILIA_POSE_ESTIMATOR_OBJECT_FACE_VERSION = (1);

    [StructLayout(LayoutKind.Sequential)]
    public class AILIAPoseEstimatorObjectFace {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)AILIA_POSE_ESTIMATOR_FACE_KEYPOINT_CNT)]
        /**
        * \~japanese
        * 検出した関節点。配列インデックスが関節番号に相当します。
        * 
        * \~english
        * Detected joint points. The sequence index corresponds to the joint number.
        */
        public AILIAPoseEstimatorKeypoint[] points;
        /**
        * \~japanese
        * このオブジェクトの検出信頼度
        * 
        * \~english
        * Detection confidence for this object
        */
        public float total_score;
    }

    /**
    * \~japanese
    *  構造体フォーマットバージョン
    *
    *  \~english
    *  Structure-format version
    */
    public const Int32 AILIA_POSE_ESTIMATOR_OBJECT_UPPOSE_VERSION = (1);

    [StructLayout(LayoutKind.Sequential)]
    public class AILIAPoseEstimatorObjectUpPose {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)AILIA_POSE_ESTIMATOR_UPPOSE_KEYPOINT_CNT)]
        /**
        * \~japanese
        * 検出した関節点。配列インデックスが関節番号に相当します。
        * 
        * \~english
        * Detected joint points. The sequence index corresponds to the joint number.
        */
        public AILIAPoseEstimatorKeypoint[] points;
        /**
        * \~japanese
        * このオブジェクトの検出信頼度
        * 
        * \~english
        * Detection confidence for this object
        */
        public float total_score;
        /**
        * \~japanese
        * points[]の中で正常に検出された関節点の個数
        * 
        * \~english
        * Number of joint points successfully detected in points[].
        */
        public Int32 num_valid_points;
        /**
        * \~japanese
        * 時間方向に、このオブジェクトにユニークなIDです。1以上の正の値です。
        * 
        * \~english
        * Unique ID for this object in the time direction. 1 or more positive values.
        */
        public Int32 id;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        /**
        * \~japanese
        * このオブジェクトのオイラー角 yaw, pitch, roll [単位radian]。現在yawのみ対応しています。角度が検出されない場合FLT_MAXが格納されます。
        * 
        * \~english
        * Euler angles yaw, pitch, roll [unit radian] for this object. Currently only yaw is supported. If no angle is detected, FLT_MAX is stored.
        */
        public float[] angle;
    }

    /**
    * \~japanese
    *  構造体フォーマットバージョン
    * 
    * \~english
    *  Structure-format version
    */
    public const Int32 AILIA_POSE_ESTIMATOR_OBJECT_HAND_VERSION=(1);

    [StructLayout(LayoutKind.Sequential)]
    public class AILIAPoseEstimatorObjectHand {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)AILIA_POSE_ESTIMATOR_HAND_KEYPOINT_CNT)]
        /**
        * \~japanese
        * 検出した関節点。配列インデックスが関節番号に相当します。
        * 
        * \~english
        * Detected joint points. The sequence index corresponds to the joint number.
        */
        public AILIAPoseEstimatorKeypoint[] points;
        /**
        * \~japanese
        * このオブジェクトの検出信頼度
        * 
        * \~english
        * Detection confidence for this object
        */
        public float total_score;
    }

    /****************************************************************
    * 姿勢検出・顔特徴点検出API
    **/

    /**
    * \~japanese
    * @brief 検出オブジェクトを作成します。
    * @param pose_estimator 検出オブジェクトポインタ
    * @param net            ネットワークオブジェクトポインタ
    * @param algorithm      検出アルゴリズム (AILIA_POSE_ESTIMATOR_ALGORITHM_*)
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    * @details
    *   caffemodelとprototxtを読み込んだAILIANetworkから検出オブジェクトを作成します。
    * 
    * \~english
    * @brief   Create a detection object.
    * @param pose_estimator   Detection object pointer
    * @param net              Network object pointer
    * @param algorithm        Detection algorithm (AILIA_POSE_ESTIMATOR_ALGORITHM_*)
    * @return
    *   Returns \ref AILIA_STATUS_SUCCESS if successful, otherwise returns an error code.
    * @details
    *   Create a detection object from AILIANetwork with caffemodel and prototxt loaded.
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaCreatePoseEstimator(ref IntPtr pose_estimator, IntPtr net, UInt32 algorithm);

    /**
    * \~japanese
    * @brief 検出オブジェクトを破棄します。
    * @param pose_estimator 検出オブジェクトポインタ
    * 
    * \~english
    * @brief   Destroy the detection object.
    * @param pose_estimator   Detection object pointer
    */
    [DllImport(LIBRARY_NAME)]
    public static extern void ailiaDestroyPoseEstimator(IntPtr pose_estimator);

    /**
    * \~japanese
    * @brief 検出閾値を設定します。
    * @param pose_estimator              検出オブジェクトポインタ
    * @param threshold                   検出閾値 0.0以上1.0以下の値で、値が小さいほど検出しやすくなります。
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    * 
    * \~english
    * @brief   Set the detection threshold.
    * @param pose_estimator   Detection object pointer
    * @param threshold        Detection threshold value between 0.0 and 1.0, the smaller the value, the easier to detect.
    * @return
    *   If successful, return \ref AILIA_STATUS_SUCCESS, otherwise return error code.
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaPoseEstimatorSetThreshold(IntPtr pose_estimator, float threshold);

    /**
    * \~japanese
    * @brief 骨格検出・顔特徴点検出を行います。
    * @param pose_estimator              検出オブジェクトポインタ
    * @param src                         画像データ(32bpp)
    * @param src_stride                  1ラインのバイト数
    * @param src_width                   画像幅
    * @param src_height                  画像高さ
    * @param src_format                  画像形式 (AILIA_IMAGE_FORMAT_*)
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    * 
    * \~english
    * @brief   Skeletal and facial feature point detection.
    * @param pose_estimator   Detection object pointer
    * @param src              Image data (32bpp)
    * @param src_stride       Bytes per line
    * @param src_width        Image width
    * @param src_height       Image height
    * @param src_format       Image format (AILIA_IMAGE_FORMAT_*)
    * @return
    *   Returns \ref AILIA_STATUS_SUCCESS if successful, otherwise returns an error code.
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaPoseEstimatorCompute(IntPtr pose_estimator, IntPtr src, UInt32 src_stride, UInt32 src_width, UInt32 src_height, UInt32 src_format);

    /**
    * \~japanese
    * @brief 認識結果の数を取得します。
    * @param pose_estimator  検出オブジェクトポインタ
    * @param obj_count       オブジェクト数  顔特徴点の場合は1または0となります。
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    * 
    * \~english
    * @brief   Get the number of recognition results.
    * @param pose_estimator   Detection object pointer
    * @param obj_count        Number of objects 1 or 0 for face feature points.
    * @return
    *   If successful, return \ref AILIA_STATUS_SUCCESS, otherwise return error code.
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaPoseEstimatorGetObjectCount(IntPtr pose_estimator, ref UInt32 obj_count);

    /**
    * \~japanese
    * @brief 骨格検出認識結果を取得します。
    * @param pose_estimator  検出オブジェクトポインタ
    * @param obj             オブジェクト情報
    * @param obj_idx         オブジェクトインデックス
    * @param version         AILIA_POSE_ESTIMATOR_OBJECT_POSE_VERSION
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    * 
    * \~english
    * @brief   Get the result of skeletal detection recognition.
    * @param pose_estimator   Detection object pointer
    * @param obj              Object information
    * @param obj_idx          Object index
    * @param version          AILIA_POSE_ESTIMATOR_OBJECT_POSE_VERSION
    * @return
    *   If successful, return \ref AILIA_STATUS_SUCCESS, otherwise return error code.
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaPoseEstimatorGetObjectPose(IntPtr pose_estimator, [In,Out] AILIAPoseEstimatorObjectPose obj, UInt32 obj_idx, UInt32 version);

    /**
    * \~japanese
    * @brief 顔特徴点検出結果を取得します。
    * @param pose_estimator  検出オブジェクトポインタ
    * @param obj             オブジェクト情報
    * @param obj_idx         オブジェクトインデックス 必ず 0 を指定してください。
    * @param version         AILIA_POSE_ESTIMATOR_OBJECT_FACE_VERSION
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    * 
    * \~english
    * @brief Get the result of face feature point detection.
    * @param pose_estimator   Detection object pointer
    * @param obj              Object information
    * @param obj_idx          Object index Must be 0.
    * @param version          AILIA_POSE_ESTIMATOR_OBJECT_FACE_VERSION
    * @return
    *   If successful, return \ref AILIA_STATUS_SUCCESS, otherwise return error code.
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaPoseEstimatorGetObjectFace(IntPtr pose_estimator, [In,Out] AILIAPoseEstimatorObjectFace obj, UInt32 obj_idx, UInt32 version);

    /**
    * \~japanese
    * @brief UpPose 認識結果を取得します。
    * @param pose_estimator  検出オブジェクトポインタ
    * @param obj             オブジェクト情報
    * @param obj_idx         オブジェクトインデックス
    * @param version         AILIA_POSE_ESTIMATOR_OBJECT_UPPOSE_VERSION
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    * 
    * \~english
    * @brief   UpPose Get the recognition result.
    * @param pose_estimator   Detection object pointer
    * @param obj              Object information
    * @param obj_idx          Object index
    * @param version          AILIA_POSE_ESTIMATOR_OBJECT_UPPOSE_VERSION
    * @return
    *   If successful, return \ref AILIA_STATUS_SUCCESS, otherwise return error code.
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaPoseEstimatorGetObjectUpPose(IntPtr pose_estimator, [In, Out] AILIAPoseEstimatorObjectUpPose obj, UInt32 obj_idx, UInt32 version);

    /**
    * \~japanese
    * @brief Hand 認識結果を取得します。
    * @param pose_estimator  検出オブジェクトポインタ
    * @param obj             オブジェクト情報
    * @param obj_idx         オブジェクトインデックス 必ず 0 を指定してください。
    * @param version         AILIA_POSE_ESTIMATOR_OBJECT_HAND_VERSION
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    * 
    * \~english
    * @brief   Hand Get the recognition result.
    * @param pose_estimator   Detection object pointer
    * @param obj              Object information
    * @param obj_idx          Object index Must be 0.
    * @param version          AILIA_POSE_ESTIMATOR_OBJECT_HAND_VERSION
    * @return
    *   If successful, return \ref AILIA_STATUS_SUCCESS, otherwise return error code.
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaPoseEstimatorGetObjectHand(IntPtr pose_estimator, [In, Out] AILIAPoseEstimatorObjectHand obj, UInt32 obj_idx, UInt32 version);
}
