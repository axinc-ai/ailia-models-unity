/* AILIA Unity Plugin Camera Sample */
/* Copyright 2018-2019 AXELL CORPORATION */

using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Runtime.InteropServices;

using UnityEngine;
using UnityEngine.UI;

namespace ailiaSDK
{
	public class AiliaCamera
	{
		//Camera settings
		private int CAMERA_WIDTH = 1280;
		private int CAMERA_HEIGHT = 720;
		private int CAMERA_FPS = 30;

		//WebCamera Instance
		private WebCamTexture webcamTexture = null;

		// Texture buffer
		private Color32[] image = new Color32[0];
		private Color32[] crop = new Color32[0];
		private int crop_width = 16;
		private int crop_height = 16;
		private bool square = true;

		//Camera ID
		public void CreateCamera(int camera_id, bool set_square = true)
		{
			DestroyCamera();
			WebCamDevice[] devices = WebCamTexture.devices;
			if (devices.Length == 0)
			{
				Debug.Log("Web Camera not found");
				return;
			}
			int id = camera_id % devices.Length;
			webcamTexture = new WebCamTexture(devices[id].name, CAMERA_WIDTH, CAMERA_HEIGHT, CAMERA_FPS);
			webcamTexture.Play();
			square = set_square;
		}

		public bool IsEnable()
		{
			if (webcamTexture == null)
			{
				return false;
			}

			//Wait until a good frame can be captured
			if (webcamTexture.width > 16 && webcamTexture.height > 16)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		public WebCamTexture GetTexture()
		{
			return webcamTexture;
		}

		private int GetAngle()
		{
			return webcamTexture.videoRotationAngle;
		}

		private void CalculateCropSize()
		{
			if (crop_width != 16 || crop_height != 16)
			{
				return;	// already calculated
			}
			if (webcamTexture.width <= 16 || webcamTexture.height <= 16)
			{
				return;				//Wait until a good frame can be captured
			}

			int size = webcamTexture.width;
			if (size > webcamTexture.height)
			{
				size = webcamTexture.height;
			}
			crop_width = webcamTexture.width;
			crop_height = webcamTexture.height;

			int angle = GetAngle();
			bool rotate90 = (angle == 90 || angle == 270);

			if(square || rotate90){
				crop_width = size;
				crop_height = size;
			}
		}

		// This method returns left-to-right bottom-to-top image
		public Color32[] GetPixels32()
		{
			if (image.Length != webcamTexture.width * webcamTexture.height)
			{
				image = webcamTexture.GetPixels32();
			}
			else
			{
				webcamTexture.GetPixels32(image);
			}

			// Get crop size
			CalculateCropSize();

			// Crop to square
			int angle = GetAngle();
			bool rotate90 = (angle == 90 || angle == 270);

			if (crop.Length != crop_width * crop_height){
				crop = new Color32[crop_width * crop_height];
			}
			int x_offset = (webcamTexture.width - crop_width) / 2;
			int y_offset = (webcamTexture.height - crop_height) / 2;

			bool v_flip = false;
			if (angle == 90) v_flip = true;
			if (angle == 180) v_flip = true;
			if (angle == 270) v_flip = false;

			if (rotate90)
			{
				for (int y = 0; y < crop_height; y++)
				{
					int src_adr_y = (y + y_offset) * webcamTexture.width;
					for (int x = 0; x < crop_width; x++)
					{
						int x2 = y;
						int y2 = x;
						if (v_flip)
						{
							y2 = crop_height - 1 - y2;
						}
						crop[y2 * crop_width + x2] = image[src_adr_y + (x + x_offset)];
					}
				}
			}
			else
			{
				for (int y = 0; y < crop_height; y++)
				{
					int y2 = y;
					if (v_flip)
					{
						y2 = crop_height - 1 - y2;
					}
					int dst_adr_y = y2 * crop_width;
					int src_adr_y = (y + y_offset) * webcamTexture.width;
					for (int x = 0; x < crop_width; x++)
					{
						crop[dst_adr_y + x] = image[src_adr_y + (x + x_offset)];
					}
				}
			}
			return crop;
		}

		public int GetWidth()
		{
			CalculateCropSize();
			return crop_width;
		}

		public int GetHeight()
		{
			CalculateCropSize();
			return crop_height;
		}

		public void DestroyCamera()
		{
			if (webcamTexture != null)
			{
				webcamTexture.Stop();
				webcamTexture = null;
			}
		}
	}
}