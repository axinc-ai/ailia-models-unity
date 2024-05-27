/* AILIA Unity Plugin Face Detector Sample */
/* Copyright 2022 AXELL CORPORATION */

using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Runtime.InteropServices;

using UnityEngine;
using UnityEngine.UI;

using ailia;

namespace ailiaSDK {
	public class AiliaFaceDetectorsSample : AiliaRenderer {
        public enum FaceDetectorModels
        {
            blazeface,
            facemesh,
            facemesh_v2,
			retinaface,
        }

		[SerializeField]
		private FaceDetectorModels ailiaModelType = FaceDetectorModels.blazeface;
		[SerializeField]
		private GameObject UICanvas = null;

		//Settings
		[SerializeField]
		private bool gpu_mode = false;
		[SerializeField]
		private int camera_id = 0;
		[SerializeField]
		private bool debug = false;
		[SerializeField]
		private bool is_square = true;

		//TestImage
		public Texture2D image = null;

		//Result
		RawImage raw_image = null;
		Text label_text = null;
		Text mode_text = null;

		//Preview
		private Texture2D preview_texture = null;

		//AILIA
		private AiliaModel ailia_face_detector = new AiliaModel();
		private AiliaModel ailia_face_recognizer = new AiliaModel();
		private AiliaModel ailia_face_blendshape= new AiliaModel();

		private AiliaBlazeface blaze_face = new AiliaBlazeface();
		private AiliaFaceMesh face_mesh = new AiliaFaceMesh();
		private AiliaFaceMeshV2 face_mesh_v2 = new AiliaFaceMeshV2();
		private AiliaRetinaface retina_face = new AiliaRetinaface();

		private AiliaCamera ailia_camera = new AiliaCamera();
		private AiliaDownload ailia_download = new AiliaDownload();

		// AILIA open file
		private bool FileOpened = false;
		private int page = 0;

