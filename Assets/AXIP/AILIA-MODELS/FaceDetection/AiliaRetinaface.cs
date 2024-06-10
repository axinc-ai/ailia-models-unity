/* AILIA Unity Plugin Blazeface Sample */
/* Copyright 2022 AXELL CORPORATION */

using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Runtime.InteropServices;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

using System.Linq;

using ailia;

namespace ailiaSDK
{
	public class AiliaRetinaface
	{

        public const int RETINAFACE_DETECTOR_INPUT_BATCH_SIZE = 1;
        public const int RETINAFACE_DETECTOR_INPUT_CHANNEL_COUNT = 3;
        private int RETINAFACE_DETECTOR_INPUT_HEIGHT_SIZE;
        private int RETINAFACE_DETECTOR_INPUT_WIDTH_SIZE;

		public const int BOX_DIM = 4;
		public const int SCORE_DIM = 2;
		public const int LANDMARK_DIM = 10;
		public const int NUM_KEYPOINTS = 5;

		public int TOP_K = 5000;
		public int KEEP_TOP_K = 750;
		public const float CONFIDENCE_THRES = 0.02f;
		public const float NMS_THRES = 0.4f;

		private float[] VARIANCE = {0.1f, 0.2f};


        public struct FaceInfo
		{
			public float score;
			public Vector2 center;
			public float width;
			public float height;
			public Vector2[] keypoints;
		}

        public List<FaceInfo> Detection(AiliaModel ailia_model, Color32[] camera, int tex_width, int tex_height)
		{
            bool status;

			RETINAFACE_DETECTOR_INPUT_HEIGHT_SIZE = tex_height;
			RETINAFACE_DETECTOR_INPUT_WIDTH_SIZE = tex_width;

            //Resize
			float[] data = new float[RETINAFACE_DETECTOR_INPUT_WIDTH_SIZE * RETINAFACE_DETECTOR_INPUT_HEIGHT_SIZE * RETINAFACE_DETECTOR_INPUT_CHANNEL_COUNT * RETINAFACE_DETECTOR_INPUT_BATCH_SIZE];
			int w = RETINAFACE_DETECTOR_INPUT_WIDTH_SIZE;
			int h = RETINAFACE_DETECTOR_INPUT_HEIGHT_SIZE;
			
			for (int y = 0; y < h; y++)
			{
				for (int x = 0; x < w; x++)
				{
					data[(y * w + x) + 0 * w * h] = (float)(((camera[(tex_height - 1 - y) * tex_width + x].r) - 123.0f));
                    data[(y * w + x) + 1 * w * h] = (float)(((camera[(tex_height - 1 - y) * tex_width + x].g) - 117.0f));
                    data[(y * w + x) + 2 * w * h] = (float)(((camera[(tex_height - 1 - y) * tex_width + x].b) - 104.0f));
				}
			}

            //SetInputBlobShape
            uint[] input_blobs = ailia_model.GetInputBlobList();
			int inputBlobIndex = ailia_model.FindBlobIndexByName("input0");
			status = ailia_model.SetInputBlobShape(
				new Ailia.AILIAShape
				{
					x = (uint)RETINAFACE_DETECTOR_INPUT_WIDTH_SIZE,
					y = (uint)RETINAFACE_DETECTOR_INPUT_HEIGHT_SIZE,
					z = (uint)RETINAFACE_DETECTOR_INPUT_CHANNEL_COUNT,
					w = RETINAFACE_DETECTOR_INPUT_BATCH_SIZE,
					dim = 4
				},
				inputBlobIndex
			);
			if (!status)
			{
				Debug.LogError("Could not set input blob shape");
				Debug.LogError(ailia_model.GetErrorDetail());
			}


            if (input_blobs != null)
			{
				bool success = ailia_model.SetInputBlobData(data, (int)input_blobs[0]);
				if (!success)
				{
					Debug.Log("Failed SetInputBlobData");
				}

				long start_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
                
				ailia_model.Update();
                
				List<FaceInfo> detections = new List<FaceInfo>();

				uint[] output_blobs = ailia_model.GetOutputBlobList();

				if (output_blobs != null && output_blobs.Length >= 3)
				{
					Ailia.AILIAShape box_shape = ailia_model.GetBlobShape((int)output_blobs[0]);
					Ailia.AILIAShape score_shape = ailia_model.GetBlobShape((int)output_blobs[1]);
                    Ailia.AILIAShape landmark_shape = ailia_model.GetBlobShape((int)output_blobs[2]);

					if (box_shape != null && score_shape != null && landmark_shape != null)
					{
						float[] box_data = new float[box_shape.x * box_shape.y * box_shape.z * box_shape.w];
						float[] score_data = new float[score_shape.x * score_shape.y * score_shape.z * score_shape.w];
                        float[] landmark_data = new float[landmark_shape.x * landmark_shape.y * landmark_shape.z * landmark_shape.w];

						ailia_model.GetBlobData(box_data, (int)output_blobs[0]);
						ailia_model.GetBlobData(score_data, (int)output_blobs[1]);
                        ailia_model.GetBlobData(landmark_data, (int)output_blobs[2]);

						detections = PostProcess(box_data, score_data, landmark_data, tex_width, tex_height);
					}
				}  
				return detections;
			}
			return null;
        }


