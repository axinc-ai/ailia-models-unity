/**
* \~japanese
* @file
* @brief AILIA Unity Plugin Native Interface
* @author AXELL Corporation
* @date  November 22, 2021
* 
* \~english
* @file
* @brief AILIA Unity Plugin Native Interface
* @author AXELL Corporation
* @date  November 22, 2021
*/
using UnityEngine;
using System.Collections;
using System.IO;
using System;
using System.Runtime.InteropServices;

public class Ailia
{

    /****************************************************************
    * ライブラリ状態定義
    **/

    /**
    * \~japanese
    * 成功
    * 
    * \~english
    * Success
    */
    public const Int32  AILIA_STATUS_SUCCESS                 =(   0);
    /**
    * \~japanese
    * 引数が不正
    * 
    * \~english
    * Invalid argument
    */
    public const Int32  AILIA_STATUS_INVALID_ARGUMENT        =(  -1);
    /**
    * \~japanese
    * ファイルアクセスに失敗した
    * 
    * \~english
    * File access failed.
    */
    public const Int32  AILIA_STATUS_ERROR_FILE_API          =(  -2);
    /**
    * \~japanese
    * ストリームバージョンか構造体バージョンが不正
    * 
    * \~english
    * 
    */
    public const Int32  AILIA_STATUS_INVALID_VERSION         =(  -3);
    /**
    * \~japanese
    * 壊れたファイルが渡された
    * 
    * \~english
    * Incorrect stream version or structure version
    */
    public const Int32  AILIA_STATUS_BROKEN                  =(  -4);
    /**
    * \~japanese
    * メモリが不足している
    * 
    * \~english
    * Insufficient memory
    */
    public const Int32  AILIA_STATUS_MEMORY_INSUFFICIENT     =(  -5);
    /**
    * \~japanese
    * スレッドの作成に失敗した
    * 
    * \~english
    * Failed to create thread.
    */
    public const Int32  AILIA_STATUS_THREAD_ERROR            =(  -6);
    /**
    * \~japanese
    * デコーダの内部状態が不正
    * 
    * \~english
    * Internal state of decoder is incorrect
    */
    public const Int32  AILIA_STATUS_INVALID_STATE           =(  -7);
    /**
    * \~japanese
    * 非対応のネットワーク
    * 
    * \~english
    * Unsupported Networks
    */
    public const Int32  AILIA_STATUS_UNSUPPORT_NET           =(  -9);
    /**
    * \~japanese
    * レイヤーの重み、入力形状などが不正
    * 
    * \~english
    * Incorrect layer weights, input shapes, etc.
    */
    public const Int32  AILIA_STATUS_INVALID_LAYER           =( -10);
    /**
    * \~japanese
    * パラメーターファイルの内容が不正
    * 
    * \~english
    * Invalid parameter file content
    */
    public const Int32  AILIA_STATUS_INVALID_PARAMINFO       =( -11);
    /**
    * \~japanese
    * 指定した要素が見つからなかった
    * 
    * \~english
    * The specified element could not be found.
    */
    public const Int32  AILIA_STATUS_NOT_FOUND               =( -12);
    /**
    * \~japanese
    * GPUで未対応のレイヤーパラメーターが与えられた
    *
    * \~english
    * Given a layer parameter not yet supported by the GPU
    */
    public const Int32  AILIA_STATUS_GPU_UNSUPPORT_LAYER     =( -13);
    /**
    * \~japanese
    * GPU上での処理中にエラー
    *
    * \~english
    * Error during processing on GPU
    */
    public const Int32  AILIA_STATUS_GPU_ERROR               =( -14);
    /**
    * \~japanese
    * 未実装
    * 
    * \~english
    * uninstalled
    */
    public const Int32  AILIA_STATUS_UNIMPLEMENTED           =( -15);
    /**
    * \~japanese
    * 許可されていない操作
    * 
    * \~english
    * Unauthorized operations
    */
    public const Int32  AILIA_STATUS_PERMISSION_DENIED       =( -16);
    /**
    * \~japanese
    * モデルの有効期限切れ
    * 
    * \~english
    * Model expiration
    */
    public const Int32  AILIA_STATUS_EXPIRED                 =( -17);
    /**
    * \~japanese
    * 形状が未確定
    * 
    * \~english
    * Shape not yet determined
    */
    public const Int32  AILIA_STATUS_UNSETTLED_SHAPE         =( -18);
    /**
    * \~japanese
    * アプリケーションからは取得できない情報だった
    * 
    * \~english
    * It was information that could not be retrieved from the application.
    */
    public const Int32  AILIA_STATUS_DATA_HIDDEN             =( -19);
    /**
    * \~japanese
    * 最適化などにより削除された
    * 
    * \~english
    * Removed by optimization, etc.
    */
    public const Int32  AILIA_STATUS_DATA_REMOVED            =AILIA_STATUS_DATA_HIDDEN;
    /**
    * \~japanese
    * 有効なライセンスが見つからない
    * 
    * \~english
    * Cannot find a valid license
    */
    public const Int32  AILIA_STATUS_LICENSE_NOT_FOUND       =( -20);
    /**
    * \~japanese
    * ライセンスが壊れている
    * 
    * \~english
    * License is broken.
    */
    public const Int32  AILIA_STATUS_LICENSE_BROKEN          =( -21);
    /**
    * \~japanese
    * ライセンスの有効期限切れ
    * 
    * \~english
    * License expiration
    */
    public const Int32  AILIA_STATUS_LICENSE_EXPIRED         =( -22);
    /**
    * \~japanese
    * 形状が5次元以上であることを示す
    * 
    * \~english
    * Indicates that the shape is more than 5 dimensional
    */
    public const Int32  AILIA_STATUS_NDIMENSION_SHAPE        =( -23);
    /**
    * \~japanese
    * 不明なエラー
    * 
    * \~english
    * Unknown error
    */
    public const Int32  AILIA_STATUS_OTHER_ERROR             =(-128);

    /* Native Binary 定義 */

    #if (UNITY_IPHONE && !UNITY_EDITOR) || (UNITY_WEBGL && !UNITY_EDITOR)
        public const String LIBRARY_NAME="__Internal";
    #else
        #if (UNITY_ANDROID && !UNITY_EDITOR) || (UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX)
            public const String LIBRARY_NAME="ailia";
        #else
            public const String LIBRARY_NAME="ailia";
        #endif
    #endif

    /****************************************************************
    /* ファイルアクセスコールバック構造体
    **/

    public delegate IntPtr ailiaCallbackOpen(IntPtr args);
    public delegate Int32 ailiaCallbackSeek(IntPtr fp, Int64 offset);
    public delegate Int64 ailiaCallbackTell(IntPtr fp);
    public delegate Int64 ailiaCallbackSize(IntPtr fp);
    public delegate Int32 ailiaCallbackRead(IntPtr dest, Int64 size, IntPtr fp);
    public delegate Int32 ailiaCallbackClose(IntPtr fp);

    [StructLayout(LayoutKind.Sequential)]
    public struct ailiaFileCallback
    {
    /**
    * \~japanese
    * ユーザ定義fopen関数
    * 
    * \~english
    * User-defined fopen function
    */
    public ailiaCallbackOpen  fopen;
    /**
    * \~japanese
    * ユーザ定義fseek関数
    * 
    * \~english
    * User-defined fseek function
    */
    public ailiaCallbackSeek  fseek;
    /**
    * \~japanese
    * ユーザ定義ftell関数
    * 
    * \~english
    * User-defined ftell function
    */
    public ailiaCallbackTell  ftell;
    /**
    * \~japanese
    * ユーザ定義fread関数
    * 
    * \~english
    * User-defined fread function
    */
    public ailiaCallbackRead  fread;
    /**
    * \~japanese
    * ユーザ定義fsize関数
    * 
    * \~english
    * User-defined fsize function
    */
    public ailiaCallbackSize  fsize;
    /**
    * \~japanese
    * ユーザ定義fclose関数
    * 
    * \~english
    * User-defined fclose function
    */
    public ailiaCallbackClose fclose;
    }

