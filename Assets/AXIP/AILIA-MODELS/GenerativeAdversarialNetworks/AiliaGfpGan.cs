/* AILIA Unity Plugin GFPGAN Sample */
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
	public class AiliaGfpGan
	{
		private const int DETECTION_WIDTH = 512;
		private const int DETECTION_HEIGHT = 512;
		private const float DSCALE = 2.0f;

		// Bilinear
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

		// Crop input data
		private float [] CropInputFace(AiliaBlazeface.FaceInfo face, Color32[] camera, int tex_width, int tex_height){
			//extract roi
			int fw = (int)(face.width * tex_width * DSCALE);
			int fh = (int)(face.height * tex_height * DSCALE);
			int fx = (int)(face.center.x * tex_width);
			int fy = (int)(face.center.y * tex_height);
			const int RIGHT_EYE=0;
			const int LEFT_EYE=1;
			float theta_x = (float)(face.keypoints[LEFT_EYE].x - face.keypoints[RIGHT_EYE].x);
			float theta_y = (float)(face.keypoints[LEFT_EYE].y - face.keypoints[RIGHT_EYE].y);
			float theta = (float)System.Math.Atan2(theta_y,theta_x);

			//get face image
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
					float x2 = (float)((ox *  cs + oy * ss) * scale + fx);
					float y2 = (float)((ox * -ss + oy * cs) * scale + fy);
					y2 = tex_height - 1 - y2; // vertical flipped to normal
					if (x2 < 0 || y2 < 0 || x2 >= tex_width || y2 >= tex_height)
					{
						data[(y * w + x) + 0 * w * h] = 0;
						data[(y * w + x) + 1 * w * h] = 0;
						data[(y * w + x) + 2 * w * h] = 0;
						continue;
					}
					Color32 v = Bilinear(camera, tex_width, tex_height, x2, y2);
					data[(y * w + x) + 0 * w * h] = (float)(v.r / 127.5 - 1.0);
					data[(y * w + x) + 1 * w * h] = (float)(v.g / 127.5 - 1.0);
					data[(y * w + x) + 2 * w * h] = (float)(v.b / 127.5 - 1.0);
				}
			}
			return data;
		}

		// Blend boundary
		void Blend(float[] output, float[] input, bool debug){
			int w = DETECTION_WIDTH;
			int h = DETECTION_HEIGHT;
			for (int y = 0; y < h; y++)
			{
				for (int x = 0; x < w; x++)
				{
					int rx = DETECTION_WIDTH / 16;
					int ry = DETECTION_HEIGHT / 16;
					float dx = Mathf.Min((float)Mathf.Min(x, (DETECTION_WIDTH - 1 - x)) / rx, 1);
					float dy = Mathf.Max(Mathf.Min((float)Mathf.Min(y, (DETECTION_HEIGHT - 1 - y)) / ry, 1) ,0);
					float alpha = Mathf.Min(dx, dy);
					for (int c = 0; c < 3; c++)
					{
						output[(y * w + x) + c * w * h] = alpha * output[(y * w + x) + c * w * h] + (1 - alpha) * input[(y * w + x) + c * w * h];
						if (debug && alpha != 1){
							output[(y * w + x) + c * w * h] = 0;
						}
					}
				}
			}
		}

		// Clip
		byte Clip(float src){
			float v = (src + 1) * 127.5f;
			v = Mathf.Min(Mathf.Max(v, 0), 255);
			return (byte)v;
		}

		// Output
		private float [] Composite(AiliaBlazeface.FaceInfo face, Color32[] camera, float[] data, int tex_width, int tex_height){
			//extract roi
			int fw = (int)(face.width * tex_width * DSCALE);
			int fh = (int)(face.height * tex_height * DSCALE);
			int fx = (int)(face.center.x * tex_width);
			int fy = (int)(face.center.y * tex_height);
			const int RIGHT_EYE=0;
			const int LEFT_EYE=1;
			float theta_x = (float)(face.keypoints[LEFT_EYE].x - face.keypoints[RIGHT_EYE].x);
			float theta_y = (float)(face.keypoints[LEFT_EYE].y - face.keypoints[RIGHT_EYE].y);
			float theta = (float)System.Math.Atan2(theta_y,theta_x);

			//put face image
			int w = DETECTION_WIDTH;
			int h = DETECTION_HEIGHT;
			float scale = 1.0f * fw / w;
			float ss=(float)System.Math.Sin(-theta);
			float cs=(float)System.Math.Cos(-theta);

			float [] average = new float [tex_width * tex_height * 3];
			int [] cnt = new int [tex_width * tex_height];
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
						continue;
					}

					average[((tex_height - 1 - y2) * tex_width + x2) * 3 + 0] += Clip(data[(y * w + x) + 0 * w * h]);
					average[((tex_height - 1 - y2) * tex_width + x2) * 3 + 1] += Clip(data[(y * w + x) + 1 * w * h]);
					average[((tex_height - 1 - y2) * tex_width + x2) * 3 + 2] += Clip(data[(y * w + x) + 2 * w * h]);

					cnt[((tex_height - 1 - y2) * tex_width + x2)] += 1;
				}
			}

			for (int y = 0; y < tex_height; y++)
			{
				for (int x = 0; x < tex_width; x++)
				{
					int c = cnt[y * tex_width + x];
					if (c == 0)
					{
						continue;
					}

					camera[y * tex_width + x].r = (byte)(average[(y * tex_width + x) * 3 + 0] / c);
					camera[y * tex_width + x].g = (byte)(average[(y * tex_width + x) * 3 + 1] / c);
					camera[y * tex_width + x].b = (byte)(average[(y * tex_width + x) * 3 + 2] / c);
				}
			}

			return data;
		}

		// Update is called once per frame
		public Color32[] GenerateImage(AiliaModel ailia_model, Color32[] camera, int tex_width, int tex_height, List<AiliaBlazeface.FaceInfo> result_detections, bool debug=false)
		{
			// create base images
			Color32[] result = new Color32 [camera.Length];
			for (int i = 0; i < result.Length; i++){
				result[i] = camera[i];
			}
			
			for (int i = 0; i < result_detections.Count; i++)
			{
				//extract roi
				AiliaBlazeface.FaceInfo face = result_detections[i];
				int fw = (int)(face.width * tex_width);
				int fh = (int)(face.height * tex_height);
				int fx = (int)(face.center.x * tex_width);
				int fy = (int)(face.center.y * tex_height);
				int w = DETECTION_WIDTH;
				int h = DETECTION_HEIGHT;

				//extract data
				float [] data = CropInputFace(face, camera, tex_width, tex_height);

				//debug display data
				if(debug){
					for (int y = 0; y < h; y++)
					{
						for (int x = 0; x < w; x++)
						{
							if (x < tex_width && y < tex_height){
								result[tex_width*(tex_height-1-y)+x].r = (byte)((data[(y * w + x) + w * h * 0] + 1) * 127.5);
								result[tex_width*(tex_height-1-y)+x].g = (byte)((data[(y * w + x) + w * h * 1] + 1) * 127.5);
								result[tex_width*(tex_height-1-y)+x].b = (byte)((data[(y * w + x) + w * h * 2] + 1) * 127.5);
							}
						}
					}
				}

				//compute
				float [] output = new float [w * h * 3];
				ailia_model.Predict(output, data);

				//blend
				Blend(output, data, debug);

				//Write output
				Composite(face, result, output, tex_width, tex_height);
			}

			return result;
		}
	}
}