		List<FaceInfo> PostProcess(float[] box_data, float[] score_data, float[] landmark_data, int tex_width, int tex_height)
		{
			List<FaceInfo> results = new List<FaceInfo>();

			float[,] reshaped_box_data = Reshape(box_data, -1, BOX_DIM);
			float[,] priors = PriorBoxForward(tex_width, tex_height);
			float[,] boxes = DecodeBox(reshaped_box_data, priors, VARIANCE);
			int[] box_scale = new int[] { tex_width, tex_height, tex_width, tex_height };
			float[,] scaled_boxes = Scale(boxes, box_scale);

			float[,] reshaped_score_data = Reshape(score_data, -1, SCORE_DIM);
			float[] scores = DecodeScore(reshaped_score_data);

			float[,] reshaped_landmark_data = Reshape(landmark_data, -1, LANDMARK_DIM);
			float[,] landmarks = DecodeLandmark(reshaped_landmark_data, priors, VARIANCE);
			int[] landmark_scale = new int[] { tex_width, tex_height, tex_width, tex_height, tex_width, tex_height, tex_width, tex_height, tex_width, tex_height };
			float[,] scaled_landmarks = Scale(landmarks, landmark_scale);

			List<int> inds = new List<int>();
			for (int i = 0; i < scores.Length; i++)
			{
				if (scores[i] > CONFIDENCE_THRES)
				{
					inds.Add(i);
				}
			}
			float[,] filterd_boxes = Filter(scaled_boxes, inds);
			float[] filterd_scores = Filter(scores, inds);
			float[,] filterd_landmarks = Filter(scaled_landmarks, inds);
			
			int top_k = TOP_K;
			if(filterd_scores.Length < TOP_K){
				top_k = filterd_scores.Length;
			}
			int[] order = filterd_scores.Select((value, index) => new { Value = value, Index = index })
					.OrderByDescending(item => item.Value)
					.Select(item => item.Index)
					.Take(top_k)
					.ToArray();
			float[,] top_K_boxes = KeepTopKBeforeNMS(filterd_boxes, order, top_k);
			float[] top_K_scores = KeepTopKBeforeNMS(filterd_scores, order, top_k);
			float[,] top_K_landmarks = KeepTopKBeforeNMS(filterd_landmarks, order, top_k);

			List<int> keep = Nms(top_K_boxes, top_K_scores, NMS_THRES);

			int row = keep.Count;
			float[,] nms_boxes = new float[row, BOX_DIM];
			float[] nms_scores = new float[row];
			float[,] nms_landmarks = new float[row, LANDMARK_DIM];
			for (int i = 0; i < row; i++)
			{
				int index = keep[i];

				for (int j = 0; j < BOX_DIM; j++)
				{
					nms_boxes[i, j] = top_K_boxes[index, j];
				}

				nms_scores[i] = top_K_scores[index];

				for (int j = 0; j < LANDMARK_DIM; j++)
				{
					nms_landmarks[i, j] = top_K_landmarks[index, j];
				}
			}

			int keep_top_k = KEEP_TOP_K;
			if(nms_scores.Length < KEEP_TOP_K){
				keep_top_k = nms_scores.Length;
			}
			float[,] keep_top_K_boxes = new float[keep_top_k, BOX_DIM];
			float[] keep_top_K_scores = new float[keep_top_k];
			float[,] keep_top_K_landmarks = new float[keep_top_k, LANDMARK_DIM];
			for (int i = 0; i < keep_top_k; i++)
			{
				for (int j = 0; j < BOX_DIM; j++)
				{
					keep_top_K_boxes[i, j] = nms_boxes[i, j];
				}

				keep_top_K_scores[i] = nms_scores[i];

				for (int j = 0; j < LANDMARK_DIM; j++)
				{
					keep_top_K_landmarks[i, j] = nms_landmarks[i, j];
				}
			}
			
			for (int i = 0; i < keep_top_k; i++)
			{
				FaceInfo faceInfo = new FaceInfo
				{
					score = keep_top_K_scores[i],
					center = new Vector2((keep_top_K_boxes[i, 0] + keep_top_K_boxes[i, 2]) / 2, (keep_top_K_boxes[i, 1] + keep_top_K_boxes[i, 3]) / 2),
					width = keep_top_K_boxes[i, 2] - keep_top_K_boxes[i, 0],
					height = keep_top_K_boxes[i, 3] - keep_top_K_boxes[i, 1],
					keypoints = new Vector2[5]
				};

				for (int j = 0; j < NUM_KEYPOINTS; j++)
				{
					faceInfo.keypoints[j] = new Vector2(keep_top_K_landmarks[i, 2 * j], keep_top_K_landmarks[i, 2 * j + 1]);
				}

				results.Add(faceInfo);
			}

			return results;

		}