		private void CreateAiliaDetector(FaceDetectorModels modelType)
		{
			string asset_path = Application.temporaryCachePath;
			var urlList = new List<ModelDownloadURL>();
			if (gpu_mode)
			{
				ailia_face_detector.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
				ailia_face_recognizer.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
			}
			switch (modelType)
			{		
				case FaceDetectorModels.blazeface:
					mode_text.text = "ailia blazeface";

					urlList.Add(new ModelDownloadURL() { folder_path = "blazeface", file_name = "blazeface.onnx.prototxt" });
					urlList.Add(new ModelDownloadURL() { folder_path = "blazeface", file_name = "blazeface.onnx" });

					StartCoroutine(ailia_download.DownloadWithProgressFromURL(urlList, () =>
					{
						FileOpened = ailia_face_detector.OpenFile(asset_path + "/blazeface.onnx.prototxt", asset_path + "/blazeface.onnx");
					}));

					break;

				case FaceDetectorModels.facemesh:
					mode_text.text = "ailia facemesh";

					urlList.Add(new ModelDownloadURL() { folder_path = "blazeface", file_name = "blazeface.onnx.prototxt" });
					urlList.Add(new ModelDownloadURL() { folder_path = "blazeface", file_name = "blazeface.onnx" });
					urlList.Add(new ModelDownloadURL() { folder_path = "facemesh", file_name = "facemesh.onnx.prototxt" });
					urlList.Add(new ModelDownloadURL() { folder_path = "facemesh", file_name = "facemesh.onnx" });

					StartCoroutine(ailia_download.DownloadWithProgressFromURL(urlList, () =>
					{
						FileOpened = ailia_face_detector.OpenFile(asset_path + "/blazeface.onnx.prototxt", asset_path + "/blazeface.onnx");
						if (FileOpened){
							FileOpened = ailia_face_recognizer.OpenFile(asset_path + "/facemesh.onnx.prototxt", asset_path + "/facemesh.onnx");
						}
					}));

					break;

				case FaceDetectorModels.facemesh_v2:
					mode_text.text = "ailia facemeshv2 (space to next page)";

					urlList.Add(new ModelDownloadURL() { folder_path = "facemesh_v2", file_name = "face_detector.onnx.prototxt" });
					urlList.Add(new ModelDownloadURL() { folder_path = "facemesh_v2", file_name = "face_detector.onnx" });
					urlList.Add(new ModelDownloadURL() { folder_path = "facemesh_v2", file_name = "face_landmarks_detector.onnx.prototxt" });
					urlList.Add(new ModelDownloadURL() { folder_path = "facemesh_v2", file_name = "face_landmarks_detector.onnx" });
					urlList.Add(new ModelDownloadURL() { folder_path = "facemesh_v2", file_name = "face_blendshapes.onnx.prototxt" });
					urlList.Add(new ModelDownloadURL() { folder_path = "facemesh_v2", file_name = "face_blendshapes.onnx" });

					StartCoroutine(ailia_download.DownloadWithProgressFromURL(urlList, () =>
					{
						FileOpened = ailia_face_detector.OpenFile(asset_path + "/face_detector.onnx.prototxt", asset_path + "/face_detector.onnx");
						if (FileOpened){
							FileOpened = ailia_face_recognizer.OpenFile(asset_path + "/face_landmarks_detector.onnx.prototxt", asset_path + "/face_landmarks_detector.onnx");
							if (FileOpened){
								FileOpened = ailia_face_blendshape.OpenFile(asset_path + "/face_blendshapes.onnx.prototxt", asset_path + "/face_blendshapes.onnx");
							}
						}
					}));

					break;

				case FaceDetectorModels.retinaface:
					mode_text.text = "ailia retinaface";

					urlList.Add(new ModelDownloadURL() { folder_path = "retinaface", file_name = "retinaface_resnet50.onnx.prototxt" });
					urlList.Add(new ModelDownloadURL() { folder_path = "retinaface", file_name = "retinaface_resnet50.onnx" });

					StartCoroutine(ailia_download.DownloadWithProgressFromURL(urlList, () =>
					{
						FileOpened = ailia_face_detector.OpenFile(asset_path + "/retinaface_resnet50.onnx.prototxt", asset_path + "/retinaface_resnet50.onnx");
					}));

					break;

				default:
					Debug.Log("Others ailia models are working in progress.");
					break;
			}
		}


		private void DestroyAiliaDetector()
		{
			ailia_face_detector.Close();
			ailia_face_recognizer.Close();
			ailia_face_blendshape.Close();
		}