    public const Int32 AILIA_FILE_CALLBACK_VERSION           = 1;

    /****************************************************************
    * 形状情報
    **/

    public const Int32  AILIA_SHAPE_VERSION = (1);

    [StructLayout(LayoutKind.Sequential)]
    public class AILIAShape {
        /**
        * \~japanese
        * X軸のサイズ
        * 
        * \~english
        * X axis size
        */
        public UInt32 x;
        /**
        * \~japanese
        * Y軸のサイズ
        * 
        * \~english
        * Y axis size
        */
        public UInt32 y;
        /**
        * \~japanese
        * Z軸のサイズ
        * 
        * \~english
        * Z axis size
        */
        public UInt32 z;
        /**
        * \~japanese
        * W軸のサイズ
        * 
        * \~english
        * W axis size
        */
        public UInt32 w;
        /**
        * \~japanese
        * 次元情報
        * 
        * \~english
        * Dimension Information
        */
        public UInt32 dim;
    }

    /****************************************************************
    * スレッド数
    **/

    public const int AILIA_MULTITHREAD_AUTO = (0);

    /****************************************************************
    * 実行環境自動設定
    **/

    public const int AILIA_ENVIRONMENT_ID_AUTO = (-1);

    /****************************************************************
    * 推論API
    */

    /**
    * \~japanese
    * @brief ネットワークオブジェクトを作成します。
    * @param net        ネットワークオブジェクトポインタへのポインタ
    * @param env_id     計算に利用する環境のID( ailiaGetEnvironment() で取得)  \ref AILIA_ENVIRONMENT_ID_AUTO にした場合は自動で選択する
    * @param num_thread スレッド数、 \ref AILIA_MULTITHREAD_AUTO で自動
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    * @details
    *   ネットワークオブジェクトを作成します。
    *   推論実行環境を自動にした場合はCPUモードになり、BLASが利用できる場合はBLASを利用します。
    *   なお、BLASを利用する場合num_threadは無視される場合があります。
    * 
    * \~english
    * @brief   Create a network object.
    * @param net          Pointer to network object pointer
    * @param env_id       ID of the environment to use for calculation (obtained with ailiaGetEnvironment()) \ref AILIA_ENVIRONMENT_ID_AUTO If set to AILIA_ENVIRONMENT_ID_AUTO, it is automatically selected
    * @param num_thread   Number of threads, automatic if \ref AILIA_MULTITHREAD_AUTO
    * @return
    *   Returns \ref AILIA_STATUS_SUCCESS if successful, otherwise returns an error code.
    *   Create network object.
    *   If the inference execution environment is set to automatic, CPU mode is used, and if BLAS is available, BLAS is used.
    *   Note that num_thread may be ignored when BLAS is used.
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaCreate(ref IntPtr net, int env_id, int num_thread);

    /**
    * \~japanese
    * @brief ネットワークオブジェクトを初期化します。(ファイルから読み込み)
    * @param net          ネットワークオブジェクトポインタ
    * @param path         　prototxtファイルのパス名(MBSC or UTF16)
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    * @details
    *   ファイルから読み込みネットワークオブジェクトを初期化します。
    * 
    * \~english
    * @brief   Initialize network object. (read from file)
    * @param net    Network object pointer
    * @param path   Pathname of prototxt file (MBSC or UTF16)
    * @return
    *   Return \ref AILIA_STATUS_SUCCESS if succeeded, otherwise return error code.
    * @details
    *   Initialize the network object by reading from the file.
    */
#if (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN)
    [DllImport(LIBRARY_NAME, EntryPoint = "ailiaOpenStreamFileW", CharSet=CharSet.Unicode)]
    public static extern int ailiaOpenStreamFile(IntPtr net, string path);
#else
    [DllImport(LIBRARY_NAME, EntryPoint = "ailiaOpenStreamFileA", CharSet=CharSet.Ansi)]
    public static extern int ailiaOpenStreamFile(IntPtr net, string path);
#endif

    /**
    * \~japanese
    * @brief ネットワークオブジェクトに重み係数を読み込みます
    * @param net          ネットワークオブジェクトポインタ
    * @param path         protobuf/onnxファイルのパス名(MBSC or UTF16)
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    * @details
    *   ファイルから読み込みネットワークオブジェクトに重みを読み込みます。
    * 
    * \~english
    * @brief        Load weight coefficients into network object
    * @param net    Network object pointer
    * @param path   Pathname of protobuf/onnx file (MBSC or UTF16)
    * @return
    *   Return \ref AILIA_STATUS_SUCCESS if successful, otherwise return error code.
    * @details
    *   Load weights from the file into the read network object.
    */
#if (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN)
    [DllImport(LIBRARY_NAME, EntryPoint = "ailiaOpenWeightFileW", CharSet=CharSet.Unicode)]
    public static extern int ailiaOpenWeightFile(IntPtr net, string path);
#else
    [DllImport(LIBRARY_NAME, EntryPoint = "ailiaOpenWeightFileA", CharSet=CharSet.Ansi)]
    public static extern int ailiaOpenWeightFile(IntPtr net, string path);
#endif

