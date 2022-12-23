/* AILIA Unity Plugin FeatureExtractor Sample */
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
	public class AiliaFeatureExtractorSample : AiliaRenderer
	{
		public enum FeatureExtractorModels
		{
			vggface2,
			arcface,	// official model
			arcfacem	// ax ratrained model
		}

		[SerializeField]
		private FeatureExtractorModels ailiaModelType = FeatureExtractorModels.arcface;

		//Settings
		public bool gpu_mode = false;
		public int camera_id = 0;
		public Texture2D test_image = null;

		//Result
		public Text label_text = null;
		public Text mode_text = null;
		public RawImage raw_image = null;

		//Preview
		private Texture2D preview_texture = null;

		//AILIA
		private AiliaDetectorModel ailia_face = new AiliaDetectorModel();
		private AiliaFeatureExtractorModel ailia_feature_extractor = new AiliaFeatureExtractorModel();	// for vggface2
		private AiliaModel ailia_arcface_model = new AiliaModel();	// for arcface

		private AiliaCamera ailia_camera = new AiliaCamera();
		private AiliaDownload ailia_download = new AiliaDownload();

		//BeforeFeatureValue
		private float[] before_feature_value = null;	// tempolary
		private float[] capture_feature_value = null;	// matching target

		//threshold for same person detection
		private float threshold_vggface2 = 1.24f;  	 	 //VGGFace2 predefined value
		private float threshold_arcface = 0.25572845f;	// arcface predefined value
		private float threshold_arcfacem = 0.45f;

		//settings for arcface
		private const int ARCFACE_BATCH = 2;
		private const int ARCFACE_WIDTH = 128;
		private const int ARCFACE_HEIGHT = 128;
		private const int ARCFACE_FEATURE_LEN = 2 * 512;

		private float threshold = 0.0f;

		private void CreateAiliaDetector()
		{
			string asset_path = Application.temporaryCachePath;

			//Face detection
			if (gpu_mode)
			{
				ailia_face.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
			}
			if (ailiaModelType == FeatureExtractorModels.arcfacem){
				uint category_n = 1;
				ailia_face.Settings(AiliaFormat.AILIA_NETWORK_IMAGE_FORMAT_RGB, AiliaFormat.AILIA_NETWORK_IMAGE_CHANNEL_FIRST, AiliaFormat.AILIA_NETWORK_IMAGE_RANGE_SIGNED_FP32, AiliaDetector.AILIA_DETECTOR_ALGORITHM_YOLOV3, category_n, AiliaDetector.AILIA_DETECTOR_FLAG_NORMAL);

				ailia_download.DownloadModelFromUrl("face-mask-detection", "face-mask-detection-yolov3-tiny.opt.onnx.prototxt");
				ailia_download.DownloadModelFromUrl("face-mask-detection", "face-mask-detection-yolov3-tiny.opt.obf.onnx");

				ailia_face.OpenFile(asset_path + "/face-mask-detection-yolov3-tiny.opt.onnx.prototxt", asset_path + "/face-mask-detection-yolov3-tiny.opt.obf.onnx");
			}else{
				uint category_n = 2;
				ailia_face.Settings(AiliaFormat.AILIA_NETWORK_IMAGE_FORMAT_RGB, AiliaFormat.AILIA_NETWORK_IMAGE_CHANNEL_FIRST, AiliaFormat.AILIA_NETWORK_IMAGE_RANGE_SIGNED_FP32, AiliaDetector.AILIA_DETECTOR_ALGORITHM_YOLOV3, category_n, AiliaDetector.AILIA_DETECTOR_FLAG_NORMAL);

				ailia_download.DownloadModelFromUrl("yolov3-face", "yolov3-face.opt.onnx.prototxt");
				ailia_download.DownloadModelFromUrl("yolov3-face", "yolov3-face.opt.onnx");

				ailia_face.OpenFile(asset_path + "/yolov3-face.opt.onnx.prototxt", asset_path + "/yolov3-face.opt.onnx");
			}

			//Feature extractor
			switch(ailiaModelType){
			case FeatureExtractorModels.arcface:
			case FeatureExtractorModels.arcfacem:
				if (gpu_mode)
				{
					ailia_arcface_model.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
				}

				if (ailiaModelType == FeatureExtractorModels.arcface){
					threshold = threshold_arcface;
					ailia_download.DownloadModelFromUrl("arcface", "arcface.onnx.prototxt");
					ailia_download.DownloadModelFromUrl("arcface", "arcface.onnx");
					ailia_arcface_model.OpenFile(asset_path + "/arcface.onnx.prototxt", asset_path + "/arcface.onnx");
				}

				if (ailiaModelType == FeatureExtractorModels.arcfacem){
					threshold = threshold_arcfacem;
					ailia_download.DownloadModelFromUrl("arcface", "arcface_mixed_90_82.onnx.prototxt");
					ailia_download.DownloadModelFromUrl("arcface", "arcface_mixed_90_82.obf.onnx");
					ailia_arcface_model.OpenFile(asset_path + "/arcface_mixed_90_82.onnx.prototxt", asset_path + "/arcface_mixed_90_82.obf.onnx");
				}

				Ailia.AILIAShape shape = new Ailia.AILIAShape();
				shape.x = ARCFACE_WIDTH;
				shape.y = ARCFACE_HEIGHT;
				shape.z = 1;
				shape.w = ARCFACE_BATCH;
				shape.dim = 4;
				ailia_arcface_model.SetInputShape(shape);
				break;
			case FeatureExtractorModels.vggface2:
				threshold = threshold_vggface2;

				if (gpu_mode)
				{
					ailia_feature_extractor.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
				}

				ailia_download.DownloadModelFromUrl("vggface2", "resnet50_scratch.prototxt");
				ailia_download.DownloadModelFromUrl("vggface2", "resnet50_scratch.caffemodel");

				string layer_name = "conv5_3";
				ailia_feature_extractor.Settings(AiliaFormat.AILIA_NETWORK_IMAGE_FORMAT_BGR, AiliaFormat.AILIA_NETWORK_IMAGE_CHANNEL_FIRST, AiliaFormat.AILIA_NETWORK_IMAGE_RANGE_SIGNED_INT8, AiliaFeatureExtractor.AILIA_FEATURE_EXTRACTOR_DISTANCE_L2NORM, layer_name);
				ailia_feature_extractor.OpenFile(asset_path + "/resnet50_scratch.prototxt", asset_path + "/resnet50_scratch.caffemodel");
				break;
			}
		}

		private void DestroyAiliaDetector()
		{
			ailia_face.Close();
			ailia_feature_extractor.Close();
			ailia_arcface_model.Close();
		}

		// Use this for initialization
		void Start()
		{
			mode_text.text = "ailia FeatureExtractor";
			CreateAiliaDetector();
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

			//Get camera image
			int tex_width = ailia_camera.GetWidth();
			int tex_height = ailia_camera.GetHeight();
			Color32[] camera = ailia_camera.GetPixels32();
			
			//Test image input
			if (test_image != null){
				camera = test_image.GetPixels32();
				tex_width = test_image.width;
				tex_height = test_image.height;
			}

			//Create preview texture
			if (preview_texture == null || preview_texture.width != tex_width || preview_texture.height != tex_height)
			{
				preview_texture = new Texture2D(tex_width, tex_height);
				raw_image.texture = preview_texture;
			}

			//Face Detection
			float det_threshold = 0.2f;
			float det_iou = 0.25f;
			List<AiliaDetector.AILIADetectorObject> list = ailia_face.ComputeFromImageB2T(camera, tex_width, tex_height, det_threshold, det_iou);

			//Face Matching
			for (int i = 0; i < list.Count; i++){
				AiliaDetector.AILIADetectorObject obj = list[i];
				bool last_face = (i == list.Count - 1);
				FaceMatching(obj, camera, tex_width, tex_height, last_face);
			}

			//Apply
			preview_texture.SetPixels32(camera);
			preview_texture.Apply();
		}

		private float[] PreprocessArcFace(Color32[] face, int w, int h){
			// GrayScale, image / 127.5 - 1.0, flip batch
			int arcface_c = ARCFACE_BATCH;
			int arcface_w = ARCFACE_WIDTH;
			int arcface_h = ARCFACE_HEIGHT;
			float [] arcface_input = new float[arcface_w * arcface_h * arcface_c];
			for (int y = 0; y < arcface_h; y++){
				for (int x = 0; x < arcface_w; x++){
					int x2 = x * w / arcface_w;
					int y2 = y * h / arcface_h;
					Color32 v = face[y2 * w + x2];
					float v2 = (v.r + v.g + v.b) / 3.0f / 127.5f - 1.0f;
					arcface_input[y*arcface_w+x] = v2;	// normal
					arcface_input[y*arcface_w+(arcface_w-1-x)+arcface_w*arcface_h] = v2; // flip
				}
			}
			return arcface_input;
		}

		private float CosinMetric(float [] features, float [] capture_feature_value)
		{
			// cos similaity
			// np.dot(x1, x2) / (np.linalg.norm(x1) * np.linalg.norm(x2))
			float dot_sum = 0.0f;
			float norm1 = 0.0f, norm2 = 0.0f;
			for (int i = 0; i < features.Length; i++){
				dot_sum += features[i] * capture_feature_value[i];
				norm1 += features[i] * features[i];
				norm2 += capture_feature_value[i] * capture_feature_value[i];
			}
			return dot_sum / (Mathf.Sqrt(norm1) * Mathf.Sqrt(norm2));
		}

		private void GetFacePosition(AiliaDetector.AILIADetectorObject box, int tex_width, int tex_height, ref int x1, ref int y1, ref int w, ref int h){
			//Convert to pixel position
			x1 = (int)(box.x * tex_width);
			y1 = (int)(box.y * tex_height);
			int x2 = (int)((box.x + box.w) * tex_width);
			int y2 = (int)((box.y + box.h) * tex_height);

			//Get face size
			w = (x2 - x1);
			h = (y2 - y1);

			float expand = 1.4f;
			bool square = false;
			if (ailiaModelType == FeatureExtractorModels.arcface){
				expand = 1.2f;
				square = true;
			}
			if (ailiaModelType == FeatureExtractorModels.arcfacem){
				expand = 1.4f;
				square = true;
			}

			int nw = (int)(w * expand);
			int nh = (int)(h * expand);
			if (square){
				if (nw < nh){
					nw = nh;
				}else{
					nh = nw;
				}
			}

			x1 -= (int)(nw - w) / 2;
			y1 -= (int)(nh - h) / 2;
			w = nw;
			h = nh;
		}

		private void GetFaceImage(Color32[] face, int x1, int y1, int w, int h, Color32[] camera, int tex_width, int tex_height)
		{
			for (int y = 0; y < h; y++)
			{
				for (int x = 0; x < w; x++)
				{
					if (x + x1 >= 0 && x + x1 < tex_width && y + y1 >= 0 && y + y1 < tex_height)
					{
						face[y * w + x] = camera[(tex_height - 1 - y - y1) * tex_width + x + x1];
					}
				}
			}
		}

		private void FaceMatching(AiliaDetector.AILIADetectorObject box, Color32[] camera, int tex_width, int tex_height, bool last_face)
		{
			//Get face image
			int x1 = 0, y1 = 0, w = 0, h = 0;
			GetFacePosition(box, tex_width, tex_height, ref x1, ref y1, ref w, ref h);
			if (w <= 0 || h <= 0)
			{
				return;
			}
			Color32[] face = new Color32[w * h];
			GetFaceImage(face, x1, y1, w, h, camera, tex_width, tex_height);

			//Feature extractor
			long start_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
			float distance = 0.0f;
			float similality = 0.0f;
			float[] features = null;
			switch(ailiaModelType){
			case FeatureExtractorModels.arcface:
			case FeatureExtractorModels.arcfacem:
				float[] face_input = PreprocessArcFace(face, w, h);
				features = new float [ARCFACE_FEATURE_LEN];
				ailia_arcface_model.Predict(features, face_input);
				if (capture_feature_value != null){
					similality = CosinMetric(features, capture_feature_value); //Calc similaity between two feature vectors
				}
				break;
			case FeatureExtractorModels.vggface2:
				features = ailia_feature_extractor.ComputeFromImage(face, w, h);
				if (capture_feature_value != null)
				{
					distance = ailia_feature_extractor.Match(capture_feature_value, features);
				}
				break;
			}
			long end_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

			//Save feature
			before_feature_value = features;

			//Display result
			DisplayResult(distance, similality, (end_time - start_time), x1, y1, w, h, tex_width, tex_height, last_face);
		}

		private void DisplayResult(float distance, float similality, float elspsed_time, int x1, int y1, int w, int h, int tex_width, int tex_height, bool last_face){
			string feature_text = "";
			Color color = Color.white;
			if (last_face && capture_feature_value == null){
				color = Color.green;
			}

			if (capture_feature_value != null)
			{
				bool same_person = false;
				if (ailiaModelType == FeatureExtractorModels.vggface2)
				{
					feature_text = "Distance " + distance + "\n";
					same_person = distance < threshold;
				}
				if (ailiaModelType == FeatureExtractorModels.arcface)
				{
					feature_text = "Similality " + similality + "\n";
					same_person = similality > threshold;
				}
				if (same_person)
				{
					feature_text += "Same person";
					color = Color.green;
				}
				else
				{
					feature_text += "Not same person";
					color = Color.red;
				}
			}
			else
			{
				feature_text = "Please capture face";
			}

			DrawRect2D(color, x1, y1, w, h, tex_width, tex_height);

			int margin = 4;
			DrawText(color, feature_text, x1 + margin, y1 + margin, tex_width, tex_height);

			if (label_text != null)
			{
				label_text.text = elspsed_time + "ms\n" + ailia_face.EnvironmentName();
			}
		}

		public void Capture()
		{
			//Remember feature vector of target face
			capture_feature_value = before_feature_value;
			if (label_text != null)
			{
				if (capture_feature_value == null)
				{
					label_text.text = "Face not found!";
				}
				else
				{
					label_text.text = "Capture success!";
				}
			}
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