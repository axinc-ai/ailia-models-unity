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
  public class AiliaBlazehand
  {
    public const int NUM_KEYPOINTS = 21;
    public const int DETECTION_WIDTH = 256;
    public const int DETECTION_HEIGHT = 256;
    public static int[,] HAND_CONNECTIONS = new int[,]{
      {0, 1}, {1, 2}, {2, 3}, {3, 4}, {5, 6}, {6, 7}, {7, 8}, {9, 10}, {10, 11}, {11, 12}, {13, 14}, {14, 15}, {15, 16}, {17, 18}, {18, 19}, {19, 20}, {0, 5}, {5, 9}, {9, 13}, {13, 17}, {0, 17},
    };

    private const float DSCALE = 2.6f;

    public struct LandmarkInfo
    {
      public float width;
      public float height;
      public Vector2[] keypoints;
      public Vector2 center;
      public float theta;
      public float[] hand_flag;
      public float[] handed;
    }

    public List<LandmarkInfo> Detection(AiliaModel ailia_model, Color32[] camera, int tex_width, int tex_height, List<AiliaBlazepalm.HandInfo> result_detections)
    {
      List<LandmarkInfo> results = new List<LandmarkInfo>();
      for (int i = 0; i < result_detections.Count; i++)
			{
				//extract roi
				AiliaBlazepalm.HandInfo hand = result_detections[i];
				int fw = (int)(hand.width * tex_width * DSCALE);
				int fh = (int)(hand.height * tex_height * DSCALE);
				int fx = (int)(hand.center.x * tex_width);
				int fy = (int)(hand.center.y * tex_height);
				float theta_x = (float)(hand.keypoints[0].x - hand.keypoints[2].x);
				float theta_y = (float)(hand.keypoints[0].y - hand.keypoints[2].y);
				float theta = (float)(System.Math.Atan2(theta_y,theta_x) - System.Math.PI/2);

				//extract data
				float[] data = new float[DETECTION_WIDTH * DETECTION_HEIGHT * 3];
				int w = DETECTION_WIDTH;
				int h = DETECTION_HEIGHT;
				float scale = 1.0f * fw / w;
				float ss=(float)System.Math.Sin(-theta);
				float cs=(float)System.Math.Cos(-theta);
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
						data[(y * w + x) + 0 * w * h] = (float)((camera[(tex_height - 1 - y2) * tex_width + x2].r) / 255);
						data[(y * w + x) + 1 * w * h] = (float)((camera[(tex_height - 1 - y2) * tex_width + x2].g) / 255);
						data[(y * w + x) + 2 * w * h] = (float)((camera[(tex_height - 1 - y2) * tex_width + x2].b) / 255);
            // int ox = (x - w/2);
						// int oy = (y - h/2);
						// int x2 = (int)((ox *  cs + oy * -ss) * scale + fx);
						// int y2 = (int)((ox * ss + oy * cs) * scale + fy);
            // if (x2 < 0 || y2 < 0 || x2 >= tex_width || y2 >= tex_height)
						// {
						// 	data[(y * w + x) + 0 * w * h] = 0;
						// 	data[(y * w + x) + 1 * w * h] = 0;
						// 	data[(y * w + x) + 2 * w * h] = 0;
						// 	continue;
						// }
            
            // data[(y * w - x + w - 1) + 0 * w * h] = (float)((camera[(tex_height - 1 - y2) * tex_width + x2].r) / 255);
						// data[(y * w - x + w - 1) + 1 * w * h] = (float)((camera[(tex_height - 1 - y2) * tex_width + x2].g) / 255);
						// data[(y * w - x + w - 1) + 2 * w * h] = (float)((camera[(tex_height - 1 - y2) * tex_width + x2].b) / 255);
					}
				}
        
        LandmarkInfo detection = new LandmarkInfo();
        uint[] input_blobs = ailia_model.GetInputBlobList();
      
        if (input_blobs != null)
		  	{
          bool success = ailia_model.SetInputBlobData(data, (int)input_blobs[0]);
          if (!success)
          {
            Debug.Log("Can not SetInputBlobData");
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
                detection = PostProcess(hand, theta, confidence_score_data, confidence_score_shape, classification_score_data, classification_score_shape,landmark_data,landmark_shape, w, h);
              }
            }
          }
			  }
        results.Add(detection);
			}
      return results;
    }

    LandmarkInfo PostProcess(AiliaBlazepalm.HandInfo hand, float theta,  float[] confidence_score_data, Ailia.AILIAShape confidence_score_shape, float[] classification_score_data, Ailia.AILIAShape classification_score_shape, float[] landmark_data, Ailia.AILIAShape landmark_shape, int input_w, int input_h)
    {
      LandmarkInfo detection = new LandmarkInfo();

      detection.center.x = hand.center.x + 0.05f * DSCALE * (float)System.Math.Sin(theta);
      detection.center.y = hand.center.y - 0.05f * DSCALE * (float)System.Math.Cos(theta);
      detection.theta = theta;
      detection.width = hand.width * DSCALE;
      detection.height = hand.height * DSCALE;
      detection.keypoints = new Vector2[NUM_KEYPOINTS];
      for(int j=0;j<NUM_KEYPOINTS;j++){
        detection.keypoints[j]=new Vector2(landmark_data[j*3+0]*256,landmark_data[j*3+1]*256);
      }

      int num_hand = confidence_score_data.Length;
      detection.hand_flag =new float[num_hand];
      detection.handed =new float[num_hand];
      for(int j=0;j<num_hand;j++){
        detection.hand_flag[j] = confidence_score_data[j];
        detection.handed[j] = classification_score_data[j];
      }
      Debug.Log(detection.handed[0]);
      return detection;
    }
  }
}