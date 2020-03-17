/* AILIA Unity Plugin Camera Sample */
/* Copyright 2018-2019 AXELL CORPORATION */

using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Runtime.InteropServices;

using UnityEngine;
using UnityEngine.UI;

public class AiliaCamera {
	//Camera settings
	private int CAMERA_WIDTH = 1280;
	private int CAMERA_HEIGHT = 720;
	private int CAMERA_FPS = 30;

	//WebCamera Instance
	private WebCamTexture webcamTexture=null;

	//Camera ID
	public void CreateCamera(int camera_id){
		DestroyCamera();
		WebCamDevice[] devices = WebCamTexture.devices;
		if(devices.Length==0){
			Debug.Log("Web Camera not found");
			return;
		}
		int id=camera_id % devices.Length;
		webcamTexture = new WebCamTexture(devices[id].name, CAMERA_WIDTH, CAMERA_HEIGHT, CAMERA_FPS );
		webcamTexture.Play();
	}

	public bool IsEnable(){
		if(webcamTexture==null){
			return false;
		}

		//Wait until a good frame can be captured
		if(webcamTexture.width>16 && webcamTexture.height>16){
			return true;
		}else{
			return false;
		}
	}

	public WebCamTexture GetTexture(){
		return webcamTexture;
	}

	private int GetAngle(){
		return webcamTexture.videoRotationAngle;
	}

	public Color32[] GetPixels32(){
		Color32[] image=webcamTexture.GetPixels32();

		//Crop to square
		int size=webcamTexture.width;
		if(size>webcamTexture.height){
			size=webcamTexture.height;
		}
		Color32[] crop=new Color32[size*size];
		int x_offset=(webcamTexture.width-size)/2;
		int y_offset=(webcamTexture.height-size)/2;
		int angle=GetAngle();
		
		bool rotate90=(angle == 90 || angle == 270);
		bool v_flip=false;
		if(angle==90) v_flip=true;
		if(angle==180) v_flip=true;
		if(angle==270) v_flip=false;

		if (rotate90){
			for(int y=0;y<size;y++){
				int src_adr_y=(y+y_offset)*webcamTexture.width;
				for(int x=0;x<size;x++){
					int x2=y;
					int y2=x;
					if(v_flip){
						y2=size-1-y2;
					}
					crop[y2*size+x2]=image[src_adr_y+(x+x_offset)];
				}
			}
		}else{
			for(int y=0;y<size;y++){
				int y2=y;
				if(v_flip){
					y2=size-1-y2;
				}
				int dst_adr_y=y2*size;
				int src_adr_y=(y+y_offset)*webcamTexture.width;
				for(int x=0;x<size;x++){
					crop[dst_adr_y+x]=image[src_adr_y+(x+x_offset)];
				}
			}
		}
		return crop;
	}

	public int GetWidth(){
		if(webcamTexture.height>webcamTexture.width){
			return webcamTexture.width;
		}
		return webcamTexture.height;
	}

	public int GetHeight(){
		if(webcamTexture.height>webcamTexture.width){
			return webcamTexture.width;
		}
		return webcamTexture.height;
	}

	public void DestroyCamera(){
		if(webcamTexture!=null){
			webcamTexture.Stop();
			webcamTexture=null;
		}
	}
}
