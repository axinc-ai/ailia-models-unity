/* AILIA Unity Plugin PoseEstimator Model Class */
/* Copyright 2020-2021 AXELL CORPORATION */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Runtime.InteropServices;

public class AiliaPoseEstimatorModel : AiliaModel{
    private IntPtr ailia_pose_estimator = IntPtr.Zero;

    private uint algorithm=AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_ALGORITHM_ACCULUS_POSE;

    //モデル設定
    public bool Settings(uint set_algorithm){
        algorithm=set_algorithm;
        return true;
    }

    //ファイルから開く
    public override bool OpenFile(string prototxt,string model_path){
        Close();
        bool status=base.OpenFile(prototxt,model_path);
        if(status==false){
            if(logging){
                Debug.Log("ailiaModelOpenFile failed");
            }
            return false;
        }
        return OpenPoseEstimator();
    }

    //コールバックから開く
    public override bool OpenEx(Ailia.ailiaFileCallback callback,IntPtr arg1,IntPtr arg2){
        Close();
        bool status=base.OpenEx(callback,arg1,arg2);
        if(status==false){
            if(logging){
                Debug.Log("ailiaModelOpenEx failed");
            }
            return false;
        }
        return OpenPoseEstimator();
    }

    //メモリから開く
    public override bool OpenMem(byte[] prototxt_buf,byte[] model_buf){
        Close();
        bool status=base.OpenMem(prototxt_buf,model_buf);
        if(status==false){
            if(logging){
                Debug.Log("ailiaModelOpenMem failed");
            }
            return false;
        }
        return OpenPoseEstimator();
    }

    private bool OpenPoseEstimator(){
        int status=AiliaPoseEstimator.ailiaCreatePoseEstimator(ref ailia_pose_estimator,base.ailia,algorithm);
        if(status!=Ailia.AILIA_STATUS_SUCCESS){
            if(logging){
                Debug.Log("ailiaCreatePoseEstimator failed "+status);
            }
            Close();
            return false;
        }
        return true;
    }

    //検出しきい値を設定する
    public bool SetThreshold(float threshold){
        int status=AiliaPoseEstimator.ailiaPoseEstimatorSetThreshold(ailia_pose_estimator,threshold);
        if(status!=Ailia.AILIA_STATUS_SUCCESS){
            if(logging){
                Debug.Log("ailiaPoseEstimatorSetThreshold failed "+status);
            }
            return false;
        }
        return true;
    }

    //画像から推論する
    public List<AiliaPoseEstimator.AILIAPoseEstimatorObjectPose> ComputePoseFromImage(Color32 [] camera,int tex_width,int tex_height){
        return ComputePoseFromImageWithFormat(camera,tex_width,tex_height,AiliaFormat.AILIA_IMAGE_FORMAT_RGBA);
    }

    //画像から推論する（上下反転）
    public List<AiliaPoseEstimator.AILIAPoseEstimatorObjectPose> ComputePoseFromImageB2T(Color32 [] camera,int tex_width,int tex_height){
        return ComputePoseFromImageWithFormat(camera,tex_width,tex_height,AiliaFormat.AILIA_IMAGE_FORMAT_RGBA_B2T);
    }

    private List<AiliaPoseEstimator.AILIAPoseEstimatorObjectPose> ComputePoseFromImageWithFormat(Color32 [] camera,int tex_width,int tex_height,uint format){
        if(ailia_pose_estimator==IntPtr.Zero){
            return null;
        }

        //バッファの固定
        GCHandle preview_handle = GCHandle.Alloc(camera, GCHandleType.Pinned);
        IntPtr preview_buf_ptr = preview_handle.AddrOfPinnedObject();

        //画像認識を行ってカテゴリを表示
        //Unityのカメラ画像は上下反転しているのでAILIA_IMAGE_FORMAT_RGBA_B2Tを指定
        int status=AiliaPoseEstimator.ailiaPoseEstimatorCompute(ailia_pose_estimator, preview_buf_ptr, (UInt32)tex_width*4,(UInt32)tex_width,(UInt32)tex_height,format);
        if(status!=Ailia.AILIA_STATUS_SUCCESS){
            if(logging){
                Debug.Log("ailiaPoseEstimatorCompute failed "+status);
            }
            return null;
        }

        //推論結果を表示
        List<AiliaPoseEstimator.AILIAPoseEstimatorObjectPose> result_list=new List<AiliaPoseEstimator.AILIAPoseEstimatorObjectPose>();
        uint count=0;
        status=AiliaPoseEstimator.ailiaPoseEstimatorGetObjectCount(ailia_pose_estimator,ref count);
        if(status!=Ailia.AILIA_STATUS_SUCCESS){
            if(logging){
                Debug.Log("ailiaPoseEstimatorGetObjectCount failed "+status);
            }
            return null;
        }

        for(int i=0;i<count;i++){
            AiliaPoseEstimator.AILIAPoseEstimatorObjectPose classifier_obj=new AiliaPoseEstimator.AILIAPoseEstimatorObjectPose();
            status=AiliaPoseEstimator.ailiaPoseEstimatorGetObjectPose(ailia_pose_estimator,classifier_obj,(uint)i,AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_OBJECT_POSE_VERSION);
            if(status!=Ailia.AILIA_STATUS_SUCCESS){
                if(logging){
                    Debug.Log("ailiaPoseEstimatorGetObjectPose failed"+status);
                }
                break;
            }
            result_list.Add(classifier_obj);
        }

        //バッファの開放
        preview_handle.Free();

        return result_list;
    }

