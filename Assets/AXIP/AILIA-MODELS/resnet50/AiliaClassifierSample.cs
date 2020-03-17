/* AILIA Unity Plugin Classifier Sample */
/* Copyright 2018-2019 AXELL CORPORATION */

using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Runtime.InteropServices;

using UnityEngine;
using UnityEngine.UI;

public class AiliaClassifierSample : AiliaRenderer {
	//Settings
	public bool gpu_mode = true;
	public bool is_english = false;
	public int camera_id = 0;

	//Output buffer
	public RawImage raw_image=null;
	public Text mode_text=null;
	public Text label_text=null;

	//Preview texture
	private Texture2D preview_texture=null;

	//ailia Instance
	private AiliaClassifierModel ailia_classifier_model=new AiliaClassifierModel();

	private AiliaCamera ailia_camera=new AiliaCamera();
	#if UNITY_ANDROID
	private AiliaDownload ailia_download=new AiliaDownload();
	#endif

	private void CreateAilia(){
		string asset_path = Application.streamingAssetsPath+"/AILIA";
		if(gpu_mode){
			ailia_classifier_model.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
		}
		ailia_classifier_model.Settings(AiliaFormat.AILIA_NETWORK_IMAGE_FORMAT_BGR, AiliaFormat.AILIA_NETWORK_IMAGE_CHANNEL_FIRST, AiliaFormat.AILIA_NETWORK_IMAGE_RANGE_SIGNED_INT8);
	#if UNITY_ANDROID
		ailia_classifier_model.OpenMem(ailia_download.DownloadModel(asset_path+"/SqueezeNet.prototxt"),ailia_download.DownloadModel(asset_path+"/SqueezeNet.caffemodel"));
	#else
		ailia_classifier_model.OpenFile(asset_path+"/SqueezeNet.prototxt",asset_path+"/SqueezeNet.caffemodel");
	#endif
	}

	private void DestroyAilia(){
		ailia_classifier_model.Close();
	}

	void Start () {
		mode_text.text="ailia Classifier";
		CreateAilia();
		ailia_camera.CreateCamera(camera_id);
	}
	
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
		
		//Classify
		long start_time=DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;;
		uint display_n=5;
		List<AiliaClassifier.AILIAClassifierClass> result_list=ailia_classifier_model.ComputeFromImageB2T(camera,tex_width,tex_height,display_n);
		long end_time=DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;;

		//Display prediction time
		if(label_text!=null){
			label_text.text=(end_time-start_time)+"ms\n"+ailia_classifier_model.EnvironmentName();
		}

		//Detection result
		int y=0;
		foreach(AiliaClassifier.AILIAClassifierClass classifier_obj in result_list){
			string result="";
			if(is_english){
				result=AiliaClassifierLabel.IMAGENET_CATEGORY[classifier_obj.category]+" "+(int)(classifier_obj.prob * 100)/100.0f;
			}else{
				result=AiliaClassifierLabel.IMAGENET_CATEGORY_JP[classifier_obj.category]+" "+(int)(classifier_obj.prob * 100)/100.0f;
			}

			int margin=4;
			Color32 color=Color.HSVToRGB (classifier_obj.category/1000.0f, 1.0f, 1.0f);
			DrawText(color,result,margin,margin+y,tex_width,tex_height);
			y+=tex_height/12;
		}

		//Apply image
		preview_texture.SetPixels32(camera);
		preview_texture.Apply();
	}

	void OnApplicationQuit () {
		DestroyAilia();
		ailia_camera.DestroyCamera();
	}

	void OnDestroy () {
		DestroyAilia();
		ailia_camera.DestroyCamera();
	}
}
