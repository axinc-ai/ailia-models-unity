﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using ailia;

namespace ailiaSDK
{
	public class AiliaPoseEstimatorsSample : AiliaRenderer
	{
		public enum PoseEstimatorModels
		{
			lightweight_human_pose_estimation,
			blazepose_fullbody,
			pose_resnet,
			e2pose
		}

		[SerializeField]
		private PoseEstimatorModels ailiaModelType = PoseEstimatorModels.lightweight_human_pose_estimation;
		[SerializeField, HideInInspector]
		private GameObject UICanvas = null;
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

		private AiliaBlazepose ailia_blazepose;
		private Texture2D textureBlazepose;
		[SerializeField]
		private ComputeShader computeShaderBlazepose;
		private AiliaPoseResnet ailia_pose_resnet;
		private AiliaE2Pose ailia_e2pose;

		// AILIA open file(model file)
		private bool FileOpened = false;

		private void CreateAiliaPoseEstimator()
		{
			string asset_path = Application.temporaryCachePath;
			var urlList = new List<ModelDownloadURL>();

			if (gpu_mode)
			{
				ailia_pose.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
			}
			switch (ailiaModelType)
			{
				case PoseEstimatorModels.lightweight_human_pose_estimation:
					ailia_pose.Settings(AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_ALGORITHM_LW_HUMAN_POSE);

					var model_path = "lightweight-human-pose-estimation";
					var weight_path = "lightweight-human-pose-estimation";
					model_path += ".opt.onnx.prototxt";
					weight_path += ".opt.onnx";
					urlList.Add(new ModelDownloadURL() { folder_path = "lightweight-human-pose-estimation", file_name = model_path });
					urlList.Add(new ModelDownloadURL() { folder_path = "lightweight-human-pose-estimation", file_name = weight_path });
					StartCoroutine(ailia_download.DownloadWithProgressFromURL(urlList, () =>
					{
						FileOpened = ailia_pose.OpenFile(asset_path + "/" + model_path, asset_path + "/" + weight_path);
					}));

					break;
				case PoseEstimatorModels.blazepose_fullbody:
					var folder_path = "blazepose-fullbody";
					var model_name = "pose_landmark_heavy";
					urlList.Add(new ModelDownloadURL() { folder_path = folder_path, file_name = model_name + ".onnx" });
					urlList.Add(new ModelDownloadURL() { folder_path = folder_path, file_name = model_name + ".onnx.prototxt" });
					urlList.Add(new ModelDownloadURL() { folder_path = folder_path, file_name = "pose_detection.onnx" });
					urlList.Add(new ModelDownloadURL() { folder_path = folder_path, file_name = "pose_detection.onnx.prototxt" });

					StartCoroutine(ailia_download.DownloadWithProgressFromURL(urlList, () =>
					{
			      		string jsonPath = Application.streamingAssetsPath + "/AILIA";
						ailia_blazepose = new AiliaBlazepose(gpu_mode, asset_path, jsonPath);
						ailia_blazepose.computeShader = computeShaderBlazepose;
						FileOpened = true;
					}));

					break;
				case PoseEstimatorModels.pose_resnet:
					var pose_folder_path = "pose_resnet";
					var pose_model_name = "pose_resnet_50_256x192";
					var detect_folder_path = "yolov3";
					var detect_model_name = "yolov3.opt2";
					urlList.Add(new ModelDownloadURL() { folder_path = pose_folder_path, file_name = pose_model_name + ".onnx" });
					urlList.Add(new ModelDownloadURL() { folder_path = pose_folder_path, file_name = pose_model_name + ".onnx.prototxt" });
					urlList.Add(new ModelDownloadURL() { folder_path = detect_folder_path, file_name = detect_model_name + ".onnx" });
					urlList.Add(new ModelDownloadURL() { folder_path = detect_folder_path, file_name = detect_model_name + ".onnx.prototxt" });
					StartCoroutine(ailia_download.DownloadWithProgressFromURL(urlList, () =>
					{
						ailia_pose_resnet = new AiliaPoseResnet(gpu_mode, asset_path);
						FileOpened = true;
					}));

					break;
				case PoseEstimatorModels.e2pose:
					var e2pose_folder_path = "e2pose";
					//var e2pose_model_name = "COCO_ResNet50_320x320";
					var e2pose_model_name = "COCO_ResNet101_512x512";
					//var e2pose_model_name = "COCO_ResNet152_448x448";
					//var e2pose_model_name = "COCO_MobileNetV2_320x320";
					//var e2pose_model_name = "COCO_MobileNetV2_448x512";
					var pose_width = 512;
					var pose_height = 512;
					urlList.Add(new ModelDownloadURL() { folder_path = e2pose_folder_path, file_name = e2pose_model_name + ".onnx" });
					urlList.Add(new ModelDownloadURL() { folder_path = e2pose_folder_path, file_name = e2pose_model_name + ".onnx.prototxt" });
					StartCoroutine(ailia_download.DownloadWithProgressFromURL(urlList, () =>
					{
						ailia_e2pose = new AiliaE2Pose(gpu_mode, asset_path + "/" + e2pose_model_name + ".onnx.prototxt", asset_path + "/" + e2pose_model_name + ".onnx", pose_width, pose_height);
						FileOpened = true;
					}));

					break;
				default:
					Debug.Log("Others ailia models are working in progress.");
					break;
			}
		}