    //画像から推論する
    public List<AiliaPoseEstimator.AILIAPoseEstimatorObjectUpPose> ComputeUpPoseFromImage(Color32[] camera, int tex_width, int tex_height)
    {
        return ComputeUpPoseFromImageWithFormat(camera, tex_width, tex_height, AiliaFormat.AILIA_IMAGE_FORMAT_RGBA);
    }

    //画像から推論する（上下反転）
    public List<AiliaPoseEstimator.AILIAPoseEstimatorObjectUpPose> ComputeUpPoseFromImageB2T(Color32[] camera, int tex_width, int tex_height)
    {
        return ComputeUpPoseFromImageWithFormat(camera, tex_width, tex_height, AiliaFormat.AILIA_IMAGE_FORMAT_RGBA_B2T);
    }
    private List<AiliaPoseEstimator.AILIAPoseEstimatorObjectUpPose> ComputeUpPoseFromImageWithFormat(Color32[] camera, int tex_width, int tex_height, uint format)
    {
        if (ailia_pose_estimator == IntPtr.Zero)
        {
            if (logging)
            {
                Debug.Log("ailia_pose_estimator is null");
            }
            return null;
        }

        //バッファの固定
        GCHandle preview_handle = GCHandle.Alloc(camera, GCHandleType.Pinned);
        IntPtr preview_buf_ptr = preview_handle.AddrOfPinnedObject();

        //画像認識を行ってカテゴリを表示
        //Unityのカメラ画像は上下反転しているのでAILIA_IMAGE_FORMAT_RGBA_B2Tを指定
        int status = AiliaPoseEstimator.ailiaPoseEstimatorCompute(ailia_pose_estimator, preview_buf_ptr, (UInt32)tex_width * 4, (UInt32)tex_width, (UInt32)tex_height, format);
        if (status != Ailia.AILIA_STATUS_SUCCESS)
        {
            if (logging)
            {
                Debug.Log("ailiaPoseEstimatorCompute failed " + status);
            }
            return null;
        }

        //推論結果を表示
        List<AiliaPoseEstimator.AILIAPoseEstimatorObjectUpPose> result_list = new List<AiliaPoseEstimator.AILIAPoseEstimatorObjectUpPose>();
        uint count = 0;
        AiliaPoseEstimator.ailiaPoseEstimatorGetObjectCount(ailia_pose_estimator, ref count);
        for (int i = 0; i < count; i++)
        {
            AiliaPoseEstimator.AILIAPoseEstimatorObjectUpPose classifier_obj = new AiliaPoseEstimator.AILIAPoseEstimatorObjectUpPose();
            status = AiliaPoseEstimator.ailiaPoseEstimatorGetObjectUpPose(ailia_pose_estimator, classifier_obj, (uint)i, AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_OBJECT_UPPOSE_VERSION);
            if (status != Ailia.AILIA_STATUS_SUCCESS)
            {
                if (logging)
                {
                    Debug.Log("ailiaPoseEstimatorGetObjectUpPose failed" + status);
                }
                break;
            }
            result_list.Add(classifier_obj);
        }

        //バッファの開放
        preview_handle.Free();

        return result_list;
    }

    //画像から推論する
    public List<AiliaPoseEstimator.AILIAPoseEstimatorObjectFace> ComputeFaceFromImage(Color32 [] camera,int tex_width,int tex_height){
        return ComputeFaceFromImageWithFormat(camera,tex_width,tex_height,AiliaFormat.AILIA_IMAGE_FORMAT_RGBA);
    }

    //画像から推論する（上下反転）
    public List<AiliaPoseEstimator.AILIAPoseEstimatorObjectFace> ComputeFaceFromImageB2T(Color32 [] camera,int tex_width,int tex_height){
        return ComputeFaceFromImageWithFormat(camera,tex_width,tex_height,AiliaFormat.AILIA_IMAGE_FORMAT_RGBA_B2T);
    }

