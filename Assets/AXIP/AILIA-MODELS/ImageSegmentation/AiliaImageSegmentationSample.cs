/* AILIA Unity Plugin Detector Sample */
/* Copyright 2018-2019 AXELL CORPORATION */

using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Runtime.InteropServices;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class AiliaImageSegmentationSample : AiliaRenderer
{
	[SerializeField, HideInInspector]
	private AiliaModelsConst.AiliaModelTypes ailiaModelType = AiliaModelsConst.AiliaModelTypes.yolov3_tiny;
	//Settings
	public bool gpu_mode = false;
	public int camera_id = 0;

	//Result
	public RawImage raw_image = null;
	public Text label_text = null;
	public Text mode_text = null;

	//Preview
	private RenderTexture preview_texture = null;

	//AILIA
	private AiliaModel ailiaModel = new AiliaModel();

	//private AiliaCamera ailia_camera = new AiliaCamera();
	private AiliaDownload ailia_download = new AiliaDownload();
	AiliaImageSource AiliaImageSource;

	private void CreateAiliaNet(AiliaModelsConst.AiliaModelTypes modelType)
	{
		string asset_path = Application.temporaryCachePath;

		mode_text.text = "ailia Detector";
		if (gpu_mode)
		{
			bool b = ailiaModel.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
		}

		ailia_download.DownloadModelFromUrl("deeplabv3", "deeplabv3.opt.onnx.prototxt");
		ailia_download.DownloadModelFromUrl("deeplabv3", "deeplabv3.opt.onnx");
		ailiaModel.OpenFile(asset_path + "/deeplabv3.opt.onnx.prototxt", asset_path + "/deeplabv3.opt.onnx");

		var shape = ailiaModel.GetInputShape();
		Debug.Log(shape.x + " " + shape.y + " " + shape.z + " " + shape.w + " " + shape.dim);

	}

	private void DestroyAiliaDetector()
	{
		ailiaModel.Close();
	}

	// Use this for initialization
	Texture2D _prevTexture;
	Texture2D _prevTexture2;
	const int INPUT_WIDHT = 513;
	const int INPUT_HEIGHT = 513;
	const int INPUT_CHANNEL = 3;
	const int OUTPUT_WIDHT = 129;
	const int OUTPUT_HEIGHT = 129;
	const int OUTPUT_CHANNEL = 21;
	void Start()
	{
		preview_texture = new RenderTexture(513, 513, 1);
		_prevTexture = new Texture2D(OUTPUT_WIDHT, OUTPUT_HEIGHT, TextureFormat.RGBA32, false);
		_prevTexture2 = new Texture2D(INPUT_WIDHT, INPUT_HEIGHT, TextureFormat.RGBA32, false);
		raw_image.texture = preview_texture;
		AiliaImageSource = gameObject.AddComponent<AiliaImageSource>();
		AiliaImageSource.CreateSource("file://G:/ax/couple.jpg");
		CreateAiliaNet(ailiaModelType);
	}

	float[] output;
	float[] input;
	Color32[] col32output;
	bool oneshot = true;
	// Update is called once per frame
	void Update()
	{
		if (!AiliaImageSource.IsPrepared)
		{
			return;
		}
		
		Rect rect = new Rect(0, 0, INPUT_WIDHT, INPUT_HEIGHT);
		if (output == null)
		{
			AiliaImageSource.Resize(INPUT_WIDHT, INPUT_HEIGHT);
			input = new float[INPUT_WIDHT * INPUT_HEIGHT * INPUT_CHANNEL];
			output = new float[OUTPUT_WIDHT * OUTPUT_HEIGHT * OUTPUT_CHANNEL];
			col32output = new Color32[OUTPUT_WIDHT * OUTPUT_HEIGHT];
		}

		if (oneshot)
		{
			oneshot = false;

			var inputImage = AiliaImageSource.GetPixels32(rect);
			int pixelNum = INPUT_WIDHT * INPUT_HEIGHT;
			for (int i = 0; i < pixelNum; i++)
			{
				// rgbrgbrgb...
				//input[i * INPUT_CHANNEL + 0] = (camera[i].r) / 127.5f - 1.0f;
				//input[i * INPUT_CHANNEL + 1] = (camera[i].g) / 127.5f - 1.0f;
				//input[i * INPUT_CHANNEL + 2] = (camera[i].b) / 127.5f - 1.0f;

				// rrr...ggg...bbb...
				input[i + pixelNum * 0] = (inputImage[i].r) / 127.5f - 1.0f;
				input[i + pixelNum * 1] = (inputImage[i].g) / 127.5f - 1.0f;
				input[i + pixelNum * 2] = (inputImage[i].b) / 127.5f - 1.0f;
			}

			// Predict
			long start_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
			bool list = ailiaModel.Predict(output, input);
			long end_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

			Debug.Log("Predict = " + list.ToString());
			Debug.Log((end_time - start_time).ToString() + "ms");

			for (int i = 0; i < col32output.Length; i++)
			{
				int maxIndex = 0;
				float maxValue = output[i + 0 * (OUTPUT_WIDHT * OUTPUT_HEIGHT)];
				for (int j = 1; j < OUTPUT_CHANNEL; j++)
				{
					if (maxValue < output[i + j * (OUTPUT_WIDHT * OUTPUT_HEIGHT)])
					{
						maxValue = output[i + j * (OUTPUT_WIDHT * OUTPUT_HEIGHT)];
						maxIndex = j;
					}
				}
				col32output[i] = Color.HSVToRGB(maxIndex * (1f / OUTPUT_CHANNEL), 0.8f, 0.8f);
			}
		}

		if (Input.GetKey(KeyCode.G))
		{
			var inputImage = AiliaImageSource.GetPixels32(rect);
			_prevTexture2.SetPixels32(inputImage);
			_prevTexture2.Apply();
			Graphics.Blit(_prevTexture2, preview_texture);
		}
		else
		{
			_prevTexture.SetPixels32(col32output);
			_prevTexture.Apply();
			Graphics.Blit(_prevTexture, preview_texture);
		}
	}

	void OnApplicationQuit()
	{
		DestroyAiliaDetector();
		//ailia_camera.DestroyCamera();
	}

	void OnDestroy()
	{
		DestroyAiliaDetector();
		//ailia_camera.DestroyCamera();
	}
}