    /**
    * \~japanese
    * @brief ネットワークオブジェクトを初期化します。(ユーザ定義ファイルアクセスコールバック)
    * @param net              ネットワークオブジェクトポインタ
    * @param fopen_args        \ref AILIA_USER_API_FOPEN に通知される引数ポインタ
    * @param callback         ユーザ定義ファイルアクセスコールバック構造体
    * @param version          ファイルアクセスコールバック構造体のバージョン( \ref AILIA_FILE_CALLBACK_VERSION )
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    * @details
    *   ファイルから読み込み、ネットワークオブジェクトを初期化します。
    * 
    * \~english
    * @brief   Initialize network object. (user-defined file access callback)
    * @param net          Network object pointer
    * @param fopen_args   Argument pointer notified to \ref AILIA_USER_API_FOPEN
    * @param callback     User-defined file access callback structure
    * @param version      Version of the file access callback structure (\ref AILIA_FILE_CALLBACK_VERSION )
    * @return
    *   If the function is successful, returns the file access callback structure version (\ref AILIA_FILE_CALLBACK_VERSION ), otherwise returns an error code.
    * @details
    *   Read from file and initialize network object.
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaOpenStreamEx(IntPtr net, IntPtr fopen_args, ailiaFileCallback callback, Int32 version);

    /**
    * \~japanese
    * @brief ネットワークオブジェクトに重み係数を読み込みます。(ユーザ定義ファイルアクセスコールバック)
    * @param net          ネットワークオブジェクトポインタ
    * @param fopen_args    \ref AILIA_USER_API_FOPEN に通知される引数ポインタ
    * @param callback     ユーザ定義ファイルアクセスコールバック構造体
    * @param version      ファイルアクセスコールバック構造体のバージョン( \ref AILIA_FILE_CALLBACK_VERSION )
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    * @details
    *   ファイルからネットワークオブジェクトに重みを読み込みます。
    * 
    * \~english
    * @brief   Load weight coefficients into network object. (user-defined file access callback)
    * @param net           Network object pointer
    * @param fopen_args    Argument pointer notified to \ref AILIA_USER_API_FOPEN
    * @param callback      User-defined file access callback structure
    * @param version       Version of the file access callback structure (\ref AILIA_FILE_CALLBACK_VERSION )
    * @return
    *   Returns the file access callback structure version (\ref AILIA_FILE_CALLBACK_VERSION ) if successful, otherwise returns an error code.
    * @details
    *   Load weights from a file into a network object.
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaOpenWeightEx(IntPtr net, IntPtr fopen_args, ailiaFileCallback callback, Int32 version);

    /**
    * \~japanese
    * @brief ネットワークオブジェクトを初期化します。(メモリから読み込み)
    * @param net          ネットワークオブジェクトポインタ
    * @param buf          prototxtファイルのデータへのポインター
    * @param buf_size     prototxtファイルのデータサイズ
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    * @details
    *   メモリから読み込み、ネットワークオブジェクトを初期化します。
    * 
    * \~english
    * @brief   Initialize network object. (read from memory)
    * @param net        Network object pointer
    * @param buf        Pointer to data in prototxt file
    * @param buf_size   Data size of prototxt file
    * @return
    *   If the function is successful, return \ref AILIA_STATUS_SUCCESS, otherwise return error code * @details
    * @details
    *   Read from memory and initialize the network object.
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaOpenStreamMem(IntPtr net, byte[] buf, UInt32 buf_size);

    /**
    * \~japanese
    * @brief ネットワークオブジェクトに重み係数を読み込みます。(メモリから読み込み)
    * @param net          ネットワークオブジェクトポインタ
    * @param buf          protobuf/onnxフファイルのデータへのポインター
    * @param buf_size     protobuf/onnxフファイルのデータサイズ
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    * @details
    *   ファイルからネットワークオブジェクトに重みを読み込みます。
    * 
    * \~english
    * @brief   Load weight coefficients into network object. (read from memory)
    * @param net        Network object pointer
    * @param buf        Pointer to data in protobuf/onnxf file
    * @param buf_size   Data size of protobuf/onnxf file
    * @return
    *   If the function successful, return \ref AILIA_STATUS_SUCCESS, otherwise return error code * @details
    * @details
    *   Load weights from the file into the network object.
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaOpenWeightMem(IntPtr net, byte[] buf, UInt32 buf_size);

    /**
    * \~japanese
    * @brief ネットワークオブジェクトを破棄します。
    * @param net ネットワークオブジェクトポインタ
    * 
    * \~english
    * @brief   destroy network object * @param net network object pointer
    * @param net   Network object pointer
    */
    [DllImport(LIBRARY_NAME)]
    public static extern void ailiaDestroy(IntPtr net);

    /**
    * \~japanese
    * @brief 推論時の入力データの形状を変更します。
    * @param net      ネットワークオブジェクトポインタ
    * @param shape    入力データの形状情報
    * @param version  AILIA_SHAPE_VERSION
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    * @details
    *   prototxtで定義されている入力形状を変更します。
    *   prototxtに記述されているランクと同じにする必要があります。
    *   なお、重み係数の形状が入力形状に依存しているなどによりエラーが返る場合があります。
    *   prototxtで定義されているランクが5次元以上の場合は ailiaSetInputShapeND() を利用してください。
    * 
    * \~english
    * @brief   Change the shape of input data during inference.
    * @param net       Network object pointer
    * @param shape     Shape information of input data
    * @param version   AILIA_SHAPE_VERSION
    * @return
    *   Return \ref AILIA_STATUS_SUCCESS if successful, otherwise return error code * @details
    * @details
    *   Change the input shape defined in prototxt.
    *   Must be the same as the rank described in prototxt.
    *   Note that an error code may be returned if the shape of the weight coefficients depends on the input shape.
    *   If the rank defined in prototxt is more than 5 dimensions, use ailiaSetInputShapeND().
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaSetInputShape(IntPtr net, [In] AILIAShape shape, UInt32 version);

    /**
    * \~japanese
    * @brief 推論時の入力データの形状を変更します。
    * @param net       ネットワークオブジェクトポインタ
    * @param shape     入力データの各次元の大きさの配列(dim-1, dim-2, ... ,1, 0)
    * @param dim       shapeの次元
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    * @details
    *   prototxtで定義されている入力形状を変更します。
    *   prototxtに記述されているランクと同じにする必要があります。
    *   なお、重み係数の形状が入力形状に依存しているなどによりエラーが返る場合があります。
    * 
    * \~english
    * @brief   Change the shape of input data during inference.
    * @param net     Network object pointer
    * @param shape   Array of sizes of each dimension of input data (dim-1, dim-2, ... ,1, 0)
    * @param dim     Dimension of shape
    * @return
    *   Return \ref AILIA_STATUS_SUCCESS if successful, otherwise return error code.
    * @details
    *   Change the input shape defined in prototxt.
    *   Must be the same as the rank described in prototxt.
    *   Note that an error may be returned due to the shape of the weight coefficients being dependent on the input shape, etc.
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaSetInputShapeND(IntPtr net, UInt32 [] shape, UInt32 dim);

    /**
    * \~japanese
    * @brief 推論時の入力データの形状を取得します。
    * @param net      ネットワークオブジェクトポインタ
    * @param shape    入力データの形状情報
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、5次元以上の場合 \ref AILIA_STATUS_NDIMENSION_SHAPE 、
    *   形状の一部が未確定の場合 \ref AILIA_STATUS_UNSETTLED_SHAPE 、それ以外のエラーの場合はエラーコードを返す。
    * @details
    *   形状が5次元以上の場合は ailiaGetInputDim() 、 ailiaGetInputShapeND() を利用してください。
    *   形状の一部が未確定の場合、該当する次元の値は0となり、それ以外の次元の値は有効な値が格納されます。
    * 
    * \~english
    * @brief   Get the shape of the input data during inference.
    * @param net     Network object pointer
    * @param shape   Shape information of input data
    * @return
    *   If the function successful \ref AILIA_STATUS_SUCCESS , if more than 5 dimensions \ref AILIA_STATUS_NDIMENSION_SHAPE , if more than 5 dimensions
    *   If the geometry is partially undetermined \ref AILIA_STATUS_UNSETTLED_SHAPE, otherwise return the error code.
    * @details
    *   Use ailiaGetInputDim() or ailiaGetInputShapeND() if the shape has more than 5 dimensions.
    *   If a part of the shape is undefined, the value of the corresponding dimension is set to 0, and the values of the other dimensions are stored as valid values.
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaGetInputShape(IntPtr net, [In,Out] AILIAShape shape, UInt32 version);

    /**
    * \~japanese
    * @brief 推論時の入力データの次元を取得します。
    * @param net       ネットワークオブジェクトポインタ
    * @param dim       入力データの次元の格納先
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、それ以外のエラーの場合はエラーコードを返す。
    * 
    * \~english
    * @brief   Get the dimension of the input data during inference.
    * @param net   Network object pointer
    * @param dim   Where dimension of input data is stored
    * @return
    *   Returns \ref AILIA_STATUS_SUCCESS in case of success, error code in case of other errors.
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaGetInputDim(IntPtr net, ref UInt32 dim);

    /**
    * \~japanese
    * @brief 推論時の入力データの形状を取得します。
    * @param net       ネットワークオブジェクトポインタ
    * @param shape     入力データの各次元の大きさの格納先配列(dim-1, dim-2, ... ,1, 0順で格納)
    * @param dim       shapeの次元
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、形状の一部が未確定の場合 \ref AILIA_STATUS_UNSETTLED_SHAPE 、
    *   それ以外のエラーの場合はエラーコードを返す。
    * @details
    *   形状の一部が未確定の場合、該当する次元の値は0となり、それ以外の次元の値は有効な値が格納されます。
    * 
    * \~english
    * @brief   Get the shape of the input data during inference.
    * @param net     Network object pointer
    * @param shape   Array to store the size of each dimension of the input data (stored in dim-1, dim-2, ... ,1, 0 in that order)
    * @param dim     Dimension of shape
    * @return
    *   If successful, \ref AILIA_STATUS_SUCCESS, if part of the shape is not yet determined, \ref AILIA_STATUS_UNSETTLED_SHAPE, if not yet determined,
    *   Return error code in case of other errors.
    * @details
    *   If a part of the shape is not yet determined, the value of the corresponding dimension is set to 0, and valid values are stored for the other dimensions.
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaGetInputShapeND(IntPtr net, UInt32[] shape, UInt32 dim);

    /**
    * \~japanese
    * @brief 推論・学習時の出力データの形状を取得します。
    * @param net      ネットワークオブジェクトポインタ
    * @param shape    出力データの形状情報
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、5次元以上の場合 \ref AILIA_STATUS_NDIMENSION_SHAPE 、
    *   それ以外のエラーの場合エラーコードを返す。
    * @details
    *   形状が5次元以上の場合は ailiaGetOutputDim() 、 ailiaGetOutputShapeND() を利用してください。
    * 
    * \~english
    * @brief   Get the shape of the output data during inference and training.
    * @param net     Network object pointer
    * @param shape   Shape information of output data
    * @return
    *   In case of success \ref AILIA_STATUS_SUCCESS, in case of 5 or more dimensions \ref AILIA_STATUS_NDIMENSION_SHAPE.
    * Return error code in case of other errors.
    * @details
    *   Use ailiaGetOutputDim() and ailiaGetOutputShapeND() if the shape has more than 5 dimensions.
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaGetOutputShape(IntPtr net, [In,Out] AILIAShape shape, UInt32 version);

    /**
    * \~japanese
    * @brief 推論時の出力データの次元を取得します。
    * @param net       ネットワークオブジェクトポインタ
    * @param dim       出力データの次元の格納先
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、それ以外のエラーの場合はエラーコードを返す。
    * 
    * \~english
    * @brief   Get the dimension of the output data during inference.
    * @param net    Network object pointer
    * @param dim    Where dimension of output data is stored
    * @return
    *   Returns \ref AILIA_STATUS_SUCCESS in case of success, error code in case of other errors.
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaGetOutputDim(IntPtr net, ref UInt32 dim);

    /**
    * \~japanese
    * @brief 推論時の出力データの形状を取得します。
    * @param net       ネットワークオブジェクトポインタ
    * @param shape     出力データの各次元の大きさの格納先配列(dim-1, dim-2, ... ,1, 0順で格納)
    * @param dim       shapeの次元
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、それ以外のエラーの場合はエラーコードを返す。
    * 
    * \~english
    * @brief   Get the shape of the output data during inference.
    * @param net     Network object pointer
    * @param shape   Array to store the size of each dimension of output data (stored in dim-1, dim-2, ... ,1, 0 in that order)
    * @param dim     Dimension of shape
    * @return
    *   Return \ref AILIA_STATUS_SUCCESS in case of success, error code in case of other errors.
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaGetOutputShapeND(IntPtr net, UInt32 [] shape, UInt32 dim);

    /**
    * \~japanese
    * @brief 推論を行い推論結果を取得します。
    * @param net                         ネットワークオブジェクトポインタ
    * @param dest                        推論結果書き出し先バッファ X,Y,Z,Wの順でnumeric型で格納 サイズはネットファイルのoutputSizeとなる
    * @param dest_size                   推論結果書き出し先バッファのbyte数
    * @param src                         推論データ X,Y,Z,Wの順でnumeric型で格納 サイズはネットファイルのinputSizeとなる
    * @param src_size                    推論データのbyte数
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    * 
    * \~english
    * @brief   Infer and get the result of inference.
    * @param net         Network object pointer
    * @param dest        Buffer to which inference result is written, stored in numeric type in the order of X,Y,Z,W Size is outputSize of net file
    * @param dest_size   Number of bytes in the buffer to which inference results are written
    * @param src         Inference data X,Y,Z,W stored in order by numeric type Size is inputSize of net file
    * @param src_size    Number of bytes of inference data
    * @return
    *   If the function is successful, returns \ref AILIA_STATUS_SUCCESS, otherwise returns an error code.
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaPredict(IntPtr net, IntPtr dest, UInt32 dest_size, IntPtr src, UInt32 src_size);

    /****************************************************************
    * 状態取得API
    **/