		private void DestroyAiliaPoseEstimator()
		{
			ailia_pose.Close();
		}

		// Use this for initialization
		void Start()
		{
			AiliaLicense.CheckAndDownloadLicense();
			mode_text.text = "ailia PoseEstimator";
			SetUIProperties();
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

			if (!FileOpened)
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
			long start_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
			List<AiliaPoseEstimator.AILIAPoseEstimatorObjectPose> pose=null;
			string env_name = "";
			if (ailiaModelType == PoseEstimatorModels.blazepose_fullbody)
			{
				pose = ailia_blazepose.RunPoseEstimation(camera, tex_width, tex_height);
				env_name = ailia_blazepose.EnvironmentName();
			}else if (ailiaModelType == PoseEstimatorModels.pose_resnet){
				pose = ailia_pose_resnet.RunPoseEstimation(camera, tex_width, tex_height);
				env_name = ailia_pose_resnet.EnvironmentName();
			}else if (ailiaModelType == PoseEstimatorModels.e2pose){
				pose = ailia_e2pose.RunPoseEstimation(camera, tex_width, tex_height);
				env_name = ailia_e2pose.EnvironmentName();
			}else{
				pose = ailia_pose.ComputePoseFromImageB2T(camera, tex_width, tex_height);
				env_name = ailia_pose.EnvironmentName();
			}
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
				label_text.text = "" + (end_time - start_time) + "ms\n" + env_name;
			}
		}

		void SetUIProperties()
		{
			if (UICanvas == null) return;
			// Set up UI for AiliaDownloader
			var downloaderProgressPanel = UICanvas.transform.Find("DownloaderProgressPanel");
			ailia_download.DownloaderProgressPanel = downloaderProgressPanel.gameObject;
			// Set up lines
			line_panel = UICanvas.transform.Find("LinePanel").gameObject;
			lines = UICanvas.transform.Find("LinePanel/Lines").gameObject;
			line = UICanvas.transform.Find("LinePanel/Lines/Line").gameObject;
			text_panel = UICanvas.transform.Find("TextPanel").gameObject;
			text_base = UICanvas.transform.Find("TextPanel/TextHolder").gameObject;

			raw_image = UICanvas.transform.Find("RawImage").gameObject.GetComponent<RawImage>();
			label_text = UICanvas.transform.Find("LabelText").gameObject.GetComponent<Text>();
			mode_text = UICanvas.transform.Find("ModeLabel").gameObject.GetComponent<Text>();
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
