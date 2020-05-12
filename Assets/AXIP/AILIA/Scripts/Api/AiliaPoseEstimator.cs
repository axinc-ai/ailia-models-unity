/* AILIA Unity Plugin Native Interface */
/* Copyright 2019 AXELL CORPORATION */

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
				//private const String LIBRARY_NAME="ailia_pose_estimate";	//for Acculus Pose
#else
	private const String LIBRARY_NAME = "ailia_pose_estimate";
	//private const String LIBRARY_NAME="ailia_pose_estimate_acculus";	//for Acculus Pose
#endif
#endif
#endif

	/****************************************************************
	* 物体情報
	**/

	public const UInt32 AILIA_POSE_ESTIMATOR_ALGORITHM_ACCULUS_POSE = (0); // 姿勢検出
	public const UInt32 AILIA_POSE_ESTIMATOR_ALGORITHM_ACCULUS_FACE = (1); // 顔特徴点検出
	public const UInt32 AILIA_POSE_ESTIMATOR_ALGORITHM_ACCULUS_UP_POSE = (2); // 上半身姿勢検出
	public const UInt32 AILIA_POSE_ESTIMATOR_ALGORITHM_ACCULUS_UP_POSE_FPGA = (3); // 上半身姿勢検出

	public const UInt32 AILIA_POSE_ESTIMATOR_ALGORITHM_OPEN_POSE = (10); // 姿勢検出
	public const UInt32 AILIA_POSE_ESTIMATOR_ALGORITHM_LW_HUMAN_POSE = (11); // 姿勢検出

	/* 姿勢検出 関節点 定義 */
	public const UInt32 AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_NOSE = (0);
	public const UInt32 AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_EYE_LEFT = (1);
	public const UInt32 AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_EYE_RIGHT = (2);
	public const UInt32 AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_EAR_LEFT = (3);
	public const UInt32 AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_EAR_RIGHT = (4);
	public const UInt32 AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_SHOULDER_LEFT = (5);
	public const UInt32 AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_SHOULDER_RIGHT = (6);
	public const UInt32 AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_ELBOW_LEFT = (7);
	public const UInt32 AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_ELBOW_RIGHT = (8);
	public const UInt32 AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_WRIST_LEFT = (9);
	public const UInt32 AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_WRIST_RIGHT = (10);
	public const UInt32 AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_HIP_LEFT = (11);
	public const UInt32 AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_HIP_RIGHT = (12);
	public const UInt32 AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_KNEE_LEFT = (13);
	public const UInt32 AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_KNEE_RIGHT = (14);
	public const UInt32 AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_ANKLE_LEFT = (15);
	public const UInt32 AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_ANKLE_RIGHT = (16);
	public const UInt32 AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_SHOULDER_CENTER = (17);
	public const UInt32 AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_BODY_CENTER = (18);

	/* 近接上半身姿勢検出 関節点 定義 */
	public const UInt32 AILIA_POSE_ESTIMATOR_UPPOSE_KEYPOINT_NOSE = (0);
	public const UInt32 AILIA_POSE_ESTIMATOR_UPPOSE_KEYPOINT_EYE_LEFT = (1);
	public const UInt32 AILIA_POSE_ESTIMATOR_UPPOSE_KEYPOINT_EYE_RIGHT = (2);
	public const UInt32 AILIA_POSE_ESTIMATOR_UPPOSE_KEYPOINT_EAR_LEFT = (3);
	public const UInt32 AILIA_POSE_ESTIMATOR_UPPOSE_KEYPOINT_EAR_RIGHT = (4);
	public const UInt32 AILIA_POSE_ESTIMATOR_UPPOSE_KEYPOINT_SHOULDER_RIGHT = (6);
	public const UInt32 AILIA_POSE_ESTIMATOR_UPPOSE_KEYPOINT_SHOULDER_LEFT = (5);
	public const UInt32 AILIA_POSE_ESTIMATOR_UPPOSE_KEYPOINT_ELBOW_LEFT = (7);
	public const UInt32 AILIA_POSE_ESTIMATOR_UPPOSE_KEYPOINT_ELBOW_RIGHT = (8);
	public const UInt32 AILIA_POSE_ESTIMATOR_UPPOSE_KEYPOINT_WRIST_LEFT = (9);
	public const UInt32 AILIA_POSE_ESTIMATOR_UPPOSE_KEYPOINT_WRIST_RIGHT = (10);
	public const UInt32 AILIA_POSE_ESTIMATOR_UPPOSE_KEYPOINT_HIP_LEFT = (11);
	public const UInt32 AILIA_POSE_ESTIMATOR_UPPOSE_KEYPOINT_HIP_RIGHT = (12);
	public const UInt32 AILIA_POSE_ESTIMATOR_UPPOSE_KEYPOINT_SHOULDER_CENTER = (13);
	public const UInt32 AILIA_POSE_ESTIMATOR_UPPOSE_KEYPOINT_BODY_CENTER = (14);

	/* 顔特徴点検出 点 定義 */
	public const UInt32 AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_CNT = (19);  // 個数
	public const UInt32 AILIA_POSE_ESTIMATOR_FACE_KEYPOINT_CNT = (68);  // 個数
	public const UInt32 AILIA_POSE_ESTIMATOR_UPPOSE_KEYPOINT_CNT = (15);  // 個数

	[StructLayout(LayoutKind.Sequential)]
	public struct AILIAPoseEstimatorKeypoint
	{
		public float x;                     // 入力画像内 X座標  [0.0 , 1.0)
		public float y;                     // 入力画像内 Y座標  [0.0 , 1.0)
		public float z_local;               // 姿勢検出のみ有効。体中心を座標0とした時に推定されるローカルZ座標。単位(スケール)は x と同じです。
		public float score;                 // この点の検出信頼度 > 0.0F。値0.0Fの場合、この点は未検出のため使用できません。
		public Int32 interpolated;          // 通常は値0です。この点が未検出で、他の点から補間可能な場合、x,yの値を補間し、interpolated=1となります。
	}

	public const UInt32 AILIA_POSE_ESTIMATOR_OBJECT_POSE_VERSION = (1); // 構造体フォーマットバージョン

	[StructLayout(LayoutKind.Sequential)]
	public class AILIAPoseEstimatorObjectPose
	{
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_CNT)]
		public AILIAPoseEstimatorKeypoint[] points; // 検出した関節点。配列インデックスが関節番号に相当します。
		public float total_score;           // このオブジェクトの検出信頼度
		public Int32 num_valid_points;      // points[]の中で正常に検出された関節点の個数
		public Int32 id;                    // 時間方向に、このオブジェクトにユニークなIDです。1以上の正の値です。
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
		public float[] angle;               // このオブジェクトのオイラー角 yaw, pitch, roll [単位radian]。現在yawのみ対応しています。角度が検出されない場合FLT_MAXが格納されます。 
	}

	public const UInt32 AILIA_POSE_ESTIMATOR_OBJECT_FACE_VERSION = (1); // 構造体フォーマットバージョン

	[StructLayout(LayoutKind.Sequential)]
	public class AILIAPoseEstimatorObjectFace
	{
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)AILIA_POSE_ESTIMATOR_FACE_KEYPOINT_CNT)]
		public AILIAPoseEstimatorKeypoint[] points; // 検出した関節点。配列インデックスが関節番号に相当します。
		public float total_score;           // このオブジェクトの検出信頼度
	}

	public const UInt32 AILIA_POSE_ESTIMATOR_OBJECT_UPPOSE_VERSION = (1); // 構造体フォーマットバージョン

	[StructLayout(LayoutKind.Sequential)]
	public class AILIAPoseEstimatorObjectUpPose
	{
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)AILIA_POSE_ESTIMATOR_UPPOSE_KEYPOINT_CNT)]
		public AILIAPoseEstimatorKeypoint[] points; // 検出した関節点。配列インデックスが関節番号に相当します。
		public float total_score;           // このオブジェクトの検出信頼度
		public Int32 num_valid_points;      // points[]の中で正常に検出された関節点の個数
		public Int32 id;                    // 時間方向に、このオブジェクトにユニークなIDです。1以上の正の値です。
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
		public float[] angle;               // このオブジェクトのオイラー角 yaw, pitch, roll [単位radian]。現在yawのみ対応しています。角度が検出されない場合FLT_MAXが格納されます。 
	}

	/****************************************************************
	* 姿勢検出・顔特徴点検出API
	**/

	/**
	*  検出オブジェクトを作成します。
	*    引数:
	*      pose_estimator - 検出オブジェクトポインタ
	*      net            - ネットワークオブジェクトポインタ
	*      algorithm      - AILIA_POSE_ESTIMATOR_ALGORITHM_*
	*    返値:
	*      成功した場合はAILIA_STATUS_SUCCESS、そうでなければエラーコードを返す。
	*    解説:
	*      caffemodelとprototxtを読み込んだAILIANetworkから検出オブジェクトを作成します。
	*/
	[DllImport(LIBRARY_NAME)]
	public static extern int ailiaCreatePoseEstimator(ref IntPtr pose_estimator, IntPtr net, UInt32 algorithm);

	/**
	*  検出オブジェクトを破棄します。
	*    引数:
	*      pose_estimator - 検出オブジェクトポインタ
	*/
	[DllImport(LIBRARY_NAME)]
	public static extern void ailiaDestroyPoseEstimator(IntPtr pose_estimator);

	/**
	*  物体認識を行います。
	*    引数:
	*      pose_estimator                  - 検出オブジェクトポインタ
	*      src                         - 画像データ
	*      src_stride                  - 1ラインのバイト数
	*      src_width                   - 画像幅
	*      src_height                  - 画像高さ
	*      src_format                  - AILIA_IMAGE_FORMAT_*
	*    返値:
	*      成功した場合はAILIA_STATUS_SUCCESS、そうでなければエラーコードを返す。
	*/
	[DllImport(LIBRARY_NAME)]
	public static extern int ailiaPoseEstimatorCompute(IntPtr pose_estimator, IntPtr src, UInt32 src_stride, UInt32 src_width, UInt32 src_height, UInt32 src_format);

	/**
	*  認識結果の数を取得します。
	*    引数:
	*      pose_estimator  - 検出オブジェクトポインタ
	*      obj_count       - オブジェクト数　　Faceの場合は1または0となります。
	*    返値:
	*      成功した場合はAILIA_STATUS_SUCCESS、そうでなければエラーコードを返す。
	*/
	[DllImport(LIBRARY_NAME)]
	public static extern int ailiaPoseEstimatorGetObjectCount(IntPtr pose_estimator, ref UInt32 obj_count);

	/**
	*  Pose 認識結果を取得します。
	*    引数:
	*      pose_estimator  - 検出オブジェクトポインタ
	*      obj             - オブジェクト情報
	*      obj_idx         - オブジェクトインデックス
	*      version         - AILIA_POSE_ESTIMATOR_OBJECT_POSE_VERSION
	*    返値:
	*      成功した場合はAILIA_STATUS_SUCCESS、そうでなければエラーコードを返す。
	*/
	[DllImport(LIBRARY_NAME)]
	public static extern int ailiaPoseEstimatorGetObjectPose(IntPtr pose_estimator, [In, Out] AILIAPoseEstimatorObjectPose obj, UInt32 obj_idx, UInt32 version);

	/**
	*  Face 認識結果を取得します。
	*    引数:
	*      pose_estimator  - 検出オブジェクトポインタ
	*      obj             - オブジェクト情報
	*      obj_idx         - オブジェクトインデックス		必ず 0 を指定してください。
	*      version         - AILIA_POSE_ESTIMATOR_OBJECT_FACE_VERSION
	*    返値:
	*      成功した場合はAILIA_STATUS_SUCCESS、そうでなければエラーコードを返す。
	*/
	[DllImport(LIBRARY_NAME)]
	public static extern int ailiaPoseEstimatorGetObjectFace(IntPtr pose_estimator, [In, Out] AILIAPoseEstimatorObjectFace obj, UInt32 obj_idx, UInt32 version);

	/**
	*  UpPose 認識結果を取得します。
	*    引数:
	*      pose_estimator  - 検出オブジェクトポインタ
	*      obj             - オブジェクト情報
	*      obj_idx         - オブジェクトインデックス
	*      version         - AILIA_POSE_ESTIMATOR_OBJECT_UPPOSE_VERSION
	*    返値:
	*      成功した場合はAILIA_STATUS_SUCCESS、そうでなければエラーコードを返す。
	*/
	[DllImport(LIBRARY_NAME)]
	public static extern int ailiaPoseEstimatorGetObjectUpPose(IntPtr pose_estimator, [In, Out] AILIAPoseEstimatorObjectUpPose obj, UInt32 obj_idx, UInt32 version);
}