    /**
    * \~japanese
    * @brief 推論時の内部データ(Blob)の数を取得します。
    * @param net          ネットワークオブジェクトポインタ
    * @param blob_count   blobの数の格納先
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    * 
    * \~english
    * @brief   Get the number of internal data (Blob) at inference.
    * @param net          Network object pointer
    * @param blob_count   Where to store the number of blobs
    * @return
    *   If the function is successful, return \ref AILIA_STATUS_SUCCESS, otherwise return error code.
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaGetBlobCount(IntPtr net, ref UInt32 blob_count);

    /**
    * \~japanese
    * @brief 推論時の内部データ(Blob)の形状を取得します。
    * @param net          ネットワークオブジェクトポインタ
    * @param shape        入力データの形状情報
    * @param blob_idx     blobのインデックス (0～ ailiaGetBlobCount() -1)
    * @param version      AILIA_SHAPE_VERSION
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    * 
    * \~english
    * @brief   Get the shape of the internal data (Blob) at the time of inference.
    * @param net        Network object pointer
    * @param shape      Shape information of input data
    * @param blob_idx   Index of blob (0~ ailiaGetBlobCount() -1)
    * @param version    AILIA_SHAPE_VERSION
    * @return
    *   If the function is successful, return \ref AILIA_STATUS_SUCCESS, otherwise return error code.
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaGetBlobShape(IntPtr net, [In,Out] AILIAShape shape, UInt32 blob_idx, UInt32 version);

    /**
    * \~japanese
    * @brief 推論時の内部データ(Blob)の次元を取得します。
    * @param net        ネットワークオブジェクトポインタ
    * @param dim        blobの次元の格納先
    * @param blob_idx   blobのインデックス (0～ ailiaGetBlobCount() -1)
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、それ以外のエラーの場合はエラーコードを返す。
    * 
    * \~english
    * @brief   Get the dimension of the internal data (Blob) at the time of inference.
    * @param net        Network object pointer
    * @param dim        Where blob's dimension is stored
    * @param blob_idx   Index of blob (0~ ailiaGetBlobCount() -1)
    * @return
    *   Return \ref AILIA_STATUS_SUCCESS in case of success, error code in case of other errors.
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaGetBlobDim(IntPtr net, ref UInt32 dim, UInt32 blob_idx);

    /**
    * \~japanese
    * @brief 推論時の内部データ(Blob)の形状を取得します。
    * @param net        ネットワークオブジェクトポインタ
    * @param shape      blobの各次元の大きさの格納先配列(dim-1, dim-2, ... ,1, 0順で格納)
    * @param dim        shapeの次元
    * @param blob_idx   blobのインデックス (0～ ailiaGetBlobCount() -1)
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、それ以外のエラーの場合はエラーコードを返す。
    * 
    * \~english
    * @brief   Get the shape of the internal data (Blob) at the time of inference.
    * @param net        Network object pointer
    * @param shape      Array to store the size of each dimension of the blob (stored in dim-1, dim-2, ... ,1, 0 in that order)
    * @param dim        Dimension of shape
    * @param blob_idx   Index of the blob (0 to ailiaGetBlobCount() -1)
    * @return
    *   Return \ref AILIA_STATUS_SUCCESS in case of success, error code in case of other errors.
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaGetBlobShapeND(IntPtr net, UInt32[] shape, UInt32 dim, UInt32 blob_idx);

    /**
    * \~japanese
    * @brief 推論時の内部データ(Blob)を取得します。
    * @param net          ネットワークオブジェクトポインタ
    * @param dest         推論結果書き出し先バッファ X,Y,Z,Wの順でnumeric型で格納
    * @param dest_size    推論結果書き出し先バッファのbyte数
    * @param blob_idx     blobのインデックス (0～ ailiaGetBlobCount() -1)
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    * @details
    *    ailiaPredict() を一度も実行していない場合は \ref AILIA_STATUS_INVALID_STATE が返ります。
    * 
    * \~english
    * @brief   Get internal data (Blob) at inference time.
    * @param net         Network object pointer
    * @param dest        Buffer to which inference result is written out Stored in numeric type in the order of X,Y,Z,W
    * @param dest_size   Number of bytes in the buffer to which inference results are written
    * @param blob_idx    Index of blob (0 to ailiaGetBlobCount() -1)
    * @return 
    *   \ref AILIA_STATUS_SUCCESS if successful, otherwise return error code.
    * @details.
    *   If ailiaPredict() has never been executed, then \ref AILIA_STATUS_INVALID_STATE is returned.
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaGetBlobData(IntPtr net, IntPtr dest, UInt32 dest_size, UInt32 blob_idx);

    /**
    * \~japanese
    * @brief 推論時の内部データ(Blob)のインデックスを名前で探し取得します。
    * @param net          ネットワークオブジェクトポインタ
    * @param blob_idx     blobのインデックス (0~ ailiaGetBlobCount() -1)
    * @param name         検索するBlob名
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    * 
    * \~english
    * @brief   Find and get the index of the internal data (Blob) at inference time by name.
    * @param net        Network object pointer
    * @param blob_idx   Index of blob (0~ ailiaGetBlobCount() -1)
    * @param name       Blob name to search
    * @return
    *   If the function is successful, return \ref AILIA_STATUS_SUCCESS, otherwise return error code.
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaFindBlobIndexByName(IntPtr net, ref UInt32 blob_idx, string name);

    /**
    * \~japanese
    * @brief 内部データ(Blob)の名前ための必要なバッファーサイズを出力します。
    * @param net              ネットワークオブジェクトポインタ
    * @param blob_idx         blobのインデックス (0~ ailiaGetBlobCount() -1)
    * @param buffer_size      出力：Blob名を出力するのに必要なバッファーサイズ(終端null文字分を含む)
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    * 
    * \~english
    * @brief   Output the required buffer size for the name of the internal data (Blob).
    * @param net           Network object pointer
    * @param blob_idx      Index of blob (0~ ailiaGetBlobCount() -1)
    * @param buffer_size   Buffer size required to output Blob name (including terminating null character)
    * @return
    *   If the function is successful, return \ref AILIA_STATUS_SUCCESS, otherwise return error code.
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaGetBlobNameLengthByIndex(IntPtr net, UInt32 blob_idx, ref UInt32 buffer_size);

    /**
    * \~japanese
    * @brief 推論時の内部データ(Blob)の名前をインデックスで探し取得します。
    * @param net          ネットワークオブジェクトポインタ
    * @param buffer       Blob名の出力先バッファ
    * @param buffer_size  バッファのサイズ(終端null文字分を含む)
    * @param blob_idx     検索するblobのインデックス
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    * 
    * \~japanese
    * @brief Find and get the name of the internal data (Blob) at the time of inference by index.
    * @param net           Network object pointer
    * @param buffer        Output destination buffer for Blob name
    * @param buffer_size   Size of buffer (including terminating null character)
    * @param blob_idx      Index of blob to search
    * @return
    *   Return \ref AILIA_STATUS_SUCCESS if successful, otherwise return error code.
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaFindBlobNameByIndex(IntPtr net, IntPtr buffer, UInt32 buffer_size, UInt32 blob_idx);

    /**
    * \~japanese
    * @brief ネットワークSummary用に必要なバッファーサイズを出力します。
    * @param net          ネットワークオブジェクトポインタ
    * @param buffer_size  バッファーのサイズの格納先(終端null文字分を含む)
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    * 
    * \~english
    * @brief   Output the required buffer size for network Summary.
    * @param net           Network object pointer
    * @param buffer_size   Where buffer size is stored (including terminating null character)
    * @return
    *   If the function is successful, return \ref AILIA_STATUS_SUCCESS, otherwise return error code.
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaGetSummaryLength(IntPtr net, ref UInt32 buffer_size);

    /**
    * \~japanese
    * @brief 各Blobの名前とシェイプを表示します。
    * @param net          ネットワークオブジェクトポインタ
    * @param buffer       サマリーの出力先
    * @param buffer_size  出力バッファーのサイズ(終端null文字分を含む)。 ailiaGetSummaryLength() で取得した値を設定してください。
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    * 
    * \~english
    * @brief   Display the name and shape of each Blob.
    * @param net           Network object pointer
    * @param buffer        Output destination of summary
    * @param buffer_size   Size of output buffer (including terminating null characters). Set the value obtained with ailiaGetSummaryLength().
    * @return
    *   Returns \ref AILIA_STATUS_SUCCESS if successful, otherwise returns error code.
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaSummary(IntPtr  net, byte[] buffer, UInt32 buffer_size);

    /****************************************************************
    * 複数入力指定・推論API
    **/

