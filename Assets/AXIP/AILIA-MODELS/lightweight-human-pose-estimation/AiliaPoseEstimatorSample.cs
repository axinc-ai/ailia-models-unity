/* AILIA Unity Plugin Simple Sample */
/* Copyright 2018-2019 AXELL CORPORATION */

using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Runtime.InteropServices;

using UnityEngine;
using UnityEngine.UI;

namespace ailiaSDK
{
	public class AiliaPoseEstimatorSample : AiliaRenderer
	{
		//Settings
		public bool gpu_mode = false;
		public int camera_id = 0;

		//Result
		public Text label_text = null;
		public Text mode_text = null;
		public RawImage raw_image = null;

		//Preview
		private Texture2D preview_texture = null;

		private AiliaPoseEstimatorModel ailia_pose = new AiliaPoseEstimatorModel();

		private AiliaCamera ailia_camera = new AiliaCamera();
		private AiliaDownload ailia_download = new AiliaDownload();

		private void CreateAiliaPoseEstimator()
		{
			string asset_path = Application.temporaryCachePath;
			if (gpu_mode)
			{
				ailia_pose.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
			}
			ailia_pose.Settings(AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_ALGORITHM_LW_HUMAN_POSE);

			ailia_download.DownloadModelFromUrl("lightweight-human-pose-estimation", "lightweight-human-pose-estimation.opt.onnx.prototxt");
			ailia_download.DownloadModelFromUrl("lightweight-human-pose-estimation", "lightweight-human-pose-estimation.opt.onnx");

			bool status = ailia_pose.OpenFile(asset_path + "/lightweight-human-pose-estimation.opt.onnx.prototxt", asset_path + "/lightweight-human-pose-estimation.opt.onnx");
			if (!status)
			{
				Debug.Log("Model not found");
			}
		}

		private void DestroyAiliaPoseEstimator()
		{
			ailia_pose.Close();
		}


		// Use this for initialization
		void Start()
		{
			mode_text.text = "ailia PoseEstimator";
			CreateAiliaPoseEstimator();
			ailia_camera.CreateCamera(camera_id);
		}

		// Update is called once per frame
		void Update()
		{
			if (!ailia_camera.IsEnable())
			{
				return;
			}

			//Clear label
			Clear();

			//Capture
			int tex_width = ailia_camera.GetWidth();
			int tex_height = ailia_camera.GetHeight();
			if (preview_texture == null)
			{
				preview_texture = new Texture2D(tex_width, tex_height);
				raw_image.texture = preview_texture;
			}
			Color32[] camera = ailia_camera.GetPixels32();

			//Pose estimation
			long start_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond; ;
			List<AiliaPoseEstimator.AILIAPoseEstimatorObjectPose> pose = ailia_pose.ComputePoseFromImageB2T(camera, tex_width, tex_height);
			long end_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond; ;

			for (int i = 0; i < pose.Count; i++)
			{
				AiliaPoseEstimator.AILIAPoseEstimatorObjectPose obj = pose[i];
				if (obj.total_score < 0)
				{
					continue;
				}

				int r = 2;

				DrawBone(Color.red, tex_width, tex_height, obj, AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_NOSE, AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_SHOULDER_CENTER, r);
				DrawBone(Color.yellow, tex_width, tex_height, obj, AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_SHOULDER_LEFT, AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_SHOULDER_CENTER, r);
				DrawBone(Color.green, tex_width, tex_height, obj, AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_SHOULDER_RIGHT, AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_SHOULDER_CENTER, r);

				DrawBone(Color.red, tex_width, tex_height, obj, AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_EYE_LEFT, AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_NOSE, r);
				DrawBone(Color.red, tex_width, tex_height, obj, AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_EYE_RIGHT, AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_NOSE, r);
				DrawBone(Color.red, tex_width, tex_height, obj, AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_EAR_LEFT, AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_EYE_LEFT, r);
				DrawBone(Color.red, tex_width, tex_height, obj, AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_EAR_RIGHT, AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_EYE_RIGHT, r);

				DrawBone(Color.yellow, tex_width, tex_height, obj, AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_ELBOW_LEFT, AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_SHOULDER_LEFT, r);
				DrawBone(Color.green, tex_width, tex_height, obj, AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_ELBOW_RIGHT, AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_SHOULDER_RIGHT, r);
				DrawBone(Color.yellow, tex_width, tex_height, obj, AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_WRIST_LEFT, AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_ELBOW_LEFT, r);
				DrawBone(Color.green, tex_width, tex_height, obj, AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_WRIST_RIGHT, AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_ELBOW_RIGHT, r);

				DrawBone(Color.blue, tex_width, tex_height, obj, AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_HIP_LEFT, AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_SHOULDER_CENTER, r);
				DrawBone(Color.green, tex_width, tex_height, obj, AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_HIP_RIGHT, AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_SHOULDER_CENTER, r);

				DrawBone(Color.blue, tex_width, tex_height, obj, AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_KNEE_LEFT, AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_HIP_LEFT, r);
				DrawBone(Color.blue, tex_width, tex_height, obj, AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_ANKLE_LEFT, AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_KNEE_LEFT, r);
				DrawBone(Color.green, tex_width, tex_height, obj, AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_KNEE_RIGHT, AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_HIP_RIGHT, r);
				DrawBone(Color.green, tex_width, tex_height, obj, AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_ANKLE_RIGHT, AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_KNEE_RIGHT, r);
			}

			//Apply
			preview_texture.SetPixels32(camera);
			preview_texture.Apply();

			if (label_text != null)
			{
				label_text.text = "" + (end_time - start_time) + "ms\n" + ailia_pose.EnvironmentName();
			}
		}

		void OnApplicationQuit()
		{
			DestroyAiliaPoseEstimator();
			ailia_camera.DestroyCamera();
		}

		void OnDestroy()
		{
			DestroyAiliaPoseEstimator();
			ailia_camera.DestroyCamera();
		}
	}
}