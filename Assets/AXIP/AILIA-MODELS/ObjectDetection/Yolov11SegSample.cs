/* AILIA Unity Plugin Detector Sample */
/* Copyright 2018-2019 AXELL CORPORATION */

using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

using ailia;

namespace ailiaSDK {
	class Mask {
		public int w;
		public int h;
		public float[] data;
	}

	class AILIADetectorObjectEX : AiliaDetector.AILIADetectorObject
	{
		public Mask mask;
	}

	public class Yolov11SegSample : AiliaRenderer {
		[SerializeField]
		private GameObject UICanvas = null;

		//Settings
		[SerializeField]
		private bool gpu_mode = false;
		[SerializeField]
		private int camera_id = 0;

		//Result
		RawImage raw_image = null;
		Text label_text = null;
		Text mode_text = null;

		//Preview
		private Texture2D preview_texture = null;

		//AILIA
		private AiliaModel ailia_model = new AiliaModel();

		private AiliaCamera ailia_camera = new AiliaCamera();
		private AiliaDownload ailia_download = new AiliaDownload();

		// AILIA open file
		private bool FileOpened = false;

		// Compute parameter
		float threshold = 0.25f;
		float iou = 0.7f;
		int detectionSize = 640;
		string[] classifierLabel;
		uint category_n = 1;

		private float[] preds_data  = new float[0];
		private float[] proto_data  = new float[0];

		private void CreateAiliaDetector()
		{
			string asset_path = Application.temporaryCachePath;
			var urlList = new List<ModelDownloadURL>();
			if (gpu_mode)
			{
				ailia_model.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
			}
			string base_url = "";

			mode_text.text = "ailia yolov11_seg Detector";
			classifierLabel = AiliaClassifierLabel.COCO_CATEGORY;
			category_n = (uint)classifierLabel.Length;

			urlList.Add(new ModelDownloadURL() { folder_path = "yolov11-seg", file_name = "yolo11n-seg.onnx.prototxt" });
			urlList.Add(new ModelDownloadURL() { folder_path = "yolov11-seg", file_name = "yolo11n-seg.onnx" });

			StartCoroutine(ailia_download.DownloadWithProgressFromURL(urlList, () =>
			{
				FileOpened = ailia_model.OpenFile(asset_path + "/yolo11n-seg.onnx.prototxt", asset_path + "/yolo11n-seg.onnx");
			}));
		}


		private void DestroyAiliaDetector()
		{
			ailia_model.Close();
		}

		// Use this for initialization
		void Start()
		{
			AiliaLicense.CheckAndDownloadLicense();
			SetUIProperties();
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
			if (!FileOpened)
			{
				return;
			}

			//Clear result
			Clear();

			//Get camera image
			int tex_width = ailia_camera.GetWidth();
			int tex_height = ailia_camera.GetHeight();
			if (preview_texture == null)
			{
				preview_texture = new Texture2D(tex_width, tex_height);
				raw_image.texture = preview_texture;
			}
			Color32[] camera = ailia_camera.GetPixels32();

			//Detection
			long start_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

			float[] input_data = Preprocess(camera, tex_width, tex_height);

			Ailia.AILIAShape input_shape = new Ailia.AILIAShape();
			input_shape.x=(uint)detectionSize;
			input_shape.y=(uint)detectionSize;
			input_shape.z=3;
			input_shape.w=1;
			input_shape.dim=4;
			bool shapeSetResult = ailia_model.SetInputBlobShape(input_shape, 0);
            if (!shapeSetResult)
            {
                Debug.LogError("Failed to set ailia_model input shape: " + ailia_model.Status);
                return;
            }
			uint[] input_blobs = ailia_model.GetInputBlobList();
			if (input_blobs == null || input_blobs.Length <= 0)
			{
				return;
			}

			ailia_model.SetInputBlobData(input_data, (int)input_blobs[0]);

			ailia_model.Update();

			uint[] output_blobs = ailia_model.GetOutputBlobList();
			if (output_blobs == null || output_blobs.Length < 2)
			{
				return;
			}

			Ailia.AILIAShape preds_shape = ailia_model.GetBlobShape((int)output_blobs[0]);
			Ailia.AILIAShape proto_shape = ailia_model.GetBlobShape((int)output_blobs[1]);

			if (preds_shape == null || proto_shape == null) {
				return;
			}

			if (preds_data.Length != preds_shape.x * preds_shape.y * preds_shape.z * preds_shape.w){
				preds_data = new float[preds_shape.x * preds_shape.y * preds_shape.z * preds_shape.w];
			}
			if (proto_data.Length != proto_shape.x * proto_shape.y * proto_shape.z * proto_shape.w){
				proto_data = new float[proto_shape.x * proto_shape.y * proto_shape.z * proto_shape.w];
			}

			if (!ailia_model.GetBlobData(preds_data, (int)output_blobs[0]) ||
				!ailia_model.GetBlobData(proto_data, (int)output_blobs[1])) {
				return;
			}

			float aspect = (float)tex_width / tex_height;
			List<AILIADetectorObjectEX> list = PostProcessing(preds_data, preds_shape, proto_data, proto_shape, tex_width, tex_height);

			long end_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

			long start_time_class = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
			foreach (AILIADetectorObjectEX obj in list)
			{
				DisplayDetectedResult(obj, ref camera, tex_width, tex_height);
			}
			long end_time_class = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

			if (label_text != null)
			{
				string env_name;
				env_name = ailia_model.EnvironmentName();
				label_text.text = (end_time - start_time) + (end_time_class - start_time_class) + "ms\n" + env_name;
			}

			//Apply
			preview_texture.SetPixels32(camera);
			preview_texture.Apply();
		}