    /**
    * \~japanese
    * @brief 入力データ(Blob)の数を取得します。
    * @param net                ネットワークオブジェクトポインタ
    * @param input_blob_count   入力blobの数の格納先
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    * 
    * \~english
    * @brief   Get the number of input data (Blob).
    * @param net                Network object pointer
    * @param input_blob_count   Where to store the number of input blobs
    * @return
    *   If the function is successful, return \ref AILIA_STATUS_SUCCESS, otherwise return error code.
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaGetInputBlobCount(IntPtr net, ref UInt32 input_blob_count);

    /**
    * \~japanese
    * @brief 入力データ(Blob)のインデックスを取得します
    * @param net              ネットワークオブジェクトポインタ
    * @param blob_idx         blobのインデックス(0～ ailiaGetBlobCount() -1)
    * @param input_blob_idx   入力blob内でのインデックス(0～ ailiaGetInputBlobCount() -1)
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    * 
    * \~english
    * @brief   Get the index of the input data (Blob)
    * @param net              Network object pointer
    * @param blob_idx         Index in blob (0~ ailiaGetBlobCount() -1)
    * @param input_blob_idx   Index in input blob (0~ ailiaGetInputBlobCount() -1)
    * @return
    *   return \ref AILIA_STATUS_SUCCESS if successful, otherwise return error code * @param input_blob_idx
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaGetBlobIndexByInputIndex(IntPtr net, ref UInt32 blob_idx, UInt32 input_blob_idx);

    /**
    * \~japanese
    * @brief 指定したBlobに入力データを与えます。
    * @param net          ネットワークオブジェクトポインタ
    * @param src          推論データ X,Y,Z,Wの順でnumeric型で格納 サイズはネットファイルのinputSizeとなる
    * @param src_size     推論データサイズ
    * @param blob_idx     入力するblobのインデックス
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    * @details
    *   複数入力があるネットワークなどで入力を指定する場合に用います。
    *   blob_idxは入力レイヤーのblob以外のものを指定した場合 \ref AILIA_STATUS_INVALID_ARGUMENT が返ります。
    * 
    * \~english
    * @brief   Give input data to the specified Blob.
    * @param net        Network object pointer
    * @param src        Inference data X,Y,Z,W stored in order by numeric type Size is inputSize of net file
    * @param src_size   Inference data size
    * @param blob_idx   Index of input blob
    * @return
    *   If successful, return \ref AILIA_STATUS_SUCCESS, otherwise return error code.
    * @details
    *   This is used to specify an input in a network with multiple inputs, etc. * blob_idx is the index of the input blob.
    *   If blob_idx is specified other than blob of input layer \ref AILIA_STATUS_INVALID_ARGUMENT} is returned.
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaSetInputBlobData(IntPtr net, IntPtr src, UInt32 src_size, UInt32 blob_idx);

    /**
    * \~japanese
    * @brief 指定したBlobの形状を変更します。
    * @param net      ネットワークオブジェクトポインタ
    * @param shape    入力データの形状情報
    * @param blob_idx 変更するblobのインデックス
    * @param version  AILIA_SHAPE_VERSION
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    * @details
    *   複数入力があるネットワークなどで入力形状を変更する場合に用います。
    *   blob_idxは入力レイヤーのblob以外のものを指定した場合 \ref AILIA_STATUS_INVALID_ARGUMENT が返ります。
    *   その他の注意点は ailiaSetInputShape() の解説を参照してください。
    *   入力形状のランクが5次元以上の場合は ailiaSetInputBlobShapeND() を利用してください。
    * 
    * \~english
    * @brief   Change the shape of the specified Blob.
    * @param net        Network object pointer
    * @param shape      Shape information of input data
    * @param blob_idx   Index of blob to be changed
    * @param version    AILIA_SHAPE_VERSION
    * @return
    * @return 
    *   Return \ref AILIA_STATUS_SUCCESS if succeeded, otherwise return error code.
    * @details
    *   This is used to change the input shape in networks with multiple inputs, etc.
    *   If blob_idx is specified other than blob of input layer \ref AILIA_STATUS_INVALID_ARGUMENT} is returned.
    *   See the description of ailiaSetInputShape() for other notes.
    *   If the input shape has a rank of 5 or more dimensions, use ailiaSetInputBlobShapeND().
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaSetInputBlobShape(IntPtr net, [In] AILIAShape shape, UInt32 blob_idx,UInt32 version);

    /**
    * \~japanese
    * @brief 指定したBlobの形状を変更します。
    * @param net      ネットワークオブジェクトポインタ
    * @param shape    入力データの各次元の大きさの配列(dim-1, dim-2, ... ,1, 0)
    * @param dim      shapeの次元
    * @param blob_idx 変更するblobのインデックス
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    * @details
    *   複数入力があるネットワークなどで入力形状を変更する場合に用います。
    *   blob_idxは入力レイヤーのblob以外のものを指定した場合 \ref AILIA_STATUS_INVALID_ARGUMENT が返ります。
    *   その他の注意点は ailiaSetInputShapeND() の解説を参照してください。
    * 
    * \~english
    * @brief   Change the shape of the specified Blob.
    * @param net        Network object pointer
    * @param shape      Array of sizes of each dimension of input data (dim-1, dim-2, ... ,1, 0)
    * @param dim dim    Dimension of shape
    * @param blob_idx   Index of blob to be changed
    * @return
    *   Return \ref AILIA_STATUS_SUCCESS if successful, otherwise return error code.
    * @details
    *   This is used to change the input shape in a network with multiple inputs, etc.
    *   If blob_idx is specified other than blob of input layer \ref AILIA_STATUS_INVALID_ARGUMENT} is returned.
    *   See the description of ailiaSetInputShapeND() for other notes.
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaSetInputBlobShapeND(IntPtr net, UInt32[] shape, UInt32 dim, UInt32 blob_idx);

    /**
    * \~japanese
    * @brief 事前に指定した入力データで推論を行います。
    * @param net  ネットワークオブジェクトポインタ
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    * @details
    *   ailiaSetInputBlobData() を用いて入力を与えた場合などに用います。
    *   推論結果は ailiaGetBlobData() で取得してください。
    * 
    * \~english
    * @brief   Perform inference with pre-specified input data.
    * @param net   Network object pointer
    * @return
    *   Returns \ref AILIA_STATUS_SUCCESS if successful, otherwise returns an error code.
    * @details
    * @details 
    *   This is used when input is given using ailiaSetInputBlobData(), etc. 
    *   Get the inference result with ailiaGetBlobData().
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaUpdate(IntPtr net);

    /**
    * \~japanese
    * @brief 出力データ(Blob)の数を取得します。
    * @param net                 ネットワークオブジェクトポインタ
    * @param output_blob_count   出力blobの数の格納先
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    * 
    * \~english
    * @brief   Get the number of output data (Blob).
    * @param net                 Network object pointer
    * @param output_blob_count   Where to store the number of output blobs
    * @return
    *   If successful, return \ref AILIA_STATUS_SUCCESS, otherwise return error code.
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaGetOutputBlobCount(IntPtr net, ref UInt32 output_blob_count);

    /**
    * \~japanese
    * @brief 出力データ(Blob)のインデックスを取得します
    * @param net               ネットワークオブジェクトポインタ
    * @param blob_idx          blobのインデックス(0～ ailiaGetBlobCount() -1)
    * @param output_blob_idx   出力blob内でのインデックス(0～ ailiaGetOutputBlobCount() -1)
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    * 
    * \~english
    * @brief   get index of output data (Blob)
    * @param net                Network object pointer
    * @param blob_idx           Index in blob (0~ ailiaGetBlobCount() -1)
    * @param output_blob_idx    Index in output blob (0~ ailiaGetOutputBlobCount() -1)
    * @return
    *   If successful, returns \ref AILIA_STATUS_SUCCESS, otherwise returns an error code.
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaGetBlobIndexByOutputIndex(IntPtr net, ref UInt32 blob_idx, UInt32 output_blob_idx);

    /****************************************************************
    * 実行環境取得・指定API
    **/


