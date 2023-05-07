/**
* \~japanese
* @file
* @brief AILIA Unity Plugin PoseEstimator Model Class
* @author AXELL Corporation
* @date  November 22, 2021
* 
* \~english
* @file
* @brief AILIA Unity Plugin PoseEstimator Model Class
* @author AXELL Corporation
* @date  November 22, 2021
*/

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
    /**
    * \~japanese
    * @brief モデル設定を行います。
    * @param set_algorithm   アルゴリズム
    * @return
    *   成功した場合 true を返す。
    * @details
    *   モデルのアルゴリズムを指定します。
    *   　
    * \~english
    * @brief   Model setting.
    * @param set_algorithm   Algorithm
    * @return
    *   Returns true on success.
    * @details
    *   Specifies the algorithm of the model.
    */
    public bool Settings(uint set_algorithm){
        algorithm=set_algorithm;
        return true;
    }

    //ファイルから開く
    /**
    * \~japanese
    * @brief モデルファイルからネットワークオブジェクトを作成します。
    * @param prototxt     prototxtファイルのパス名(MBSC or UTF16)
    * @param model_path   protobuf/onnxファイルのパス名(MBSC or UTF16)
    * @return
    *   成功した場合は ture を、失敗した場合は false を返す。
    * @details        
    *   モデルファイルからネットワークオブジェクトを作成します。
    * 
    * \~english
    * @brief   Create a network object from a model file.
    * @param prototxt     Path name of the prototxt file (MBSC or UTF16)
    * @param model_path   Pathname of the protobuf/onnx file (MBSC or UTF16)
    * @return
    *   If this funcyion is successful, it returns true.
    * @details
    *   Create a network object from a model file.
    */
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
    /**
    * \~japanese
    * @brief ファイルコールバックからネットワークオブジェクトを作成します。
    * @param callback   ユーザ定義ファイルアクセスコールバック関数構造体
    * @param alg1       \ref AILIA_USER_API_FOPEN に通知される引数ポインタ
    * @param alg2       \ref AILIA_USER_API_FOPEN に通知される引数ポインタ
    * @return
    *   成功した場合は ture を、失敗した場合は false を返す。
    * @details
    *   ファイルコールバックからネットワークオブジェクトを作成します。
    *   
    * \~english
    * @brief   Creates a network object from a file callback.
    * @param callback   User-defined file access callback function structure
    * @param alg1       Argument pointer notified to \ref AILIA_USER_API_FOPEN
    * @param alg2       Argument pointer notified to \ref AILIA_USER_API_FOPEN
    * @return
    *   If this funcyion is successful, it returns true.
    * @details
    *   Creates a network object from a file callback.
    */
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
    /**
    * \~japanese
    * @brief メモリからネットワークオブジェクトを作成します。
    * @param prototxt     prototxtファイルのデータへのポインタ 
    * @param model_path   protobuf/onnxファイルのデータへのポインタ
    * @return
    *   成功した場合は ture を、失敗した場合は false を返す。
    * @details        
    *   メモリからネットワークオブジェクトを作成します。
    * 
    * \~english
    * @brief   Creates network objects from memory.
    * @param prototxt     Pointer to data in prototxt file
    * @param model_path   Pointer to data in protobuf/onnx file
    * @return
    *   If this funcyion is successful, it returns true.
    * @details
    *   Creates network objects from memory.
    */
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

    /**
    * \~japanese
    * @brief 骨格検出インスタンスを作成します。
    * @return
    *   成功した場合は true を、失敗した場合は false を返す。
    * @details        
    *   骨格検出インスタンスを作成します。
    * 
    * \~english
    * @brief   Create a pose estimation instance.
    * @return
    *   Returns true on success, false on failure.
    * @details        
    *  Create a pose estimation instance.
    */
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
    /**
    * \~japanese
    * @brief 検出閾値を設定します。
    * @param threshold   検出閾値 0.0以上1.0以下の値で、値が小さいほど検出しやすくなります。
    * @return
    *   成功した場合は true を、失敗した場合は false を返す。
    * @details        
    *   検出閾値を設定します。
    * 
    * \~english
    * @brief   Sets the detection threshold.
    * @param threshold   Detection threshold A value between 0.0 and 1.0; the smaller the value, the easier it is to detect.
    * @return
    *   Returns true on success, false on failure.
    * @details        
    *   Set the detection threshold.
    */
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
    /**
    * \~japanese
    * @brief 画像から骨格検出を行います。
    * @param image               検出対象画像
    * @param image_width         画像幅
    * @param image_height        画像高さ
    * @return
    *   検出結果のリスト
    * @details
    *   画像から骨格検出を行い、検出結果をリストで返します。
    * 
    * \~english
    * @brief   Skeletal detection is performed from images.
    * @param image             Image to be detected
    * @param image_width       Image width
    * @param image_height      Image height
    * @return
    *   List of detection results
    * @details
    *   Performs skeletal detection from an image and returns a list of detection results.
    */
    public List<AiliaPoseEstimator.AILIAPoseEstimatorObjectPose> ComputePoseFromImage(Color32 [] image,int image_width,int image_height){
        return ComputePoseFromImageWithFormat(image,image_width,image_height,AiliaFormat.AILIA_IMAGE_FORMAT_RGBA);
    }

    //画像から推論する（上下反転）
    /**
    * \~japanese
    * @brief 上下反転の画像から骨格検出を行います。
    * @param image               検出対象画像
    * @param image_width         画像幅
    * @param image_height        画像高さ
    * @return
    *   検出結果のリスト
    * @details
    *   画像から骨格検出を行い、検出結果をリストで返します。
    * 
    * \~english
    * @brief   Skeletal detection is performed from an bottom-top image.
    * @param image             Image to be detected
    * @param image_width       Image width
    * @param image_height      Image height
    * @return
    *   List of detection results
    * @details
    *   Performs skeletal detection from an image and returns a list of detection results.
    */
    public List<AiliaPoseEstimator.AILIAPoseEstimatorObjectPose> ComputePoseFromImageB2T(Color32 [] image,int image_width,int image_height){
        return ComputePoseFromImageWithFormat(image,image_width,image_height,AiliaFormat.AILIA_IMAGE_FORMAT_RGBA_B2T);
    }

    /**
    * \~japanese
    * @brief 画像から骨格検出を行い、結果を表示します。
    * @param image               検出対象画像
    * @param image_width         画像幅
    * @param image_height        画像高さ
    * @param foramt              画像形式 (AILIA_IMAGE_FORMAT_*)            
    * @return
    *   検出結果のリスト
    * @details
    *   画像から骨格検出を行い、検出結果をリストで返します。
    * 
    * \~english
    * @brief   Performs skeletal detection from the image and displays the results.
    * @param image             Image to be detected
    * @param image_width       Image width
    * @param image_height      Image height
    * @param foramt            Image Format (AILIA_IMAGE_FORMAT_*)            
    * @return
    *   List of detection results
    * @details
    *   Performs skeletal detection from an image and returns a list of detection results.
    */
    private List<AiliaPoseEstimator.AILIAPoseEstimatorObjectPose> ComputePoseFromImageWithFormat(Color32 [] image,int image_width,int image_height,uint format){
        if(ailia_pose_estimator==IntPtr.Zero){
            return null;
        }

        //バッファの固定
        GCHandle preview_handle = GCHandle.Alloc(image, GCHandleType.Pinned);
        IntPtr preview_buf_ptr = preview_handle.AddrOfPinnedObject();

        //画像認識を行ってカテゴリを表示
        //Unityのカメラ画像は上下反転しているのでAILIA_IMAGE_FORMAT_RGBA_B2Tを指定
        int status=AiliaPoseEstimator.ailiaPoseEstimatorCompute(ailia_pose_estimator, preview_buf_ptr, (UInt32)image_width*4,(UInt32)image_width,(UInt32)image_height,format);
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
    /**
    * \~japanese
    * @brief 画像から上半身骨格検出を行います。
    * @param image               検出対象画像
    * @param image_width         画像幅
    * @param image_height        画像高さ
    * @return
    *   検出結果のリスト
    * @details
    *   画像から上半身の骨格検出を行い、検出結果をリストで返します。
    * 
    * \~english
    * @brief   Detection is performed from the image.
    * @param image             Image to be detected
    * @param image_width       Image width
    * @param image_height      Image Height
    * @return
    *   List of detection results
    * @details
    *   Performs upper body skeleton detection from an image and returns a list of detection results.
    */
    public List<AiliaPoseEstimator.AILIAPoseEstimatorObjectUpPose> ComputeUpPoseFromImage(Color32[] image, int image_width, int image_height)
    {
        return ComputeUpPoseFromImageWithFormat(image, image_width, image_height, AiliaFormat.AILIA_IMAGE_FORMAT_RGBA);
    }

    //画像から推論する（上下反転）
    /**
    * \~japanese
    * @brief 上下反転の画像から上半身骨格検出を行います。
    * @param image             検出対象画像
    * @param image_width         画像幅
    * @param image_height        画像高さ
    * @return
    *   検出結果のリスト
    * @details
    *   画像から上半身骨格検出を行い、検出結果をリストで返します。
    * 
    * \~english
    * @brief   Detection is performed from an upside-down image.
    * @param image             Image to be detected
    * @param image_width       Image width
    * @param image_height      Image height
    * @return
    *   List of detection results
    * @details
    *   Performs upper body skeleton detection from an image and returns a list of detection results.
    */
    public List<AiliaPoseEstimator.AILIAPoseEstimatorObjectUpPose> ComputeUpPoseFromImageB2T(Color32[] image, int image_width, int image_height)
    {
        return ComputeUpPoseFromImageWithFormat(image, image_width, image_height, AiliaFormat.AILIA_IMAGE_FORMAT_RGBA_B2T);
    }

    /**
   * \~japanese
   * @brief 画像から検出を行い、結果を表示します。（UpPoseは何を検出してる？）
   * @param image             検出対象画像
   * @param image_width         画像幅
   * @param image_height        画像高さ
   * @param foramt            画像形式 (AILIA_IMAGE_FORMAT_*)
   * @return
    *   検出結果のリスト
   * @details
   *    画像から上半身骨格検出を行い、検出結果をリストで返します。
   * 
   * \~english
   * @brief   Performs detection from the image and displays the results.
   * @param image             Image to be detected
   * @param image_width       Image width
   * @param image_height      Image height
   * @param foramt            Image format (AILIA_IMAGE_FORMAT_*)
   * @return
    *   List of detection results
   * @details      
   *    Performs upper body skeleton detection from an image and returns a list of detection results.
   */  
    private List<AiliaPoseEstimator.AILIAPoseEstimatorObjectUpPose> ComputeUpPoseFromImageWithFormat(Color32[] image, int image_width, int image_height, uint format)
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
        GCHandle preview_handle = GCHandle.Alloc(image, GCHandleType.Pinned);
        IntPtr preview_buf_ptr = preview_handle.AddrOfPinnedObject();

        //画像認識を行ってカテゴリを表示
        //Unityのカメラ画像は上下反転しているのでAILIA_IMAGE_FORMAT_RGBA_B2Tを指定
        int status = AiliaPoseEstimator.ailiaPoseEstimatorCompute(ailia_pose_estimator, preview_buf_ptr, (UInt32)image_width * 4, (UInt32)image_width, (UInt32)image_height, format);
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
        status=AiliaPoseEstimator.ailiaPoseEstimatorGetObjectCount(ailia_pose_estimator, ref count);
        if (status != Ailia.AILIA_STATUS_SUCCESS)
        {
            if (logging)
            {
                Debug.Log("ailiaPoseEstimatorGetObjectCount failed " + status);
            }
            return null;
        }

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
   /**
   * \~japanese
   * @brief 画像から顔特徴点検出を行います。
   * @param image             検出対象画像
   * @param image_width         画像幅
   * @param image_height        画像高さ
   * @return
   *   検出結果のリスト
   * @details
   *   画像から顔特徴点検出を行い結果をリストで返します。
   * 
   * \~english
   * @brief   Performs facial feature point detection from images.
   * @param image             Image to be detected
   * @param image_width       Image width
   * @param image_height      Image height
   * @return
   *   List of detection results
   * @details
   *   Performs facial feature point detection from an image and returns a list of results.
   */
    public List<AiliaPoseEstimator.AILIAPoseEstimatorObjectFace> ComputeFaceFromImage(Color32 [] image,int image_width,int image_height){
        return ComputeFaceFromImageWithFormat(image,image_width,image_height,AiliaFormat.AILIA_IMAGE_FORMAT_RGBA);
    }

    //画像から推論する（上下反転）
   /**
   * \~japanese
   * @brief 上下反転画像から顔特徴点検出を行います。
   * @param image               検出対象画像
   * @param image_width         画像幅
   * @param image_height        画像高さ
   * @return
   *   検出結果のリスト
   * @details
   *   画像から顔特徴点検出を行い結果をリストで返します。
   * 
   * \~english
   * @brief   Detects facial feature points from bottom-top images.
   * @param image             Image to be detected
   * @param image_width       Image width
   * @param image_height      Image height
   * @return
   *   List of detection results
   * @details
   *   Performs facial feature point detection from an image and returns a list of results.
   */
    public List<AiliaPoseEstimator.AILIAPoseEstimatorObjectFace> ComputeFaceFromImageB2T(Color32 [] image,int image_width,int image_height){
        return ComputeFaceFromImageWithFormat(image,image_width,image_height,AiliaFormat.AILIA_IMAGE_FORMAT_RGBA_B2T);
    }

    /**
    * \~japanese
    * @brief 画像から顔特徴点検出を行い、結果を表示します。
    * @param image             検出対象画像
    * @param image_width       画像幅
    * @param image_height      画像高さ
    * @param foramt            画像形式 (AILIA_IMAGE_FORMAT_*)
    * @return
    *   検出結果のリスト
    * @details
    *   画像から顔特徴点検出を行い結果をリストで返します。
    * 
    * \~english
    * @brief   Performs facial feature point detection from an image and displays the results.
    * @param image             image to be detected
    * @param image_width       Image width
    * @param image_height      Image height
    * @param foramt            Image format (AILIA_IMAGE_FORMAT_*)
    * @return
　  *   List of detection results
    * @details
    *   Performs facial feature point detection from an image and returns a list of results.
    */
    private List<AiliaPoseEstimator.AILIAPoseEstimatorObjectFace> ComputeFaceFromImageWithFormat(Color32 [] image,int image_width,int image_height,uint format){
        //バッファの固定
        GCHandle preview_handle = GCHandle.Alloc(image, GCHandleType.Pinned);
        IntPtr preview_buf_ptr = preview_handle.AddrOfPinnedObject();

        //画像認識を行ってカテゴリを表示
        //Unityのカメラ画像は上下反転しているのでAILIA_IMAGE_FORMAT_RGBA_B2Tを指定
        int status=AiliaPoseEstimator.ailiaPoseEstimatorCompute(ailia_pose_estimator, preview_buf_ptr, (UInt32)image_width*4,(UInt32)image_width,(UInt32)image_height,format);
        if(status!=Ailia.AILIA_STATUS_SUCCESS){
            if(logging){
                Debug.Log("ailiaPoseEstimatorCompute failed "+status);
            }
            return null;
        }

        //推論結果を表示
        List<AiliaPoseEstimator.AILIAPoseEstimatorObjectFace> result_list=new List<AiliaPoseEstimator.AILIAPoseEstimatorObjectFace>();
        uint count=0;
        status=AiliaPoseEstimator.ailiaPoseEstimatorGetObjectCount(ailia_pose_estimator,ref count);
        if (status != Ailia.AILIA_STATUS_SUCCESS)
        {
            if (logging)
            {
                Debug.Log("ailiaPoseEstimatorGetObjectCount failed " + status);
            }
            return null;
        }

        for (int i=0;i<count;i++){
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
    /**
    * \~japanese
    * @brief 画像から手の検出を行います。
    * @param image               検出対象画像
    * @param image_width         画像幅
    * @param image_height        画像高さ
    * @return
    *   検出結果のリスト
    * @details
    *   画像から手の検出を行い結果をリストで返します。
    *   
    * \~english
    * @brief   Detects hands from an image.
    * @param image             Image to be detected
    * @param image_width       Image width
    * @param image_height      Image height
    * @return
    *   List of detection results
    * @details
    *   Detects hands from an image and returns a list of results.
    */
    public List<AiliaPoseEstimator.AILIAPoseEstimatorObjectHand> ComputeHandFromImage(Color32 [] image,int image_width,int image_height){
        return ComputeHandFromImageWithFormat(image,image_width,image_height,AiliaFormat.AILIA_IMAGE_FORMAT_RGBA);
    }

    //画像から推論する（上下反転）
    /**
    * \~japanese
    * @brief 上下反転画像から手の検出を行います。
    * @param image               検出対象画像
    * @param image_width         画像幅
    * @param image_height        画像高さ
    * @return
    *   検出結果のリスト
    * @details
    *   画像から手の検出を行い結果をリストで返します。
    * 
    * \~english
    * @brief   Detects hands from an bottom-top image.
    * @param image             Image to be detected
    * @param image_width       Image width
    * @param image_height      Image height
    * @return
    *   List of detection results
    * @details
    *   Detects hands from an image and returns a list of results.
    */
    public List<AiliaPoseEstimator.AILIAPoseEstimatorObjectHand> ComputeHandFromImageB2T(Color32 [] image,int image_width,int image_height){
        return ComputeHandFromImageWithFormat(image,image_width,image_height,AiliaFormat.AILIA_IMAGE_FORMAT_RGBA_B2T);
    }

    /**
    * \~japanese
    * @brief 画像から手検出を行い、結果を返します。
    * @param image             検出対象画像
    * @param image_width         画像幅
    * @param image_height        画像高さ
    * @param foramt            画像形式 (AILIA_IMAGE_FORMAT_*)
    * @return
    *   検出結果のリスト
    * @details      
    *   画像から手の検出を行い結果をリストで返します。
    * 
    * \~english
    * @brief Performs hand detection from an image and returns the result.
    * @param image             Image to be detected
    * @param image_width       Image width
    * @param image_height      Image height
    * @param foramt            Image format (AILIA_IMAGE_FORMAT_*)
    * @return
    *   List of detection results
    * @details      
    *   Detects hands from an image and returns a list of results.
    */
    private List<AiliaPoseEstimator.AILIAPoseEstimatorObjectHand> ComputeHandFromImageWithFormat(Color32 [] image,int image_width,int image_height,uint format){
        //バッファの固定
        GCHandle preview_handle = GCHandle.Alloc(image, GCHandleType.Pinned);
        IntPtr preview_buf_ptr = preview_handle.AddrOfPinnedObject();

        //画像認識を行ってカテゴリを表示
        //Unityのカメラ画像は上下反転しているのでAILIA_IMAGE_FORMAT_RGBA_B2Tを指定
        int status=AiliaPoseEstimator.ailiaPoseEstimatorCompute(ailia_pose_estimator, preview_buf_ptr, (UInt32)image_width*4,(UInt32)image_width,(UInt32)image_height,format);
        if(status!=Ailia.AILIA_STATUS_SUCCESS){
            if(logging){
                Debug.Log("ailiaPoseEstimatorCompute failed "+status);
            }
            return null;
        }

        //推論結果を表示
        List<AiliaPoseEstimator.AILIAPoseEstimatorObjectHand> result_list=new List<AiliaPoseEstimator.AILIAPoseEstimatorObjectHand>();
        uint count=0;
        status=AiliaPoseEstimator.ailiaPoseEstimatorGetObjectCount(ailia_pose_estimator,ref count);
        if (status != Ailia.AILIA_STATUS_SUCCESS)
        {
            if (logging)
            {
                Debug.Log("ailiaPoseEstimatorGetObjectCount failed " + status);
            }
            return null;
        }

        for (int i=0;i<count;i++){
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
    /**
    * \~japanese
    * @brief 検出オブジェクトを破棄します。
    * @return
    *   なし。
    * @details        
    *   骨格検出オブジェクトを破棄します。
    * 
    * \~english
    * @brief   Destroy the detection object.
    * @return
    *   Return nothing.
    * @details        
    *   Destroys the detection object.
    */
    public override void Close(){
        if(ailia_pose_estimator!=IntPtr.Zero){
            AiliaPoseEstimator.ailiaDestroyPoseEstimator(ailia_pose_estimator);
            ailia_pose_estimator=IntPtr.Zero;
        }
        base.Close();
    }

    /**
    * \~japanese
    * @brief リソースを解放します。
    *   
    *  \~english
    * @brief   Release resources.
    */
    public override void Dispose()
    {
        Dispose(true);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing){
            // release managed resource
        }
        Close(); // release unmanaged resource
        base.Dispose(disposing);
    }

    ~AiliaPoseEstimatorModel(){
        Dispose(false);
    }
}
