/* AILIA Unity Plugin FeatureExtractor Sample */
/* Copyright 2018-2019 AXELL CORPORATION */

using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Runtime.InteropServices;

using UnityEngine;
using UnityEngine.UI;

public class AiliaFeatureExtractorSample : AiliaRenderer {
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
	private AiliaFeatureExtractorModel ailia_feature_extractor=new AiliaFeatureExtractorModel();

	private AiliaCamera ailia_camera=new AiliaCamera();
	#if UNITY_ANDROID
	private AiliaDownload ailia_download=new AiliaDownload();
	#endif

	//BeforeFeatureValue
	private float [] before_feature_value=null;
	private float [] capture_feature_value=null;

	//threshold for same person detection
	private float threshold=1.24f;	//VGGFace2 predefined value

	private void CreateAiliaDetector(){
		string asset_path = Application.streamingAssetsPath+"/AILIA";

		//Face detection
		uint category_n=1;
		if(gpu_mode){
			ailia_face.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
		}
		ailia_face.Settings(AiliaFormat.AILIA_NETWORK_IMAGE_FORMAT_RGB,AiliaFormat.AILIA_NETWORK_IMAGE_CHANNEL_FIRST,AiliaFormat.AILIA_NETWORK_IMAGE_RANGE_SIGNED_FP32,AiliaDetector.AILIA_DETECTOR_ALGORITHM_YOLOV1,category_n,AiliaDetector.AILIA_DETECTOR_FLAG_NORMAL);
	#if UNITY_ANDROID
		ailia_face.OpenMem(ailia_download.DownloadModel(asset_path+"/yolo_face.prototxt"),ailia_download.DownloadModel(asset_path+"/yolo_face_fp12.caffemodel"));
	#else
		ailia_face.OpenFile(asset_path+"/yolo_face.prototxt",asset_path+"/yolo_face_fp12.caffemodel");
	#endif

		//Feature extractor
		if(gpu_mode){
			ailia_feature_extractor.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
		}
		string layer_name="conv5_3";
		ailia_feature_extractor.Settings(AiliaFormat.AILIA_NETWORK_IMAGE_FORMAT_BGR, AiliaFormat.AILIA_NETWORK_IMAGE_CHANNEL_FIRST, AiliaFormat.AILIA_NETWORK_IMAGE_RANGE_SIGNED_INT8, AiliaFeatureExtractor.AILIA_FEATURE_EXTRACTOR_DISTANCE_L2NORM,layer_name);
	#if UNITY_ANDROID
		ailia_feature_extractor.OpenMem(ailia_download.DownloadModel(asset_path+"/resnet50_scratch.prototxt"),ailia_download.DownloadModel(asset_path+"/resnet50_scratch_fp12.caffemodel"));
	#else
		ailia_feature_extractor.OpenFile(asset_path+"/resnet50_scratch.prototxt",asset_path+"/resnet50_scratch_fp12.caffemodel");
	#endif
	}

	private void DestroyAiliaDetector(){
		ailia_face.Close();
		ailia_feature_extractor.Close();
	}

	// Use this for initialization
	void Start () {
		mode_text.text="ailia FeatureExtractor";
		CreateAiliaDetector();
		ailia_camera.CreateCamera(camera_id);
	}
	
	// Update is called once per frame
	void Update () {
		if(!ailia_camera.IsEnable()){
			return;
		}

		//Clear label
		Clear();

		//Get camera image
		int tex_width = ailia_camera.GetWidth();
		int tex_height = ailia_camera.GetHeight();
		if(preview_texture==null){
			preview_texture = new Texture2D(tex_width,tex_height);
			raw_image.texture = preview_texture;
		}
		Color32[] camera  = ailia_camera.GetPixels32();

		//Detection
		float threshold=0.2f;
		float iou=0.25f;
		List<AiliaDetector.AILIADetectorObject> list=ailia_face.ComputeFromImageB2T(camera,tex_width,tex_height,threshold,iou);

		foreach(AiliaDetector.AILIADetectorObject obj in list){
			FaceClassifier(obj,camera,tex_width,tex_height);
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

		//Get face image
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

		//Feature extractor
		long start_time=DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;;
		float [] feature=ailia_feature_extractor.ComputeFromImage(face,w,h);

		//Match
		float distance=0.0f;
		string feature_text="";
		Color color=Color.white;
		if(capture_feature_value!=null){
			distance=ailia_feature_extractor.Match(capture_feature_value,feature);
			feature_text="Distance "+distance+"\n";
			if(distance<threshold){
				feature_text+="Same person";
				color=Color.green;
			}else{
				feature_text+="Not same person";
				color=Color.red;
			}
		}else{
			feature_text="Please capture some face";
		}
		before_feature_value=feature;
		long end_time=DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;;

		DrawRect2D(color, x1, y1, w, h, tex_width, tex_height);

		int margin=4;
		DrawText(color,feature_text,x1+margin,y1+margin,tex_width,tex_height);

		if(label_text!=null){
			label_text.text=(end_time-start_time)+"ms\n"+ailia_face.EnvironmentName();
		}
	}

	public void Capture(){
		//Remember feature
		capture_feature_value=before_feature_value;
		if(label_text!=null){
			if(capture_feature_value==null){
				label_text.text="Face not found!";
			}else{
				label_text.text="Capture success!";
			}
		}
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
