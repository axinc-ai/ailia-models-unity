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

using ailia;

namespace ailiaSDK
{
	public class AiliaBlazehand
	{
		// Constant Parameters
		AiliaBlazehandAnchors anchors_holder = new AiliaBlazehandAnchors();

		public const int DETECTION_WIDTH = 256;
		public const int DETECTION_HEIGHT = 256;
		public const int PALM_NUM_KEYPOINTS = 7;
		public const int HAND_NUM_KEYPOINTS = 21;
		public const int HAND_NUM_PARTIAL_LANDMARKS = 12;
		private int[] PARTIAL_LANDMARKS_ID = new int[] {0, 1, 2, 3, 5, 6, 9, 10, 13, 14, 17, 18};
		public const int HAND_NUM_CONNECTIONS = 21;
		public static int[,] HAND_CONNECTIONS = new int[,]{
			{0, 1}, {1, 2}, {2, 3}, {3, 4}, {5, 6}, {6, 7}, {7, 8}, {9, 10}, {10, 11}, {11, 12}, {13, 14}, {14, 15}, {15, 16}, {17, 18}, {18, 19}, {19, 20}, {0, 5}, {5, 9}, {9, 13}, {13, 17}, {0, 17},
		};

		public const int KP1 = 0;
		public const int KP2 = 2;
		public const float THETA0 = (float)(System.Math.PI / 2.0f);
		public const float DC = 0.1f;
		public const float DC2 = 30.0f;
		public const float DSCALE = 2.6f;

		// Parameters for Main()
		public static bool tracking = false;
		private bool usedBlazepalm = false;
		private float[] tracked_hands = new float[] {0.0f, 0.0f};

		// Parameters for ROI
		private int pre_hands_num = 0;
		private float[] pre_width = new float[2];
		private float[] pre_height = new float[2];
		private float[] pre_theta = new float[2];
		private Vector2[] pre_center = new Vector2[2];

		// Information of palm detected by blazepalm
		public struct PalmInfo
		{
			public float width;
			public float height;
			public float theta;
			public Vector2 center;
			public float score;
			public Vector2[] keypoints;
		}

		// Information of hand detected by blazehand
		public struct HandInfo
		{
				public float width;
				public float height;
				public float theta;
				public Vector2 center;
				public float hand_flag;
				public float handed;
				public Vector2[] keypoints;
				public Vector2[] landmarks;
		}



		public List<HandInfo> Main(AiliaModel ailia_palm_detector, AiliaModel ailia_hand_detector, Color32[] camera, int tex_width, int tex_height)
		{
			float THRESH = 0.5f;

			float[] input_data = PalmPreProcess(camera, tex_width, tex_height);
			int num_detected;

			// Perform palm detection on 1st frame and if at least 1 hand has low confidence (not detected)
			List<PalmInfo> palms = null;
			if (tracked_hands[0] < THRESH || tracked_hands[1] < THRESH)
			{
				tracking = false;
				// blazepalm
				palms = PalmDetection(ailia_palm_detector, input_data, tex_width, tex_height);
				num_detected = palms.Count;
				if(num_detected > 0)
				{
					tracking = true;
					usedBlazepalm = true;
				}
			}
			else
			{
				num_detected = pre_hands_num;
				usedBlazepalm = false;
			}

			if (tracking)
			{
				pre_hands_num = 0;
				tracked_hands[0] = 0.0f;
				tracked_hands[1] = 0.0f;

				// Detect hand landmarks for each palm
				List<HandInfo> results = new List<HandInfo>();
				if(num_detected > 2)
				{
					num_detected = 2;
				}
				for (int i = 0; i < num_detected; i++)
				{
					int w = DETECTION_WIDTH;

					float width;
					float height;
					float theta;
					Vector2 center;
					float scale;

					if(usedBlazepalm)
					{
						// information for ROI from blazepalm
						PalmInfo palm = palms[i];
						width = palm.width;
						height = palm.height;
						theta = palm.theta;
						center.x = palm.center.x;
						center.y = palm.center.y;

						int fw = (int)(palm.width * tex_width * DSCALE);
						scale = 1.0f * fw / w;
					}
					else
					{
						// information for ROI from blazehand
						width = pre_width[i];
						height = pre_height[i];
						theta = pre_theta[i];
						center.x = pre_center[i].x;
						center.y = pre_center[i].y;

						int fw = (int)(pre_width[i] * tex_width * DSCALE);
						scale = 1.0f * fw / w;
					}

					float[] roi = HandPreProcess(camera, tex_width, tex_height, center, theta, scale);

					// blazehand
					HandInfo hand = HandDetection(ailia_hand_detector, roi, tex_width, tex_height, center, width, height, theta);

					tracked_hands[i] = hand.hand_flag;
					pre_width[i] = hand.width;
					pre_height[i] = hand.height;
					pre_theta[i] = hand.theta;
					pre_center[i] = new Vector2(hand.center.x, hand.center.y);

					results.Add(hand);
				}
				return results;
			}
			return null;
		}



