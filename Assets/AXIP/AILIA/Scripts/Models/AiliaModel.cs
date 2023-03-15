/**
* \~japanese
* @file
* @brief AILIA Unity Plugin Model Class
* @author AXELL Corporation
* @date  November 22, 2021
* 
* \~english
* @file
* @brief AILIA Unity Plugin Model Class
* @author AXELL Corporation
* @date  November 22, 2021
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Runtime.InteropServices;

public class AiliaModel : IDisposable
{
    protected IntPtr ailia = IntPtr.Zero;

    private int env_id = Ailia.AILIA_ENVIRONMENT_ID_AUTO;
    private uint memory_mode = Ailia.AILIA_MEMORY_OPTIMAIZE_DEFAULT;
    private bool disalbe_layer_fusion = false;

    private string env_name = "auto";

    protected bool logging = true;

    /** 環境選択（簡易）*/

    /**
    * \~japanese
    * @brief 指定した種類の計算環境を選択します。
    * @param type   環境の種別( \ref AILIA_ENVIRONMENT_TYPE_CPU  or BLAS or GPU)
    * @return
    *   成功した場合 true を、失敗した場合は false を返す。
    * @details        
    *   簡易的に環境種別から計算環境を選択します。
    *   明示的に計算環境を指定する場合は、GetEnvironmentCount()、GetEnvironment()、SelectEnvironment()を使用してください。
    *
    * \~english
    * @brief Selects the specified type of calculation environment.
    * @param type   Type of environment ( \ref AILIA_ENVIRONMENT_TYPE_CPU  or BLAS or GPU)
    * @return
    *   Returns true on success, false on failure.
    * @details        
    *   Simply select the calculation environment from the environment type.
    *   To explicitly specify the computing environment, use GetEnvironmentCount(), GetEnvironment(), and SelectEnvironment().
    */
    public bool Environment(int type)
    {
        int count = GetEnvironmentCount();
        if (count == -1)
        {
            return false;
        }

        for (int i = 0; i < count; i++)
        {
            Ailia.AILIAEnvironment env = GetEnvironment(i);
            if (env == null)
            {
                return false;
            }

            if (env.type == type)
            {
                if (!SelectEnvironment(i))
                {
                    return false;
                }
                if (env.backend == Ailia.AILIA_ENVIRONMENT_BACKEND_CUDA || env.backend == Ailia.AILIA_ENVIRONMENT_BACKEND_VULKAN)
                {
                    return true;    //優先
                }
            }
        }
        return true;
    }

    /**
    * \~japanese
    * @brief 選択された環境名を表示します。
    * @return
    *   選択された環境名
    * @details        
    *   選択された環境名を返します。
    *
    * \~english
    * @brief Displays the name of the selected environment.
    * @return
    *   Selected Environment Name
    * @details        
    *   Returns the name of the selected environment.
    */
    public string EnvironmentName()
    {
        return env_name;
    }

    /** 環境選択（詳細）*/

    /** 
    * \~japanese
    * @brief 利用可能な計算環境(CPU, GPU)の数を取得します。
    * @return
    *   成功した場合は利用可能な環境の数を、失敗した場合は-1を返す。　
    * @details        
    *   利用可能な環境の数を取得します。
    *
    * \~english
    * @brief Get the number of available computing environments (CPU, GPU).
    * @return
    *   Returns the number of available environments on success, -1 on failure.　
    * @details        
    *   Get the number of available environments.
    */
    public int GetEnvironmentCount()
    {
        SetTemporaryCachePath();
        int count = 0;
        int status = Status = Ailia.ailiaGetEnvironmentCount(ref count);
        if (status != Ailia.AILIA_STATUS_SUCCESS)
        {
            if (logging)
            {
                Debug.Log("ailiaGetEnvironmentCount failed " + status + " (" + GetStatusString(status) + ")");
            }
            return -1;
        }
        return count;
    }

    /**
    * \~japanese
    * @brief 指定したインデックスの計算環境を取得します。
    * @param idx   環境情報のインデックス(0~ ailiaGetEnvironmentCount() -1)
    * @return
    *   成功した場合は利用可能な環境を、失敗した場合は null を返す。
    * @details        
    *   指定した計算環境の詳細情報を取得します。
    *
    * \~english
    * @brief Obtains the computing environment for the specified index.
    * @param idx   Index of Environmental Information (0~ ailiaGetEnvironmentCount() -1)
    * @return
    *   Returns the available environment on success or null on failure.
    * @details        
    *   Obtains detailed information about the specified computing environment.
    */
    public Ailia.AILIAEnvironment GetEnvironment(int idx)
    {
        IntPtr env_ptr = IntPtr.Zero;
        int status = Status = Ailia.ailiaGetEnvironment(ref env_ptr, (uint)idx, Ailia.AILIA_ENVIRONMENT_VERSION);
        if (status != Ailia.AILIA_STATUS_SUCCESS)
        {
            if (logging)
            {
                Debug.Log("ailiaGetEnvironment failed " + status + " (" + GetStatusString(status) + ")");
            }
            return null;
        }
        Ailia.AILIAEnvironment env = (Ailia.AILIAEnvironment)Marshal.PtrToStructure(env_ptr, typeof(Ailia.AILIAEnvironment));
        if (logging)
        {
            //Debug.Log("ENV id:"+env.id+", name:"+Marshal.PtrToStringAnsi(env.name)+", type:"+env.type);
        }
        return env;
    }

    /**
    * \~japanese
    * @brief 計算環境を選択します。
    * @param idx    環境情報のインデックス (0~ ailiaGetEnvironmentCount() -1)
    * @return
    *   成功した場合は true 、失敗した場合は false を返す。
    * @details        
    *   指定したインデックスの計算環境を推論環境として選択します。
    *
    * \~english
    * @brief Select the calculation environment.
    * @param idx    Index of Environmental Information (0~ ailiaGetEnvironmentCount() -1)
    * @return
    *   Returns true on success, false on failure.
    * @details        
    *   Selects the computing environment for the specified index as the inference environment.
    */
    public bool SelectEnvironment(int idx)
    {
        Ailia.AILIAEnvironment env = GetEnvironment(idx);
        if (env == null)
        {
            return false;
        }
        env_id = env.id;
        env_name = Marshal.PtrToStringAnsi(env.name);
        return true;
    }

    /**
    * \~japanese
    * @brief 選択された計算環境を取得します。
    * @return
    *   成功した場合は計算環境情報を、失敗した場合は null を返す。
    * @details        
    *   選択された計算環境の詳細情報を取得します。
    *
    * \~english
    * @brief Retrieves the selected computing environment.
    * @return
    *   Returns computed environment information on success or null on failure.
    * @details        
    *   Retrieves detailed information about the selected calculation environment.
    */
    public Ailia.AILIAEnvironment GetSelectedEnvironment()
    {
        IntPtr env_ptr = IntPtr.Zero;
        int status = Status = Ailia.ailiaGetSelectedEnvironment(ailia, ref env_ptr, Ailia.AILIA_ENVIRONMENT_VERSION);
        if (status != Ailia.AILIA_STATUS_SUCCESS)
        {
            if (logging)
            {
                Debug.Log("ailiaGetSelectedEnvironment failed " + status + " (" + GetStatusString(status) + ")");
            }
            return null;
        }
        Ailia.AILIAEnvironment env = (Ailia.AILIAEnvironment)Marshal.PtrToStructure(env_ptr, typeof(Ailia.AILIAEnvironment));
        return env;
    }

    /**
    * \~japanese
    * @brief Vulkanのshader cacheディレクトリを設定します。
    * @return
    *   成功した場合は true 、失敗した場合は false を返す。
    * @details        
    *    Vulkanのシェーダをキャッシュするディレクトリを指定します。
    *    このAPIを呼び出していない場合、Androidの計算環境にVulkanを指定できません。
    *    temporaryCachePathなど、書き込み権限のあるディレクトリを指定してください。
    *
    * \~english
    * @brief Set the shader cache directory for Vulkan.
    * @return
    *   Returns true on success, false on failure.
    * @details        
    *    Specify the directory to cache shaders for Vulkan.
    *    If this API is not called, Vulkan cannot be specified for the Android computation environment.
    *    Specify a directory to which you have write permission, such as temporaryCachePath.
    */
    private bool SetTemporaryCachePath()
    {
#if (UNITY_ANDROID && !UNITY_EDITOR)
        using( AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer") )
        {
            using( AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity") )
            {
                using( AndroidJavaObject cacheDir = currentActivity.Call<AndroidJavaObject>( "getCacheDir" ) )
                {
                    string _CacheDir = cacheDir.Call<string>( "getCanonicalPath" );
                    int status=Status=Ailia.ailiaSetTemporaryCachePath(_CacheDir);
                    if(status!=Ailia.AILIA_STATUS_SUCCESS){
                        if(logging){
                            Debug.Log("ailiaSetTemporaryCachePath failed "+status+" ("+GetStatusString(status)+")");
                        }
                        return false;
                    }
                }
            }
        }
#endif
        return true;
    }

    /**************************************************************** 
     * ライセンスエラーの表示
     */

    /**
     * \~japanese
     * @brief ライセンスエラーを表示します。
     * @param status   ライブラリ状態を示す数値
     * @return
     *   なし
     * @details        
     *   ライセンスエラーがある場合にエラーメッセージを表示します。
     *   有効なライセンスが見つからない場合は"License file not found"、
     *   ライセンスの有効期限が切れている場合は"License expired"と表示します。
     *   
     * \~english
     * @brief   Displays license errors.
     * @param status   Numerical value indicating library status
     * @return
     *   no return value
     * @details        
     *   Displays an error message if there is a license error.
     *   If a valid license cannot be found, the message "License file not found" will be displayed.
     *   If the license has expired, the message "License expired" will be displayed.
     */
    private void DisplayLicenseError(int status)
    {
        if (status == Ailia.AILIA_STATUS_LICENSE_NOT_FOUND)
        {
            Debug.LogError("License file not found");
        }
        if (status == Ailia.AILIA_STATUS_LICENSE_EXPIRED)
        {
            Debug.LogError("License expired");
        }
    }

    /****************************************************************
     * メモリモードの設定
     */

    /**
    *  \~japanese
    *  @brief メモリモードを設定します
    *  @param set_memory_mode   メモリモード(論理和で複数指定可) AILIA_MEMORY_XXX (デフォルト: \ref AILIA_MEMORY_REDUCE_CONSTANT )
    *  @return
    *    なし
    *  @details
    *     メモリの使用方針の設定を変更します。 \ref AILIA_MEMORY_NO_OPTIMIZATION 以外を指定した場合は、
    *   推論時に確保する中間バッファーを開放するため、推論時のメモリ使用量を削減することができます。
    *   
    *  \~english
    *  @brief   Sets the memory mode
    *  @param set_memory_mode   Memory mode (multiple specifiable by logical. AILIA_MEMORY_XXX (default: \ref AILIA_MEMORY_REDUCE_CONSTANT )
    *  @return
    *    no return value
    *  @details
    *    Changes the memory usage policy setting; 
    *    if anything other than  \ref AILIA_MEMORY_NO_OPTIMIZATION is specified,
    *    the intermediate buffer allocated during inference is released, thus reducing the amount of memory used during inference. 
    */
    public void SetMemoryMode(uint set_memory_mode)
    {
        memory_mode = set_memory_mode;
    }

    /**
    *  \~japanese
    *  @brief レイヤー統合による高速化を無効化します。
    *  @details
    *    レイヤー統合による高速化を無効化します。デフォルトは有効です。
    *    
    *  \~english
    *  @brief   Disables speedup due to layer fusion.
    *  @details
    *    Disables speedup due to layer fusion. Default is enabled.
    */
    public void DisableLayerFusion()
    {
        disalbe_layer_fusion = true;
    }

    /****************************************************************
     * ファイルを開く（ファイル）
     */

    /**
    * \~japanese
    * @brief モデルファイルからネットワークオブジェクトを作成します。
    * @param prototxt_path   　prototxtファイルのパス名(MBSC or UTF16)
    * @param model_path      protobuf/onnxファイルのパス名(MBSC or UTF16)
    * @return
    *   成功した場合はtrue、失敗した場合はfalseを返す。
    * @detail
    *   モデルファイルからネットワークオブジェクトを作成します。
    *   
    * \~english
    * @brief   Create a network object from a model file.
    * @param prototxt_path   　Pathname of the prototxt file(MBSC or UTF16)
    * @param model_path      Path name of protobuf/onnx file(MBSC or UTF16)
    * @return
    *   If this function is successful, it returns  true  , or  false  otherwise.
    * @detail
    *   Create a network object from a model file.
    */
    public virtual bool OpenFile(string prototxt_path, string model_path)
    {
        Close();

        int status = Status = Ailia.ailiaCreate(ref ailia, env_id, Ailia.AILIA_MULTITHREAD_AUTO);
        if (status != Ailia.AILIA_STATUS_SUCCESS)
        {
            DisplayLicenseError(status);
            if (logging)
            {
                Debug.Log("ailiaCreate failed " + status + " (" + GetStatusString(status) + ")");
            }
            return false;
        }

        if (memory_mode != Ailia.AILIA_MEMORY_OPTIMAIZE_DEFAULT)
        {
            status = Status = Ailia.ailiaSetMemoryMode(ailia, memory_mode);
            if (status != Ailia.AILIA_STATUS_SUCCESS)
            {
                if (logging)
                {
                    Debug.Log("ailiaSetMemoryMode failed " + status + " (" + GetStatusString(status) + ")");
                }
                Close();
                return false;
            }
        }
        if (disalbe_layer_fusion)
        {
            status = Status = Ailia.ailiaDisableLayerFusion(ailia);
            if (status != Ailia.AILIA_STATUS_SUCCESS)
            {
                if (logging)
                {
                    Debug.Log("ailiaDisableLayerFusion failed " + status + " (" + GetStatusString(status) + ")");
                }
                Close();
                return false;
            }
        }

        if (prototxt_path != null)
        {
            status = Status = Ailia.ailiaOpenStreamFile(ailia, prototxt_path);
            if (status != Ailia.AILIA_STATUS_SUCCESS)
            {
                if (logging)
                {
                    Debug.Log("ailiaOpenStreamFile failed " + status + " (" + GetStatusString(status) + ")");
                }
                Close();
                return false;
            }
        }

        status = Status = Ailia.ailiaOpenWeightFile(ailia, model_path);
        if (status != Ailia.AILIA_STATUS_SUCCESS)
        {
            if (logging)
            {
                Debug.Log("ailiaOpenWeightFile failed " + status + " (" + GetStatusString(status) + ")");
            }
            Close();
            return false;
        }

        return true;
    }

    /****************************************************************
     * ファイルを開く（メモリ）
     */

    /**
    * \~japanese
    * @brief メモリからネットワークオブジェクトを作成します。
    * @param prototxt_buf   prototxtファイルのデータへのポインタ 
    * @param model_buf      protobuf/onnxファイルのデータへのポインタ
    * @return
    *   成功した場合はtrue、失敗した場合はfalseを返す。
    * @detail
    *   メモリからネットワークオブジェクトを作成します。
    *   
    * \~english
    * @brief   Creates network objects from memory.
    * @param prototxt_buf   Pointer to data in 　prototxt　 file 
    * @param model_buf     Pointer to data in   protobuf/onnx  file
    * @return
    *   If this function is successful, it returns  true  , or  false  otherwise.
    * @detail
    *   Creates network objects from memory.
    */
    public virtual bool OpenMem(byte[] prototxt_buf, byte[] model_buf)
    {
        Close();

        if (model_buf == null || model_buf.Length == 0 || (prototxt_buf != null && prototxt_buf.Length == 0))
        {
            if (logging)
            {
                Debug.Log("input buffer is empty");
            }
            return false;
        }

        int status = Status = Ailia.ailiaCreate(ref ailia, env_id, Ailia.AILIA_MULTITHREAD_AUTO);
        if (status != Ailia.AILIA_STATUS_SUCCESS)
        {
            DisplayLicenseError(status);
            if (logging)
            {
                Debug.Log("ailiaCreate failed " + status + " (" + GetStatusString(status) + ")");
            }
            return false;
        }

        if (memory_mode != Ailia.AILIA_MEMORY_OPTIMAIZE_DEFAULT)
        {
            status = Status = Ailia.ailiaSetMemoryMode(ailia, memory_mode);
            if (status != Ailia.AILIA_STATUS_SUCCESS)
            {
                if (logging)
                {
                    Debug.Log("ailiaSetMemoryMode failed " + status + " (" + GetStatusString(status) + ")");
                }
                Close();
                return false;
            }
        }
        if (disalbe_layer_fusion)
        {
            status = Status = Ailia.ailiaDisableLayerFusion(ailia);
            if (status != Ailia.AILIA_STATUS_SUCCESS)
            {
                if (logging)
                {
                    Debug.Log("ailiaDisableLayerFusion failed " + status + " (" + GetStatusString(status) + ")");
                }
                Close();
                return false;
            }
        }

        if (prototxt_buf != null)
        {
            status = Status = Ailia.ailiaOpenStreamMem(ailia, prototxt_buf, (uint)prototxt_buf.Length);
            if (status != Ailia.AILIA_STATUS_SUCCESS)
            {
                if (logging)
                {
                    Debug.Log("ailiaOpenStreamMem failed " + status + " (" + GetStatusString(status) + ")");
                }
                Close();
                return false;
            }
        }

        status = Status = Ailia.ailiaOpenWeightMem(ailia, model_buf, (uint)model_buf.Length);
        if (status != Ailia.AILIA_STATUS_SUCCESS)
        {
            if (logging)
            {
                Debug.Log("ailiaOpenWeightMem filed " + status + " (" + GetStatusString(status) + ")");
            }
            Close();
            return false;
        }

        return true;
    }

    /****************************************************************
    * ファイルを開く（コールバック）
    */

    /**
    *  \~japanese
    *  @brief ファイルコールバックからネットワークオブジェクトを作成します。
    *  @param callback   ユーザ定義ファイルアクセスコールバック関数構造体
    *  @param arg1       \ref AILIA_USER_API_FOPEN に通知される引数ポインタ
    *  @param arg2       \ref AILIA_USER_API_FOPEN に通知される引数ポインタ
    *  @return
    *    成功した場合はtrue、失敗した場合はfalseを返す。
    *  @detail
    *  　ファイルコールバックからネットワークオブジェクトを作成します。
    *    
    *  \~english
    *  @brief  Creates a network object from a file callback.
    *  @param callback   User-defined file access callback function structure
    *  @param arg1       Argument pointer notified to \ref AILIA_USER_API_FOPEN
    *  @param arg2       Argument pointer notified to \ref AILIA_USER_API_FOPEN
    *  @return
    *    If this function is successful, it returns  true  , or  false  otherwise.
    *  @detail
    *  　Creates a network object from a file callback.
    */

    public virtual bool OpenEx(Ailia.ailiaFileCallback callback, IntPtr arg1, IntPtr arg2)
    {
        Close();

        int status = Status = Ailia.ailiaCreate(ref ailia, env_id, Ailia.AILIA_MULTITHREAD_AUTO);
        if (status != Ailia.AILIA_STATUS_SUCCESS)
        {
            DisplayLicenseError(status);
            if (logging)
            {
                Debug.Log("ailiaCreate failed " + status + " (" + GetStatusString(status) + ")");
            }
            return false;
        }

        if (memory_mode != Ailia.AILIA_MEMORY_OPTIMAIZE_DEFAULT)
        {
            status = Status = Ailia.ailiaSetMemoryMode(ailia, memory_mode);
            if (status != Ailia.AILIA_STATUS_SUCCESS)
            {
                if (logging)
                {
                    Debug.Log("ailiaSetMemoryMode failed " + status + " (" + GetStatusString(status) + ")");
                }
                Close();
                return false;
            }
        }
        if (disalbe_layer_fusion)
        {
            status = Status = Ailia.ailiaDisableLayerFusion(ailia);
            if (status != Ailia.AILIA_STATUS_SUCCESS)
            {
                if (logging)
                {
                    Debug.Log("ailiaDisableLayerFusion failed " + status + " (" + GetStatusString(status) + ")");
                }
                Close();
                return false;
            }
        }

        if (arg1 != null)
        {
            status = Status = Ailia.ailiaOpenStreamEx(ailia, arg1, callback, Ailia.AILIA_FILE_CALLBACK_VERSION);
            if (status != Ailia.AILIA_STATUS_SUCCESS)
            {
                if (logging)
                {
                    Debug.Log("ailiaOpenStreamFileEx failed " + status + " (" + GetStatusString(status) + ")");
                }
                Close();
                return false;
            }
        }

        status = Status = Ailia.ailiaOpenWeightEx(ailia, arg2, callback, Ailia.AILIA_FILE_CALLBACK_VERSION);
        if (status != Ailia.AILIA_STATUS_SUCCESS)
        {
            if (logging)
            {
                Debug.Log("ailiaOpenWeightFileEx failed " + status + " (" + GetStatusString(status) + ")");
            }
            Close();
            return false;
        }

        return true;
    }

    /****************************************************************
     * 推論する
     */

    /** 
    * \~japanese
    * @brief 推論を行い推論結果を取得します。
    * @param output_data   出力データの書き出し先
    * @param input_data    推論データ
    * @return
    *   成功した場合はtrue、失敗した場合はfalseを返す。
    * @detail
    *   input_dataを入力としてAilia.ailiaPredict()で推論を行い、output_dataに出力します。
    *   このAPIは1入力1出力のモデルで使用可能です。
    *   多入出力のモデルを推論する場合は、SetInputBlobData、Update、GetBlobData APIを使用してください。
    *   
    * \~english
    * @brief   Perform inference and obtain inference results.
    * @param output_data   Output data export destination
    * @param input_data    Inference data
    * @return
    *   If this function is successful, it returns  true  , or  false  otherwise.
    * @detail
    *   Ailia.ailiaPredict()  is used to infer with input_data as input and output to output_data.
    *   This API is available for 1-input, 1-output models.
    *   To infer a multi-input/output model, use the SetInputBlobData, Update, and GetBlobData APIs. 
    */

    public bool Predict(float[] output_data, float[] input_data)
    {
        if (ailia == IntPtr.Zero)
        {
            return false;
        }

        //バッファの固定
        GCHandle input_buf_handle = GCHandle.Alloc(input_data, GCHandleType.Pinned);
        IntPtr input_buf_ptr = input_buf_handle.AddrOfPinnedObject();

        GCHandle output_buf_handle = GCHandle.Alloc(output_data, GCHandleType.Pinned);
        IntPtr output_buf_ptr = output_buf_handle.AddrOfPinnedObject();

        //推論
        int status = Status = Ailia.ailiaPredict(ailia, output_buf_ptr, (uint)(output_data.Length * 4), input_buf_ptr, (uint)(input_data.Length * 4));

        //バッファの開放
        input_buf_handle.Free();
        output_buf_handle.Free();

        if (status != Ailia.AILIA_STATUS_SUCCESS)
        {
            if (logging)
            {
                Debug.Log("ailiaPredict failed " + status + " (" + GetStatusString(status) + ")");
            }
            return false;
        }
        return true;
    }

    /****************************************************************
    * 入力形式の取得
    */

    /**
    * \~japanese
    * @brief 推論時の入力データの形状を取得します。
    * @return
    *   成功した場合、入力データの各次元の大きさが格納された配列 shape 
    *   それ以外の場合はnullを返す。
    * @detail
    *   AILIAクラスのailiaGetInputShape()メソッドを用いて、推論時の入力データの形状を取得します。
    *   形状が5次元以上の場合は GetInputShapeND() を利用してください。
    *   形状の一部が未確定の場合、該当する次元の値は0となり、それ以外の次元の値は有効な値が shape に格納されます。
    *   失敗した場合には、それに応じたエラーメッセージが表示されます。
    * 
    * \~japanese
    * @brief   Obtains the shape of the input data at the time of inference.
    * @return
    *   If successful, an array containing the size of each dimension of the input data shape Otherwise,  null  is returned.
    * @detail
    *   The ailiaGetInputShape() method of the AILIA class is used to obtain the shape of the input data during inference.
    *   If the shape has more than 5 dimensions, use  GetInputShapeND().
    *   If a part of the shape is not yet determined, the value of the corresponding dimension is set to 0, and valid values for the other dimensions are stored in shape.
    *   In case of failure, a corresponding error message will be displayed. 
    */
    public Ailia.AILIAShape GetInputShape()
    {
        Ailia.AILIAShape shape = new Ailia.AILIAShape();
        int status = Status = Ailia.ailiaGetInputShape(ailia, shape, Ailia.AILIA_SHAPE_VERSION);
        if (status != Ailia.AILIA_STATUS_SUCCESS)
        {
            if (logging)
            {
                Debug.Log("ailiaGetInputShape failed " + status + " (" + GetStatusString(status) + ")");
            }
            return null;
        }
        return shape;
    }

    /**
    * \~japanese
    * @brief 推論時の入力データの形状を取得します。
    * @return
    *   成功した場合、入力データの各次元の大きさが格納された配列 shape 
    *   それ以外の場合はnullを返す。
    * @detail
    *   AILIA.ailiaGetInputDim() を用いて、推論時の入力データの次元を取得します。
    *   AILIA.ailiaGetInputShapeND() を用いて、推論時の入力データの形状を取得します。
    *   形状が5次元以上の場合は GetInputShapeND() を利用してください。
    *   形状の一部が未確定の場合、該当する次元の値は0となり、それ以外の次元の値は有効な値が shape に格納されます。
    *   失敗した場合には、それに応じたエラーメッセージが表示されます。
    *   
    * \~english
    * @brief Obtains the shape of the input data at the time of inference.
    * @return
    *   If successful, an array containing the size of each dimension of the input data is returned. shape Otherwise, null is returned.
    * @detail
    *   AILIA.ailiaGetInputDim() is used to obtain the dimension of the input data during inference.
    *   AILIA.ailiaGetInputShapeND() is used to obtain the shape of the input data at the time of inference.
    *   If the shape has more than 5 dimensions, use GetInputShapeND().
    *   If a part of the shape is not yet determined, the value of the corresponding dimension is set to 0, and valid values for the other dimensions are stored in shape.
    *   In case of failure, a corresponding error message will be displayed.
    */
    public uint[] GetInputShapeND()
    {
        uint dim = 0;
        int status = Status = Ailia.ailiaGetInputDim(ailia, ref dim);
        if (status != Ailia.AILIA_STATUS_SUCCESS)
        {
            if (logging)
            {
                Debug.Log("ailiaGetInputDim failed " + status + " (" + GetStatusString(status) + ")");
            }
            return null;
        }
        uint[] shape = new uint[dim];
        status = Status = Ailia.ailiaGetInputShapeND(ailia, shape, dim);
        if (status != Ailia.AILIA_STATUS_SUCCESS)
        {
            if (logging)
            {
                Debug.Log("ailiaGetInputShapeND failed " + status + " (" + GetStatusString(status) + ")");
            }
            return null;
        }
        return shape;
    }

    /****************************************************************
     * 入力形式を設定
     */

    /** 
    * \~japanese
    * @brief 推論時の入力データの形状を設定します。
    * @param shape   入力データの形状情報
    * @return
    *   成功した場合は true 、失敗した場合は false を返す。
    * @detail
    *   prototxtで定義されている入力形状を変更します。
    *   prototxtに記述されているランクと同じにする必要があります。
    *   なお、重み係数の形状が入力形状に依存しているなどによりエラーが返る場合があります。
    *   prototxtで定義されているランクが5次元以上の場合は SetInputShapeND() を利用してください。
    *   
    * \~english
    * @brief   Sets the shape of the input data during inference.
    * @param shape   Shape information of input data
    * @return
    *   If this function is successful, it returns  true  , or  false  otherwise.
    * @detail
    *   Changes the input geometry defined in prototxt.
    *   It should be the same as the rank described in prototxt.
    *   Note that an error may be returned if the shape of the weight coefficients depends on the input geometry, for example.
    *   If the rank defined in prototxt is 5 or more dimensions, use  SetInputShapeND().
    */
    public bool SetInputShape(Ailia.AILIAShape shape)
    {
        if (ailia == IntPtr.Zero)
        {
            return false;
        }

        int status = Status = Ailia.ailiaSetInputShape(ailia, shape, Ailia.AILIA_SHAPE_VERSION);
        if (status != Ailia.AILIA_STATUS_SUCCESS)
        {
            if (logging)
            {
                Debug.Log("ailiaSetInputShape failed " + status + " (" + GetStatusString(status) + ")");
            }
            return false;
        }
        return true;
    }

    /**
    * \~japanese
    * @brief 推論時の入力データの形状を変更します。
    * @param net       ネットワークオブジェクトポインタ
    * @param shape     入力データの各次元の大きさの配列(dim-1, dim-2, ... ,1, 0)
    * @param dim       shapeの次元
    * @return
    *   成功した場合は ture 、そうでなければ false を返す。
    * @details
    *   prototxtで定義されている入力形状を変更します。
    *   prototxtに記述されているランクと同じにする必要があります。
    *   なお、重み係数の形状が入力形状に依存しているなどによりエラーが返る場合があります。
    *   
    * \~english
    * @brief   Change the shape of the input data during inference.
    * @param net       Network object pointer
    * @param shape     Array of sizes of each dimension of input data (dim-1, dim-2, ... ,1, 0)
    * @param dim       Dimensions of shape
    * @return
    *   If this function is successful, it returns  true  , or  false  otherwise.
    * @details
    *   Changes the input geometry defined in prototxt.
    *   It should be the same as the rank described in prototxt.
    *   Note that an error may be returned if the shape of the weight coefficients depends on the input geometry, for example.
    */
    public bool SetInputShapeND(uint[] shape, int dim)
    {
        if (ailia == IntPtr.Zero || dim < 0)
        {
            return false;
        }

        int status = Status = Ailia.ailiaSetInputShapeND(ailia, shape, (uint)dim);
        if (status != Ailia.AILIA_STATUS_SUCCESS)
        {
            if (logging)
            {
                Debug.Log("ailiaSetInputShapeND failed " + status + " (" + GetStatusString(status) + ")");
            }
            return false;
        }
        return true;
    }


    /**
    * \~japanese
    * @brief 推論時の出力データの形状を取得します。
    * @return
    *   成功した場合は shape 、そうでなければ null を返す。
    * @details
    *   Ailia.ailiaGetOutputShape() で推論時の出力データの形状を取得します。
    *   形状が5次元以上の場合は GetOutputShapeND() を利用してください。
    *   
    * \~english
    * @brief   Obtains the shape of the output data during inference.
    * @return
    *   Return shape on success, null otherwise.
    * @details
    *   Ailia.ailiaGetOutputShape() to get the shape of the output data during inference.
    *   If the shape has more than 5 dimensions, use GetOutputShapeND().
    */
    public Ailia.AILIAShape GetOutputShape()
    {
        if (ailia == IntPtr.Zero)
        {
            return null;
        }
        Ailia.AILIAShape shape = new Ailia.AILIAShape();
        int status = Status = Ailia.ailiaGetOutputShape(ailia, shape, Ailia.AILIA_SHAPE_VERSION);
        if (status != Ailia.AILIA_STATUS_SUCCESS)
        {
            if (logging)
            {
                Debug.Log("ailiaGetOutputShape failed " + status + " (" + GetStatusString(status) + ")");
            }
            return null;
        }
        return shape;
    }

    /**
    * \~japanese
    * @brief 推論時の出力データの形状を取得します。
    * @return
    *   成功した場合は shape 、そうでなければ null を返す。
    * @details
    *   ailiaGetOutputDim() で推論時の出力データの次元を取得します。
    *   Ailia.ailiaGetOutputShapeND() で推論時の出力データの形状を取得します。
    *   
    * \~english
    * @brief   Obtains the shape of the output data during inference.
    * @return
    *   Ifsccessful, return shape, otheerwise null.
    * @details
    *   Get the dimension of the output data during inference with ailiaGetOutputDim().
    *   Ailia.ailiaGetOutputShapeND() to get the shape of the output data during inference.
    */
    public uint[] GetOutputShapeND()
    {
        uint dim = 0;
        int status = Status = Ailia.ailiaGetOutputDim(ailia, ref dim);
        if (status != Ailia.AILIA_STATUS_SUCCESS)
        {
            if (logging)
            {
                Debug.Log("ailiaGetOutputDim failed " + status + " (" + GetStatusString(status) + ")");
            }
            return null;
        }
        uint[] shape = new uint[dim];
        status = Status = Ailia.ailiaGetOutputShapeND(ailia, shape, dim);
        if (status != Ailia.AILIA_STATUS_SUCCESS)
        {
            if (logging)
            {
                Debug.Log("ailiaGetOutputShapeND failed " + status + " (" + GetStatusString(status) + ")");
            }
            return null;
        }
        return shape;
    }

    /**
    * \~japanese
    * @brief 推論時の内部データ(Blob)の形状（レイヤー形式）を取得します。（Obsolete）
    * @param layer_name   検索するBlob名
    * @return
    *   成功した場合は名前で指定したBlobの形状を、そうでなければ null を返す。
    * @details
    *   Ailia.ailiaFindBlobIndexByName() でBlob名からBlobのインデックスを取得します。
    *   Ailia.ailiaGetBlobShape() で推論時の内部データ(Blob)の形状（レイヤー形式）を取得し shape に格納します。
    *   
    * \~english
    * @brief   Obtains the shape (layer format) of the internal data (Blob) at the time of inference. (Obsolete)
    * @param layer_name   Blob name to search
    * @return
    *   If successful, return the shape of the Blob specified by name, otherwise null.
    * @details
    *   Ailia.ailiaFindBlobIndexByName() to get the index of a Blob from the Blob name.
    *   Ailia.ailiaGetBlobShape() obtains the shape (layer format) of the internal data (Blob) at the time of inference and stores it in shape.
    */

    [System.Obsolete("This is an obsolete method")]
    public Ailia.AILIAShape GetBlobShape(string layer_name)
    {
        if (ailia == IntPtr.Zero)
        {
            return null;
        }
        Ailia.AILIAShape shape = new Ailia.AILIAShape();
        uint id = 0;
        int status = Status = Ailia.ailiaFindBlobIndexByName(ailia, ref id, layer_name);
        if (status != Ailia.AILIA_STATUS_SUCCESS)
        {
            if (logging)
            {
                Debug.Log("ailiaFindBlobIndexByName failed " + status + " (" + GetStatusString(status) + ")");
            }
            return null;
        }
        status = Status = Ailia.ailiaGetBlobShape(ailia, shape, id, Ailia.AILIA_SHAPE_VERSION);
        if (status != Ailia.AILIA_STATUS_SUCCESS)
        {
            if (logging)
            {
                Debug.Log("ailiaGetBlobShape failed " + status + " (" + GetStatusString(status) + ")");
            }
            return null;
        }
        return shape;
    }

    /**
    * \~japanese
    * @brief 推論時の内部データ(Blob)のインデックスを名前で探し取得します。
    * @param layer_name   検索するBlob名
    * @return
    *   成功した場合は 名前で指定したBlobのインデックスを 、そうでなければ -1 を返す。
    * @details
    *   Ailia.ailiaFindBlobIndexByName() で推論時の内部データ(Blob)のインデックスを名前で探し取得します。
    *   
    * \~english
    * @brief Look up and retrieve the index of the internal data (Blob) at the time of inference by name.
    * @param layer_name   Blob name to search
    * @return
    *   Returns the index of the Blob specified by name on success, -1 otherwise.
    * @details
    *   Ailia.ailiaFindBlobIndexByName() searches for and retrieves the index of the internal data (Blob) at the time of inference by name.
    */
    public int FindBlobIndexByName(string name)
    {
        uint idx = 0;
        int status = Status = Ailia.ailiaFindBlobIndexByName(ailia, ref idx, name);
        if (status != Ailia.AILIA_STATUS_SUCCESS)
        {
            if (logging)
            {
                Debug.Log("FindBlobIndexByName failed " + status + " (" + GetStatusString(status) + ")");
            }
            return -1;
        }
        return (int)idx;
    }


    /** 
     * \~japanese
     * @brief 推論時の内部データ(Blob)の形状（レイヤー形式）を取得します。
     * @param idx   Blobのインデックス
     * @return
     *   成功した場合は名前で指定したBlobの形状を、そうでなければ null を返す。
     * @details
     *   Ailia.ailiaGetBlobShape() で推論時の内部データ(Blob)の形状（レイヤー形式）を取得し shape に格納します。
     *   
     * \~english
     * @brief   Obtains the shape (layer format) of the internal data (Blob) at the time of inference.
     * @param idx   Index of Blob
     * @return
     *   If successful, return the shape of the Blob specified by name, otherwise null.
     * @details
     *   Ailia.ailiaGetBlobShape() obtains the shape (layer format) of the internal data (Blob) at the time of inference and stores it in shape.
     */
    public Ailia.AILIAShape GetBlobShape(int idx)
    {
        if (ailia == IntPtr.Zero || idx < 0)
        {
            return null;
        }

        Ailia.AILIAShape shape = new Ailia.AILIAShape();
        int status = Status = Ailia.ailiaGetBlobShape(ailia, shape, (uint)idx, Ailia.AILIA_SHAPE_VERSION);
        if (status != Ailia.AILIA_STATUS_SUCCESS)
        {
            if (logging)
            {
                Debug.Log("ailiaGetBlobShape failed " + status + " (" + GetStatusString(status) + ")");
            }
            return null;
        }
        return shape;
    }


    /**
    * \~japanese
    * @brief 推論時の内部データ(Blob)を取得します。
    * @param output_data   推論結果の書き出し先
    * @param idx           Blobのインデックス (0～ ailiaGetBlobCount() -1)
    * @return
    *   成功した場合は true を、失敗した場合は false を返す。
    * @details      
    *   Ailia.ailiaGetBlobData() で推論時の内部データ(Blob)を取得します。
    *   推論を一度も実行していない場合は失敗します。
    *   
    * \~english
    * @brief   Obtains internal data (Blob) at the time of inference.
    * @param output_data   Where to export inference results
    * @param idx           Index of Blob (0 to ailiaGetBlobCount() -1)
    * @return
    *   Returns true on success, false on failure.
    * @details      
    *   Ailia.ailiaGetBlobData() to get the internal data (Blob) at the time of inference.
    *   If the inference has never been performed, it fails.
    */
    public bool GetBlobData(float[] output_data, int idx)
    {
        if (ailia == IntPtr.Zero || idx < 0)
        {
            return false;
        }

        GCHandle output_buf_handle = GCHandle.Alloc(output_data, GCHandleType.Pinned);
        IntPtr output_buf_ptr = output_buf_handle.AddrOfPinnedObject();
        int status = Status = Ailia.ailiaGetBlobData(ailia, output_buf_ptr, (uint)(output_data.Length * 4), (uint)idx);
        output_buf_handle.Free();

        if (status != Ailia.AILIA_STATUS_SUCCESS)
        {
            if (logging)
            {
                Debug.Log("ailiaGetBlobData failed " + status + " (" + GetStatusString(status) + ")");
            }
            return false;
        }
        return true;
    }


    /**
     * \~japanese
     * @brief 指定したBlobに入力データを設定します。
     * @param input_data   推論データ X,Y,Z,Wの順でnumeric型で格納 サイズはネットファイルのinputSizeとなる　（バッファの確保してるので厳密にailia.csの説明と同じでいいかわからない）
     * @param idx          設定を変更するBlobのインデックス
     * @return
     *   成功した場合は true を、失敗した場合は false を返す。
     * @details        
     *   Ailia.ailiaSetInPutBlobData() で指定したBlobに入力データを与えます。
     *   
     * \~english
     * @brief   Set input data to the specified Blob.
     * @param input_data   Inference data X,Y,Z,W, in that order, stored as numeric type Size is inputSize of net file (I'm not sure if this is strictly the same as ailia.cs description, since buffer is allocated)
     * @param idx          Index of Blob to change settings
     * @return
     *   Returns true on success, false on failure.
     * @details        
     *   Ailia.ailiaSetInPutBlobData() gives the input data to the specified Blob.
     */
    public bool SetInputBlobData(float[] input_data, int idx)
    {
        if (ailia == IntPtr.Zero || idx < 0)
        {
            return false;
        }

        GCHandle input_buf_handle = GCHandle.Alloc(input_data, GCHandleType.Pinned);
        IntPtr input_buf_ptr = input_buf_handle.AddrOfPinnedObject();
        int status = Status = Ailia.ailiaSetInputBlobData(ailia, input_buf_ptr, (uint)(input_data.Length * 4), (uint)idx);
        input_buf_handle.Free();

        if (status != Ailia.AILIA_STATUS_SUCCESS)
        {
            if (logging)
            {
                Debug.Log("ailiaSetInputBlobData failed " + status + " (" + GetStatusString(status) + ")");
            }
            return false;
        }
        return true;
    }

    /****************************************************************
     * Blobの形式を設定
     */
    /**
    * \~japanese
    * @brief 指定した入力データ(Blob)の形式を設定します。(4次元以下)
    * @param shape   入力データの形状情報
    * @param idx     変更するBlobのインデックス
    * @return
    *   成功した場合は true を、失敗した場合は false を返す。
    * @detail
    *   複数入力があるネットワークなどで入力形状を変更する場合に用います。
    *   Ailia.ailiaSetInputBlobShape() で指定した入力データ(Blob)の形式を変更します。
    *   入力形状のランクが5次元以上の場合は、SetInputShapeND() を利用してください。
    *   
    * \~english
    * @brief   Sets the format of the specified input data (Blob). (4 dimensions or less)
    * @param shape   Shape information of input data
    * @param idx     Index of Blob to be changed
    * @return
    *   Returns true on success, false on failure.
    * @detail
    *   Used to change the input shape of a network with multiple inputs, for example.
    *   Changes the format of the input data (Blob) specified in Ailia.ailiaSetInputBlobShape().
    *   If the input shape has a rank of 5 or more dimensions, use SetInputShapeND().
    */
    public bool SetInputBlobShape(Ailia.AILIAShape shape, int idx)
    {
        if (ailia == IntPtr.Zero || idx < 0)
        {
            return false;
        }

        int status = Status = Ailia.ailiaSetInputBlobShape(ailia, shape, (uint)idx, Ailia.AILIA_SHAPE_VERSION);
        if (status != Ailia.AILIA_STATUS_SUCCESS)
        {
            if (logging)
            {
                Debug.Log("ailiaSetInputBlobShape failed " + status + " (" + GetStatusString(status) + ")");
            }
            return false;
        }
        return true;
    }

    /**
    * \~japanese
    * @brief 入力データ(Blob)の形式を設定します。(5次元以上)
    * @param shape   入力データの形状情報
    * @param dim     shape の次元
    * @param idx     変更するBlobのインデックス
    * @return
    *   成功した場合は true を、失敗した場合は false を返す。
    * @detail
    *   複数入力があるネットワークなどで入力形状を変更する場合に用います。
    *   Ailia.ailiaSetInputBlobShapeND() で指定した入力データ(Blob)の形式を変更します。
    *   
    * \~english
    * @brief   Sets the format of the input data (Blob). (5D or more)
    * @param shape   Shape information of input data
    * @param dim     Dimensions of shape
    * @param idx     Index of Blob to be changed
    * @return
    *   Returns true on success, false on failure.
    * @detail
    *   Used to change the input shape of a network with multiple inputs, for example.
    *   Changes the format of the input data (Blob) specified in Ailia.ailiaSetInputBlobShapeND().
    */
    public bool SetInputBlobShapeND(uint[] shape, int dim, int idx)
    {
        if (ailia == IntPtr.Zero || idx < 0 || dim < 0)
        {
            return false;
        }

        int status = Status = Ailia.ailiaSetInputBlobShapeND(ailia, shape, (uint)dim, (uint)idx);
        if (status != Ailia.AILIA_STATUS_SUCCESS)
        {
            if (logging)
            {
                Debug.Log("ailiaSetInputBlobShapeND failed " + status + " (" + GetStatusString(status) + ")");
            }
            return false;
        }
        return true;
    }

    /****************************************************************
     * Blobのリストを取得
     */
    /**
    * \~japanese
    * @brief 入力データ(Blob)のインデックスのリストを取得します。
    * @return
    *   成功した場合は入力データ(Blob)のリストを、失敗した場合は null を返します。
    * @detail
    *   Ailia.ailiaGetInputBlobCount()で入力データ(Blob)の数をcountとして取得します。
    *   Ailia.ailiaGetBlobIndexByInputIndex()でBlobのインデックスをidxとして取得し、リストを作成します。
    *   
    * \~english
    * @brief   Obtains a list of the indices of the input data (Blob).
    * @return
    *   Returns a list of input data (Blob) on success or null on failure.
    * @detail
    *   Ailia.ailiaGetInputBlobCount() to get the number of input data (Blob) as count
    *   Ailia.ailiaGetBlobIndexByInputIndex() to get the index of the Blob as idx and create a list.
    */
    public uint[] GetInputBlobList()
    {
        if (ailia == IntPtr.Zero)
        {
            return null;
        }

        uint count = 0;
        int status = Status = Ailia.ailiaGetInputBlobCount(ailia, ref count);
        if (status != Ailia.AILIA_STATUS_SUCCESS || count == 0)
        {
            return null;
        }

        uint[] r = new uint[count];
        for (uint i = 0; i < count; i++)
        {
            uint idx = 0;
            status = Status = Ailia.ailiaGetBlobIndexByInputIndex(ailia, ref idx, i);
            if (status != Ailia.AILIA_STATUS_SUCCESS)
            {
                break;
            }
            r[i] = idx;
        }

        return r;
    }

    /**
    * \~japanese
    * @brief 出力データ(Blob)のインデックスのリストを取得します。
    * @return
    *   成功した場合は入力データ(Blob)のリストを、失敗した場合は null を返します。
    * @detail
    *   Ailia.ailiaGetInputBlobCount()で出力データ(Blob)の数をcountとして取得します。
    *   Ailia.ailiaGetBlobIndexByInputIndex()でBlobのインデックスをidxとして取得し、リストを作成します。
    *   
    * \~english
    * @brief   Obtains a list of indices of the output data (Blob).
    * @return
    *   Returns a list of input data (Blob) on success or null on failure.
    * @detail
    *   Ailia.ailiaGetInputBlobCount() to get the number of output data (Blob) as count.
    *   Ailia.ailiaGetBlobIndexByInputIndex() to get the index of the Blob as idx and create a list.
    */
    public uint[] GetOutputBlobList()
    {
        if (ailia == IntPtr.Zero)
        {
            return null;
        }

        uint count = 0;
        int status = Status = Ailia.ailiaGetOutputBlobCount(ailia, ref count);
        if (status != Ailia.AILIA_STATUS_SUCCESS || count == 0)
        {
            return null;
        }

        uint[] r = new uint[count];
        for (uint i = 0; i < count; i++)
        {
            uint idx = 0;
            status = Status = Ailia.ailiaGetBlobIndexByOutputIndex(ailia, ref idx, i);
            if (status != Ailia.AILIA_STATUS_SUCCESS)
            {
                break;
            }
            r[i] = idx;
        }
        return r;
    }

    /****************************************************************
     * 推論する
     */
    /**
    * \~japanese
    * @brief 事前に入力したデータで推論を行います。
    * @return
    *   成功した場合は true 、失敗した場合は false を返します。
    * @detail
    *   SetInputBlobData() を用いて入力を与えた場合などに用います。
    *   推論結果は GetBlobData() で取得してください。
    *   
    * \~english
    * @brief   Inference is performed with pre-populated data.
    * @return
    *   Returns true on success, false on failure.
    * @detail
    *   This is used, for example, when SetInputBlobData() is used to provide input.
    *   Inference results should be obtained with GetBlobData().
    */
    public bool Update()
    {
        if (ailia == IntPtr.Zero)
        {
            return false;
        }
        int status = Status = Ailia.ailiaUpdate(ailia);
        if (status != Ailia.AILIA_STATUS_SUCCESS)
        {
            if (logging)
            {
                Debug.Log("ailiaUpdate failed " + status + " (" + GetStatusString(status) + ")");
            }
            return false;
        }
        return true;
    }

    /****************************************************************
     * 開放する
     */
    /**
    * \~japanese
    * @brief ネットワークオブジェクトを破棄します。
    * @details
    *   ネットワークオブジェクトを破棄し、初期化します。
    *   
    *  \~english
    * @brief   Destroys network objects.
    * @details
    *   Destroys and initializes the network object.
    */
    public virtual void Close()
    {
        if (ailia != IntPtr.Zero)
        {
            Ailia.ailiaDestroy(ailia);
            ailia = IntPtr.Zero;
        }
    }

    /**
    * \~japanese
    * @brief リソースを解放します。
    *   
    *  \~english
    * @brief   Release resources.
    */
    public virtual void Dispose()
    {
        Dispose(true);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing){
            // release managed resource
        }
        Close(); // release unmanaged resource
    }

    ~AiliaModel(){
        Dispose(false);
    }

    /****************************************************************
     * ステータス文字列の取得
     */
    /**
    * \~japanese
    * @brief ステータスコードに対応する文字列を返します。
    * @param status   ステータスコード
    * @return
    *   成功した場合はエラー詳細の文字列を返します。
    * @detail
    *  ステータスコードに対応するエラー詳細を文字列として返します。
    *   
    * \~english
    * @brief Returns a string corresponding to the status code.
    * * @param status   Status code
    * @return
    *   Returns a string with error details on success.
    * @detail
    *   Returns the error details corresponding to the status code as a string.
    */
    public string GetStatusString(int status)
    {
        return Marshal.PtrToStringAnsi(Ailia.ailiaGetStatusString(status));
    }

    //エラー詳細の取得
    /**
    * \~japanese
    * @brief エラーの詳細を返します。
    * @return
    *   成功した場合はエラー詳細の文字列を返します。
    * @detail
    *   エラー詳細を文字列として返します。
    *   
    * \~english
    * @brief   Returns error details.
    * @return
    *   Returns a string with error details on success.
    * @detail
    *   Returns the error details as a string.
    */
    public string GetErrorDetail()
    {
        return Marshal.PtrToStringAnsi(Ailia.ailiaGetErrorDetail(ailia));
    }


    /**
    *  \~japanese
    *  @brief ステータスコード
    *  @detail
    *    ライブラリのステータスコードを取得します。
    *    
    *  \~english
    *  @brief   Status code
    *  @detail
    *    Get the library status code.
    */
    public int Status { get; protected set; }

    //プロファイルモード有効
    /**
    *  \~japanese
    *  @brief プロファイルモードを有効にします。
    *  @return
    *    成功した場合は true 、失敗した場合は false を返します。
    *  @detail
    *    プロファイルモードを有効にします。プロファイルモードを有効にして推論後、Summary APIでプロファイル結果を取得します。
    *    
    *  \~english
    *  @brief   Enable profile mode
    *  @return
    *    Returns true on success, false on failure.
    *  @detail
    *    Enable profile mode. After enabling profile mode and inference, get profile results in Summary API.
    */
    public bool SetProfileMode(uint profile_mode)
    {
        int status = Status = Ailia.ailiaSetProfileMode(ailia, profile_mode);
        if (status != Ailia.AILIA_STATUS_SUCCESS)
        {
            if (logging)
            {
                Debug.Log("ailiaSetProfileMode failed " + status + " (" + GetStatusString(status) + ")");
            }
            return false;
        }
        return true;
    }

    //プロファイル結果取得

    /**
    * \~japanese
    * @brief ネットワーク情報およびプロファイル結果を取得します。
    * @return
    *   成功した場合はネットワーク情報およびプロファイル結果を示すASCII文字列を、失敗した場合は null を返します。
    * @detail
    *   ネットワーク情報およびプロファイル結果を含む文字列を取得します。
    *   
    * \~english
    * @brief   Obtain network information and profile results
    * @return
    *   Returns an ASCII string displaying the network information and profile results on success, or null on failure.
    * @detail
    *   Obtains a string containing network information and profile results.
    */
    public string GetSummary()
    {
        uint buffer_size = 0;
        int status = Status = Ailia.ailiaGetSummaryLength(ailia, ref buffer_size);
        if (status != Ailia.AILIA_STATUS_SUCCESS)
        {
            if (logging)
            {
                Debug.Log("ailiaGetSummaryLength failed " + status + " (" + GetStatusString(status) + ")");
            }
            return null;
        }
        byte[] buffer = new byte[buffer_size];
        status = Status = Ailia.ailiaSummary(ailia, buffer, buffer_size);
        if (status != Ailia.AILIA_STATUS_SUCCESS)
        {
            if (logging)
            {
                Debug.Log("ailiaSummary failed " + status + " (" + GetStatusString(status) + ")");
            }
            return null;
        }
        return System.Text.Encoding.ASCII.GetString(buffer);
    }
}
