/* AILIA Unity Plugin Detector Sample */
/* Copyright 2018-2019 AXELL CORPORATION */

using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Runtime.InteropServices;

using UnityEngine;
using UnityEngine.UI;

public class AiliaDetectorFaceSample : AiliaRenderer {
	//Settings
	public bool gpu_mode = false;
	public int camera_id = 0;

	//Result
	public Text label_text=null;
	public Text mode_text=null;
	public RawImage raw_image=null;

	//Preview
	private Texture2D preview_texture=null;

	//AILIA
	private AiliaDetectorModel ailia_face=new AiliaDetectorModel();
	private AiliaClassifierModel ailia_gender=new AiliaClassifierModel();
	private AiliaClassifierModel ailia_emotion=new AiliaClassifierModel();

	private AiliaCamera ailia_camera=new AiliaCamera();
	private AiliaDownload ailia_download=new AiliaDownload();

	private void CreateAiliaDetector(){
		string asset_path = Application.temporaryCachePath;
		
		//Face Detection
		uint category_n=1;
		if(gpu_mode){
			ailia_face.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
		}
		ailia_face.Settings (AiliaFormat.AILIA_NETWORK_IMAGE_FORMAT_RGB, AiliaFormat.AILIA_NETWORK_IMAGE_CHANNEL_FIRST, AiliaFormat.AILIA_NETWORK_IMAGE_RANGE_UNSIGNED_FP32, AiliaDetector.AILIA_DETECTOR_ALGORITHM_YOLOV3, category_n, AiliaDetector.AILIA_DETECTOR_FLAG_NORMAL);

		ailia_download.DownloadModelFromUrl("yolov3-face","yolov3-face.opt.onnx.prototxt");
		ailia_download.DownloadModelFromUrl("yolov3-face","yolov3-face.opt.onnx");

		ailia_face.OpenFile(asset_path+"/yolov3-face.opt.onnx.prototxt",asset_path+"/yolov3-face.opt.onnx");

		//Emotion Detection
		if(gpu_mode){
			ailia_emotion.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
		}
		ailia_emotion.Settings(AiliaFormat.AILIA_NETWORK_IMAGE_FORMAT_GRAY, AiliaFormat.AILIA_NETWORK_IMAGE_CHANNEL_FIRST, AiliaFormat.AILIA_NETWORK_IMAGE_RANGE_SIGNED_FP32);

		ailia_download.DownloadModelFromUrl("face_classification","emotion_miniXception.prototxt");
		ailia_download.DownloadModelFromUrl("face_classification","emotion_miniXception.caffemodel");

		ailia_emotion.OpenFile(asset_path+"/emotion_miniXception.prototxt",asset_path+"/emotion_miniXception.caffemodel");

		//Gender Detection
		if(gpu_mode){
			ailia_gender.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
		}
		ailia_gender.Settings(AiliaFormat.AILIA_NETWORK_IMAGE_FORMAT_GRAY, AiliaFormat.AILIA_NETWORK_IMAGE_CHANNEL_FIRST, AiliaFormat.AILIA_NETWORK_IMAGE_RANGE_SIGNED_FP32);

		ailia_download.DownloadModelFromUrl("face_classification","gender_miniXception.prototxt");
		ailia_download.DownloadModelFromUrl("face_classification","gender_miniXception.caffemodel");

		ailia_gender.OpenFile(asset_path+"/gender_miniXception.prototxt",asset_path+"/gender_miniXception.caffemodel");
	}

	private void DestroyAiliaDetector(){
		ailia_face.Close();
		ailia_emotion.Close();
		ailia_gender.Close();
	}

	// Use this for initialization
	void Start () {
		mode_text.text="ailia FaceDetector";
		CreateAiliaDetector();
		ailia_camera.CreateCamera(camera_id);
	}
	
	// Update is called once per frame
	void Update () {
		if(!ailia_camera.IsEnable()){
			return;
		}

		//Clear result
		Clear();

		//Get camera image
		int tex_width = ailia_camera.GetWidth();
		int tex_height = ailia_camera.GetHeight();
		if(preview_texture==null){
			preview_texture = new Texture2D(tex_width,tex_height);
			raw_image.texture = preview_texture;
		}
		Color32[] camera  = ailia_camera.GetPixels32();

		//Detection result
		float threshold=0.2f;
		float iou=0.25f;
		long start_time_face=DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;;
		List<AiliaDetector.AILIADetectorObject> list=ailia_face.ComputeFromImageB2T(camera,tex_width,tex_height,threshold,iou);
		long end_time_face=DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;;

		long start_time_class=DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;;
		foreach(AiliaDetector.AILIADetectorObject obj in list){
			FaceClassifier(obj,camera,tex_width,tex_height);
		}
		long end_time_class=DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;;

		if(label_text!=null){
			label_text.text=""+((end_time_face-start_time_face)+(end_time_class-start_time_class))+"ms\n"+ailia_face.EnvironmentName();
		}

		//Apply
		preview_texture.SetPixels32(camera);
		preview_texture.Apply();
	}

	private void GetFace(Color32 [] face,int x1,int y1,int w,int h,Color32 [] camera,int tex_width,int tex_height){
		for(int y=0;y<h;y++){
			for(int x=0;x<w;x++){
				if(x+x1>=0 && x+x1<tex_width && y+y1>=0 && y+y1<tex_height){
					face[y*w+x]=camera[(tex_height-1-y-y1)*tex_width+x+x1];
				}
			}
		}
	}

	private void FaceClassifier(AiliaDetector.AILIADetectorObject box,Color32 [] camera,int tex_width,int tex_height){
		//Convert to pixel position
		int x1=(int)(box.x*tex_width);
		int y1=(int)(box.y*tex_height);
		int x2=(int)((box.x+box.w)*tex_width);
		int y2=(int)((box.y+box.h)*tex_height);

		//Get face
		Color32 [] face;

		int w=(x2-x1);
		int h=(y2-y1);

		float expand=1.4f;
		x1-=(int)(w*expand-w)/2;
		y1-=(int)(h*expand-h)/2;
		w=(int)(w*expand);
		h=(int)(h*expand);

		if(w<=0 || h<=0){
			return;
		}

		face=new Color32[w*h];

		GetFace(face,x1,y1,w,h,camera,tex_width,tex_height);

		//Estimate emotion
		const int max_class_count = 1;
		List<AiliaClassifier.AILIAClassifierClass> gender_obj = ailia_gender.ComputeFromImage (face, w, h, max_class_count);

		//Estimate gender
		List<AiliaClassifier.AILIAClassifierClass> emotion_obj = ailia_emotion.ComputeFromImage (face, w, h, max_class_count);

		//Draw Box
		Color color=Color.white;
		color=Color.HSVToRGB (emotion_obj[0].category/7.0f, 1.0f, 1.0f);	
		DrawRect2D(color, x1, y1, w, h, tex_width, tex_height);

		string text="";
		text+=AiliaClassifierLabel.EMOTION_CATEGORY [emotion_obj [0].category];
		text+=" "+emotion_obj [0].prob+"\n";
		text+=AiliaClassifierLabel.GENDER_CATEGORY [gender_obj [0].category];
		text+= " " +gender_obj [0].prob;

		int margin=4;
		DrawText(color,text,x1+margin,y1+margin,tex_width,tex_height);
	}

	void OnApplicationQuit () {
		DestroyAiliaDetector();
		ailia_camera.DestroyCamera();
	}

	void OnDestroy () {
		DestroyAiliaDetector();
		ailia_camera.DestroyCamera();
	}
}
