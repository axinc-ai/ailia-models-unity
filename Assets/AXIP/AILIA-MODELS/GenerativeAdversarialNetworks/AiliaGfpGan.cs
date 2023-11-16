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

namespace ailiaSDK
{
	public class AiliaGfpGan
	{
		private const int DETECTION_WIDTH = 512;
		private const int DETECTION_HEIGHT = 512;

		// Crop input data
		private float [] CropInputFace(int fx, int fy, int fw, int fh, Color32[] camera, int tex_width, int tex_height){
			float[] data = new float[DETECTION_WIDTH * DETECTION_HEIGHT * 6];
			int w = DETECTION_WIDTH;
			int h = DETECTION_HEIGHT;
			float scale = 1.0f * fw / w;
			for (int y = 0; y < h; y++)
			{
				for (int x = 0; x < w; x++)
				{
					int ox = (x - w/2);
					int oy = (y - h/2);
					int x2 = (int)((ox) * scale + fx);
					int y2 = (int)((oy) * scale + fy);
					if (x2 < 0 || y2 < 0 || x2 >= tex_width || y2 >= tex_height)
					{
						data[(y * w + x) * 6 + 0] = 0;
						data[(y * w + x) * 6 + 1] = 0;
						data[(y * w + x) * 6 + 2] = 0;

						data[(y * w + x) * 6 + 3] = 0;
						data[(y * w + x) * 6 + 4] = 0;
						data[(y * w + x) * 6 + 5] = 0;
						continue;
					}

					// img
					data[(y * w + x) * 6 + 0] = (float)((camera[(tex_height - 1 - y2) * tex_width + x2].r) / 127.5 - 1);
					data[(y * w + x) * 6 + 1] = (float)((camera[(tex_height - 1 - y2) * tex_width + x2].g) / 127.5 - 1);
					data[(y * w + x) * 6 + 2] = (float)((camera[(tex_height - 1 - y2) * tex_width + x2].b) / 127.5 - 1);

					// img masked
					if (y >= h / 2){
						data[(y * w + x) * 6 + 3] = 0;
						data[(y * w + x) * 6 + 4] = 0;
						data[(y * w + x) * 6 + 5] = 0;
					}else{
						data[(y * w + x) * 6 + 3] = data[(y * w + x) * 6 + 0];
						data[(y * w + x) * 6 + 4] = data[(y * w + x) * 6 + 1];
						data[(y * w + x) * 6 + 5] = data[(y * w + x) * 6 + 2];
					}
				}
			}
			return data;
		}

		byte Blend(byte dst, float src, float alpha){
			float v = (float)dst * (1 - alpha) + src * alpha * 255;
			v = Mathf.Min(Mathf.Max(v, 0), 255);
			return (byte)v;
		}

		// Output
		private void Composite(int fx, int fy, int fw, int fh, Color32[] result, int tex_width, int tex_height, float [] output){
			for (int y = 0; y < fh; y++)
			{
				for (int x = 0; x < fw; x++)
				{
					int x2 = x * DETECTION_WIDTH / fw;
					int y2 = y * DETECTION_HEIGHT / fh;
					int x3 = fx - fw / 2 + x;
					int y3 = fy - fh / 2 + y;
					if (x3 >= 0 && x3 < tex_width && y3 >= 0 && y3 < tex_height){
						int rx = DETECTION_WIDTH / 16;
						int ry = DETECTION_HEIGHT / 16;
						float dx = Mathf.Min((float)Mathf.Min(x2, (DETECTION_WIDTH - 1 - x2)) / rx, 1);
						float dy = Mathf.Max(Mathf.Min((float)Mathf.Min(y2 - DETECTION_HEIGHT/2, (DETECTION_HEIGHT - 1 - y2)) / ry, 1) ,0);
						float alpha = Mathf.Min(dx, dy);
						int d_adr = (tex_height - 1 - y3) * tex_width + x3;
						result[d_adr].r = Blend(result[d_adr].r, output[(y2 * DETECTION_WIDTH + x2) * 3 + 0], alpha);
						result[d_adr].g = Blend(result[d_adr].g, output[(y2 * DETECTION_WIDTH + x2) * 3 + 1], alpha);
						result[d_adr].b = Blend(result[d_adr].b, output[(y2 * DETECTION_WIDTH + x2) * 3 + 2], alpha);
					}
				}
			}
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
				float [] data = CropInputFace(fx, fy, fw, fh, camera, tex_width, tex_height);

				//debug display data
				if(debug){
					for (int y = 0; y < h; y++)
					{
						for (int x = 0; x < w; x++)
						{
							result[tex_width*(tex_height-1-y)+x].r = (byte)((data[(y * w + x) * 6 + 0] + 1) * 127.5);
							result[tex_width*(tex_height-1-y)+x].g = (byte)((data[(y * w + x) * 6 + 1] + 1) * 127.5);
							result[tex_width*(tex_height-1-y)+x].b = (byte)((data[(y * w + x) * 6 + 2] + 1) * 127.5);

							result[tex_width*(tex_height-1-y)+x+w].r = (byte)((data[(y * w + x) * 6 + 3] + 1) * 127.5);
							result[tex_width*(tex_height-1-y)+x+w].g = (byte)((data[(y * w + x) * 6 + 4] + 1) * 127.5);
							result[tex_width*(tex_height-1-y)+x+w].b = (byte)((data[(y * w + x) * 6 + 5] + 1) * 127.5);
						}
					}
				}

				//compute
				float [] output = new float [w * h * 3];
				ailia_model.Predict(output, data);

				//display
				if(debug){
					for (int y = 0; y < h; y++)
					{
						for (int x = 0; x < w; x++)
						{
							result[tex_width*(tex_height-1-y)+x+w*2].r = (byte)((output[(y * w + x) * 3 + 0] ) * 255.0);
							result[tex_width*(tex_height-1-y)+x+w*2].g = (byte)((output[(y * w + x) * 3 + 1] ) * 255.0);
							result[tex_width*(tex_height-1-y)+x+w*2].b = (byte)((output[(y * w + x) * 3 + 2] ) * 255.0);
						}
					}
				}

				//Write output
				Composite(fx, fy, fw, fh, result, tex_width, tex_height, output);
			}

			return result;
		}
	}
}