		private float[,] PriorBoxForward(int image_width, int image_height){

			int[][] minSizes = new int[][]
			{
				new int[] {16, 32},
				new int[] {64, 128},
				new int[] {256, 512}
			};
			int[] steps = {8, 16, 32};
			int[] imageSize = {image_height, image_width};
			int[][] featureMaps = steps.Select(step => new int[] 
			{
				(int)Math.Ceiling((double)imageSize[0] / step),
				(int)Math.Ceiling((double)imageSize[1] / step)
        	}).ToArray();

			
			List<float> anchors = new List<float>();
			for (int k = 0; k < featureMaps.Length; k++)
			{
				var f = featureMaps[k];
				for (int i = 0; i < f[0]; i++)
				{
					for (int j = 0; j < f[1]; j++)
					{
						foreach (var minSize in minSizes[k]) 
						{
							float s_kx = minSize / (float)imageSize[1];
							float s_ky = minSize / (float)imageSize[0];
							float dense_cx = (float)((j + 0.5f) * steps[k] / imageSize[1]);
							float dense_cy = (float)((i + 0.5f) * steps[k] / imageSize[0]);
							anchors.AddRange(new float[] { dense_cx, dense_cy, s_kx, s_ky });
						}
					}
				}
			}
			
			return Reshape(anchors.ToArray(), -1, 4);

		}


		private T[,] Reshape<T>(T[] array, int rows, int columns)
		{
			if (rows == -1)
			{
				rows = array.Length / columns;
			}

			T[,] result = new T[rows, columns];
			for (int i = 0; i < array.Length; i++)
			{
				int row = i / columns;
				int column = i % columns;
				result[row, column] = array[i];
			}

			return result;
		}


		private float[,] DecodeBox(float[,] src_box, float[,] priors, float[] variances)
		{
			int numBoxes = priors.GetLength(0);
			float[,] dst_box = new float[numBoxes, 4];

			for (int i = 0; i < numBoxes; i++)
			{
				float center_x = priors[i, 0] + src_box[i, 0] * variances[0] * priors[i, 2];
				float center_y = priors[i, 1] + src_box[i, 1] * variances[0] * priors[i, 3];
				float width = priors[i, 2] * (float)Math.Exp(src_box[i, 2] * variances[1]);
				float height = priors[i, 3] * (float)Math.Exp(src_box[i, 3] * variances[1]);

				dst_box[i, 0] = center_x - width / 2;
				dst_box[i, 1] = center_y - height / 2;
				dst_box[i, 2] = center_x + width / 2; 
				dst_box[i, 3] = center_y + height / 2;
			}

			return dst_box;
		}


		private float[] DecodeScore(float[,] src_score)
		{
			int numElements = src_score.GetLength(0);
			float[] dst_score = new float[numElements];

			for (int i = 0; i < numElements; i++)
			{
				dst_score[i] = src_score[i, 1];
			}

			return dst_score;
		}


		private float[,] DecodeLandmark(float[,] src_landmark, float[,] priors, float[] variances)
		{
			int numBoxes = priors.GetLength(0);
			float[,] dst_landmark = new float[numBoxes, 10];
			for (int i = 0; i < numBoxes; i++)
			{
				for (int j = 0; j < NUM_KEYPOINTS; j++)
				{
					dst_landmark[i, 2 * j] = priors[i, 0] + src_landmark[i, 2 * j] * variances[0] * priors[i, 2];
					dst_landmark[i, 2 * j + 1] = priors[i, 1] + src_landmark[i, 2 * j + 1] * variances[0] * priors[i, 3];
				}
			}

			return dst_landmark;
		}


