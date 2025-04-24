/* AILIA Unity Plugin Segment Sample */
/* Copyright 2025 AXELL CORPORATION and ax Inc. */

using ailia;
using ailiaSDK;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class SegmentationModel
{
	private AiliaModel ailiaModel;

	int InputWidth;
	int InputHeight;
	int InputChannel;
	int OutputWidth;
	int OutputHeight;
	int OutputChannel;
	float[] output;
	float[] input;
	Color32[] outputImage;
	Color32[] colorPalette = AiliaImageUtil.CreatePalette(256, 127);

	public List<ModelDownloadURL> GetModelURLs(ImageSegmentaionModels modelType)
	{
		string serverFolderName = "";
		string prototxtName = "";
		string onnxName = "";
		switch (modelType)
		{
			case ImageSegmentaionModels.HRNetV2_W18_Small_v2:
				serverFolderName = "hrnet";
				prototxtName = "HRNetV2-W18-Small-v2.onnx.prototxt";
				onnxName = "HRNetV2-W18-Small-v2.onnx";
				break;
			case ImageSegmentaionModels.HRNetV2_W18_Small_v1:
				serverFolderName = "hrnet";
				prototxtName = "HRNetV2-W18-Small-v1.onnx.prototxt";
				onnxName = "HRNetV2-W18-Small-v1.onnx";
				break;
			case ImageSegmentaionModels.HRNetV2_W48:
				serverFolderName = "hrnet";
				prototxtName = "HRNetV2-W48.onnx.prototxt";
				onnxName = "HRNetV2-W48.onnx";
				break;
			case ImageSegmentaionModels.hair_segmentation:
				serverFolderName = "hair_segmentation";
				prototxtName = "hair_segmentation.opt.onnx.prototxt";
				onnxName = "hair_segmentation.opt.onnx";
				break;
			case ImageSegmentaionModels.pspnet_hair_segmentation:
				serverFolderName = "pspnet-hair-segmentation";
				prototxtName = "pspnet-hair-segmentation.onnx.prototxt";
				onnxName = "pspnet-hair-segmentation.onnx";
				break;
			case ImageSegmentaionModels.deeplabv3:
				serverFolderName = "deeplabv3";
				prototxtName = "deeplabv3.opt.onnx.prototxt";
				onnxName = "deeplabv3.opt.onnx";
				break;
			case ImageSegmentaionModels.u2net:
				serverFolderName = "u2net";
				prototxtName = "u2net_opset11.onnx.prototxt";
				onnxName = "u2net_opset11.onnx";
				break;
			case ImageSegmentaionModels.modnet:
				serverFolderName = "modnet";
				prototxtName = "modnet.opt.onnx.prototxt";
				onnxName = "modnet.opt.onnx";
				break;
		}

		List<ModelDownloadURL> urlList = new List<ModelDownloadURL>();
		urlList.Add(new ModelDownloadURL() { folder_path = serverFolderName, file_name = prototxtName });
		urlList.Add(new ModelDownloadURL() { folder_path = serverFolderName, file_name = onnxName });
		return urlList;
	}

    public bool InitializeModels(ImageSegmentaionModels modelType, bool gpuMode)
    {
		List<ModelDownloadURL> urlList = GetModelURLs(modelType);
		string asset_path = Application.temporaryCachePath;
		string prototxtName = urlList[0].file_name;
		string onnxName = urlList[1].file_name;

		ailiaModel = new AiliaModel();
		if (gpuMode)
		{
			// call before OpenFile
			ailiaModel.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
		}
		return ailiaModel.OpenFile(asset_path + "/" + prototxtName, asset_path + "/" + onnxName);
	}

	private void SetInputShape(ImageSegmentaionModels imageSegmentaionModels, int imageWidth, int imageHeight)
	{
		Ailia.AILIAShape shape = null;
		switch (imageSegmentaionModels)
		{
			case ImageSegmentaionModels.HRNetV2_W18_Small_v2:
			case ImageSegmentaionModels.HRNetV2_W18_Small_v1:
			case ImageSegmentaionModels.HRNetV2_W48:
			case ImageSegmentaionModels.pspnet_hair_segmentation:
			case ImageSegmentaionModels.deeplabv3:
				shape = ailiaModel.GetInputShape();
				InputWidth = (int)shape.x;
				InputHeight = (int)shape.y;
				InputChannel = (int)shape.z;
				shape = ailiaModel.GetOutputShape();
				OutputWidth = (int)shape.x;
				OutputHeight = (int)shape.y;
				OutputChannel = (int)shape.z;
				break;

			case ImageSegmentaionModels.hair_segmentation:
				shape = new Ailia.AILIAShape();
				shape.x = 3;
				shape.y = (uint)imageWidth;
				shape.z = (uint)imageHeight;
				shape.w = 1;
				shape.dim = 4;
				ailiaModel.SetInputShape(shape);
				InputWidth = imageWidth;
				InputHeight = imageHeight;
				InputChannel = 3;
				OutputWidth = imageWidth;
				OutputHeight = imageHeight;
				OutputChannel = 1;
				break;

			case ImageSegmentaionModels.u2net:
				shape = ailiaModel.GetInputShape();
				InputWidth = (int)shape.x;
				InputHeight = (int)shape.y;
				InputChannel = (int)shape.z;
				shape = ailiaModel.GetOutputShape();
				OutputWidth = (int)shape.x;
				OutputHeight = (int)shape.y;
				OutputChannel = (int)shape.z;
				break;

			case ImageSegmentaionModels.modnet:
				shape = new Ailia.AILIAShape();
				shape.x = (uint)imageWidth/ 32 * 32;
				shape.y = (uint)imageHeight/ 32 * 32;
				shape.z = 3;
				shape.w = 1;
				shape.dim = 4;
				ailiaModel.SetInputShape(shape);
				InputWidth = imageWidth/ 32 * 32;
				InputHeight = imageHeight/ 32 * 32;
				InputChannel = 3;
				shape = ailiaModel.GetOutputShape();
				OutputWidth = (int)shape.x;
				OutputHeight = (int)shape.y;
				OutputChannel = (int)shape.z;
				Debug.Log("" + OutputChannel + "/" + OutputWidth + "/" + OutputHeight);
				//OutputWidth = AiliaImageSource.Width/32*32;
				//OutputHeight = AiliaImageSource.Height/32*32;
				//OutputChannel = 1;
				break;
		}
	}

	public void AllocateInputAndOutputTensor(ImageSegmentaionModels imageSegmentaionModels, int imageWidth, int imageHeight)
	{
		SetInputShape(imageSegmentaionModels, imageWidth, imageHeight);

		// texture & buffer allocate
		//if (false){//imageSegmentaionModels == ImageSegmentaionModels.segment_anything1){
			// 元画像サイズ
			//InputWidth = imageWidth;
			//InputHeight = imageHeight;
			//OutputWidth = InputWidth;
			//OutputHeight = InputHeight;
		//} else {
		//	// 事前にリサイズ
		//	AiliaImageSource.Resize(InputWidth, InputHeight);
		//}

		input = new float[InputWidth * InputHeight * InputChannel];
		output = new float[OutputWidth * OutputHeight * OutputChannel];
		outputImage = new Color32[OutputWidth * OutputHeight];
	}

	public bool ProcessFrame(ImageSegmentaionModels imageSegmentaionModels, Color32[] inputImage, int inputImageWidth, int inputImageHeight){
		inputImage = ResizeImage(inputImage, inputImageWidth, inputImageHeight, InputWidth, InputHeight);
		InputDataProcessing(imageSegmentaionModels, inputImage, input);
		return ailiaModel.Predict(output, input);
	}

	public (Color32[], int, int) PostProcesss(ImageSegmentaionModels imageSegmentaionModels, int inputImageWidth, int inputImageHeight){
		if (imageSegmentaionModels == ImageSegmentaionModels.hair_segmentation)
			LabelPaintHairSegmentation(output, outputImage, Color.red);
		else if (imageSegmentaionModels == ImageSegmentaionModels.pspnet_hair_segmentation)
			LabelPaintPspnet(output, outputImage, Color.red);
		else if (imageSegmentaionModels == ImageSegmentaionModels.u2net || imageSegmentaionModels == ImageSegmentaionModels.modnet)
			LabelPaintU2netModnet(output, outputImage, Color.green);
		else
			LabelPaint(output, outputImage, OutputChannel, colorPalette);
		
		outputImage = ResizeImage(outputImage, OutputWidth, OutputHeight, inputImageWidth, inputImageHeight);

		return (outputImage, inputImageWidth, inputImageHeight);
	}

	Color32 [] ResizeImage(Color32[] inputImage, int tex_width, int tex_height, int InputWidth, int InputHeight){
		Color32[] outputImage = new Color32[InputWidth * InputHeight];
		for(int y=0;y<InputHeight;y++){
			for(int x=0;x<InputWidth;x++){
				int x2 = x*tex_width/InputWidth;
				int y2 = y*tex_height/InputHeight;
				outputImage[y*InputWidth+x]=inputImage[y2*tex_width+x2];
			}
		}
		return outputImage;
	}

	void InputDataProcessing(ImageSegmentaionModels imageSegmentaionModels, Color32[] inputImage, float[] processedInputBuffer)
	{
		float weight = 1f / 255f;
		float bias = 0;
		bool rgbRepeats = false;
		switch (imageSegmentaionModels)
		{
			case ImageSegmentaionModels.hair_segmentation:
				rgbRepeats = true;
				break;
			case ImageSegmentaionModels.pspnet_hair_segmentation:
				InputDataProcessing_PSP(inputImage, processedInputBuffer);
				return;
			case ImageSegmentaionModels.deeplabv3:
				weight = 1f / 127.5f;
				bias = -1;
				break;
			case ImageSegmentaionModels.modnet:
				weight = 1f / 127.5f;
				bias = -1;
				break;
			default:
				break;
		}

		// flatten input data
		if (rgbRepeats)
		{
			for (int i = 0; i < inputImage.Length; i++)
			{
				// rgbrgbrgb...
				processedInputBuffer[i * 3 + 0] = (inputImage[i].r) * weight + bias;
				processedInputBuffer[i * 3 + 1] = (inputImage[i].g) * weight + bias;
				processedInputBuffer[i * 3 + 2] = (inputImage[i].b) * weight + bias;
			}
		}
		else
		{
			for (int i = 0; i < inputImage.Length; i++)
			{
				// rrr...ggg...bbb...
				processedInputBuffer[i + inputImage.Length * 0] = (inputImage[i].r) * weight + bias;
				processedInputBuffer[i + inputImage.Length * 1] = (inputImage[i].g) * weight + bias;
				processedInputBuffer[i + inputImage.Length * 2] = (inputImage[i].b) * weight + bias;
			}
		}
	}

	void InputDataProcessing_PSP(Color32[] inputImage, float[] processedInputBuffer)
	{
		for (int i = 0; i < inputImage.Length; i++)
		{
			// rrr...ggg...bbb...
			processedInputBuffer[i + inputImage.Length * 0] = (inputImage[i].r * (1f / 255f) - 0.485f) * (1f / 0.229f);
			processedInputBuffer[i + inputImage.Length * 1] = (inputImage[i].g * (1f / 255f) - 0.456f) * (1f / 0.224f);
			processedInputBuffer[i + inputImage.Length * 2] = (inputImage[i].b * (1f / 255f) - 0.406f) * (1f / 0.225f);
		}
	}

	void LabelPaint(float[] labelData, Color32[] pixelBuffer, int channel, Color32[] palette)
	{
		Debug.Assert(palette.Length >= channel, "wrong palette size");
		Debug.Assert(labelData.Length == pixelBuffer.Length * channel, "wrong parameter");

		for (int i = 0; i < pixelBuffer.Length; i++)
		{
			int maxIndex = 0;
			float maxValue = labelData[i + 0 * (pixelBuffer.Length)];
			for (int c = 1; c < channel; c++)
			{
				if (maxValue < labelData[i + c * pixelBuffer.Length])
				{
					maxValue = labelData[i + c * pixelBuffer.Length];
					maxIndex = c;
				}
			}
			pixelBuffer[i] = palette[maxIndex];
		}
	}

	void LabelPaintHairSegmentation(float[] labelData, Color32[] pixelBuffer, Color32 color)
	{
		Debug.Assert(labelData.Length == pixelBuffer.Length, "wrong parameter");

		for (int i = 0; i < pixelBuffer.Length; i++)
		{
			pixelBuffer[i] = color;
			pixelBuffer[i].a = (byte)(Mathf.Clamp01(labelData[i]) * 255);
		}
	}

	void LabelPaintPspnet(float[] labelData, Color32[] pixelBuffer, Color32 color)
	{
		Debug.Assert(labelData.Length == pixelBuffer.Length, "wrong parameter");

		for (int i = 0; i < pixelBuffer.Length; i++)
		{
			pixelBuffer[i] = color;

			float k = Mathf.Exp(labelData[i]);
			pixelBuffer[i].a = (byte)(k / (1.0f + k) * 255);
		}
	}

	void LabelPaintU2netModnet(float[] labelData, Color32[] pixelBuffer, Color32 color)
	{
		Debug.Assert(labelData.Length == pixelBuffer.Length, "wrong parameter");	
		for (int i = 0; i < pixelBuffer.Length; i++)
		{
			pixelBuffer[i] = color;
			pixelBuffer[i].a = (byte)(255 - Mathf.Clamp01(labelData[i]) * 255);
		}	 
	}

	public void Destroy()
	{
		ailiaModel?.Close();
	}

	public string EnvironmentName()
	{
		return ailiaModel.EnvironmentName();
	}
}