		private float[] Preprocess(Color32[] camera, int width, int height)
		{
			float ratio = Mathf.Min((float)detectionSize / height, (float)detectionSize / width);
			int newHeight = Mathf.RoundToInt(height * ratio);
			int newWidth = Mathf.RoundToInt(width * ratio);

			int dh = detectionSize - newHeight;
			int dw = detectionSize - newWidth;

			int stride = 32;
			dw = dw % stride;
			dh = dh % stride;

			int top = Mathf.RoundToInt(dh / 2f - 0.1f);
			int bottom = Mathf.RoundToInt(dh / 2f + 0.1f);
			int left = Mathf.RoundToInt(dw / 2f - 0.1f);
			int right = Mathf.RoundToInt(dw / 2f + 0.1f);

			float[] processedData = new float[3 * detectionSize * detectionSize];

			// Border color
			byte borderR = 114;
			byte borderG = 114;
			byte borderB = 114;

			for (int y = 0; y < detectionSize; y++)
			{
				for (int x = 0; x < detectionSize; x++)
				{
					int srcX = x - left;
					int srcY = y - top;

					byte r = borderR;
					byte g = borderG;
					byte b = borderB;

					if (srcX >= 0 && srcX < newWidth && srcY >= 0 && srcY < newHeight)
					{
						int origX = Mathf.FloorToInt(srcX / ratio);
						int origY = Mathf.FloorToInt(srcY / ratio);

						origX = Mathf.Clamp(origX, 0, width - 1);
						origY = Mathf.Clamp(origY, 0, height - 1);

						Color32 pixel = camera[(height - 1 - origY) * width + origX];
						r = pixel.r;
						g = pixel.g;
						b = pixel.b;
					}

					// HWC -> CHW
					int destIndex = y * detectionSize + x;
					processedData[destIndex] = r / 255.0f;
					processedData[detectionSize * detectionSize + destIndex] = g / 255.0f;
					processedData[2 * detectionSize * detectionSize + destIndex] = b / 255.0f;
				}
			}

			return processedData;
		}