		private float[,] Scale(float[,] src, int[] scale)
		{
			int src_width = src.GetLength(0);
			int src_height = src.GetLength(1);
			float[,] dst = new float[src_width, src_height];

			if(src_height != scale.Length){
				return null;
			}

			for (int i = 0; i < src_width; i++)
			{
				for (int j = 0; j < src_height; j++)
				{
					dst[i, j] = src[i, j] * scale[j];
				}
			}

			return dst;
		}


		private float[] Filter(float[] src, List<int> inds){

			float[] dst = new float[inds.Count];
			for (int i = 0; i < inds.Count; i++)
			{
				int index = inds[i];
				dst[i] = src[index];
			}

			return dst;
		}


		private float[,] Filter(float[,] src, List<int> inds){

			int src_element_num = src.GetLength(1);

			float[,] dst = new float[inds.Count, src_element_num];

			for (int i = 0; i < inds.Count; i++)
			{
				int index = inds[i];
				for (int j = 0; j < src_element_num; j++)
				{
					dst[i, j] = src[index, j];
				}
			}

			return dst;

		}


		private float[] KeepTopKBeforeNMS(float[] src, int[] order, int top_k)
		{
			float[] dst = new float[top_k];

			for (int i = 0; i < top_k; i++)
			{
				int index = order[i];
				dst[i] = src[index];
			}

			return dst;
		}


		private float[,] KeepTopKBeforeNMS(float[,] src, int[] order, int top_k)
		{
			int src_element_num = src.GetLength(1);
			
			float[,] dst = new float[top_k, src_element_num];

			for (int i = 0; i < top_k; i++)
			{
				int index = order[i];
				for (int j = 0; j < src_element_num; j++)
				{
					dst[i, j] = src[index, j];
				}
			}

			return dst;
		}

		
		private List<int> Nms(float[,] boxes, float[] scores, float thresh)
		{
			int numBoxes = boxes.GetLength(0);
			float[] x1 = new float[numBoxes];
			float[] y1 = new float[numBoxes];
			float[] x2 = new float[numBoxes];
			float[] y2 = new float[numBoxes];
			for (int i = 0; i < numBoxes; i++)
			{
				x1[i] = boxes[i, 0];
				y1[i] = boxes[i, 1];
				x2[i] = boxes[i, 2];
				y2[i] = boxes[i, 3];
			}

			float[] areas = new float[numBoxes];
			for (int i = 0; i < numBoxes; i++)
			{
				areas[i] = (x2[i] - x1[i] + 1) * (y2[i] - y1[i] + 1);
			}

			int[] order = scores.Select((s, i) => new KeyValuePair<int, float>(i, s))
								.OrderByDescending(pair => pair.Value)
								.Select(pair => pair.Key).ToArray();

			List<int> keep = new List<int>();
			while (order.Length > 0)
			{
				int i = order[0];
				keep.Add(i);

				List<float> xx1 = new List<float>();
				List<float> yy1 = new List<float>();
				List<float> xx2 = new List<float>();
				List<float> yy2 = new List<float>();

				for (int j = 1; j < order.Length; j++)
				{
					xx1.Add(Math.Max(x1[i], x1[order[j]]));
					yy1.Add(Math.Max(y1[i], y1[order[j]]));
					xx2.Add(Math.Min(x2[i], x2[order[j]]));
					yy2.Add(Math.Min(y2[i], y2[order[j]]));
				}

				List<float> w = xx2.Select((x, idx) => Math.Max(0, x - xx1[idx] + 1)).ToList();
				List<float> h = yy2.Select((y, idx) => Math.Max(0, y - yy1[idx] + 1)).ToList();
				List<float> inter = new List<float>();
				for (int k = 0; k < w.Count; k++)
				{
					inter.Add(w[k] * h[k]);
				}

				List<int> inds = new List<int>();
				for (int k = 0; k < order.Length - 1; k++)
				{
					float ovr = inter[k] / (areas[i] + areas[order[k + 1]] - inter[k]);
					if (ovr <= thresh)
					{
						inds.Add(k);
					}
				}

				List<int> newOrder = new List<int>();
				foreach (int ind in inds)
				{
					newOrder.Add(order[ind + 1]);
				}
				order = newOrder.ToArray();
			}
			return keep;
		}
		
	}
}