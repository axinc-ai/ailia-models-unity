/* AILIA Unity Plugin FaceMesh Sample */
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
	public class AiliaFaceMesh
	{
		public const int NUM_KEYPOINTS = 468;
		public const int DETECTION_WIDTH = 192;
		public const int DETECTION_HEIGHT = 192;

		private const float DSCALE = 1.5f;

		public struct FaceMeshInfo
		{
			public float width;
			public float height;
			public Vector2[] keypoints;
			public Vector2 center;
			public float theta;
		}

		// Update is called once per frame
		public List<FaceMeshInfo> Detection(AiliaModel ailia_model, Color32[] camera, int tex_width, int tex_height, List<AiliaBlazeface.FaceInfo> result_detections, bool debug=false)
		{
			List<FaceMeshInfo> result = new List<FaceMeshInfo>();
			for (int i = 0; i < result_detections.Count; i++)
			{
				//extract roi
				AiliaBlazeface.FaceInfo face = result_detections[i];
				int fw = (int)(face.width * tex_width * DSCALE);
				int fh = (int)(face.height * tex_height * DSCALE);
				int fx = (int)(face.center.x * tex_width);
				int fy = (int)(face.center.y * tex_height);
				const int RIGHT_EYE=0;
				const int LEFT_EYE=1;
				float theta_x = (float)(face.keypoints[LEFT_EYE].x - face.keypoints[RIGHT_EYE].x);
				float theta_y = (float)(face.keypoints[LEFT_EYE].y - face.keypoints[RIGHT_EYE].y);
				float theta = (float)System.Math.Atan2(theta_y,theta_x);

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
						data[(y * w + x) + 0 * w * h] = (float)((camera[(tex_height - 1 - y2) * tex_width + x2].r) / 127.5 - 1.0);
						data[(y * w + x) + 1 * w * h] = (float)((camera[(tex_height - 1 - y2) * tex_width + x2].g) / 127.5 - 1.0);
						data[(y * w + x) + 2 * w * h] = (float)((camera[(tex_height - 1 - y2) * tex_width + x2].b) / 127.5 - 1.0);
					}
				}

				//debug display data
				if(debug){
					for (int y = 0; y < h; y++)
					{
						for (int x = 0; x < w; x++)
						{
							camera[tex_width*(tex_height-1-y)+x].r = (byte)((data[y * w + x + 0 * w * h] + 1.0) * 127.5);
							camera[tex_width*(tex_height-1-y)+x].g = (byte)((data[y * w + x + 1 * w * h] + 1.0) * 127.5);
							camera[tex_width*(tex_height-1-y)+x].b = (byte)((data[y * w + x + 2 * w * h] + 1.0) * 127.5);
						}
					}
				}

				//compute
				float [] output = new float [NUM_KEYPOINTS * 3];
				bool success = ailia_model.Predict(output,data);
				if (!success)
				{
					Debug.Log("Can not Predict");
				}

				//object
				FaceMeshInfo facemesh_info = new FaceMeshInfo();
				facemesh_info.center = face.center;
				facemesh_info.theta = theta;
				facemesh_info.width = face.width * DSCALE;
				facemesh_info.height = face.height * DSCALE;
				facemesh_info.keypoints = new Vector2[NUM_KEYPOINTS];
				for(int j=0;j<NUM_KEYPOINTS;j++){
					facemesh_info.keypoints[j]=new Vector2(output[j*3+0],output[j*3+1]);
				}

				//display
				if(debug){
					for(int j=0;j<NUM_KEYPOINTS;j++){
						int x = (int)(output[j*3+0]);
						int y = (int)(output[j*3+1]);
						if(x>=0 && y>=0 && x<=w && y<=h){
							camera[tex_width*(tex_height-1-y)+x].r = 0;
							camera[tex_width*(tex_height-1-y)+x].g = 255;
							camera[tex_width*(tex_height-1-y)+x].b = 0;
						}
					}
				}

				result.Add(facemesh_info);
			}

			return result;
		}

	}
}