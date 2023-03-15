/**
* \~japanese
* @file
* @brief AILIA Unity Plugin Feature Extractor Model Class
* @author AXELL Corporation
* @date  November 22, 2021
* 
* \~english
* @file
* @brief AILIA Unity Plugin Feature Extractor Model Class
* @author AXELL Corporation
* @date  November 22, 2021
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Runtime.InteropServices;

public class AiliaFeatureExtractorModel : AiliaModel{
    private IntPtr ailia_feature_extractor = IntPtr.Zero;

    private uint format=AiliaFormat.AILIA_NETWORK_IMAGE_FORMAT_BGR;
    private uint channel=AiliaFormat.AILIA_NETWORK_IMAGE_CHANNEL_FIRST;
    private uint range=AiliaFormat.AILIA_NETWORK_IMAGE_RANGE_SIGNED_INT8;
    private string layer_name="";
    private uint distace_type=AiliaFeatureExtractor.AILIA_FEATURE_EXTRACTOR_DISTANCE_L2NORM;

    //モデル設定
    /**
    * \~japanese
    * @brief モデル設定を行います。
    * @param set_format          ネットワークの画像フォーマット (AILIA_NETWORK_IMAGE_FORMAT_*)
    * @param set_channel         ネットワークの画像チャンネル (AILIA_NETWORK_IMAGE_CHANNEL_*)
    * @param set_range           ネットワークの画像レンジ (AILIA_NETWORK_IMAGE_RANGE_*)
    * @param set_distance_type   特徴に対応したレイヤーの名称 (VGG16の場合はfc1, NULLで最終レイヤー)
    * @param set_layer_name      特徴に対応したレイヤーの名称 (VGG16の場合はfc1, NULLで最終レイヤー)
    * @return
    *   成功した場合 true を返す。
    * @details        
    *   ネットワークの画像の前処理と、距離計算の設定を行います。
    * 
    * \~english
    * @brief   Model setting.
    * @param set_format          The network image format (AILIA_NETWORK_IMAGE_FORMAT_*)
    * @param set_channel         The network image channel (AILIA_NETWORK_IMAGE_CHANNEL_*)
    * @param set_range           The network image range (AILIA_NETWORK_IMAGE_RANGE_*)
    * @param set_distance_type   The type of the distance in feature space
    * @param set_layer_name      The name of the layer corresponding to the feature (fc1 for VGG16 and NULL for the last layer)
    * @return
    *   Returns true on success.
    * @details
    *   Pre-process images of the network and set up distance calculations.
    */
    public bool Settings(uint set_format,uint set_channel,uint set_range,uint set_distance_type,string set_layer_name){
        format=set_format;
        channel=set_channel;
        range=set_range;
        distace_type=set_distance_type;
        layer_name=set_layer_name;
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
    * \~english
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
        return OpenFeatureExtractor();
    }

    //コールバックから開く
    /**
    * \~japanese
    * @brief ファイルコールバックからネットワークオブジェクトを作成します。
    * @param callback   ユーザ定義ファイルアクセスコールバック関数構造体
    * @param alg1       \ref AILIA_USER_API_FOPEN に通知される引数ポインタ
    * @param alg2       \ref AILIA_USER_API_FOPEN に通知される引数ポインタ
    * @return
    *   成功した場合はtrue、失敗した場合はfalseを返す。
    * @details        
    *   ファイルコールバックからネットワークオブジェクトを作成します。
    * 
    * \~english
    * @brief   Creates a network object from a file callback.
    * @param callback   User-defined file access callback function structure
    * @param alg1       Argument pointer notified to \ref AILIA_USER_API_FOPEN
    * @param alg2       Argument pointer notified to \ref AILIA_USER_API_FOPEN
    * @return
    *   If this function is successful, it returns  true  , or  false  otherwise.
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
        return OpenFeatureExtractor();
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
        return OpenFeatureExtractor();
    }

    private bool OpenFeatureExtractor(){
        int status=AiliaFeatureExtractor.ailiaCreateFeatureExtractor(ref ailia_feature_extractor,ailia,format,channel,range,layer_name);
        if(status!=Ailia.AILIA_STATUS_SUCCESS){
            if(logging){
                Debug.Log("ailiaCreateFeatureExtractor failed "+status);
            }
            Close();
            return false;
        }
        return true;
    }

    //画像から特徴量を取得する
    /**
    * \~japanese
    * @brief 画像から特徴量を取得します。
    * @param image               検出対象画像
    * @param image_width         画像幅
    * @param image_height        画像高さ
    * @return
    *   特徴ベクトル
    * @details
    *   画像から特徴ベクトルを計算します。  
    *   
    * \~english
    * @brief   Acquire features from images.
    * @param image             Image to be extraction
    * @param image_width       Image width
    * @param image_height      Image height
    * @return
    *   Feature vector
    * @details
    *   Calculate feature vectors from images.  
    */
    public float[] ComputeFromImage(Color32 [] image,int image_width,int image_height){
        return ComputeFromImageWithFormat(image,image_width,image_height,AiliaFormat.AILIA_IMAGE_FORMAT_RGBA);
    }

    //画像から特徴量を取得する（上下反転）
    /**
    * \~japanese
    * @brief 上下反転の画像から特徴量を取得します。
    * @param image               検出対象画像
    * @param image_width         画像幅
    * @param image_height        画像高さ
    * @return
    *   特徴ベクトル
    * @details
    *   画像から特徴ベクトルを計算します。  
    *
    * \~english
    * @brief   Obtain features from the upside-down image.
    * @param image             Image to be extraction
    * @param image_width       Image width
    * @param image_height      Image height
    * @return
    *   Feature vector
    * @details
    *   Calculate feature vectors from images.  
    */
    public float[] ComputeFromImageB2T(Color32 [] image,int image_width,int image_height){
        return ComputeFromImageWithFormat(image,image_width,image_height,AiliaFormat.AILIA_IMAGE_FORMAT_RGBA_B2T);
    }

    /**
    * \~japanese
    * @brief 画像から特徴量と特徴間の距離を取得します。
    * @param image             検出対象画像
    * @param image_width       画像幅
    * @param image_height      画像高さ
    * @param foramt            画像形式 (AILIA_IMAGE_FORMAT_*)            
    * @return
    *   特徴ベクトル
    * @details
    *   画像から特徴ベクトルを計算します。  
    * 
    * \~english
    * @brief   Obtain feature values and distances between features from images.
    * @param image             Image to be extraction
    * @param image_width       Image width
    * @param image_height      Image height
    * @param foramt            Image format (AILIA_IMAGE_FORMAT_*)            
    * @return
    *   Feature vector
    * @details
    *   Calculate feature vectors from images.  
    */
    private float[] ComputeFromImageWithFormat(Color32 [] image,int image_width,int image_height,uint format){
        if(ailia_feature_extractor==IntPtr.Zero){
            return null;
        }

        //特徴量のサイズを取得
        Ailia.AILIAShape shape=base.GetBlobShape(base.FindBlobIndexByName(layer_name));
        if(shape==null){
            if(logging){
                Debug.Log("GetBlobShape failed");
            }
            return null;
        }

        //出力先の確保
        float [] output_buf=new float[shape.w*shape.z*shape.y*shape.x];
        GCHandle output_handle = GCHandle.Alloc(output_buf, GCHandleType.Pinned);
        IntPtr output_buf_ptr = output_handle.AddrOfPinnedObject();

        //バッファの固定
        GCHandle preview_handle = GCHandle.Alloc(image, GCHandleType.Pinned);
        IntPtr preview_buf_ptr = preview_handle.AddrOfPinnedObject();

        //特徴量取得
        int status=AiliaFeatureExtractor.ailiaFeatureExtractorCompute(ailia_feature_extractor, output_buf_ptr, (UInt32)output_buf.Length*4, preview_buf_ptr, (UInt32)image_width*4,(UInt32)image_width,(UInt32)image_height,format);
        if(status!=Ailia.AILIA_STATUS_SUCCESS){
            if(logging){
                Debug.Log("ailiaFeatureExtractorCompute failed "+status);
            }
            return null;
        }

        //バッファの開放
        preview_handle.Free();
        output_handle.Free();

        return output_buf;
    }

    //特徴量同士の距離を計算する
    /**
    * \~japanese
    * @brief 特徴量同士の距離を計算します。
    * @param feature1         特徴量1
    * @param feature2         特徴量2
    * @return
    *   計算された距離
    * @details
    *   特徴量1と特徴量2の間の距離を計算します。
    * 
    * \~english
    * @brief Calculates the distance between features.
    * @param feature1         Feature 1
    * @param feature2         Feature 2
    * @return
    *   Calculated Distance
    * @details
    *   Calculate the distance between feature 1 and feature 2.
    */
    public float Match(float [] feature1,float [] feature2){
        if(feature1==null || feature2==null){
            if(logging){
                Debug.Log("input feature is empty");
            }
            return float.NaN;
        }

        float distance=0;

        GCHandle feature1_handle = GCHandle.Alloc(feature1, GCHandleType.Pinned);
        IntPtr feature1_buf_ptr = feature1_handle.AddrOfPinnedObject();

        GCHandle feature2_handle = GCHandle.Alloc(feature2, GCHandleType.Pinned);
        IntPtr feature2_buf_ptr = feature2_handle.AddrOfPinnedObject();

        int status=AiliaFeatureExtractor.ailiaFeatureExtractorMatch(ailia_feature_extractor,ref distance, distace_type, feature1_buf_ptr, (uint)feature1.Length*4, feature2_buf_ptr, (uint)feature2.Length*4);
        if(status!=Ailia.AILIA_STATUS_SUCCESS){
            if(logging){
                Debug.Log("ailiaFeatureExtractorMatch failed "+status);
            }
            return float.NaN;
        }

        feature1_handle.Free();
        feature2_handle.Free();
        return distance;
    }

    //開放する
    /**
    * \~japanese
    * @brief 特徴抽出クオブジェクトを破棄します。
    * @return
    *   なし。
    * @details        
    *   特徴抽出クオブジェクトを破棄します。
    * 
    * \~english
    * @brief   Destroy feature extraction quobjects.
    * @return
    *   Return nothing.
    * @details        
    *   Destroys the feature extractor object.
    */
    public override void Close(){
        if(ailia_feature_extractor!=IntPtr.Zero){
            AiliaFeatureExtractor.ailiaDestroyFeatureExtractor(ailia_feature_extractor);
            ailia_feature_extractor=IntPtr.Zero;
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

    ~AiliaFeatureExtractorModel(){
        Dispose(false);
    }
}