    private List<AiliaPoseEstimator.AILIAPoseEstimatorObjectFace> ComputeFaceFromImageWithFormat(Color32 [] camera,int tex_width,int tex_height,uint format){
        //バッファの固定
        GCHandle preview_handle = GCHandle.Alloc(camera, GCHandleType.Pinned);
        IntPtr preview_buf_ptr = preview_handle.AddrOfPinnedObject();

        //画像認識を行ってカテゴリを表示
        //Unityのカメラ画像は上下反転しているのでAILIA_IMAGE_FORMAT_RGBA_B2Tを指定
        int status=AiliaPoseEstimator.ailiaPoseEstimatorCompute(ailia_pose_estimator, preview_buf_ptr, (UInt32)tex_width*4,(UInt32)tex_width,(UInt32)tex_height,format);
        if(status!=Ailia.AILIA_STATUS_SUCCESS){
            if(logging){
                Debug.Log("ailiaPoseEstimatorCompute failed "+status);
            }
            return null;
        }

        //推論結果を表示
        List<AiliaPoseEstimator.AILIAPoseEstimatorObjectFace> result_list=new List<AiliaPoseEstimator.AILIAPoseEstimatorObjectFace>();
        uint count=0;
        AiliaPoseEstimator.ailiaPoseEstimatorGetObjectCount(ailia_pose_estimator,ref count);
        for(int i=0;i<count;i++){
            AiliaPoseEstimator.AILIAPoseEstimatorObjectFace classifier_obj=new AiliaPoseEstimator.AILIAPoseEstimatorObjectFace();
            status=AiliaPoseEstimator.ailiaPoseEstimatorGetObjectFace(ailia_pose_estimator,classifier_obj,(uint)i,AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_OBJECT_FACE_VERSION);
            if(status!=Ailia.AILIA_STATUS_SUCCESS){
                if(logging){
                    Debug.Log("ailiaPoseEstimatorGetObjectFace failed"+status);
                }
                break;
            }
            result_list.Add(classifier_obj);
        }

        //バッファの開放
        preview_handle.Free();

        return result_list;
    }

    //画像から推論する
    public List<AiliaPoseEstimator.AILIAPoseEstimatorObjectHand> ComputeHandFromImage(Color32 [] camera,int tex_width,int tex_height){
        return ComputeHandFromImageWithFormat(camera,tex_width,tex_height,AiliaFormat.AILIA_IMAGE_FORMAT_RGBA);
    }

    //画像から推論する（上下反転）
    public List<AiliaPoseEstimator.AILIAPoseEstimatorObjectHand> ComputeHandFromImageB2T(Color32 [] camera,int tex_width,int tex_height){
        return ComputeHandFromImageWithFormat(camera,tex_width,tex_height,AiliaFormat.AILIA_IMAGE_FORMAT_RGBA_B2T);
    }

    private List<AiliaPoseEstimator.AILIAPoseEstimatorObjectHand> ComputeHandFromImageWithFormat(Color32 [] camera,int tex_width,int tex_height,uint format){
        //バッファの固定
        GCHandle preview_handle = GCHandle.Alloc(camera, GCHandleType.Pinned);
        IntPtr preview_buf_ptr = preview_handle.AddrOfPinnedObject();

        //画像認識を行ってカテゴリを表示
        //Unityのカメラ画像は上下反転しているのでAILIA_IMAGE_FORMAT_RGBA_B2Tを指定
        int status=AiliaPoseEstimator.ailiaPoseEstimatorCompute(ailia_pose_estimator, preview_buf_ptr, (UInt32)tex_width*4,(UInt32)tex_width,(UInt32)tex_height,format);
        if(status!=Ailia.AILIA_STATUS_SUCCESS){
            if(logging){
                Debug.Log("ailiaPoseEstimatorCompute failed "+status);
            }
            return null;
        }

        //推論結果を表示
        List<AiliaPoseEstimator.AILIAPoseEstimatorObjectHand> result_list=new List<AiliaPoseEstimator.AILIAPoseEstimatorObjectHand>();
        uint count=0;
        AiliaPoseEstimator.ailiaPoseEstimatorGetObjectCount(ailia_pose_estimator,ref count);
        for(int i=0;i<count;i++){
            AiliaPoseEstimator.AILIAPoseEstimatorObjectHand classifier_obj=new AiliaPoseEstimator.AILIAPoseEstimatorObjectHand();
            status=AiliaPoseEstimator.ailiaPoseEstimatorGetObjectHand(ailia_pose_estimator,classifier_obj,(uint)i,AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_OBJECT_HAND_VERSION);
            if(status!=Ailia.AILIA_STATUS_SUCCESS){
                if(logging){
                    Debug.Log("ailiaPoseEstimatorGetObjectHand failed"+status);
                }
                break;
            }
            result_list.Add(classifier_obj);
        }

        //バッファの開放
        preview_handle.Free();

        return result_list;
    }

    //開放する
    public override void Close(){
        if(ailia_pose_estimator!=IntPtr.Zero){
            AiliaPoseEstimator.ailiaDestroyPoseEstimator(ailia_pose_estimator);
            ailia_pose_estimator=IntPtr.Zero;
        }
        base.Close();
    }
}