    public const int AILIA_ENVIRONMENT_VERSION = (2);

    public const int AILIA_ENVIRONMENT_TYPE_CPU     = (0);
    public const int AILIA_ENVIRONMENT_TYPE_BLAS    = (1);
    public const int AILIA_ENVIRONMENT_TYPE_GPU     = (2);
    public const int AILIA_ENVIRONMENT_TYPE_REMOTE  = (3);

    public const int AILIA_ENVIRONMENT_BACKEND_NONE          = (0);
    public const int AILIA_ENVIRONMENT_BACKEND_CUDA          = (2);
    public const int AILIA_ENVIRONMENT_BACKEND_MPS           = (3);
    public const int AILIA_ENVIRONMENT_BACKEND_VULKAN        = (6);

    public const int AILIA_ENVIRONMENT_PROPERTY_NORMAL       = (0);
    /**
    * \~japanese
    * 省電力なGPU(内蔵GPUなど)を用いることを示す(MPS用)
    * 
    * \~english
    * Indicates the use of a power-saving GPU (e.g., built-in GPU) (for MPS).
    */
    public const int AILIA_ENVIRONMENT_PROPERTY_LOWPOWER     = (1);
    /**
    * \~japanese
    * FP16で動作することを示す
    * 
    * \~english
    * Indicates that it works with FP16.
    */
    public const int AILIA_ENVIRONMENT_PROPERTY_FP16         = (2);