		// Use this for initialization
		void Start()
		{
			SetUIProperties();
			CreateAiliaDetector(ailiaModelType);
			ailia_camera.CreateCamera(camera_id, is_square);
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

			//Clear result
			Clear();

			//Get camera image
			int tex_width = ailia_camera.GetWidth();
			int tex_height = ailia_camera.GetHeight();
			Color32[] camera = ailia_camera.GetPixels32();

			//Test image
			if (image != null){
				tex_width = image.width;
				tex_height = image.height;
				camera = image.GetPixels32();
			}

			//Output image
			if (preview_texture == null)
			{
				preview_texture = new Texture2D(tex_width, tex_height);
				raw_image.texture = preview_texture;
				if (!is_square){
					Vector2 size = raw_image.rectTransform.sizeDelta;
					float ratio = tex_width / (float)tex_height;
					raw_image.rectTransform.sizeDelta = new Vector2(ratio * size.y, size.y);
					size = line_panel.GetComponent<RectTransform>().sizeDelta;
					line_panel.GetComponent<RectTransform>().sizeDelta = new Vector2(ratio * size.y, size.y);
				}
			}

			long detection_time = 0;
			long recognition_time = 0;
			//Draw result
			if(ailiaModelType==FaceDetectorModels.blazeface){
				long start_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
				List<AiliaBlazeface.FaceInfo> result_detections = blaze_face.Detection(ailia_face_detector, camera, tex_width, tex_height);
				long end_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
				detection_time = (end_time - start_time);

				for (int i = 0; i < result_detections.Count; i++)
				{
					AiliaBlazeface.FaceInfo face = result_detections[i];
					int fw = (int)(face.width * tex_width);
					int fh = (int)(face.height * tex_height);
					int fx = (int)(face.center.x * tex_width) - fw / 2;
					int fy = (int)(face.center.y * tex_height) - fh / 2;
					DrawRect2D(Color.blue, fx, fy, fw, fh, tex_width, tex_height);

					for (int k = 0; k < AiliaBlazeface.NUM_KEYPOINTS; k++)
					{
						int x = (int)(face.keypoints[k].x * tex_width);
						int y = (int)(face.keypoints[k].y * tex_height);
						DrawRect2D(Color.blue, x, y, 1, 1, tex_width, tex_height);
					}
				}
			}
			if(ailiaModelType==FaceDetectorModels.facemesh){
				//Compute
				long start_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
				List<AiliaBlazeface.FaceInfo> result_detections = blaze_face.Detection(ailia_face_detector, camera, tex_width, tex_height);
				long end_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
				detection_time = (end_time - start_time);

				long rec_start_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
				List<AiliaFaceMesh.FaceMeshInfo> result_facemesh = new List<AiliaFaceMesh.FaceMeshInfo>();
				result_facemesh = face_mesh.Detection(ailia_face_recognizer, camera, tex_width, tex_height, result_detections, debug);
				long rec_end_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
				recognition_time = (rec_end_time - rec_start_time);

				//Draw result
				for (int i = 0; i < result_facemesh.Count; i++)
				{
					AiliaFaceMesh.FaceMeshInfo face = result_facemesh[i];
					DrawKeyPoints(face.width, face.height, face.theta, face.center, face.keypoints, AiliaFaceMesh.DETECTION_WIDTH, AiliaFaceMesh.DETECTION_HEIGHT, tex_width, tex_height);
				}
			}
			if(ailiaModelType==FaceDetectorModels.facemesh_v2){
				//Compute
				long start_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
				List<AiliaBlazeface.FaceInfo> result_detections = blaze_face.Detection(ailia_face_detector, camera, tex_width, tex_height);
				long end_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
				detection_time = (end_time - start_time);

				long rec_start_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
				List<AiliaFaceMeshV2.FaceMeshV2Info> result_facemesh_v2 = face_mesh_v2.Detection(ailia_face_recognizer, ailia_face_blendshape, camera, tex_width, tex_height, result_detections, debug);
				long rec_end_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
				recognition_time = (rec_end_time - rec_start_time);

				//Draw result
				for (int i = 0; i < result_facemesh_v2.Count; i++)
				{
					AiliaFaceMeshV2.FaceMeshV2Info face = result_facemesh_v2[i];
					DrawKeyPoints(face.width, face.height, face.theta, face.center, face.keypoints, AiliaFaceMeshV2.DETECTION_WIDTH, AiliaFaceMeshV2.DETECTION_HEIGHT, tex_width, tex_height);
					DrawBlendshape(face.blendshape, tex_width, tex_height);
				}
			}
			if(ailiaModelType==FaceDetectorModels.retinaface){
				//Compute
				
				long start_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
				List<AiliaRetinaface.FaceInfo> result_detections = retina_face.Detection(ailia_face_detector, camera, tex_width, tex_height);
				long end_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
				detection_time = (end_time - start_time);
				
				//Draw result
				for (int i = 0; i < result_detections.Count; i++)
				{
					AiliaRetinaface.FaceInfo face = result_detections[i];

					int fw = (int)(face.width);
					int fh = (int)(face.height);
					int fx = (int)((face.center.x) - face.width / 2);
					int fy = (int)((face.center.y) - face.height / 2);
					DrawRect2D(Color.red, fx, fy, fw, fh, tex_width, tex_height, 0.5f);
					
					string text = face.score.ToString();
					float scale = fw * 0.003f;
					DrawText(Color.white, text, fx, fy, tex_width, tex_height, alpha: 0.0f, scale: scale, text_color: Color.white);

					List<Color> color_list = new List<Color>() {Color.red, Color.yellow, Color.magenta, Color.green, Color.blue};
					for (int k = 0; k < AiliaRetinaface.NUM_KEYPOINTS; k++)
					{
						int x = (int)(face.keypoints[k].x);
						int y = (int)(face.keypoints[k].y);
						DrawRect2D(color_list[k], x, y, 1, 1, tex_width, tex_height, 0.5f);
					}
				}
			}

			if (label_text != null)
			{
				if(ailiaModelType==FaceDetectorModels.facemesh){
					label_text.text = detection_time + "ms + " + recognition_time + "ms\n" + ailia_face_detector.EnvironmentName();
				}else{
					label_text.text = detection_time + "ms\n" + ailia_face_detector.EnvironmentName();
				}
			}

			//Apply
			preview_texture.SetPixels32(camera);
			preview_texture.Apply();
		}