		private List<AILIADetectorObjectEX> PostProcessing(float[] preds, Ailia.AILIAShape predsShape,
                                                                float[] proto, Ailia.AILIAShape protoShape,
                                                                int originalWidth, int originalHeight)
		{
			List<AILIADetectorObjectEX> results = new List<AILIADetectorObjectEX>();

			float confThres = threshold;
			float iouThres = iou;

			int nc = classifierLabel.Length;
			int mi = 4 + nc;
			int numCandidates = (int)predsShape.x;
			int numAttributes = (int)predsShape.y;

			if (mi > numAttributes)
			{
				Debug.LogError("Invalid model output shape");
				return results;
			}

			List<int> validIndices = new List<int>();
			for (int i = 0; i < numCandidates; i++)
			{
				float maxClassConf = 0;
				for (int j = 4; j < mi; j++)
				{
					float val = preds[j * numCandidates + i];
					if (val > maxClassConf) maxClassConf = val;
				}

				if (maxClassConf > confThres)
				{
					validIndices.Add(i);
				}
			}

			if (validIndices.Count == 0)
			{
				return results;
			}

			List<float[]> validBoxes = new List<float[]>();
			List<float> validScores = new List<float>();
			List<int> validClassIds = new List<int>();
			List<float[]> validMasks = new List<float[]>();
			foreach (int idx in validIndices)
			{
				float cx = preds[idx];
				float cy = preds[1 * numCandidates + idx];
				float w = preds[2 * numCandidates + idx];
				float h = preds[3 * numCandidates + idx];

				// (center_x, center_y, width, height) to (x1, y1, x2, y2)
				float x1 = cx - w / 2;
				float y1 = cy - h / 2;
				float x2 = cx + w / 2;
				float y2 = cy + h / 2;

				int bestClassId = 0;
				float bestClassScore = 0;
				for (int c = 0; c < nc; c++)
				{
					float score = preds[(4 + c) * numCandidates + idx];
					if (score > bestClassScore)
					{
						bestClassScore = score;
						bestClassId = c;
					}
				}

				float[] maskCoeffs = new float[numAttributes - mi];
				for (int m = 0; m < maskCoeffs.Length; m++)
				{
					maskCoeffs[m] = preds[(mi + m) * numCandidates + idx];
				}

				validBoxes.Add(new float[] { x1, y1, x2, y2 });
				validScores.Add(bestClassScore);
				validClassIds.Add(bestClassId);
				validMasks.Add(maskCoeffs);
			}

			// Sort by confidence and limit to max_nms
			int maxNms = Math.Min(30000, validScores.Count);
			int[] sortedIndices = validScores.Select((score, i) => new KeyValuePair<float, int>(score, i))
											.OrderByDescending(x => x.Key)
											.Take(maxNms)
											.Select(x => x.Value)
											.ToArray();

			// Apply NMS (Non-Maximum Suppression)
			List<int> keepIndices = NMSUtils.BatchedNMS(
				sortedIndices.Select(i => validBoxes[i]).ToList(),
				sortedIndices.Select(i => validScores[i]).ToList(),
				sortedIndices.Select(i => validClassIds[i]).ToList(),
				iouThres
			);

			// Limit detections to max_det
			int maxDet = Math.Min(300, keepIndices.Count);
			keepIndices = keepIndices.Take(maxDet).ToList();

			// Calculate scaling factors for the original image
			int imgHeight = detectionSize;
			int imgWidth = detectionSize;
			float gain = Math.Min((float)imgHeight / originalHeight, (float)imgWidth / originalWidth);
			int padW = (int)((imgWidth - originalWidth * gain) / 2);
			int padH = (int)((imgHeight - originalHeight * gain) / 2);

			// Process masks and create detector objects
			foreach (int i in keepIndices)
			{
				int origIdx = sortedIndices[i];
				float[] box = validBoxes[origIdx];
				float score = validScores[origIdx];
				int classId = validClassIds[origIdx];
				float[] maskCoeffs = validMasks[origIdx];

				Mask mask = ProcessMask(proto, protoShape, maskCoeffs, box, imgWidth, imgHeight);

				// Scale boxes back to original image
				float x1 = (box[0] - padW) / gain;
				float y1 = (box[1] - padH) / gain;
				float x2 = (box[2] - padW) / gain;
				float y2 = (box[3] - padH) / gain;

				// Clip to image boundaries
				x1 = Math.Max(0, Math.Min(originalWidth - 1, x1));
				y1 = Math.Max(0, Math.Min(originalHeight - 1, y1));
				x2 = Math.Max(0, Math.Min(originalWidth - 1, x2));
				y2 = Math.Max(0, Math.Min(originalHeight - 1, y2));

				// Create detector object
				AILIADetectorObjectEX detObj = new AILIADetectorObjectEX();
				detObj.category = (uint)classId;
				detObj.prob = score;
				detObj.x = x1 / originalWidth;
				detObj.y = y1 / originalHeight;
				detObj.w = (x2 - x1) / originalWidth;
				detObj.h = (y2 - y1) / originalHeight;
				detObj.mask = mask;

				results.Add(detObj);
			}

			Debug.Log("Number objects: " + results.Count);

			return results;
		}

