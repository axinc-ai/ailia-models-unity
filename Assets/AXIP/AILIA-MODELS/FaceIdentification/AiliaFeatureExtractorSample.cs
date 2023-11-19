/* AILIA Unity Plugin FeatureExtractor Sample */
/* Copyright 2018-2022 AXELL CORPORATION */

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
			arcfacem,	// ax ratrained model
			person_reid_baseline
		}

		[SerializeField]
		private FeatureExtractorModels ailiaModelType = FeatureExtractorModels.arcface;
		[SerializeField]
		private GameObject UICanvas = null;

		//Settings
		public bool gpu_mode = false;
		public int camera_id = 0;
		public Texture2D test_image = null;
		public bool debug_feature_input = false;

		//Result
		public Text label_text = null;
		public Text mode_text = null;
		public RawImage raw_image = null;

		//Preview
		private Texture2D preview_texture = null;	// test using image

		//AILIA
		private AiliaDetectorModel ailia_detector = new AiliaDetectorModel();
		private AiliaFeatureExtractorModel ailia_feature_extractor = new AiliaFeatureExtractorModel();	// for vggface2
		private AiliaModel ailia_feature_model = new AiliaModel();	// for arcface and person_reid_baseline

		private AiliaCamera ailia_camera = new AiliaCamera();
		private AiliaDownload ailia_download = new AiliaDownload();

		//BeforeFeatureValue
		private float[] before_feature_value = null;	// tempolary
		private float[] capture_feature_value = null;	// matching target

		//threshold for same person detection
		private float threshold_vggface2 = 1.24f;  	 	 //VGGFace2 predefined value
		private float threshold_arcface = 0.25572845f;	// arcface predefined value
		private float threshold_arcfacem = 0.45f;
		private float threshold_person_reid_baseline = 0.45f;

		//settings for arcface and person reid
		private const int ARCFACE_BATCH = 2;
		private const int ARCFACE_WIDTH = 128;
		private const int ARCFACE_HEIGHT = 128;
		private const int ARCFACE_FEATURE_LEN = 2 * 512;

		private const int PERSON_REID_BASELINE_CHANNELS = 3;
		private const int PERSON_REID_BASELINE_WIDTH = 128;
		private const int PERSON_REID_BASELINE_HEIGHT = 256;
		private const int PERSON_REID_BASELINE_FEATURE_LEN = 512;

		private float threshold = 0.0f;

		private bool FileOpened1 = false;
		private bool FileOpened2 = false;

		private void SetInputShape(AiliaModel model, uint x, uint y, uint z, uint w){
			Ailia.AILIAShape shape = new Ailia.AILIAShape();
			shape.x = x;
			shape.y = y;
			shape.z = z;
			shape.w = w;
			shape.dim = 4;
			model.SetInputShape(shape);
		}

		private void CreateAiliaDetector()
		{
			string asset_path = Application.temporaryCachePath;
			uint category_n = 0;
			var urlList1 = new List<ModelDownloadURL>();
			var urlList2 = new List<ModelDownloadURL>();

			//Face or Person detection
			if (gpu_mode)
			{
				ailia_detector.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
			}
			switch (ailiaModelType){
			case FeatureExtractorModels.arcfacem:
				category_n = 1;
				ailia_detector.Settings(AiliaFormat.AILIA_NETWORK_IMAGE_FORMAT_RGB, AiliaFormat.AILIA_NETWORK_IMAGE_CHANNEL_FIRST, AiliaFormat.AILIA_NETWORK_IMAGE_RANGE_SIGNED_FP32, AiliaDetector.AILIA_DETECTOR_ALGORITHM_YOLOV3, category_n, AiliaDetector.AILIA_DETECTOR_FLAG_NORMAL);

				urlList1.Add(new ModelDownloadURL() { folder_path = "face-mask-detection", file_name = "face-mask-detection-yolov3-tiny.opt.onnx.prototxt" });
				urlList1.Add(new ModelDownloadURL() { folder_path = "face-mask-detection", file_name = "face-mask-detection-yolov3-tiny.opt.obf.onnx" });

				StartCoroutine(ailia_download.DownloadWithProgressFromURL(urlList1, () =>
				{
					FileOpened1 = ailia_detector.OpenFile(asset_path + "/face-mask-detection-yolov3-tiny.opt.onnx.prototxt", asset_path + "/face-mask-detection-yolov3-tiny.opt.obf.onnx");
				}));
				break;
			case FeatureExtractorModels.arcface:
			case FeatureExtractorModels.vggface2:
				category_n = 2;
				ailia_detector.Settings(AiliaFormat.AILIA_NETWORK_IMAGE_FORMAT_RGB, AiliaFormat.AILIA_NETWORK_IMAGE_CHANNEL_FIRST, AiliaFormat.AILIA_NETWORK_IMAGE_RANGE_SIGNED_FP32, AiliaDetector.AILIA_DETECTOR_ALGORITHM_YOLOV3, category_n, AiliaDetector.AILIA_DETECTOR_FLAG_NORMAL);

				urlList1.Add(new ModelDownloadURL() { folder_path = "yolov3-face", file_name = "yolov3-face.opt.onnx.prototxt" });
				urlList1.Add(new ModelDownloadURL() { folder_path = "yolov3-face", file_name = "yolov3-face.opt.onnx" });

				StartCoroutine(ailia_download.DownloadWithProgressFromURL(urlList1, () =>
				{
					FileOpened1 = ailia_detector.OpenFile(asset_path + "/yolov3-face.opt.onnx.prototxt", asset_path + "/yolov3-face.opt.onnx");
				}));
				break;
			case FeatureExtractorModels.person_reid_baseline:
				category_n = 80;
				ailia_detector.Settings(AiliaFormat.AILIA_NETWORK_IMAGE_FORMAT_BGR, AiliaFormat.AILIA_NETWORK_IMAGE_CHANNEL_FIRST, AiliaFormat.AILIA_NETWORK_IMAGE_RANGE_UNSIGNED_INT8, AiliaDetector.AILIA_DETECTOR_ALGORITHM_YOLOX, category_n, AiliaDetector.AILIA_DETECTOR_FLAG_NORMAL);

				urlList1.Add(new ModelDownloadURL() { folder_path = "yolox", file_name = "yolox_tiny.opt.onnx.prototxt" });
				urlList1.Add(new ModelDownloadURL() { folder_path = "yolox", file_name = "yolox_tiny.opt.onnx" });

				StartCoroutine(ailia_download.DownloadWithProgressFromURL(urlList1, () =>
				{
					FileOpened1 = ailia_detector.OpenFile(asset_path + "/yolox_tiny.opt.onnx.prototxt", asset_path + "/yolox_tiny.opt.onnx");
				}));
				break;
			}

			//Feature extractor
			switch(ailiaModelType){
			case FeatureExtractorModels.arcface:
			case FeatureExtractorModels.arcfacem:
				if (gpu_mode)
				{
					ailia_feature_model.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
				}

				if (ailiaModelType == FeatureExtractorModels.arcface){
					threshold = threshold_arcface;

					urlList2.Add(new ModelDownloadURL() { folder_path = "arcface", file_name = "arcface.onnx.prototxt" });
					urlList2.Add(new ModelDownloadURL() { folder_path = "arcface", file_name = "arcface.onnx" });

					StartCoroutine(ailia_download.DownloadWithProgressFromURL(urlList2, () =>
					{
						FileOpened2 = ailia_feature_model.OpenFile(asset_path + "/arcface.onnx.prototxt", asset_path + "/arcface.onnx");
						SetInputShape(ailia_feature_model, ARCFACE_WIDTH, ARCFACE_HEIGHT, 1, ARCFACE_BATCH);
					}));
				}

				if (ailiaModelType == FeatureExtractorModels.arcfacem){
					threshold = threshold_arcfacem;

					urlList2.Add(new ModelDownloadURL() { folder_path = "arcface", file_name = "arcface_mixed_90_82.onnx.prototxt" });
					urlList2.Add(new ModelDownloadURL() { folder_path = "arcface", file_name = "arcface_mixed_90_82.obf.onnx" });

					StartCoroutine(ailia_download.DownloadWithProgressFromURL(urlList2, () =>
					{
						FileOpened2 = ailia_feature_model.OpenFile(asset_path + "/arcface_mixed_90_82.onnx.prototxt", asset_path + "/arcface_mixed_90_82.obf.onnx");
						SetInputShape(ailia_feature_model, ARCFACE_WIDTH, ARCFACE_HEIGHT, 1, ARCFACE_BATCH);
					}));
				}
				break;
			case FeatureExtractorModels.vggface2:
				threshold = threshold_vggface2;

				if (gpu_mode)
				{
					ailia_feature_extractor.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
				}

				urlList2.Add(new ModelDownloadURL() { folder_path = "vggface2", file_name = "resnet50_scratch.prototxt" });
				urlList2.Add(new ModelDownloadURL() { folder_path = "vggface2", file_name = "resnet50_scratch.caffemodel" });

				StartCoroutine(ailia_download.DownloadWithProgressFromURL(urlList2, () =>
				{
					string layer_name = "conv5_3";
					ailia_feature_extractor.Settings(AiliaFormat.AILIA_NETWORK_IMAGE_FORMAT_BGR, AiliaFormat.AILIA_NETWORK_IMAGE_CHANNEL_FIRST, AiliaFormat.AILIA_NETWORK_IMAGE_RANGE_SIGNED_INT8, AiliaFeatureExtractor.AILIA_FEATURE_EXTRACTOR_DISTANCE_L2NORM, layer_name);
					FileOpened2 = ailia_feature_extractor.OpenFile(asset_path + "/resnet50_scratch.prototxt", asset_path + "/resnet50_scratch.caffemodel");
				}));
				break;
			case FeatureExtractorModels.person_reid_baseline:
				if (gpu_mode)
				{
					ailia_feature_model.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
				}

				threshold = threshold_person_reid_baseline;

				urlList2.Add(new ModelDownloadURL() { folder_path = "person_reid_baseline_pytorch", file_name = "ft_ResNet50.onnx.prototxt" });
				urlList2.Add(new ModelDownloadURL() { folder_path = "person_reid_baseline_pytorch", file_name = "ft_ResNet50.onnx" });

				StartCoroutine(ailia_download.DownloadWithProgressFromURL(urlList2, () =>
				{
					FileOpened2 = ailia_feature_model.OpenFile(asset_path + "/ft_ResNet50.onnx.prototxt", asset_path + "/ft_ResNet50.onnx");
					SetInputShape(ailia_feature_model, PERSON_REID_BASELINE_WIDTH, PERSON_REID_BASELINE_HEIGHT, PERSON_REID_BASELINE_CHANNELS, 1);
				}));
				break;
			}
		}

		private void DestroyAiliaDetector()
		{
			ailia_detector.Close();
			ailia_feature_extractor.Close();
			ailia_feature_model.Close();
		}

		private List<AiliaDetector.AILIADetectorObject> FilterOnlyPerson(List<AiliaDetector.AILIADetectorObject> list){
			List<AiliaDetector.AILIADetectorObject> list2 = new List<AiliaDetector.AILIADetectorObject>();
			for (int i = 0; i < list.Count; i++){
				AiliaDetector.AILIADetectorObject obj = list[i];
				if (obj.category == 0){
					list2.Add(obj);
				}
			}
			return list2;
		}

		// Use this for initialization
		void Start()
		{
			SetUIProperties();
			mode_text.text = "ailia FeatureExtractor";
			CreateAiliaDetector();
			ailia_camera.CreateCamera(camera_id);
		}

		// Update is called once per frame
		void Update()
		{
			if (!ailia_camera.IsEnable() && test_image == null)
			{
				return;
			}
			if (!FileOpened1 || !FileOpened2)
			{
				return;
			}

			//Clear label
			Clear();

			//Get camera image
			int tex_width = 0;
			int tex_height = 0;
			Color32[] camera = null;

			//Web camera input
			if (test_image == null){
				tex_width = ailia_camera.GetWidth();
				tex_height = ailia_camera.GetHeight();
				camera = ailia_camera.GetPixels32();
			}
			
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

			//Face or Person Detection
			float det_threshold = 0.2f;
			float det_iou = 0.25f;
			if (ailiaModelType == FeatureExtractorModels.person_reid_baseline){
				det_threshold = 0.4f;
			}

			List<AiliaDetector.AILIADetectorObject> list = ailia_detector.ComputeFromImageB2T(camera, tex_width, tex_height, det_threshold, det_iou);
			if (ailiaModelType == FeatureExtractorModels.person_reid_baseline){
				list = FilterOnlyPerson(list);
			}

			for (int i = 0; i < list.Count; i++){
				AiliaDetector.AILIADetectorObject obj = list[i];
				bool last_face = (i == list.Count - 1);
				FaceMatching(obj, camera, tex_width, tex_height, last_face);
			}

			//Apply
			preview_texture.SetPixels32(camera);
			preview_texture.Apply();
		}

		private Color32 Bilinear(Color32[] face, int w, int h, float fx, float fy)
		{
			int x2 = (int)fx;
			int y2 = (int)fy;
			float xa = 1.0f - (fx - x2);
			float xb = 1.0f - xa;
			float ya = 1.0f - (fy - y2);
			float yb = 1.0f - ya;
			Color32 c1 = face[y2 * w + x2];
			Color32 c2 = (x2+1 < w) ? face[y2 * w + x2 + 1] : c1;
			Color32 c3 = (y2+1 < h) ? face[(y2 + 1) * w + x2] : c1;
			Color32 c4 = (x2+1 < w && y2+1 < h) ? face[(y2 + 1) * w + x2 + 1] : c1;
			byte r = (byte)(c1.r * xa * ya + c2.r * xb * ya + c3.r * xa * yb + c4.r * xb * yb);
			byte g = (byte)(c1.g * xa * ya + c2.g * xb * ya + c3.g * xa * yb + c4.g * xb * yb);
			byte b = (byte)(c1.b * xa * ya + c2.b * xb * ya + c3.b * xa * yb + c4.b * xb * yb);
			return new Color32(r, g, b, 255);
		}

		private float[] PreprocessArcFace(Color32[] face, int w, int h)
		{
			// GrayScale, image / 127.5 - 1.0, flip batch
			float [] arcface_input = new float[ARCFACE_WIDTH * ARCFACE_HEIGHT * ARCFACE_BATCH];
			for (int y = 0; y < ARCFACE_HEIGHT; y++){
				for (int x = 0; x < ARCFACE_WIDTH; x++){
					float fx = x * w / ARCFACE_WIDTH;
					float fy = y * h / ARCFACE_HEIGHT;
					Color32 v = Bilinear(face, w, h, fx, fy);
					float v2 = (v.r + v.g + v.b) / 3.0f;
					v2 = v2 / 127.5f - 1.0f;
					arcface_input[y*ARCFACE_WIDTH+x] = v2;	// normal
					arcface_input[y*ARCFACE_WIDTH+(ARCFACE_WIDTH-1-x)+ARCFACE_WIDTH*ARCFACE_HEIGHT] = v2; // flip
				}
			}
			return arcface_input;
		}

		private float[] PreprocessPersonReIDBaseline(Color32[] face, int w, int h)
		{
			// Rgb, imagenet mean
			float [] input_data = new float[PERSON_REID_BASELINE_WIDTH * PERSON_REID_BASELINE_HEIGHT * PERSON_REID_BASELINE_CHANNELS];
			int stride = PERSON_REID_BASELINE_WIDTH * PERSON_REID_BASELINE_HEIGHT;
			for (int y = 0; y < PERSON_REID_BASELINE_HEIGHT; y++){
				for (int x = 0; x < PERSON_REID_BASELINE_WIDTH; x++){
					float fx = x * w / PERSON_REID_BASELINE_WIDTH;
					float fy = y * h / PERSON_REID_BASELINE_HEIGHT;
					Color32 v = Bilinear(face, w, h, fx, fy);
					input_data[y * PERSON_REID_BASELINE_WIDTH + x + stride * 0] = (v.r / 255.0f - 0.485f) / 0.229f;
					input_data[y * PERSON_REID_BASELINE_WIDTH + x + stride * 1] = (v.g / 255.0f - 0.456f) / 0.224f;
					input_data[y * PERSON_REID_BASELINE_WIDTH + x + stride * 2] = (v.b / 255.0f - 0.406f) / 0.225f;
				}
			}
			return input_data;
		}

		private void DebugArcFaceInput(float [] face_input, Color32 [] camera, int tex_width, int tex_height)
		{
			for (int y = 0; y < ARCFACE_HEIGHT; y++){
				for (int x = 0; x < ARCFACE_WIDTH; x++){
					// normal
					byte r = (byte)((face_input[y * ARCFACE_WIDTH + x] + 1.0f) * 127.5f);
					camera[(tex_height - 1 - y) * tex_width + x] = new Color32(r, r, r, 255);
					
					// flip
					r = (byte)((face_input[y * ARCFACE_WIDTH + x + ARCFACE_WIDTH * ARCFACE_HEIGHT] + 1.0f) * 127.5f);
					camera[(tex_height - 1 - y) * tex_width + x + ARCFACE_WIDTH] = new Color32(r, r, r, 255);
				}
			}
		}

		private void DebugPersonReIDInput(float [] face_input, Color32 [] camera, int tex_width, int tex_height)
		{
			int stride = PERSON_REID_BASELINE_WIDTH * PERSON_REID_BASELINE_HEIGHT;
			for (int y = 0; y < PERSON_REID_BASELINE_HEIGHT; y++){
				for (int x = 0; x < PERSON_REID_BASELINE_WIDTH; x++){
					byte r = (byte)((face_input[y * PERSON_REID_BASELINE_WIDTH + x + stride * 0] * 0.229f + 0.485f) * 255.0f);
					byte g = (byte)((face_input[y * PERSON_REID_BASELINE_WIDTH + x + stride * 1] * 0.224f + 0.456f) * 255.0f);
					byte b = (byte)((face_input[y * PERSON_REID_BASELINE_WIDTH + x + stride * 2] * 0.225f + 0.406f) * 255.0f);
					camera[(tex_height - 1 - y) * tex_width + x] = new Color32(r, g, b, 255);					
				}
			}
		}

		private float CosinMetric(float [] features, float [] capture_feature_value)
		{
			// cos similaity between two features
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

		private void GetFacePosition(AiliaDetector.AILIADetectorObject box, int tex_width, int tex_height, ref int x1, ref int y1, ref int w, ref int h)
		{
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
			bool rectangle = false;
			if (ailiaModelType == FeatureExtractorModels.arcface){
				expand = 1.2f;
				square = true;
			}
			if (ailiaModelType == FeatureExtractorModels.arcfacem){
				expand = 1.4f;
				square = true;
			}
			if (ailiaModelType == FeatureExtractorModels.person_reid_baseline){
				expand = 1.0f;
				rectangle = true;
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
			if (rectangle){
				h = w * 2;
			}
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
					}else{
						face[y * w + x] = Color.black;
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
			float[] face_input = null;
			switch(ailiaModelType){
			case FeatureExtractorModels.arcface:
			case FeatureExtractorModels.arcfacem:
				face_input = PreprocessArcFace(face, w, h);
				if (debug_feature_input){
					DebugArcFaceInput(face_input, camera, tex_width, tex_height);
				}
				features = new float [ARCFACE_FEATURE_LEN];
				ailia_feature_model.Predict(features, face_input);
				if (capture_feature_value != null){
					similality = CosinMetric(features, capture_feature_value); //Calc similaity between two feature vectors
				}
				break;
			case FeatureExtractorModels.person_reid_baseline:
				face_input = PreprocessPersonReIDBaseline(face, w, h);
				if (debug_feature_input){
					DebugPersonReIDInput(face_input, camera, tex_width, tex_height);
				}
				features = new float [PERSON_REID_BASELINE_FEATURE_LEN];
				ailia_feature_model.Predict(features, face_input);
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

		private void DisplayResult(float distance, float similality, float elspsed_time, int x1, int y1, int w, int h, int tex_width, int tex_height, bool last_face)
		{
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
				if (ailiaModelType == FeatureExtractorModels.arcface || ailiaModelType == FeatureExtractorModels.arcfacem || ailiaModelType == FeatureExtractorModels.person_reid_baseline)
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
				feature_text = "Please capture target object";
			}

			DrawRect2D(color, x1, y1, w, h, tex_width, tex_height);

			int margin = 4;
			DrawText(color, feature_text, x1 + margin, y1 + margin, tex_width, tex_height);

			if (label_text != null)
			{
				label_text.text = elspsed_time + "ms\n" + ailia_detector.EnvironmentName();
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
					label_text.text = "Target object not found!";
				}
				else
				{
					label_text.text = "Capture success!";
				}
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