		private void DrawKeyPoints(float face_width, float face_height, float face_theta, Vector2 face_center, Vector2[] face_keypoints, int dw, int dh, int tex_width, int tex_height){
			int fw = (int)(face_width * tex_width);
			int fh = (int)(face_height * tex_height);
			int fx = (int)(face_center.x * tex_width) - fw / 2;
			int fy = (int)(face_center.y * tex_height) - fh / 2;
			DrawAffine2D(Color.green, fx, fy, fw, fh, tex_width, tex_height, face_theta);

			float scale = 1.0f * fw / dw;

			float ss=(float)System.Math.Sin(face_theta);
			float cs=(float)System.Math.Cos(face_theta);

			AiliaFaceMeshDrawUtils draw_utils = new AiliaFaceMeshDrawUtils();
			for (int t = 0; t < 10; t++){
				int [] keypoints = draw_utils.GetKeypoints(t);
				Color32 color = draw_utils.GetColor(t);
				float thickness = draw_utils.GetThickness(t) * 0.2f;
				for (int i = 0; i < keypoints.Length; i+=2){
					int from = keypoints[i];
					int to = keypoints[i+1];
					if (from < face_keypoints.Length && to < face_keypoints.Length){
						int x1 = (int)(face_center.x * tex_width  + ((face_keypoints[from].x - dw/2) * cs + (face_keypoints[from].y - dh/2) * -ss)* scale);
						int y1 = (int)(face_center.y * tex_height + ((face_keypoints[from].x - dw/2) * ss + (face_keypoints[from].y - dh/2) *  cs)* scale);
						int x2 = (int)(face_center.x * tex_width  + ((face_keypoints[to].x - dw/2) * cs + (face_keypoints[to].y - dh/2) * -ss)* scale);
						int y2 = (int)(face_center.y * tex_height + ((face_keypoints[to].x - dw/2) * ss + (face_keypoints[to].y - dh/2) *  cs)* scale);
						DrawLine(color, x1, y1, 0, x2, y2, 0, tex_width, tex_height, thickness);
					}
				}
			}
		}

		private void DrawBlendshape(float [] face_blendshape, int tex_width, int tex_height){
			int y = 0;
			int margin = 4;
			int w = tex_width / 2;
			int h = tex_height / 24;
			int page_n = 20;
			int max_page = (face_blendshape.Length + page_n - 1) / page_n;
			for (int i = 0; i < page_n; i++)
			{
				int k = (page % max_page) * page_n + i;
				if (k >= face_blendshape.Length){
					continue;
				}
				string result = "";
				result = AiliaFaceMeshV2.BlendshapeLabels[k] + " " + (int)(face_blendshape[k] * 100) + "%";

				Color32 color = Color.HSVToRGB(1.0f * k / face_blendshape.Length, 1.0f, 1.0f);
				DrawText(color, result, margin, margin + y, tex_width, tex_height);
				DrawRect2D(color, margin + w, margin + y, (int)(w * face_blendshape[k]), h - margin * 2, tex_width, tex_height);
				y += h;
			}

			if (Input.GetKeyDown(KeyCode.Space)){
				page = page + 1;
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
			DestroyAiliaDetector();
			ailia_camera.DestroyCamera();
		}

		void OnDestroy()
		{
			DestroyAiliaDetector();
			ailia_camera.DestroyCamera();
		}
	}
}

