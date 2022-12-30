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
    */
    public const Int32 AILIA_POSE_ESTIMATOR_ALGORITHM_ACCULUS_POSE             = (0);
    /**
    * \~japanese
    *  顔特徴点検出
    */
    public const Int32 AILIA_POSE_ESTIMATOR_ALGORITHM_ACCULUS_FACE             = (1);
    /**
    * \~japanese
    *  上半身姿勢検出
    */
    public const Int32 AILIA_POSE_ESTIMATOR_ALGORITHM_ACCULUS_UP_POSE          = (2);
    /**
    * \~japanese
    *  上半身姿勢検出
    */
    public const Int32 AILIA_POSE_ESTIMATOR_ALGORITHM_ACCULUS_UP_POSE_FPGA     = (3);
    /**
    * \~japanese
    *  手姿勢検出
    */
    public const Int32 AILIA_POSE_ESTIMATOR_ALGORITHM_ACCULUS_HAND             = (5);

    /**
    * \~japanese
    *  姿勢検出
    */
    public const Int32 AILIA_POSE_ESTIMATOR_ALGORITHM_OPEN_POSE                = (10);
    /**
    * \~japanese
    *  姿勢検出
    */
    public const Int32 AILIA_POSE_ESTIMATOR_ALGORITHM_LW_HUMAN_POSE            = (11);
    /**
    * \~japanese
    *  姿勢検出
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
    */
    public const Int32 AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_CNT                  = (19);
    /**
    * \~japanese
    *  個数
    */
    public const Int32 AILIA_POSE_ESTIMATOR_FACE_KEYPOINT_CNT                  = (68);
    /**
    * \~japanese
    *  個数
    */
    public const Int32 AILIA_POSE_ESTIMATOR_UPPOSE_KEYPOINT_CNT                = (15);
    /**
    * \~japanese
    *  個数
    */
    public const Int32 AILIA_POSE_ESTIMATOR_HAND_KEYPOINT_CNT                  = (21);

    [StructLayout(LayoutKind.Sequential)]
    public struct AILIAPoseEstimatorKeypoint {
        /**
        * \~japanese
        * 入力画像内 X座標  [0.0 , 1.0)
        */
        public float x;
        /**
        * \~japanese
        * 入力画像内 Y座標  [0.0 , 1.0)
        */
        public float y;
        /**
        * \~japanese
        * 姿勢検出のみ有効。体中心を座標0とした時に推定されるローカルX座標。単位(スケール)は x と同じです。
        */
        public float x_local;
        /**
        * \~japanese
        * 姿勢検出のみ有効。体中心を座標0とした時に推定されるローカルY座標。単位(スケール)は x と同じです。
        */
        public float y_local;
        /**
        * \~japanese
        * 姿勢検出のみ有効。体中心を座標0とした時に推定されるローカルZ座標。単位(スケール)は x と同じです。
        */
        public float z_local;
        /**
        * \~japanese
        * この点の検出信頼度。値0.0Fの場合、この点は未検出のため使用できません。
        */
        public float score;
        /**
        * \~japanese
        * 通常は値0です。この点が未検出で、他の点から補間可能な場合、x,yの値を補間し、interpolated=1となります。
        */
        public Int32 interpolated;
    }


    /*
    //追加
    [StructLayout(LayoutKind.Sequential)]
    public struct AILIAPoseEstimatorKeypointCoord3D
    {
        
        public float x;
        
        public float y;
        
        public float z;
    }
    */


    /**
    * \~japanese
    *  構造体フォーマットバージョン
    */
    public const Int32 AILIA_POSE_ESTIMATOR_OBJECT_POSE_VERSION = (1);

    [StructLayout(LayoutKind.Sequential)]
    public class AILIAPoseEstimatorObjectPose {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_CNT)]
        /**
        * \~japanese
        * 検出した関節点。配列インデックスが関節番号に相当します。
        */
        public AILIAPoseEstimatorKeypoint[] points;
        /**
        * \~japanese
        * 検出した関節点の3次元座標。配列インデックスが関節番号に相当します。
        */
        //public AILIAPoseEstimatorKeypointCoord3D[] pointsCoord3D; //追加
        /**
        * \~japanese
        * このオブジェクトの検出信頼度
        */
        public float total_score;
        /**
        * \~japanese
        * points[]の中で正常に検出された関節点の個数
        */
        public Int32 num_valid_points;
        /**
        * \~japanese
        * 時間方向に、このオブジェクトにユニークなIDです。1以上の正の値です。
        */
        public Int32 id;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        /**
        * \~japanese
        * このオブジェクトのオイラー角 yaw, pitch, roll [単位radian]。現在yawのみ対応しています。角度が検出されない場合FLT_MAXが格納されます。
        */
        public float [] angle;
    }

    /**
    * \~japanese
    *  構造体フォーマットバージョン
    */
    public const Int32 AILIA_POSE_ESTIMATOR_OBJECT_FACE_VERSION = (1);

    [StructLayout(LayoutKind.Sequential)]
    public class AILIAPoseEstimatorObjectFace {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)AILIA_POSE_ESTIMATOR_FACE_KEYPOINT_CNT)]
        /**
        * \~japanese
        * 検出した関節点。配列インデックスが関節番号に相当します。
        */
        public AILIAPoseEstimatorKeypoint[] points;
        /**
        * \~japanese
        * このオブジェクトの検出信頼度
        */
        public float total_score;
    }

    /**
    * \~japanese
    *  構造体フォーマットバージョン
    */
    public const Int32 AILIA_POSE_ESTIMATOR_OBJECT_UPPOSE_VERSION = (1);

    [StructLayout(LayoutKind.Sequential)]
    public class AILIAPoseEstimatorObjectUpPose {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)AILIA_POSE_ESTIMATOR_UPPOSE_KEYPOINT_CNT)]
        /**
        * \~japanese
        * 検出した関節点。配列インデックスが関節番号に相当します。
        */
        public AILIAPoseEstimatorKeypoint[] points;
        /**
        * \~japanese
        * このオブジェクトの検出信頼度
        */
        public float total_score;
        /**
        * \~japanese
        * points[]の中で正常に検出された関節点の個数
        */
        public Int32 num_valid_points;
        /**
        * \~japanese
        * 時間方向に、このオブジェクトにユニークなIDです。1以上の正の値です。
        */
        public Int32 id;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        /**
        * \~japanese
        * このオブジェクトのオイラー角 yaw, pitch, roll [単位radian]。現在yawのみ対応しています。角度が検出されない場合FLT_MAXが格納されます。
        */
        public float[] angle;
    }

    /**
    * \~japanese
    *  構造体フォーマットバージョン
    */
    public const Int32 AILIA_POSE_ESTIMATOR_OBJECT_HAND_VERSION=(1);

    [StructLayout(LayoutKind.Sequential)]
    public class AILIAPoseEstimatorObjectHand {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)AILIA_POSE_ESTIMATOR_HAND_KEYPOINT_CNT)]
        /**
        * \~japanese
        * 検出した関節点。配列インデックスが関節番号に相当します。
        */
        public AILIAPoseEstimatorKeypoint[] points;
        /**
        * \~japanese
        * このオブジェクトの検出信頼度
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
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaCreatePoseEstimator(ref IntPtr pose_estimator, IntPtr net, UInt32 algorithm);

    /**
    * \~japanese
    * @brief 検出オブジェクトを破棄します。
    * @param pose_estimator 検出オブジェクトポインタ
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
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaPoseEstimatorGetObjectHand(IntPtr pose_estimator, [In, Out] AILIAPoseEstimatorObjectHand obj, UInt32 obj_idx, UInt32 version);
}