		private Mask ProcessMask(float[] proto, Ailia.AILIAShape protoShape, float[] maskCoeffs, float[] box, int imgWidth, int imgHeight)
		{
			int mw = (int)protoShape.x;
			int mh = (int)protoShape.y;
			int c = (int)protoShape.z;

			float[] mask = new float[mh * mw];

			for (int y = 0; y < mh; y++)
			{
				for (int x = 0; x < mw; x++)
				{
					float sum = 0;
					for (int i = 0; i < c; i++)
					{
						int protoIdx = i * mh * mw + y * mw + x;
						sum += maskCoeffs[i] * proto[protoIdx];
					}
					mask[y * mw + x] = MathUtils.Sigmoid(sum);
				}
			}

			Mask maskData = new Mask();
			maskData.w = mw;
			maskData.h = mh;
			maskData.data = mask;

			return maskData;
		}

		private void DisplayDetectedResult(AILIADetectorObjectEX box, ref Color32[] camera, int tex_width, int tex_height)
		{
			//Convert to pixel domain
			int x1 = (int)(box.x * tex_width);
			int y1 = (int)(box.y * tex_height);
			int x2 = (int)((box.x + box.w) * tex_width);
			int y2 = (int)((box.y + box.h) * tex_height);

			int w = (x2 - x1);
			int h = (y2 - y1);

			if (w <= 0 || h <= 0)
			{
				return;
			}

			Color color = Color.white;
			color = Color.HSVToRGB((float)box.category / category_n, 1.0f, 1.0f);
			DrawRect2D(color, x1, y1, w, h, tex_width, tex_height);

			// Draw segmentation mask
			if (box.mask != null)
			{
				int maskWidth = box.mask.w;
				int maskHeight = box.mask.h;
				float[] maskData = box.mask.data;

				Color maskColor = new Color(color.r, color.g, color.b, 0.5f);

				for (int y = y1; y < y2; y++)
				{
					float normalizedY = (float)(y - y1) / h;

					for (int x = x1; x < x2; x++)
					{
						float normalizedX = (float)(x - x1) / w;

						// Calculate mask index with proper interpolation
						float maskX = normalizedX * maskWidth;
						float maskY = normalizedY * maskHeight;

						// Bilinear interpolation
						int maskX1 = Mathf.FloorToInt(maskX);
						int maskY1 = Mathf.FloorToInt(maskY);
						int maskX2 = Mathf.Min(maskX1 + 1, maskWidth - 1);
						int maskY2 = Mathf.Min(maskY1 + 1, maskHeight - 1);

						float wx = maskX - maskX1;
						float wy = maskY - maskY1;

						float v1 = maskData[maskY1 * maskWidth + maskX1];
						float v2 = maskData[maskY1 * maskWidth + maskX2];
						float v3 = maskData[maskY2 * maskWidth + maskX1];
						float v4 = maskData[maskY2 * maskWidth + maskX2];

						float maskValue = v1 * (1 - wx) * (1 - wy) + v2 * wx * (1 - wy) +
										v3 * (1 - wx) * wy + v4 * wx * wy;

						if (maskValue > 0.5)
						{
							int idx = (tex_height - 1 - y) * tex_width + x;
							if (idx >= 0 && idx < camera.Length)
							{
								Color32 pixelColor = camera[idx];
								camera[idx] = Color32.Lerp(pixelColor, maskColor, 0.5f);
							}
						}
					}
				}
			}

			float p = (int)(box.prob * 100) / 100.0f;
			string text = "";
			text += classifierLabel[box.category];
			text += " " + p;
			int margin = 4;
			DrawText(color, text, x1 + margin, y1 + margin, tex_width, tex_height);
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

