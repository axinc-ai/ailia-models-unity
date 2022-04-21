using SA;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ailiaSDK
{
	public class AiliaPoseEstimatorsSample : AiliaRenderer
	{
		[SerializeField, HideInInspector]
		private AiliaModelsConst.AiliaModelTypes ailiaModelType = AiliaModelsConst.AiliaModelTypes.openpose;
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

		private AiliaBlazepose ailiaBlazepose;
		private Texture2D textureBlazepose;
		[SerializeField, HideInInspector]
		private ComputeShader computeShaderBlazepose;
		[SerializeField, HideInInspector]
		public Animator ikTarget;
		FullBodyIKBehaviour fbik;
		private (BodyPartIndex, FullBodyIK.Effector)[] bodyPartsMatching;
		RenderTexture avatarViewTexture;


		// normal model or optimized model for lightweight-human-pose-estimation
		[SerializeField]
		private bool optimizedModel = true;
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
				case AiliaModelsConst.AiliaModelTypes.openpose:
					/*
					// Download url is uncertain.
					ailia_pose.Settings(AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_ALGORITHM_OPEN_POSE);

					urlList.Add(new ModelDownloadURL() { folder_path = "openpose", file_name = "pose_deploy.prototxt" });
					urlList.Add(new ModelDownloadURL() { folder_path = "openpose", file_name = "pose_iter_440000.caffemodel" });

					StartCoroutine(ailia_download.DownloadWithProgressFromURL(urlList, () =>
					{
						FileOpened = ailia_pose.OpenFile(asset_path + "/pose_deploy.prototxt", asset_path + "/pose_iter_440000.caffemodel");
					}));
					*/
					break;
				case AiliaModelsConst.AiliaModelTypes.lightweight_human_pose_estimation:
					ailia_pose.Settings(AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_ALGORITHM_LW_HUMAN_POSE);

					var model_path = "lightweight-human-pose-estimation";
					var weight_path = "lightweight-human-pose-estimation";
					if (optimizedModel)
					{
						model_path += ".opt.onnx.prototxt";
						weight_path += ".opt.onnx";
					}
					else
					{
						model_path += ".onnx.prototxt";
						weight_path += ".onnx";
					}
					urlList.Add(new ModelDownloadURL() { folder_path = "lightweight-human-pose-estimation", file_name = model_path });
					urlList.Add(new ModelDownloadURL() { folder_path = "lightweight-human-pose-estimation", file_name = weight_path });
					StartCoroutine(ailia_download.DownloadWithProgressFromURL(urlList, () =>
					{
						FileOpened = ailia_pose.OpenFile(asset_path + "/" + model_path, asset_path + "/" + weight_path);
					}));

					break;
				case AiliaModelsConst.AiliaModelTypes.lightweight_human_pose_estimation_3d:

					break;
				case AiliaModelsConst.AiliaModelTypes.blazepose_fullbody:
					var folder_path = "blazepose-fullbody";
					var model_name = "pose_landmark_heavy";
					urlList.Add(new ModelDownloadURL() { folder_path = folder_path, file_name = model_name + ".onnx" });
					urlList.Add(new ModelDownloadURL() { folder_path = folder_path, file_name = model_name + ".onnx.prototxt" });
					urlList.Add(new ModelDownloadURL() { folder_path = folder_path, file_name = "pose_detection.onnx" });
					urlList.Add(new ModelDownloadURL() { folder_path = folder_path, file_name = "pose_detection.onnx.prototxt" });

					string assetPath = Application.streamingAssetsPath + "/AILIA";
					ailia_download.SetSaveFolderPath(assetPath);
					StartCoroutine(ailia_download.DownloadWithProgressFromURL(urlList, () =>
					{
						fbik = ikTarget.GetComponent<FullBodyIKBehaviour>();

						ailiaBlazepose = new AiliaBlazepose(gpu_mode, fbik);
						ailiaBlazepose.computeShader = computeShaderBlazepose;

						ailiaBlazepose.Smooth = true;

						bodyPartsMatching = new (BodyPartIndex, FullBodyIK.Effector)[]
						{
							(BodyPartIndex.LeftShoulder, fbik.fullBodyIK.leftArmEffectors.arm),
							(BodyPartIndex.LeftElbow, fbik.fullBodyIK.leftArmEffectors.elbow),
							(BodyPartIndex.LeftWrist, fbik.fullBodyIK.leftArmEffectors.wrist),

							(BodyPartIndex.RightShoulder, fbik.fullBodyIK.rightArmEffectors.arm),
							(BodyPartIndex.RightElbow, fbik.fullBodyIK.rightArmEffectors.elbow),
							(BodyPartIndex.RightWrist, fbik.fullBodyIK.rightArmEffectors.wrist),

							(BodyPartIndex.LeftKnee, fbik.fullBodyIK.leftLegEffectors.knee),
							(BodyPartIndex.LeftAnkle, fbik.fullBodyIK.leftLegEffectors.foot),

							(BodyPartIndex.RightKnee, fbik.fullBodyIK.rightLegEffectors.knee),
							(BodyPartIndex.RightAnkle, fbik.fullBodyIK.rightLegEffectors.foot),
						};

						FileOpened = true;
					}));
					//avatarViewTexture = new RenderTexture(1024, 1024, 32);
					//raw_image.texture = avatarViewTexture;
					//GameObject.Find("avatarCamera").GetComponent<Camera>().targetTexture = avatarViewTexture;

					break;
				default:
					Debug.Log("Others ailia models are working in progress.");
					break;
			}
		}

		private void DestroyAiliaPoseEstimator()
		{
			avatarViewTexture?.Release();
			ailia_pose.Close();
		}

		// Use this for initialization
		void Start()
		{
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
			if (ailiaModelType == AiliaModelsConst.AiliaModelTypes.blazepose_fullbody)
			{
				if(ailiaBlazepose != null)
				{
					textureBlazepose = ailia_camera.GetTexture2D(textureBlazepose);
					ailiaBlazepose.RunPoseEstimation(textureBlazepose);

					List<AiliaPoseEstimator.AILIAPoseEstimatorObjectPose> result_list=new List<AiliaPoseEstimator.AILIAPoseEstimatorObjectPose>();
					int [] keypoint_list={
					    (int)BodyPartIndex.Nose,
						(int)BodyPartIndex.LeftEye,
						(int)BodyPartIndex.RightEye,
						(int)BodyPartIndex.LeftEar,
						(int)BodyPartIndex.RightEar,
						(int)BodyPartIndex.LeftShoulder,
						(int)BodyPartIndex.RightShoulder,
						(int)BodyPartIndex.LeftElbow,
						(int)BodyPartIndex.RightElbow,
						(int)BodyPartIndex.LeftWrist,
						(int)BodyPartIndex.RightWrist,
						(int)BodyPartIndex.LeftHip,
						(int)BodyPartIndex.RightHip,
						(int)BodyPartIndex.LeftKnee,
						(int)BodyPartIndex.RightKnee,
						(int)BodyPartIndex.LeftAnkle,
						(int)BodyPartIndex.RightAnkle};

					AiliaPoseEstimator.AILIAPoseEstimatorObjectPose one_pose=new AiliaPoseEstimator.AILIAPoseEstimatorObjectPose();
					one_pose.points = new AiliaPoseEstimator.AILIAPoseEstimatorKeypoint[19];
					for(int i=0;i<19;i++){
						Vector3 pos = Vector3.zero;
						float conf = 0;
						if(i<=16){
							pos = ailiaBlazepose.landmarks[keypoint_list[i]].position;
							conf = ailiaBlazepose.landmarks[keypoint_list[i]].confidence;
						}
						if(i==17){
							pos = (ailiaBlazepose.landmarks[(int)BodyPartIndex.LeftShoulder].position + ailiaBlazepose.landmarks[(int)BodyPartIndex.RightShoulder].position)/2;
							conf = Math.Min(ailiaBlazepose.landmarks[(int)BodyPartIndex.LeftShoulder].confidence,ailiaBlazepose.landmarks[(int)BodyPartIndex.RightShoulder].confidence);
						}
						if(i==18){
							pos = (ailiaBlazepose.landmarks[(int)BodyPartIndex.LeftHip].position + ailiaBlazepose.landmarks[(int)BodyPartIndex.RightHip].position + ailiaBlazepose.landmarks[(int)BodyPartIndex.LeftShoulder].position + ailiaBlazepose.landmarks[(int)BodyPartIndex.RightShoulder].position)/4;
						}
						AiliaPoseEstimator.AILIAPoseEstimatorKeypoint keypoint =new AiliaPoseEstimator.AILIAPoseEstimatorKeypoint();
						Debug.Log(pos);
						keypoint.x = pos.x;
						keypoint.y = pos.y;
						keypoint.z_local = pos.z;
						keypoint.score = conf;
						one_pose.points[i] = keypoint;
					}
					result_list.Add(one_pose);
					pose = result_list;
				}
			}else{
				pose = ailia_pose.ComputePoseFromImageB2T(camera, tex_width, tex_height);
			}
			long end_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond; ;

			for (int i = 0; i < pose.Count; i++)
			{
				AiliaPoseEstimator.AILIAPoseEstimatorObjectPose obj = pose[i];
				Debug.Log(obj.total_score);
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

		/*
		public void AnimateWithPosition(int layerIndex)
		{
			if (ailiaModelType == AiliaModelsConst.AiliaModelTypes.blazepose_fullbody && ikTarget != null && ailiaBlazepose != null)
			{
				float weight = 1;

				Vector3 hipsPos = (
					(ailiaBlazepose.GetLandmarkPosition(BodyPartIndex.LeftHip) ?? Vector3.zero) +
					(ailiaBlazepose.GetLandmarkPosition(BodyPartIndex.RightHip) ?? Vector3.zero)
				) / 2;

				SetEffectorPosition(fbik.fullBodyIK.bodyEffectors.hips, hipsPos, weight);

				foreach (var match in bodyPartsMatching)
				{
					Vector3 position = ailiaBlazepose.GetLandmarkPosition(match.Item1) ?? match.Item2.worldPosition;
					SetEffectorPosition(match.Item2, position, weight);
				}

				Vector3 middleMouth = (
					(ailiaBlazepose.GetLandmarkPosition(BodyPartIndex.LeftMouth) ?? Vector3.zero) +
					(ailiaBlazepose.GetLandmarkPosition(BodyPartIndex.RightMouth) ?? Vector3.zero)
				) / 2;

				Vector3 lookLeft = (
					(ailiaBlazepose.GetLandmarkPosition(BodyPartIndex.LeftEye) ?? Vector3.zero) -
					(ailiaBlazepose.GetLandmarkPosition(BodyPartIndex.LeftEar) ?? Vector3.zero)

				) / 2;
				Vector3 lookRight = (
					(ailiaBlazepose.GetLandmarkPosition(BodyPartIndex.RightEye) ?? Vector3.zero) -
					(ailiaBlazepose.GetLandmarkPosition(BodyPartIndex.RightEar) ?? Vector3.zero)
				) / 2;
				Vector3 faceForward = (lookLeft + lookRight) / 2;

				ikTarget.SetLookAtWeight(1);
				ikTarget.SetLookAtPosition(ikTarget.GetBoneTransform(HumanBodyBones.Head).position + faceForward);

			}
		}
		private void SetEffectorPosition(FullBodyIK.Effector effector, Vector3 position, float weight)
		{
			effector.pull = 1;
			effector.positionEnabled = true;
			effector.positionWeight = weight;
			effector.transform.position = position;
		}
		*/

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