    [StructLayout(LayoutKind.Sequential)]
    public class AILIAEnvironment {
        /**
        * \~japanese
        * 環境を識別するID
        */
        public Int32 id;
        /**
        * \~japanese
        * 環境の種別( \ref AILIA_ENVIRONMENT_TYPE_CPU  or BLAS or GPU)
        * 
        * ~\~english
        * Environment type (\ref AILIA_ENVIRONMENT_TYPE_CPU or BLAS or GPU)
        */
        public Int32 type;
        /**
        * \~japanese
        * デバイス名(シングルトンで保持されており開放不要)(Marshal.PtrToStringAnsiでstringに変換可能)
        * 
        * \~english
        * Device name (held in singleton, no need to open) (can be converted to string with Marshal.PtrToStringAnsi)
        */
        public IntPtr name;
        /**
        * \~japanese
        * 環境のバックエンド (AILIA_ENVIRONMENT_BACKEND_*) (type== \ref AILIA_ENVIRONMENT_TYPE_GPU の場合有効)
        * 
        * \~english
        * Environment backend (AILIA_ENVIRONMENT_BACKEND_*) (valid for type== \ref AILIA_ENVIRONMENT_TYPE_GPU)
        */
        public Int32 backend;
        /**
        * \~japanese
        * 環境の特性などを示す(AILIA_ENVIRONMENT_PROPERTY_* の論理和)
        * 
        * \~english
        * Indicates environmental characteristics, etc. (logical OR of AILIA_ENVIRONMENT_PROPERTY_*)
        */
        public Int32 props;
    }

    /**
    * \~japanese
    * @brief 一時キャッシュディレクトリを指定します
    * @param cache_dir    一時キャッシュディレクトリ
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    * @details
    *   指定したキャッシュディレクトリは推論実行環境毎に最適化したマシンコードを生成して保存するためにシステムが利用します。
    *   ailia の実行開始時に一度だけ呼び出してください。二回目以降の呼び出しに対しては無視して成功を返します。
    *   複数スレッドから呼び出された場合も内部で排他制御しているので特に問題は発生しません。
    *   Vulkan の shader cache 機能など、この API を呼ぶまで利用できないものがあります。
    *   cache_dirにはContext.getCacheDir()で取得したファイルパスを指定してください。
    * 
    * \~english
    * @brief   Specify temporary cache directory
    * @param cache_dir   temporary cache directory
    * @return
    *    If successful, return \ref AILIA_STATUS_SUCCESS, otherwise return error code.
    * @details
    *    The specified cache directory is used by the system to generate and store machine code optimized for each inference execution environment.
    *    Only one call should be made at the start of ailia execution. For the second and subsequent calls, ignore them and return success.
    *    No particular problem occurs when called by multiple threads because of the internal exclusion control.
    *    Some functions, such as Vulkan's shader cache function, cannot be used until this API is called.
    *    Specify the file path obtained by Context.getCacheDir() for cache_dir.
    */
#if (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN)
    [DllImport(LIBRARY_NAME, EntryPoint = "ailiaSetTemporaryCachePathW", CharSet=CharSet.Unicode)]
    public static extern int ailiaSetTemporaryCachePath(string path);
#else
    [DllImport(LIBRARY_NAME, EntryPoint = "ailiaSetTemporaryCachePathA", CharSet=CharSet.Ansi)]
    public static extern int ailiaSetTemporaryCachePath(string path);
#endif

    /**
    * \~japanese
    * @brief 利用可能な計算環境(CPU, GPU)の数を取得します
    * @param env_count     環境情報の数の格納先
    * @return
    *   利用可能な環境の件数
    * 
    * \~english
    * @brief   Get the number of available computing environments (CPU, GPU)
    * @param env_count   Where to store the number of environment information
    * @return
    *    Number of available environments
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaGetEnvironmentCount(ref Int32 env_count);

    /**
    * \~japanese
    * @brief 計算環境の一覧を取得します
    * @param env          環境情報の格納先(AILIANetworkインスタンスを破棄するまで有効)
    * @param env_idx      環境情報のインデックス(0~ ailiaGetEnvironmentCount() -1)
    * @param version      AILIA_ENVIRONMENT_VERSION
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    * @details
    *   取得したポインタから以下のように構造体に変換して下さい。
    *   @code
    *   System.IntPtr env_ptr = System.IntPtr.Zero;
    *   Ailia.ailiaGetEnvironment(ref env_ptr,(uint)environment, Ailia.AILIA_ENVIRONMENT_VERSION);
    *   Ailia.AILIAEnvironment  env = (Ailia.AILIAEnvironment)Marshal.PtrToStructure(env_ptr, typeof(Ailia.AILIAEnvironment));
    *   @endcode
    * 
    * \~japanese
    * @brief   Get a list of computation environments
    * @param env        Where environment information is stored (valid until AILIANetwork instance is destroyed)
    * @param env_idx    Index of environment information (0~ ailiaGetEnvironmentCount() -1)
    * @param version    AILIA_ENVIRONMENT_VERSION
    * @return
    *    Return \ref AILIA_STATUS_SUCCESS if successful, otherwise return error code.
    * @details
    *    Convert the obtained pointer to a structure as follows.
    * @code
    *    System.IntPtr env_ptr = System.IntPtr.Zero;
    *    Ailia.ailiaGetEnvironment(ref env_ptr,(uint)environment, Ailia.AILIA_ENVIRONMENT_VERSION);
    *    Ailia.AILIAEnvironment env = (Ailia.AILIAEnvironment)Marshal.PtrToStructure(env_ptr, typeof(Ailia.AILIAEnvironment));
    *    @endcode
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaGetEnvironment(ref IntPtr env, UInt32 env_idx, UInt32 version);

    /**
    * \~japanese
    * @brief 選択された計算環境を取得します
    * @param net          ネットワークオブジェクトポインタ
    * @param env          計算環境情報の格納先(AILIANetworkインスタンスを破棄するまで有効)
    * @param version      AILIA_ENVIRONMENT_VERSION
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    * 
    * \~english
    * @brief Get the selected computing environment
    * @param net          Network object pointer
    * @param env          Where computation environment information is stored (valid until AILIANetwork instance is destroyed)
    * @param version      AILIA_ENVIRONMENT_VERSION
    * @return
    *    If successful, return \ref AILIA_STATUS_SUCCESS, otherwise return error code.
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaGetSelectedEnvironment(IntPtr net, ref IntPtr env, UInt32 version);

    /****************************************************************
    * メモリモードAPI
    **/

    /**
    * \~japanese
    * 中間バッファーの開放は行わない。
    * 
    * \~english
    * Intermediate buffers are not opened.
    */
    public const Int32 AILIA_MEMORY_NO_OPTIMIZATION                        = (0);
    /**
    * \~japanese
    * 重みなどの定数となる中間バッファーを開放する。
    * 
    * \~english
    * Open intermediate buffers that are constants for weights, etc.
    */
    public const Int32 AILIA_MEMORY_REDUCE_CONSTANT                        = (1);
    /**
    * \~japanese
    * 入力指定のinitializerを変更不可にし、重みなどの定数となる中間バッファーを開放する。
    * 
    * \~english
    * The input designation initializer is made unchangeable, and intermediate buffers that are constants for weights, etc., are released.
    */
    public const Int32 AILIA_MEMORY_REDUCE_CONSTANT_WITH_INPUT_INITIALIZER = (2);
    /**
    * \~japanese
    * 推論時の中間バッファーを開放する
    * 
    * \~english
    * Open intermediate buffers during inference.
    */
    public const Int32 AILIA_MEMORY_REDUCE_INTERSTAGE                      = (4);
    /**
    * \~japanese
    * 中間バッファーを共有して推論する。 \ref AILIA_MEMORY_REDUCE_INTERSTAGE と併用した場合、共有可能な中間バッファーは開放しない。
    * 
    *  \~english
    * Share intermediate buffers to infer. When used with \ref AILIA_MEMORY_REDUCE_INTERSTAGE, the shareable intermediate buffer is not released.
    */
    public const Int32 AILIA_MEMORY_REUSE_INTERSTAGE                       = (8);

