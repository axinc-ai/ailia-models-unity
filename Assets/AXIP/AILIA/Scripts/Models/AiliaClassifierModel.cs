/**
* \~japanese
* @file
* @brief AILIA Unity Plugin Classifier Model Class
* @author AXELL Corporation
* @date  November 22, 2021
* 
* \~english
* @file
* @brief AILIA Unity Plugin Classifier Model Class
* @author AXELL Corporation
* @date  November 22, 2021
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Runtime.InteropServices;

public class AiliaClassifierModel : AiliaModel{
    private IntPtr ailia_classifier = IntPtr.Zero;

    private uint format=AiliaFormat.AILIA_NETWORK_IMAGE_FORMAT_BGR;
    private uint channel=AiliaFormat.AILIA_NETWORK_IMAGE_CHANNEL_FIRST;
    private uint range=AiliaFormat.AILIA_NETWORK_IMAGE_RANGE_SIGNED_INT8;

    //モデル設定

    /**
    * \~japanese
    * @brief モデル設定を行います。
    * @param set_format    ネットワークの画像フォーマット （AILIA_NETWORK_IMAGE_FORMAT_*）
    * @param set_channel   ネットワークの画像チャンネル (AILIA_NETWORK_IMAGE_CHANNEL_*)
    * @param set_range     ネットワークの画像レンジ （AILIA_NETWORK_IMAGE_RANGE_*）
    * @return
    *   成功した場合 true を返す。
    * @details
    *   必要な画像の前処理の設定を行います。
    * \~english
    * @brief Model setting.
    * @param set_format    The network image format (AILIA_NETWORK_IMAGE_FORMAT_*)
    * @param set_channel   The network image channel (AILIA_NETWORK_IMAGE_CHANNEL_*)
    * @param set_range     The network image range (AILIA_NETWORK_IMAGE_RANGE_*)
    * @return
    *   If this funcyion is successful, it returns true.
    * @details        
    *   Configure the necessary image preprocessing settings.
    */
    public bool Settings(uint set_format,uint set_channel,uint set_range){
        format=set_format;
        channel=set_channel;
        range=set_range;
        return true;
    }

    //ファイルから開く

    /**
    * \~japanese
    * @brief モデルファイルからネットワークオブジェクトを作成します。
    * @param prototxt     prototxtファイルのパス名(MBSC or UTF16)
    * @param model_path   protobuf/onnxファイルのパス名(MBSC or UTF16)
    * @return
    *   成功した場合はtrue、失敗した場合はfalseを返す。
    * @details        
    *   モデルファイルからネットワークオブジェクトを作成します。
    * 
    * * \~english
    * @brief   Create a network object from a model file.
    * @param prototxt     Path name of the prototxt file (MBSC or UTF16)
    * @param model_path   Pathname of the protobuf/onnx file (MBSC or UTF16)
    * @return
    *   If this function is successful, it returns  true  , or  false  otherwise.
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
        return OpenClassifier();
    }

    //コールバックから開く

    /**
    * \~japanese
    * @brief ファイルコールバックからネットワークオブジェクトを作成します。
    * @param callback   ユーザ定義ファイルアクセスコールバック関数構造体
    * @param alg1       \ref AILIA_USER_API_FOPEN に通知される引数ポインタ
    * @param alg2       \ref AILIA_USER_API_FOPEN に通知される引数ポインタ
    * @return
    *    成功した場合はtrue、失敗した場合はfalseを返す。
    * @details
    *  　ファイルコールバックからネットワークオブジェクトを作成します。
    * 
    * \~english
    * @brief  Creates a network object from a file callback.
    * @param callback   User-defined file access callback function structure
    * @param alg1       Argument pointer notified to \ref AILIA_USER_API_FOPEN
    * @param alg2       Argument pointer notified to \ref AILIA_USER_API_FOPEN
    * @return
    *    If this function is successful, it returns  true  , or  false  otherwise.
    * @details
    *  　Creates a network object from a file callback.
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
        return OpenClassifier();
    }

    //メモリから開く

    /**
    * \~japanese
    * @brief メモリからネットワークオブジェクトを作成します。
    * @param prototxt     prototxtファイルのデータへのポインタ 
    * @param model_path   protobuf/onnxファイルのデータへのポインタ
    * @return
    *   成功した場合はtrue、失敗した場合はfalseを返す。
    * @details        
    *   メモリからネットワークオブジェクトを作成します。
    * 
    * \~english
    * @brief   Creates network objects from memory.
    * @param prototxt     Pointer to data in prototxt file 
    * @param model_path   Pointer to data in protobuf/onnx file
    * @return
    *   If this function is successful, it returns  true  , or  false  otherwise.
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
        return OpenClassifier();
    }

    /**
    * \~japanese
    * @brief 識別オブジェクトを作成します。
    * @return
    *   成功した場合は true を、失敗した場合は false を返します。
    * @details        
    *   AiliaClassifier.ailiaCreateClassifier() で識別オブジェクトを作成します。
    *   
    * \~english
    * @brief   Create an classification object.
    * @return
    *   Returns true on success, false on failure.
    * @details        
    *   Create an classification object with AiliaClassifier.ailiaCreateClassifier().
    */
    private bool OpenClassifier(){
        int status=AiliaClassifier.ailiaCreateClassifier(ref ailia_classifier,ailia,format,channel,range);
        if(status!=Ailia.AILIA_STATUS_SUCCESS){
            if(logging){
                Debug.Log("ailiaCreateClassifier failed "+status);
            }
            Close();
            return false;
        }
        return true;
    }

    //画像から推論する

    /**
    * \~japanese
    * @brief 画像から物体識別を行います。
    * @param image             検出対象画像
    * @param image_width         画像幅
    * @param image_height        画像高さ
    * @param max_class_count   認識結果の最大個数
    * @return
    *   識別結果のオブジェクトのリスト。
    * @details
    *   画像から物体識別を行いリストを返します。
    *
    * \~english
    * @brief   Performs object classification from images.
    * @param image             Image to be classify
    * @param image_width       Image width
    * @param image_height      Image height
    * @param max_class_count   Maximum number of classification results
    * @return
    *   List of objects resulting from the classification.
    * @details
    *   Performs object classification from an image and returns a list.
    */
    public List<AiliaClassifier.AILIAClassifierClass> ComputeFromImage(Color32 [] image,int image_width,int image_height,uint max_class_count){
        return ComputeFromImageWithFormat(image,image_width,image_height,max_class_count,AiliaFormat.AILIA_IMAGE_FORMAT_RGBA);
    }

    //画像から推論する（上下反転）
    /**
    * \~japanese
    * @brief 上下反転画像から物体識別を行います。
    * @param image             検出対象画像
    * @param image_width         画像幅
    * @param image_height        画像高さ
    * @param max_class_count   認識結果の最大個数
    * @return
    *   識別結果のオブジェクトのリスト。
    * @details
    *   画像から物体識別を行いリストを返します。
    *
    * \~english
    * @brief   OPerforms object classification from bottom-top images.
    * @param image             Image to be classify
    * @param image_width       Image width
    * @param image_height      Image height
    * @param max_class_count   Maximum number of classification results
    * @return
    *   List of objects resulting from the classification.
    * @details        
    *   Performs object classification from an image and returns a list.
    */
    public List<AiliaClassifier.AILIAClassifierClass> ComputeFromImageB2T(Color32 [] image,int image_width,int image_height,uint max_class_count){
        return ComputeFromImageWithFormat(image,image_width,image_height,max_class_count,AiliaFormat.AILIA_IMAGE_FORMAT_RGBA_B2T);
    }


    /**
    * \~japanese
    * @brief 画像から物体識別を行います。
    * @param image             検出対象画像
    * @param image_width         画像幅
    * @param image_height        画像高さ
    * @param max_class_count   認識結果の最大個数
    * @param format            画像形式
    * @return
    *   識別結果のオブジェクトのリスト。
    * @details        
    *   画像から物体識別を行いリストを返します。
    * 
    * \~english
    * @brief   OPerforms object classification from images.
    * @param image             Image to be classify
    * @param image_width       Image width
    * @param image_height      Image height
    * @param max_class_count   Maximum number of classification results
    * @param format            Image format
    * @return
    *   List of objects resulting from the classification.
    * @details        
    *   Performs object classification from an image and returns a list.
    */
    private List<AiliaClassifier.AILIAClassifierClass> ComputeFromImageWithFormat(Color32 [] image,int image_width,int image_height,uint max_class_count,uint format){
        if(ailia_classifier==IntPtr.Zero){
            return null;
        }

        //バッファの固定
        GCHandle preview_handle = GCHandle.Alloc(image, GCHandleType.Pinned);
        IntPtr preview_buf_ptr = preview_handle.AddrOfPinnedObject();

        //画像認識を行ってカテゴリを表示
        //Unityのカメラ画像は上下反転しているのでAILIA_IMAGE_FORMAT_RGBA_B2Tを指定
        int status=AiliaClassifier.ailiaClassifierCompute(ailia_classifier, preview_buf_ptr, (UInt32)image_width*4,(UInt32)image_width,(UInt32)image_height,format,max_class_count);
        if(status!=Ailia.AILIA_STATUS_SUCCESS){
            if(logging){
                Debug.Log("ailiaClassifierCompute failed "+status);
            }
            return null;
        }

        //推論結果を表示
        List<AiliaClassifier.AILIAClassifierClass> result_list=new List<AiliaClassifier.AILIAClassifierClass>();
        for(int i=0;i<max_class_count;i++){
            AiliaClassifier.AILIAClassifierClass classifier_obj=new AiliaClassifier.AILIAClassifierClass();
            status=AiliaClassifier.ailiaClassifierGetClass(ailia_classifier,classifier_obj,(uint)i,AiliaClassifier.AILIA_CLASSIFIER_CLASS_VERSION);
            if(status!=Ailia.AILIA_STATUS_SUCCESS){
                if(logging){
                    Debug.Log("ailiaClassifierGetClass failed"+status);
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
    * @brief 識別オブジェクトを破棄します。
    * @return
    *   なし。
    * @details        
    *   識別オブジェクトを破棄します。
    * 
    * \~english
    * @brief   Destroy the classification object.
    * @return
    *   Return nothing.
    * @details        
    *   Destroys the classification object.
    */
    public override void Close(){
        if(ailia_classifier!=IntPtr.Zero){
            AiliaClassifier.ailiaDestroyClassifier(ailia_classifier);
            ailia_classifier=IntPtr.Zero;
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

    ~AiliaClassifierModel(){
        Dispose(false);
    }
}
