/**
* \~japanese
* @file
* @brief AILIA Unity Plugin Detector Model Class
* @author AXELL Corporation
* @date  November 22, 2021
* 
* \~english
* @file
* @brief AILIA Unity Plugin Detector Model Class
* @author AXELL Corporation
* @date  November 22, 2021
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Runtime.InteropServices;


public class AiliaDetectorModel : AiliaModel {
    private IntPtr ailia_detector = IntPtr.Zero;

    uint format=AiliaFormat.AILIA_NETWORK_IMAGE_FORMAT_RGB;
    uint channel=AiliaFormat.AILIA_NETWORK_IMAGE_CHANNEL_FIRST;
    uint range=AiliaFormat.AILIA_NETWORK_IMAGE_RANGE_SIGNED_FP32;
    uint algorithm=AiliaDetector.AILIA_DETECTOR_ALGORITHM_YOLOV1;
    uint category_n=1;
    uint flag=AiliaDetector.AILIA_DETECTOR_FLAG_NORMAL;

    //モデルの設定を行う
    /** 
    * \~japanese
    * @brief モデルの設定を行います。
    * @param set_format      ネットワークの画像フォーマット
    * @param set_channel     ネットワークのチャンネルフォーマット
    * @param set_range       ネットワークのレンジ
    * @param set_algorithm   アルゴリズム
    * @param set_category    認識対象カテゴリ数
    * @param set_flag        フラグ
    * @return
    *   設定が完了すると true を返す。
    * @details
    *   モデルの設定を行います。
    *   
    * \~english
    * @brief   Set up the model.
    * @param set_format       Network image format
    * @param set_channel      Network channel format
    * @param set_range        Netwoek range
    * @param set_algorithm    Algorithm
    * @param set_category     Number of categories to be recognized
    * @param set_flag         Flag
    * @return
    *   Returns true when configuration is complete.
    * @details
    *   Set up the model.
    */
    public bool Settings(uint set_format,uint set_channel,uint set_range,uint set_algorithm,uint set_category_n,uint set_flag){
        format=set_format;
        channel=set_channel;
        range=set_range;
        algorithm=set_algorithm;
        category_n=set_category_n;
        flag=set_flag;
        return true;
    }

    //YoloV2などのためにアンカーズ（anchors又はbiases）の情報を設定する
    /** 
    * \~japanese
    * @brief YoloV2などのためにアンカーズ（anchors又はbiases）の情報を設定します。
    * @param anchors   アンカーズの寸法 (検出ボックスの可能な形、高さと広さ)
    * @return
    *   成功した場合は true を、失敗した場合は false を返す。
    * @details
    *   AiliaDetector.ailiaDetectorSetAnchors()でYoloV2などのためにアンカーズ (anchors又はbiases) の情報を設定します。
    *   YoloV2などは既定の複数な形の検出ボックスを同時に試しています。このデータはそのボックスの複数な形の情報を記述します。
    *   anchorsには{x,y,x,y...}の形式で格納します。
    *   
    * \~english
    * @brief   Set the anchors (anchors or biases) information for YoloV2 and others.
    * @param anchors   Dimensions of the anchors (possible shape, height and width of the detection box)
    * @return
    *   Returns true on success, false on failure.
    * @details
    *   AiliaDetector.ailiaDetectorSetAnchors() sets the anchors (anchors or biases) information for YoloV2 and others.
    *   YoloV2, for example, tries to detect multiple default shapes of a box at the same time. This data describes the information of the multiple forms of the box.
    *   The anchors are stored in the form {x,y,x,y...} The nchors are stored in the form {x,y,x,y...}.
    */
    public bool Anchors(float [] anchors){
        UInt32 anchors_count=(UInt32)(anchors.Length/2);
        if (ailia_detector == IntPtr.Zero) {
            if(logging){
                Debug.Log("ailia_detector must be opened");
            }
            return false;
        }
        int status=AiliaDetector.ailiaDetectorSetAnchors(ailia_detector,anchors,anchors_count);
        if(status!=Ailia.AILIA_STATUS_SUCCESS){
            if(logging){
                Debug.Log("ailiaDetectorSetAnchors failed "+status);
            }
            return false;
        }
        return true;
    }

    //YoloV3の入力形状を設定する
    /** 
    * \~japanese
    * @brief YoloV3とYOLOXの入力形状を設定します。
    * @param x   モデルの入力画像幅
    * @param y   モデルの入力画像高さ
    * @return
    *   成功した場合は ture を、失敗した場合は false を返す。
    * @details
    *   AiliaDetector.ailiaDetectorSetInputShape()で YoloV3でのモデルへの入力画像サイズを指定します。
    *   
    *   YoloV3とYOLOXでは単一のモデルが任意の入力解像度に対応します。(32 の倍数制限あり)
    *   計算量の削減等でモデルへの入力画像サイズを指定する場合この API を実行してください。
    *   Open() と  Compute() の間に実行する必要があります。 
    *   この API を実行しない場合、デフォルトの 416x416 を利用します。
    *   YOLOv3 以外で実行した場合、 \ref AILIA_STATUS_INVALID_STATE  を返します。
    *   
    * \~english
    * @brief   Sets the input geometry for YoloV3 and YOLOX.
    * @param x   Model input image width
    * @param y   Model input image height
    * @return
    *   Return ture on success, false on failure.
    * @details
    *   AiliaDetector.ailiaDetectorSetInputShape() allows you to specify the input image size for the model in YoloV3 and YOLOX.
    *   
    *   YoloV3 and YOLOX allows a single model to support any input resolution. (with a multiple limit of 32)
    *   Use this API when specifying the input image size to the model, for example, to reduce computational complexity.
    *   It must be executed between Open() and Compute().
    *   If this API is not executed, the default of 416x416 is used.
    *   If executed outside of YOLOv3, returns \ref AILIA_STATUS_INVALID_STATE
    */
    public bool SetInputShape(uint x,uint y){
        if (ailia_detector == IntPtr.Zero) {
            if(logging){
                Debug.Log("ailia_detector must be opened");
            }
            return false;
        }
        int status=AiliaDetector.ailiaDetectorSetInputShape(ailia_detector,x,y); 
        if(status!=Ailia.AILIA_STATUS_SUCCESS){
            if(logging){
                Debug.Log("ailiaDetectorSetInputShape failed "+status);
            }
            return false;
        }
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
        if(!status){
            if(logging){
                Debug.Log("ailiaModelOpenFile failed");
            }
            return false;
        }
        return OpenDetector();
    }

    //コールバックから開く
    /** 
    * \~japanese
    * @brief ファイルコールバックからネットワークオブジェクトを作成します。
    * @param callback   ユーザ定義ファイルアクセスコールバック関数構造体
    * @param alg1       \ref AILIA_USER_API_FOPEN に通知される引数ポインタ
    * @param arg2       \ref AILIA_USER_API_FOPEN に通知される引数ポインタ
    * @return
    *    成功した場合はtrue、失敗した場合はfalseを返す。
    * @details
    *  　ファイルコールバックからネットワークオブジェクトを作成します。
    *   
    * \~english
    * @brief  Creates a network object from a file callback.
    * @param callback   User-defined file access callback function structure
    * @param alg1       Argument pointer notified to \ref AILIA_USER_API_FOPEN
    * @param arg2       Argument pointer notified to \ref AILIA_USER_API_FOPEN
    * @return
    *    If this function is successful, it returns  true  , or  false  otherwise.
    * @details
    *  　Creates a network object from a file callback.
    */
    public override bool OpenEx(Ailia.ailiaFileCallback callback,IntPtr arg1,IntPtr arg2){
        Close();
        bool status=base.OpenEx(callback,arg1,arg2);
        if(!status){
            if(logging){
                Debug.Log("ailiaModelOpenEx failed");
            }
            return false;
        }
        return OpenDetector();
    }

    //メモリから開く
    /** 
    * \~japanese
    * @brief メモリからネットワークオブジェクトを作成します。
    * @param prototxt_buf   prototxtファイルのデータへのポインタ
    * @param model_buf      protobuf/onnxファイルのデータへのポインタ
    * @return
    *   成功した場合はtrue、失敗した場合はfalseを返す。
    * @details
    *   メモリからネットワークオブジェクトを作成します。
    *   
    * \~english
    * @brief   Creates network objects from memory.
    * @param prototxt_buf   Pointer to data in prototxt file
    * @param model_buf      Pointer to data in protobuf/onnx file
    * @return
    *   If this function is successful, it returns  true  , or  false  otherwise.
    * @details
    *   Creates network objects from memory.
    */
    public override bool OpenMem(byte[] prototxt_buf,byte[] model_buf){
        Close();
        bool status=base.OpenMem(prototxt_buf,model_buf);
        if(!status){
            if(logging){
                Debug.Log("ailiaModelOpenMem failed");
            }
            return false;
        }
        return OpenDetector();
    }

    /** 
    * \~japanese
    * @brief 検出オブジェクトを作成します。
    * @return
    *   成功した場合は true を、失敗した場合は false を返す。
    * @details
    *   検出オブジェクトを作成します。
    *   
    * \~english
    * @brief   Create a detection object.
    * @return
    *   Returns true on success, false on failure.
    * @details
    *   Creates a detection object.
    */
    private bool OpenDetector(){
        int status=AiliaDetector.ailiaCreateDetector(ref ailia_detector,ailia,format,channel,range,algorithm,category_n,flag);
        if(status!=Ailia.AILIA_STATUS_SUCCESS){
            if(logging){
                Debug.Log("ailiaCreateDetector failed "+status);
            }
            Close();
            return false;
        }
        return true;
    }

    //画像から推論する
    /** 
    * \~japanese
    * @brief 画像から推論し結果を取得する
    * @param image        入力画像
    * @param tex_width    画像幅
    * @param tec_height   画像高さ
    * @param threadhold   検出しきい値(0.1f等)(小さいほど検出されやすくなり、検出数増加)
    * @param iou          重複除外しきい値(0.45f等)(小さいほど重複を許容せず検出数減少)
    * @return
    *    検出されたオブジェクトのリスト。
    * @details
    *    与えられた画像から推論を行い、推論結果をリストで取得します。
    *    
    * \~english
    * @brief   Detection from images and obtaining results
    * @param image        Input Image
    * @param tex_width    Image width
    * @param tec_height   Image height
    * @param threadhold   Detection threshold (e.g., 0.1f) (the smaller the threshold, the easier it is to detect and the greater the number of detections)
    * @param iou          Duplicate exclusion threshold (e.g., 0.45f) (the smaller the threshold, the less duplicates are tolerated and the fewer the number of detections)
    * @return
    *   List of detected objects.
    * @details
    *   Inference is made from the given image and the results of inference are retrieved in a list.
    */
    public List<AiliaDetector.AILIADetectorObject> ComputeFromImage(Color32 [] image,int tex_width,int tex_height,float threshold,float iou){
        return ComputeFromImageWithFormat(image,tex_width,tex_height,threshold,iou,AiliaFormat.AILIA_IMAGE_FORMAT_RGBA);
    }

    //画像から推論する（上下反転）
    /** 
    * \~japanese
    * @brief 上下反転された画像から推論し結果を取得する
    * @param image        入力画像
    * @param tex_width    画像幅
    * @param tec_height   画像高さ
    * @param threadhold   検出しきい値(0.1f等)(小さいほど検出されやすくなり、検出数増加)
    * @param iou          重複除外しきい値(0.45f等)(小さいほど重複を許容せず検出数減少)
    * @return
    *   検出されたオブジェクトのリスト。
    * @details
    *   与えられた画像から推論を行い、推論結果をリストで取得します。
    *    
    * \~english
    * @brief   Detection from bottom-top images and obtaining results
    * @param image        Input image
    * @param tex_width    Image width
    * @param tec_height   Image height
    * @param threadhold   Detection threshold (e.g., 0.1f) (the smaller the threshold, the easier it is to detect and the greater the number of detections)
    * @param iou          Duplicate exclusion threshold (e.g., 0.45f) (the smaller the threshold, the less duplicates are tolerated and the fewer the number of detections)
    * @return
    *   List of detected objects.
    * @details
    *   Inference is made from the given image and the results of inference are retrieved in a list.
    */
    public List<AiliaDetector.AILIADetectorObject> ComputeFromImageB2T(Color32 [] image,int tex_width,int tex_height,float threshold,float iou){
        return ComputeFromImageWithFormat(image,tex_width,tex_height,threshold,iou,AiliaFormat.AILIA_IMAGE_FORMAT_RGBA_B2T);
    }

    /** 
    * \~japanese
    * @brief 画像から推論し結果を取得する
    * @param image        入力画像
    * @param tex_width    画像幅
    * @param tec_height   画像高さ
    * @param threadhold   検出しきい値(0.1f等)(小さいほど検出されやすくなり、検出数増加)
    * @param iou          重複除外しきい値(0.45f等)(小さいほど重複を許容せず検出数減少)
    * @param format       画像フォーマット(AILIA_IMAGE_FORMAT_*)
    * @return
    *   検出されたオブジェクトのリスト。
    * @details
    *   与えられた画像から推論を行い、推論結果をリストで取得します。
    *   
    * \~english
    * @brief   Detection from images and obtaining results
    * @param image        Input image
    * @param tex_width    Image width
    * @param tec_height   Image height
    * @param threadhold   Detection threshold (e.g., 0.1f) (the smaller the threshold, the easier it is to detect and the greater the number of detections)
    * @param iou          Duplicate exclusion threshold (e.g., 0.45f) (the smaller the threshold, the less duplicates are tolerated and the fewer the number of detections)
    * @param format       Image format (AILIA_IMAGE_FORMAT_*)
    * @return
    *   List of detected objects.
    * @details
    *   Inference is made from the given image and the results of inference are retrieved in a list.
    */
    private List<AiliaDetector.AILIADetectorObject> ComputeFromImageWithFormat(Color32 [] image,int tex_width,int tex_height,float threshold,float iou,uint format){
        if(ailia_detector==IntPtr.Zero){
            return null;
        }

        //バッファの固定
        GCHandle preview_handle = GCHandle.Alloc(image, GCHandleType.Pinned);
        IntPtr preview_buf_ptr = preview_handle.AddrOfPinnedObject();

        //画像認識を行ってカテゴリを表示
        int status=AiliaDetector.ailiaDetectorCompute(ailia_detector, preview_buf_ptr, (UInt32)tex_width*4,(UInt32)tex_width,(UInt32)tex_height,format,threshold,iou);
        if(status!=Ailia.AILIA_STATUS_SUCCESS){
            if(logging){
                Debug.Log("ailiaDetectorCompute failed "+status);
            }
            return null;
        }

        //推論結果を表示
        List<AiliaDetector.AILIADetectorObject> result_list=new List<AiliaDetector.AILIADetectorObject>();
        uint count=0;
        AiliaDetector.ailiaDetectorGetObjectCount(ailia_detector,ref count);//fun
        for(uint i=0;i<count;i++){
            AiliaDetector.AILIADetectorObject detector_obj=new AiliaDetector.AILIADetectorObject();//object
            status=AiliaDetector.ailiaDetectorGetObject(ailia_detector,detector_obj,(uint)i,AiliaClassifier.AILIA_CLASSIFIER_CLASS_VERSION);//fun
            if(status!=Ailia.AILIA_STATUS_SUCCESS){
                if(logging){
                    Debug.Log("ailiaDetectorGetObject failed "+status);
                }
                break;
            }
            result_list.Add(detector_obj);
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
    *   なし
    * @details
    *   検出オブジェクトを破棄します。
    *   
    * \~english
    * @brief   Discard the detection object.
    * @return
    *   Return nothing
    * @details
    *    Destroys the detection object. 
    */
    public override void Close(){
        if(ailia_detector!=IntPtr.Zero){
            AiliaDetector.ailiaDestroyDetector(ailia_detector);
            ailia_detector=IntPtr.Zero;
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

    ~AiliaDetectorModel(){
        Dispose(false);
    }
}
