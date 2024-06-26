﻿/* AILIA Unity Plugin FaceMeshV2 Sample */
/* Copyright 2023 AXELL CORPORATION */

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
	public class AiliaFaceMeshV2
	{
		public const int NUM_KEYPOINTS = 478;
		public const int DETECTION_WIDTH = 256;
		public const int DETECTION_HEIGHT = 256;
		public const int BLENDSHAPE_COUNT = 52;

		private const float DSCALE = 1.5f;

		// Blenshape Label
		public static string [] BlendshapeLabels = {
			"_neutral",
			"browDownLeft",
			"browDownRight",
			"browInnerUp",
			"browOuterUpLeft",
			"browOuterUpRight",
			"cheekPuff",
			"cheekSquintLeft",
			"cheekSquintRight",
			"eyeBlinkLeft",
			"eyeBlinkRight",
			"eyeLookDownLeft",
			"eyeLookDownRight",
			"eyeLookInLeft",
			"eyeLookInRight",
			"eyeLookOutLeft",
			"eyeLookOutRight",
			"eyeLookUpLeft",
			"eyeLookUpRight",
			"eyeSquintLeft",
			"eyeSquintRight",
			"eyeWideLeft",
			"eyeWideRight",
			"jawForward",
			"jawLeft",
			"jawOpen",
			"jawRight",
			"mouthClose",
			"mouthDimpleLeft",
			"mouthDimpleRight",
			"mouthFrownLeft",
			"mouthFrownRight",
			"mouthFunnel",
			"mouthLeft",
			"mouthLowerDownLeft",
			"mouthLowerDownRight",
			"mouthPressLeft",
			"mouthPressRight",
			"mouthPucker",
			"mouthRight",
			"mouthRollLower",
			"mouthRollUpper",
			"mouthShrugLower",
			"mouthShrugUpper",
			"mouthSmileLeft",
			"mouthSmileRight",
			"mouthStretchLeft",
			"mouthStretchRight",
			"mouthUpperUpLeft",
			"mouthUpperUpRight",
			"noseSneerLeft",
			"noseSneerRight"
		};

		// Input idxs for blendshape
		private int [] LandmarksSubsetIdxs = {
			0, 1, 4, 5, 6, 7, 8, 10, 13, 14, 17, 21, 33, 37, 39,
			40, 46, 52, 53, 54, 55, 58, 61, 63, 65, 66, 67, 70, 78, 80,
			81, 82, 84, 87, 88, 91, 93, 95, 103, 105, 107, 109, 127, 132, 133,
			136, 144, 145, 146, 148, 149, 150, 152, 153, 154, 155, 157, 158, 159, 160,
			161, 162, 163, 168, 172, 173, 176, 178, 181, 185, 191, 195, 197, 234, 246,
			249, 251, 263, 267, 269, 270, 276, 282, 283, 284, 285, 288, 291, 293, 295,
			296, 297, 300, 308, 310, 311, 312, 314, 317, 318, 321, 323, 324, 332, 334,
			336, 338, 356, 361, 362, 365, 373, 374, 375, 377, 378, 379, 380, 381, 382,
			384, 385, 386, 387, 388, 389, 390, 397, 398, 400, 402, 405, 409, 415, 454,
			466, 468, 469, 470, 471, 472, 473, 474, 475, 476, 477
		};

		public struct FaceMeshV2Info
		{
			public float width;
			public float height;
			public Vector2[] keypoints;
			public Vector2 center;
			public float theta;
			public float[] blendshape;
		}

		// Warp
		private float[] WarpPerspective(AiliaBlazeface.FaceInfo face, float theta, Color32[] camera, int tex_width, int tex_height){
			//extract roi
			int fw = (int)(face.width * tex_width * DSCALE);
			int fh = (int)(face.height * tex_height * DSCALE);
			int fx = (int)(face.center.x * tex_width);
			int fy = (int)(face.center.y * tex_height);

			//extract data
			float[] data = new float[DETECTION_WIDTH * DETECTION_HEIGHT * 3];
			int w = DETECTION_WIDTH;
			int h = DETECTION_HEIGHT;
			float scale = 1.0f * fw / w;
			float ss=(float)System.Math.Sin(theta);
			float cs=(float)System.Math.Cos(theta);
			for (int y = 0; y < h; y++)
			{
				for (int x = 0; x < w; x++)
				{
					int ox = (x - w/2);
					int oy = (y - h/2);
					int x2 = (int)((ox *  cs + oy * -ss) * scale + fx);
					int y2 = (int)((ox *  ss + oy *  cs) * scale + fy);
					if (x2 < 0 || y2 < 0 || x2 >= tex_width || y2 >= tex_height)
					{
						data[(y * w + x) * 3 + 0] = 0;
						data[(y * w + x) * 3 + 1] = 0;
						data[(y * w + x) * 3 + 2] = 0;
						continue;
					}
					data[(y * w + x) * 3 + 0] = (float)((camera[(tex_height - 1 - y2) * tex_width + x2].r) / 255.0);
					data[(y * w + x) * 3 + 1] = (float)((camera[(tex_height - 1 - y2) * tex_width + x2].g) / 255.0);
					data[(y * w + x) * 3 + 2] = (float)((camera[(tex_height - 1 - y2) * tex_width + x2].b) / 255.0);
				}
			}
			return data;
		}

		// Update is called once per frame
		public List<FaceMeshV2Info> Detection(AiliaModel ailia_model,AiliaModel blendshape_model, Color32[] camera, int tex_width, int tex_height, List<AiliaBlazeface.FaceInfo> result_detections, bool debug=false)
		{
			List<FaceMeshV2Info> result = new List<AiliaFaceMeshV2.FaceMeshV2Info>();
			for (int i = 0; i < result_detections.Count; i++)
			{
				//getface
				AiliaBlazeface.FaceInfo face = result_detections[i];
				int w = DETECTION_WIDTH;
				int h = DETECTION_HEIGHT;
				const int RIGHT_EYE=0;
				const int LEFT_EYE=1;
				float theta_x = (float)(face.keypoints[LEFT_EYE].x - face.keypoints[RIGHT_EYE].x);
				float theta_y = (float)(face.keypoints[LEFT_EYE].y - face.keypoints[RIGHT_EYE].y);
				float theta = (float)System.Math.Atan2(theta_y,theta_x);
				float [] data = WarpPerspective(face, theta, camera, tex_width, tex_height);

				//debug display data
				if(debug){
					for (int y = 0; y < h; y++)
					{
						for (int x = 0; x < w; x++)
						{
							camera[tex_width*(tex_height-1-y)+x].r = (byte)((data[(y * w + x) * 3 + 0]) * 255.0);
							camera[tex_width*(tex_height-1-y)+x].g = (byte)((data[(y * w + x) * 3 + 1]) * 255.0);
							camera[tex_width*(tex_height-1-y)+x].b = (byte)((data[(y * w + x) * 3 + 2]) * 255.0);
						}
					}
				}

				//set input shape
				uint[] input_blobs = ailia_model.GetInputBlobList();
				bool success = true;
				if (input_blobs != null)
				{
					Ailia.AILIAShape input_shape = new Ailia.AILIAShape();
					input_shape.x=3;
					input_shape.y=(uint)DETECTION_WIDTH;
					input_shape.z=(uint)DETECTION_HEIGHT;
					input_shape.w=1;
					input_shape.dim=4;

					success = ailia_model.SetInputBlobShape(input_shape, (int)input_blobs[0]);
					if (!success){
						Debug.Log("SetInputBlobShape failed");
					}
				}

				//compute
				float [] output = new float [NUM_KEYPOINTS * 3];
				success = ailia_model.Predict(output,data);
				if (!success)
				{
					Debug.Log("Can not Predict");
				}

				//object
				FaceMeshV2Info facemesh_info = new FaceMeshV2Info();
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

				//Blendshape
				int fw = (int)(face.width * tex_width * DSCALE);
				float scale = 1.0f * fw / w;
				float ss=(float)System.Math.Sin(facemesh_info.theta);
				float cs=(float)System.Math.Cos(facemesh_info.theta);
				float [] landmarks = new float[LandmarksSubsetIdxs.Length * 2];
				for (int j = 0; j < LandmarksSubsetIdxs.Length; j++){
					int k = LandmarksSubsetIdxs[j];
					float x = (facemesh_info.center.x * tex_width  + ((facemesh_info.keypoints[k].x - DETECTION_WIDTH/2) * cs + (facemesh_info.keypoints[k].y - DETECTION_HEIGHT/2) * -ss)* scale);
					float y = (facemesh_info.center.y * tex_height + ((facemesh_info.keypoints[k].x - DETECTION_WIDTH/2) * ss + (facemesh_info.keypoints[k].y - DETECTION_HEIGHT/2) *  cs)* scale);
					landmarks[j*2+0] = x;
					landmarks[j*2+1] = y;
				}
				facemesh_info.blendshape = new float [BLENDSHAPE_COUNT];
				blendshape_model.Predict(facemesh_info.blendshape, landmarks);
		
				result.Add(facemesh_info);
			}
			return result;
		}
	}
}