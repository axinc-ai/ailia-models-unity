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
	public class Mask {
		public int w;
		public int h;
		public float[] data;
	}

	public class AILIADetectorObjectEX : AiliaDetector.AILIADetectorObject
	{
		public Mask mask;
	}

	public class Yolov11Seg {
		//Settings
		private bool gpu_mode = false;

		//AILIA
		private AiliaModel ailia_model = new AiliaModel();

		// AILIA open file
		private bool FileOpened = false;

		// Compute parameter
		float threshold = 0.25f;
		float iou = 0.7f;
		int detectionSize = 640;
		string[] classifierLabel;

		private float[] preds_data  = new float[0];
		private float[] proto_data  = new float[0];

		public Yolov11Seg(string[] classifierLabel, bool gpu_mode = false,
			int detectionSize = 640, float threshold = 0.25f, float iou = 0.7f)
		{
			this.classifierLabel = classifierLabel;
			this.gpu_mode = gpu_mode;
			this.detectionSize = detectionSize;
			this.threshold = threshold;
			this.iou = iou;
		}

		public string EnvironmentName()
		{
			return ailia_model.EnvironmentName();
		}

		public int GetCategoryCount()
		{
			return classifierLabel.Length;
		}

		public String GetCategoryName(uint index)
		{
			if (index < 0 || index >= classifierLabel.Length)
			{
				return "";
			}
			return classifierLabel[index];
		}

		public bool Open(String prototxtPath, String modelPath)
		{
			AiliaLicense.CheckAndDownloadLicense();

			if (gpu_mode)
			{
				ailia_model.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
			}

			FileOpened = ailia_model.OpenFile(prototxtPath, modelPath);
			return FileOpened;
		}

		public void Close()
		{
			ailia_model.Close();
		}

		// Update is called once per frame
		public List<AILIADetectorObjectEX> Predict(Color32[] pixels, int width, int height)
		{
			if (!FileOpened)
			{
				Debug.LogError("AILIA model file not opened");
				return null;
			}

			int tex_width = width;
			int tex_height = height;

			float[] input_data = Preprocess(pixels, tex_width, tex_height);

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
                return null;
            }

			uint[] input_blobs = ailia_model.GetInputBlobList();
			if (input_blobs == null || input_blobs.Length <= 0)
			{
				return null;
			}

			ailia_model.SetInputBlobData(input_data, (int)input_blobs[0]);

			ailia_model.Update();

			uint[] output_blobs = ailia_model.GetOutputBlobList();

			if (output_blobs == null || output_blobs.Length < 1)
			{
				return null;
			}

			Ailia.AILIAShape preds_shape = ailia_model.GetBlobShape((int)output_blobs[0]);
			Ailia.AILIAShape proto_shape = output_blobs.Length < 2 ? null : ailia_model.GetBlobShape((int)output_blobs[1]);

			if (preds_shape == null) {
				return null;
			}

			if (preds_data.Length != preds_shape.x * preds_shape.y * preds_shape.z * preds_shape.w){
				preds_data = new float[preds_shape.x * preds_shape.y * preds_shape.z * preds_shape.w];
			}
			if (proto_shape != null && proto_data.Length != proto_shape.x * proto_shape.y * proto_shape.z * proto_shape.w){
				proto_data = new float[proto_shape.x * proto_shape.y * proto_shape.z * proto_shape.w];
			}

			if (!ailia_model.GetBlobData(preds_data, (int)output_blobs[0])) {
				return null;
			}

			if (proto_shape != null && !ailia_model.GetBlobData(proto_data, (int)output_blobs[1])) {
				return null;
			}

			float aspect = (float)tex_width / tex_height;
			List<AILIADetectorObjectEX> list = PostProcessing(preds_data, preds_shape, proto_data, proto_shape, tex_width, tex_height);

			return list;
		}

		private float[] Preprocess(Color32[] pixels, int width, int height)
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

						Color32 pixel = pixels[(height - 1 - origY) * width + origX];
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

			return results;
		}

		private Mask ProcessMask(float[] proto, Ailia.AILIAShape protoShape, float[] maskCoeffs, float[] box, int imgWidth, int imgHeight)
		{
			if (protoShape == null)
			{
				return null;
			}

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

			// crop the mask
			int x1 = (int)(box[0] * mw / imgWidth);
			int y1 = (int)(box[1] * mh / imgHeight);
			int x2 = (int)(box[2] * mw / imgWidth);
			int y2 = (int)(box[3] * mh / imgHeight);

			int newW = x2 - x1;
			int newH = y2 - y1;
			float[] croppedMask = new float[newW * newH];
			for (int y = 0; y < newH; y++)
			{
				for (int x = 0; x < newW; x++)
				{
					int srcIdx = (y1 + y) * mw + (x1 + x);
					int destIdx = y * newW + x;
					croppedMask[destIdx] = mask[srcIdx];
				}
			}

			Mask maskData = new Mask();
			maskData.w = newW;
			maskData.h = newH;
			maskData.data = croppedMask;

			// TODO: fix mask with padding

			return maskData;
		}
	}
}