    public const Int32 AILIA_MEMORY_OPTIMAIZE_DEFAULT = (AILIA_MEMORY_REDUCE_CONSTANT);

    /**
    * \~japanese
    * @brief 推論時のメモリの使用方針を設定します
    * @param net        ネットワークオブジェクトポインタ
    * @param mode       メモリモード(論理和で複数指定可) AILIA_MEMORY_XXX (デフォルト: \ref AILIA_MEMORY_REDUCE_CONSTANT )
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    * @details
    *   メモリの使用方針を変更します。 \ref AILIA_MEMORY_NO_OPTIMIZATION 以外を指定した場合は、
    *   推論時に確保する中間バッファーを開放するため、推論時のメモリ使用量を削減することができます。
    *    ailiaCreate() の直後に指定する必要があります。ailiaOpenを呼び出した後は変更することができません。
    *   なお、中間バッファーを開放するように指定した場合、該当するBlobに対し、 ailiaGetBlobData() を呼び出すと
    *    \ref AILIA_STATUS_DATA_HIDDEN エラーが返ります。
    * 
    * \~english
    * @brief   Sets the memory usage policy during inference
    * @param net        network object pointer
    * @param mode       Memory mode (multiple specifiable by logical OR) AILIA_MEMORY_XXX (default: \ref AILIA_MEMORY_REDUCE_CONSTANT )
    * @return
    *   If successful, returns \ref AILIA_STATUS_SUCCESS, otherwise returns an error code.
    * @details
    *   Change the memory usage policy. If you specify anything other than \ref AILIA_MEMORY_NO_OPTIMIZATION
    *   The amount of memory used during inference can be reduced because the intermediate buffer allocated during inference is released.
    *   It must be specified immediately after ailiaCreate(); it cannot be changed after a call to ailiaOpen.
    *   Note that if you specify that intermediate buffers are to be released, calling ailiaGetBlobData() for the corresponding Blob will return
    *   \ref AILIA_STATUS_DATA_HIDDEN error will be returned.
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaSetMemoryMode(IntPtr net, UInt32 mode);

    /**
	* \~japanese
	* @brief 推論時のレイヤー統合を無効化します
	* @param net ネットワークオブジェクトポインタ
	* @return
	*   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
	* @details
	*   レイヤー統合により取得できなくなるBlobを取得する必要がある場合などに用います。
	*   ailiaCreate() の直後に指定する必要があります。ailiaOpenを呼び出した後は変更することができません。
	*   なお、レイヤー統合を無効化すると推論速度が低下する場合があります。
	* 
	* \~english
	* @brief   Disables layer integration during inference.
	* @param net   Network object pointer
	* @return
	*   If successful, returns \ref AILIA_STATUS_SUCCESS, otherwise returns an error code.
	* @details
	*   This is used when it is necessary to retrieve a Blob that can no longer be retrieved due to layer integration, etc. * Must be specified immediately after ailiaCreate().
	*   Must be specified immediately after ailiaCreate(); it cannot be changed after a call to ailiaOpen.
	*   Note that disabling layer integration may reduce inference speed.
	*/
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaDisableLayerFusion(IntPtr net);

    /****************************************************************
    * プロファイルモードAPI
    **/

    /**
    * \~japanese
    * プロファイルモードを無効にします(デフォルト)
    */
    public const UInt32 AILIA_PROFILE_DISABLE     = (0x00);
    /**
    * \~japanese
    * レイヤー別の処理時間を計測します。複数回推論した場合は初回実行を除く平均値が保存されます。
    */
    public const UInt32 AILIA_PROFILE_AVERAGE     = (0x01);

    /**
    * \~japanese
    * @brief プロファイルモードをセットします
    * @param net          ネットワークオブジェクトポインタ
    * @param mode         プロファイルモード
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    * @details
    *   プロファイルモードを指定します。デフォルトは無効です。
    *   ailiaOpenStreamXXXを呼び出したあとに呼び出してください。
    *   プロファイルモードを有効にした場合、 ailiaSummary() の出力にプロファイル結果が追加されます。
    * 
    * \~english
    * @brief   Set profile mode
    * @param net          network object pointer 
    * @param mode         profile mode
    * @return
    *   If successful, returns \ref AILIA_STATUS_SUCCESS, otherwise returns an error code.
    * @details
    *   Specifies the profile mode. Default is disabled.
    *   Call it after calling ailiaOpenStreamXXX.
    *   If profile mode is enabled, the profile result is added to the output of ailiaSummary().
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaSetProfileMode(IntPtr net, UInt32 mode);

    /****************************************************************
    * 情報取得API
    **/

    /**
    * \~japanese
    * @brief ステータスコードを説明する文字列を返します
    * @param status   ステータスコード
    * @return
    *   ステータスコードを説明する文字列
    * @details
    *   返値は解放する必要はありません。
    *   文字列の有効期間はailiaのライブラリをアンロードするまでです。
    *   取得したポイントから以下のように文字列に変換して下さい。
    *   @code
    *   Marshal.PtrToStringAnsi(Ailia.ailiaGetStatusString(status))
    *   @endcode
    * 
    * \~english
    * @brief   Returns a string describing the status code
    * @param status   Status code
    * @return
    *   String describing the status code
    * @details
    *   The return value does not need to be released.
    *   The string is valid until the ailia library is unloaded.
    *   Convert from the point obtained to a string as follows
    *   @code
    *   Marshal.PtrToStringAnsi(Ailia.ailiaGetStatusString(status))
    *   @endcode
    */
    [DllImport(LIBRARY_NAME)]
    public static extern IntPtr ailiaGetStatusString(Int32 status);

    /**
    * \~japanese
    * @brief エラーの詳細を返します
    * @param net   ネットワークオブジェクトポインタ
    * @return
    *   エラー詳細
    * @details
    *   返値は解放する必要はありません。
    *   文字列の有効期間は次にailiaのAPIを呼ぶまでです。
    *   取得したポイントから以下のように文字列に変換して下さい。
    *   @code
    *   Marshal.PtrToStringAnsi(Ailia.ailiaGetErrorDetail(net))
    *   @endcode
    * 
    * \~english
    * @brief   Returns error details
    * @param net   Network object pointer
    * @return
    *   Error Details
    * @details
    *   The return value does not need to be released.
    *   The string is valid until the next call to ailia's API.
    *   Convert from the point obtained to a string as follows
    *   @code
    *   Marshal.PtrToStringAnsi(Ailia.ailiaGetErrorDetail(net))
    *   @endcode
    */
    [DllImport(LIBRARY_NAME)]
    public static extern IntPtr ailiaGetErrorDetail(IntPtr net);

    /**
    * \~japanese
    * @brief ライブラリバージョンを取得します。
    * @return
    *   バージョン番号(Marshal.PtrToStringAnsiでstringに変換可能)
    * @details
    *   返値は解放する必要はありません。
    * 
    * \~english
    * @brief   Get the library version.
    * @return
    *   Version number (can be converted to string with Marshal.PtrToStringAnsi)
    * @details
    *   The return value does not need to be released.
    */
    [DllImport(LIBRARY_NAME)]
    public static extern IntPtr ailiaGetVersion();
}