		float[] PalmPreProcess(Color32[] camera, int tex_width, int tex_height)
		{
			//Resize input data
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
			return data;
		}



		List<PalmInfo> PalmDetection(AiliaModel ailia_model, float[] data, int tex_width, int tex_height)
		{
			uint[] input_blobs = ailia_model.GetInputBlobList();
			if (input_blobs != null)
			{
				bool success = ailia_model.SetInputBlobData(data, (int)input_blobs[0]);
				if (!success)
				{
					Debug.Log("Can not SetInputBlobData for blazepalm");
				}

				ailia_model.Update();

				List<PalmInfo> detections = null;

				uint[] output_blobs = ailia_model.GetOutputBlobList();
				if (output_blobs != null && output_blobs.Length >= 2)
				{
					Ailia.AILIAShape box_shape = ailia_model.GetBlobShape((int)output_blobs[0]);
					Ailia.AILIAShape score_shape = ailia_model.GetBlobShape((int)output_blobs[1]);

					if (box_shape != null && score_shape != null)
					{
						float[] box_data = new float[box_shape.x * box_shape.y * box_shape.z * box_shape.w];
						float[] score_data = new float[score_shape.x * score_shape.y * score_shape.z * score_shape.w];

						if (ailia_model.GetBlobData(box_data, (int)output_blobs[0]) &&
								ailia_model.GetBlobData(score_data, (int)output_blobs[1]))
						{
							float aspect = (float)tex_width / tex_height;
							detections = PalmPostProcess(box_data, box_shape, score_data, score_shape, aspect);
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



		List<PalmInfo> PalmPostProcess(float[] box_data, Ailia.AILIAShape box_shape, float[] score_data, Ailia.AILIAShape score_shape, float aspect)
		{
			List<PalmInfo> results = new List<PalmInfo>();

			int w = DETECTION_WIDTH;
			int h = DETECTION_HEIGHT;
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
					PalmInfo palm = new PalmInfo();
					palm.score = score_data[y];
					palm.width = box_data[ib + 2] / w * anchor_scalew;
					palm.height = box_data[ib + 3] / h * anchor_scaleh * aspect;

					palm.keypoints = new Vector2[PALM_NUM_KEYPOINTS];
					for (int i = 0; i < PALM_NUM_KEYPOINTS; i++)
					{
						float kx = box_data[ib + 4 + i * 2 + 0] / w * anchor_scalew + anchor_offx;
						float ky = box_data[ib + 4 + i * 2 + 1] / h * anchor_scaleh + anchor_offy;
						palm.keypoints[i] = new Vector2(kx, ky * aspect);
					}

					// information for ROI
					float theta_x = (float)(palm.keypoints[KP1].x - palm.keypoints[KP2].x);
					float theta_y = (float)(palm.keypoints[KP1].y - palm.keypoints[KP2].y);
					palm.theta = (float)(System.Math.Atan2(theta_y,theta_x) - THETA0);

					float cx = box_data[ib + 0] / w * anchor_scalew + anchor_offx;
					float cy = box_data[ib + 1] / h * anchor_scaleh + anchor_offy;
					palm.center = new Vector2(cx + DC * (float)System.Math.Sin(palm.theta), cy * aspect - DC * (float)System.Math.Cos(palm.theta));

					results.Add(palm);
				}
			}
			return results;
		}

		List<PalmInfo> WeightedNonMaxSuppression(List<PalmInfo> palms)
		{
			List<PalmInfo> results = new List<PalmInfo>();

			float MIN_SUPPRESSION_THRESH = 0.3f;
			while (palms.Count > 0)
			{
				PalmInfo maxScoreHand = palms[0];
				for (int i = 1; i < palms.Count; i++)
				{
					if (palms[i].score > maxScoreHand.score)
					{
						maxScoreHand = palms[i];
					}
				}
				palms.Remove(maxScoreHand);
				results.Add(maxScoreHand);

				int numHands = palms.Count;
				for (int i = 0; i < numHands; i++)
				{
					float oiu = BBoxIoU(maxScoreHand.center.x, maxScoreHand.center.y, maxScoreHand.width, maxScoreHand.height,
						palms[i].center.x, palms[i].center.y, palms[i].width, palms[i].height);
					if (oiu > MIN_SUPPRESSION_THRESH)
					{
						palms.RemoveAt(i);
						i--;
						numHands--;
					}
				}
			}
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



		float[] HandPreProcess(Color32[] camera, int tex_width, int tex_height, Vector2 center, float theta, float scale)
		{
			float[] data = new float[DETECTION_WIDTH * DETECTION_HEIGHT * 3];
			int w = DETECTION_WIDTH;
			int h = DETECTION_HEIGHT;
			float ss=(float)System.Math.Sin(-theta);
			float cs=(float)System.Math.Cos(-theta);
			int fx = (int)(center.x * tex_width);
			int fy = (int)(center.y * tex_height);
			for (int y = 0; y < h; y++)
			{
				for (int x = 0; x < w; x++)
				{
					int ox = (x - w/2);
					int oy = (y - h/2);
					int x2 = (int)((ox *  cs + oy * ss) * scale + fx);
					int y2 = (int)((ox * -ss + oy * cs) * scale + fy);
					if (x2 < 0 || y2 < 0 || x2 >= tex_width || y2 >= tex_height)
					{
						data[(y * w + x) + 0 * w * h] = 0;
						data[(y * w + x) + 1 * w * h] = 0;
						data[(y * w + x) + 2 * w * h] = 0;
						continue;
					}
					data[(y * w + x) + 0 * w * h] = (float)((camera[(tex_height - 1 - y2) * tex_width + x2].r) / 255.0f);
					data[(y * w + x) + 1 * w * h] = (float)((camera[(tex_height - 1 - y2) * tex_width + x2].g) / 255.0f);
					data[(y * w + x) + 2 * w * h] = (float)((camera[(tex_height - 1 - y2) * tex_width + x2].b) / 255.0f);
				}
			}
			//debug display data
			// for (int y = 0; y < h; y++)
			// {
			// 	for (int x = 0; x < w; x++)
			// 	{
			// 		camera[tex_width*(tex_height-1-y)+x].r = (byte)((data[y * w + x + 0 * w * h]) * 255.0f);
			// 		camera[tex_width*(tex_height-1-y)+x].g = (byte)((data[y * w + x + 1 * w * h]) * 255.0f);
			// 		camera[tex_width*(tex_height-1-y)+x].b = (byte)((data[y * w + x + 2 * w * h]) * 255.0f);
			// 	}
			// }
			
			return data;
		}



		HandInfo HandDetection(AiliaModel ailia_model, float[] data, int tex_width, int tex_height
		, Vector2 center, float width, float height, float theta)
		{
			HandInfo detection = new HandInfo();
			uint[] input_blobs = ailia_model.GetInputBlobList();
			
			if (input_blobs != null)
			{
				bool success = ailia_model.SetInputBlobData(data, (int)input_blobs[0]);
				if (!success)
				{
					Debug.Log("Can not SetInputBlobData for blazehand");
				}

				ailia_model.Update();

				uint[] output_blobs = ailia_model.GetOutputBlobList();
				if (output_blobs != null && output_blobs.Length >= 3)
				{
					Ailia.AILIAShape confidence_score_shape = ailia_model.GetBlobShape((int)output_blobs[0]);
					Ailia.AILIAShape classification_score_shape = ailia_model.GetBlobShape((int)output_blobs[1]);
					Ailia.AILIAShape landmark_shape = ailia_model.GetBlobShape((int)output_blobs[2]);

					if (confidence_score_shape != null && classification_score_shape != null && landmark_shape != null)
					{
						float[] confidence_score_data = new float[confidence_score_shape.x * confidence_score_shape.y * confidence_score_shape.z * confidence_score_shape.w];
						float[] classification_score_data = new float[classification_score_shape.x * classification_score_shape.y * classification_score_shape.z * classification_score_shape.w];
						float[] landmark_data = new float[landmark_shape.x * landmark_shape.y * landmark_shape.z * landmark_shape.w];

						if (ailia_model.GetBlobData(confidence_score_data, (int)output_blobs[0]) &&
								ailia_model.GetBlobData(classification_score_data, (int)output_blobs[1]) &&
								ailia_model.GetBlobData(landmark_data , (int)output_blobs[2]))
						{
							detection = HandPostProcess(center, width, height, theta, confidence_score_data, confidence_score_shape, classification_score_data, classification_score_shape,landmark_data,landmark_shape,tex_width, tex_height);
						}
					}
				}
			}
			return detection;
		}



		HandInfo HandPostProcess(Vector2 center, float width, float height, float theta,float[] confidence_score_data, Ailia.AILIAShape confidence_score_shape, float[] classification_score_data, Ailia.AILIAShape classification_score_shape, float[] landmark_data, Ailia.AILIAShape landmark_shape, int tex_width, int tex_height)
		{
			HandInfo detection = new HandInfo();
			detection.keypoints = new Vector2[HAND_NUM_KEYPOINTS];
			for(int j = 0; j < HAND_NUM_KEYPOINTS; j++)
			{
				detection.keypoints[j]=new Vector2(landmark_data[j*3+0]*256,landmark_data[j*3+1]*256);
			}

			// denormalized landmarks
			detection.landmarks = new Vector2[HAND_NUM_KEYPOINTS];

			int fw = (int)(width * DSCALE * tex_width);
			float scale = 1.0f * fw / DETECTION_WIDTH;
			float ss=(float)System.Math.Sin(theta);
			float cs=(float)System.Math.Cos(theta);
			for (int k = 0; k < HAND_NUM_KEYPOINTS; k++)
			{
				int x = (int)(center.x * tex_width  + ((detection.keypoints[k].x - DETECTION_WIDTH/2) * cs + (detection.keypoints[k].y - DETECTION_HEIGHT/2) * -ss) * scale);
				int y = (int)(center.y * tex_height + ((detection.keypoints[k].x - DETECTION_WIDTH/2) * ss + (detection.keypoints[k].y - DETECTION_HEIGHT/2) * cs) * scale);
				detection.landmarks[k]=new Vector2(x,y);
			}
		
			// score
			detection.hand_flag = confidence_score_data[0];
			detection.handed = classification_score_data[0];

			// information for ROI
			Vector2[] partial_landmarks = new Vector2[HAND_NUM_PARTIAL_LANDMARKS];
			for (int j = 0; j < HAND_NUM_PARTIAL_LANDMARKS; j++)
			{
				partial_landmarks[j]= new Vector2(detection.landmarks[PARTIAL_LANDMARKS_ID[j]].x, detection.landmarks[PARTIAL_LANDMARKS_ID[j]].y);
			}

			Vector2 partial_landmarks_min = new Vector2(tex_width * 100.0f, tex_width * 100.0f);
			Vector2 partial_landmarks_max = new Vector2(-tex_width * 100.0f, -tex_width * 100.0f);
			for (int j = 0; j < HAND_NUM_PARTIAL_LANDMARKS; j++)
			{
				partial_landmarks_min.x = Math.Min(partial_landmarks_min.x, partial_landmarks[j].x);
				partial_landmarks_min.y = Math.Min(partial_landmarks_min.y, partial_landmarks[j].y);
				partial_landmarks_max.x = Math.Max(partial_landmarks_max.x, partial_landmarks[j].x);
				partial_landmarks_max.y = Math.Max(partial_landmarks_max.y, partial_landmarks[j].y);
			}

			float rotation = ComputeRotation(partial_landmarks);
			float aspect = (float)tex_width / tex_height;
			float cx = (partial_landmarks_min.x + partial_landmarks_max.x) / 2.0f + DC2 * (float)System.Math.Sin(rotation);
			float cy = ((partial_landmarks_min.y + partial_landmarks_max.y) / 2.0f) * aspect  - DC2 * (float)System.Math.Cos(rotation);

			float partial_landmarks_width = partial_landmarks_max.x - partial_landmarks_min.x;
			float partial_landmarks_height = partial_landmarks_max.y - partial_landmarks_min.y;
			float partial_landmarks_size;
			partial_landmarks_size = (partial_landmarks_width + partial_landmarks_height) / 2.0f;

			detection.width = partial_landmarks_size/ tex_width;
			detection.height = partial_landmarks_size / tex_height;
			detection.theta = rotation;
			detection.center = new Vector2(cx / tex_width, cy / tex_height);

			pre_hands_num++;

			return detection;
		}

		float ComputeRotation (Vector2[] partial_landmarks)
		{
			int kWristJoint = 0;
			int kIndexFingerPIPJoint = 4;
			int kMiddleFingerPIPJoint = 6;
			int kRingFingerPIPJoint = 8;

			float rotation_x0 = partial_landmarks[kWristJoint].x;
			float rotation_y0 = partial_landmarks[kWristJoint].y;

			float rotation_x1 = partial_landmarks[kIndexFingerPIPJoint].x / 4.0f + partial_landmarks[kMiddleFingerPIPJoint].x / 2.0f + partial_landmarks[kRingFingerPIPJoint].x / 4.0f;
			float rotation_y1 = partial_landmarks[kIndexFingerPIPJoint].y / 4.0f + partial_landmarks[kMiddleFingerPIPJoint].y / 2.0f + partial_landmarks[kRingFingerPIPJoint].y / 4.0f;

			float rotation = (float)(System.Math.Atan2(rotation_y1-rotation_y0,rotation_x1-rotation_x0) + THETA0);

			return rotation;
		}
	}
}