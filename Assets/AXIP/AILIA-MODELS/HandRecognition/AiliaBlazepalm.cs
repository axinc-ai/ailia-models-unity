/* AILIA Unity Plugin Blazehand Sample */
/* Copyright 2022 AXELL CORPORATION */

using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Runtime.InteropServices;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace ailiaSDK
{
	public class AiliaBlazepalm
	{
		AiliaBlazepalmAnchors anchors_holder = new AiliaBlazepalmAnchors();
		private float thresh = 0.5f;
		private bool tracking = false;
		private float[] tracked_hands = new float[2] {0.0f, 0.0f};
		// private (float,float,float,float) rois;

		public const int NUM_KEYPOINTS = 7;
		public const int DETECTION_WIDTH = 256;
		public const int DETECTION_HEIGHT = 256;

		public struct HandInfo
		{
			public float score;
			public Vector2 center;
			public float width;
			public float height;
			public Vector2[] keypoints;
		}

		public List<HandInfo> Detection(AiliaModel ailia_model, Color32[] camera, int tex_width, int tex_height)
		{
			//リサイズ
			float[] data = new float[DETECTION_WIDTH * DETECTION_HEIGHT * 3];
			int w = DETECTION_WIDTH;
			int h = DETECTION_HEIGHT;
			float scale = 1.0f * tex_width / w;
			for (int y = 0; y < h; y++)
			{
				for (int x = 0; x < w; x++)
				{
					int y2 = (int)(1.0 * y * scale);
					int x2 = (int)(1.0 * x * scale);
					if (x2 < 0 || y2 < 0 || x2 >= tex_width || y2 >= tex_height)
					{
						data[(y * w + x) + 0 * w * h] = 0;
						data[(y * w + x) + 1 * w * h] = 0;
						data[(y * w + x) + 2 * w * h] = 0;
						continue;
					}
					data[(y * w + x) + 0 * w * h] = (float)((camera[(tex_height - 1 - y2) * tex_width + x2].r) / 255.0);
					data[(y * w + x) + 1 * w * h] = (float)((camera[(tex_height - 1 - y2) * tex_width + x2].g) / 255.0);
					data[(y * w + x) + 2 * w * h] = (float)((camera[(tex_height - 1 - y2) * tex_width + x2].b) / 255.0);
				}
			}

			// Palm detection
			uint[] input_blobs = ailia_model.GetInputBlobList();
			if (input_blobs != null)
			{
				bool success = ailia_model.SetInputBlobData(data, (int)input_blobs[0]);
				if (!success)
				{
					Debug.Log("Can not SetInputBlobData");
				}

				ailia_model.Update();

				List<HandInfo> detections = null;

				uint[] output_blobs = ailia_model.GetOutputBlobList();
				if (output_blobs != null && output_blobs.Length >= 2)
				{
					Ailia.AILIAShape box_shape = ailia_model.GetBlobShape((int)output_blobs[0]);
					Ailia.AILIAShape score_shape = ailia_model.GetBlobShape((int)output_blobs[1]);
					// Debug.Log("box_shape = " + box_shape.x + "," + box_shape.y + "," + box_shape.z + "," + box_shape.w);
					// Debug.Log("score_shape = " + score_shape.x + "," + score_shape.y + "," + score_shape.z + "," + score_shape.w);

					if (box_shape != null && score_shape != null)
					{
						float[] box_data = new float[box_shape.x * box_shape.y * box_shape.z * box_shape.w];
						float[] score_data = new float[score_shape.x * score_shape.y * score_shape.z * score_shape.w];

						if (ailia_model.GetBlobData(box_data, (int)output_blobs[0]) &&
								ailia_model.GetBlobData(score_data, (int)output_blobs[1]))
						{
							float aspect = (float)tex_width / tex_height;
							detections = PostProcess(box_data, box_shape, score_data, score_shape, w, h, aspect);
						}
					}
				}

				if (detections != null)
				{
					detections = WeightedNonMaxSuppression(detections);
					// Debug.Log("Num hands: " + detections.Count);
				}
				return detections;
			}
			
			return null;
		}

		List<HandInfo> PostProcess(float[] box_data, Ailia.AILIAShape box_shape, float[] score_data, Ailia.AILIAShape score_shape, int input_w, int input_h, float aspect)
		{
			List<HandInfo> results = new List<HandInfo>();

			const float SCORE_THRESH = 100.0f;
			const float MIN_SCORE_THRESH = 0.75f;
			int box_w = (int)box_shape.x;
			int box_h = (int)box_shape.y;

			for (int y = 0; y < box_h; y++)
			{
				score_data[y] = Mathf.Clamp(score_data[y], -SCORE_THRESH, SCORE_THRESH);
				score_data[y] = Sigmoid(score_data[y]);
				if (score_data[y] > MIN_SCORE_THRESH)
				{
					float anchor_offx = 0.0f;
					float anchor_offy = 0.0f;
					float anchor_scalew = 1.0f;
					float anchor_scaleh = 1.0f;
					double [] anchors = anchors_holder.anchors;
					if (anchors.Length == box_h * 4)
					{
						anchor_offx = (float)anchors[y * 4 + 0];
						anchor_offy = (float)anchors[y * 4 + 1];
						anchor_scalew = (float)anchors[y * 4 + 2];
						anchor_scaleh = (float)anchors[y * 4 + 3];
					}

					int ib = y * box_w;
					HandInfo hand = new HandInfo();
					hand.score = score_data[y];
					hand.width = box_data[ib + 2] / input_w * anchor_scalew;
					hand.height = box_data[ib + 3] / input_h * anchor_scaleh * aspect;
					float cx = box_data[ib + 0] / input_w * anchor_scalew + anchor_offx;
					float cy = box_data[ib + 1] / input_h * anchor_scaleh + anchor_offy;
					hand.center = new Vector2(cx, cy * aspect);

					hand.keypoints = new Vector2[NUM_KEYPOINTS];
					for (int i = 0; i < NUM_KEYPOINTS; i++)
					{
						float kx = box_data[ib + 4 + i * 2 + 0] / input_w * anchor_scalew + anchor_offx;
						float ky = box_data[ib + 4 + i * 2 + 1] / input_h * anchor_scaleh + anchor_offy;
						hand.keypoints[i] = new Vector2(kx, ky * aspect);
					}

					results.Add(hand);
				}
			}

			return results;
		}

		List<HandInfo> WeightedNonMaxSuppression(List<HandInfo> hands)
		{
			List<HandInfo> results = new List<HandInfo>();

			float MIN_SUPPRESSION_THRESH = 0.3f;
			while (hands.Count > 0)
			{
				// Debug.Log("hands.Count="+hands.Count);
				HandInfo maxScoreHand = hands[0];
				for (int i = 1; i < hands.Count; i++)
				{
					if (hands[i].score > maxScoreHand.score)
					{
						maxScoreHand = hands[i];
					}
				}
				hands.Remove(maxScoreHand);
				results.Add(maxScoreHand);

				int numHands = hands.Count;
				for (int i = 0; i < numHands; i++)
				{
					float oiu = BBoxIoU(maxScoreHand.center.x, maxScoreHand.center.y, maxScoreHand.width, maxScoreHand.height,
						hands[i].center.x, hands[i].center.y, hands[i].width, hands[i].height);
					if (oiu > MIN_SUPPRESSION_THRESH)
					{
						hands.RemoveAt(i);
						i--;
						numHands--;
					}
				}
			}
			// Debug.Log("results.Count="+results.Count);
			return results;
		}

		float Overlap(float x1, float w1, float x2, float w2)
		{
			float l1 = x1 - w1 / 2;
			float l2 = x2 - w2 / 2;
			float left = l1 > l2 ? l1 : l2;
			float r1 = x1 + w1 / 2;
			float r2 = x2 + w2 / 2;
			float right = r1 < r2 ? r1 : r2;
			return right - left;
		}
		float BoxIntersection(float box1_x, float box1_y, float box1_w, float box1_h,
													float box2_x, float box2_y, float box2_w, float box2_h)
		{
			float w = Overlap(box1_x, box1_w, box2_x, box2_w);
			float h = Overlap(box1_y, box1_h, box2_y, box2_h);
			if (w < 0 || h < 0) return 0;
			float area = w * h;
			return area;
		}
		float BoxUnion(float box1_x, float box1_y, float box1_w, float box1_h,
									 float box2_x, float box2_y, float box2_w, float box2_h)
		{
			float i = BoxIntersection(box1_x, box1_y, box1_w, box1_h, box2_x, box2_y, box2_w, box2_h);
			float u = box1_w * box1_h + box2_w * box2_h - i;
			return u;
		}
		float BBoxIoU(float box1_x, float box1_y, float box1_w, float box1_h,
									 float box2_x, float box2_y, float box2_w, float box2_h)
		{
			return BoxIntersection(box1_x, box1_y, box1_w, box1_h, box2_x, box2_y, box2_w, box2_h) /
							BoxUnion(box1_x, box1_y, box1_w, box1_h, box2_x, box2_y, box2_w, box2_h);
		}

		float Sigmoid(float x)
		{
			return 1 / (1 + Mathf.Exp(-x));
		}
	}
}