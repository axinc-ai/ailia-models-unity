/**
* \~japanese
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
    */
    public const Int32  AILIA_STATUS_SUCCESS                 =(   0);
    /**
    * \~japanese
    * 引数が不正
    */
    public const Int32  AILIA_STATUS_INVALID_ARGUMENT        =(  -1);
    /**
    * \~japanese
    * ファイルアクセスに失敗した
    */
    public const Int32  AILIA_STATUS_ERROR_FILE_API          =(  -2);
    /**
    * \~japanese
    * ストリームバージョンか構造体バージョンが不正
    */
    public const Int32  AILIA_STATUS_INVALID_VERSION         =(  -3);
    /**
    * \~japanese
    * 壊れたファイルが渡された
    */
    public const Int32  AILIA_STATUS_BROKEN                  =(  -4);
    /**
    * \~japanese
    * メモリが不足している
    */
    public const Int32  AILIA_STATUS_MEMORY_INSUFFICIENT     =(  -5);
    /**
    * \~japanese
    * スレッドの作成に失敗した
    */
    public const Int32  AILIA_STATUS_THREAD_ERROR            =(  -6);
    /**
    * \~japanese
    * デコーダの内部状態が不正
    */
    public const Int32  AILIA_STATUS_INVALID_STATE           =(  -7);
    /**
    * \~japanese
    * 非対応のネットワーク
    */
    public const Int32  AILIA_STATUS_UNSUPPORT_NET           =(  -9);
    /**
    * \~japanese
    * レイヤーの重み、入力形状などが不正
    */
    public const Int32  AILIA_STATUS_INVALID_LAYER           =( -10);
    /**
    * \~japanese
    * パラメーターファイルの内容が不正
    */
    public const Int32  AILIA_STATUS_INVALID_PARAMINFO       =( -11);
    /**
    * \~japanese
    * 指定した要素が見つからなかった
    */
    public const Int32  AILIA_STATUS_NOT_FOUND               =( -12);
    /**
    * \~japanese
    * GPUで未対応のレイヤーパラメーターが与えられた
    */
    public const Int32  AILIA_STATUS_GPU_UNSUPPORT_LAYER     =( -13);
    /**
    * \~japanese
    * GPU上での処理中にエラー
    */
    public const Int32  AILIA_STATUS_GPU_ERROR               =( -14);
    /**
    * \~japanese
    * 未実装
    */
    public const Int32  AILIA_STATUS_UNIMPLEMENTED           =( -15);
    /**
    * \~japanese
    * 許可されていない操作
    */
    public const Int32  AILIA_STATUS_PERMISSION_DENIED       =( -16);
    /**
    * \~japanese
    * モデルの有効期限切れ
    */
    public const Int32  AILIA_STATUS_EXPIRED                 =( -17);
    /**
    * \~japanese
    * 形状が未確定
    */
    public const Int32  AILIA_STATUS_UNSETTLED_SHAPE         =( -18);
    /**
    * \~japanese
    * アプリケーションからは取得できない情報だった
    */
    public const Int32  AILIA_STATUS_DATA_HIDDEN             =( -19);
    /**
    * \~japanese
    * 最適化などにより削除された
    */
    public const Int32  AILIA_STATUS_DATA_REMOVED            =AILIA_STATUS_DATA_HIDDEN;
    /**
    * \~japanese
    * 有効なライセンスが見つからない
    */
    public const Int32  AILIA_STATUS_LICENSE_NOT_FOUND       =( -20);
    /**
    * \~japanese
    * ライセンスが壊れている
    */
    public const Int32  AILIA_STATUS_LICENSE_BROKEN          =( -21);
    /**
    * \~japanese
    * ライセンスの有効期限切れ
    */
    public const Int32  AILIA_STATUS_LICENSE_EXPIRED         =( -22);
    /**
    * \~japanese
    * 形状が5次元以上であることを示す
    */
    public const Int32  AILIA_STATUS_NDIMENSION_SHAPE        =( -23);
    /**
    * \~japanese
    * 不明なエラー
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
    */
    public ailiaCallbackOpen  fopen;
    /**
    * \~japanese
    * ユーザ定義fseek関数
    */
    public ailiaCallbackSeek  fseek;
    /**
    * \~japanese
    * ユーザ定義ftell関数
    */
    public ailiaCallbackTell  ftell;
    /**
    * \~japanese
    * ユーザ定義fread関数
    */
    public ailiaCallbackRead  fread;
    /**
    * \~japanese
    * ユーザ定義fsize関数
    */
    public ailiaCallbackSize  fsize;
    /**
    * \~japanese
    * ユーザ定義fclose関数
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
        */
        public UInt32 x;
        /**
        * \~japanese
        * Y軸のサイズ
        */
        public UInt32 y;
        /**
        * \~japanese
        * Z軸のサイズ
        */
        public UInt32 z;
        /**
        * \~japanese
        * W軸のサイズ
        */
        public UInt32 w;
        /**
        * \~japanese
        * 次元情報
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
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaCreate(ref IntPtr net, int env_id, int num_thread);

    /**
    * \~japanese
    * @brief ネットワークオブジェクトを初期化します。(ファイルから読み込み)
    * @param net          ネットワークオブジェクトポインタ
    * @param path         ailiaファイルのパス名(MBSC or UTF16)
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    * @details
    *   ファイルから読み込みネットワークオブジェクトを初期化します。
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
    *   ファイルから読み込みネットワークオブジェクトに重みを読み込みます
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
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaOpenWeightMem(IntPtr net, byte[] buf, UInt32 buf_size);

    /**
    * \~japanese
    * @brief ネットワークオブジェクトを破棄します。
    * @param net ネットワークオブジェクトポインタ
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
    *    ailiaSetInputBlobData() を用いて入力を与えた場合などに用います。
    *   推論結果は ailiaGetBlobData() で取得してください。
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
    public const int AILIA_ENVIRONMENT_BACKEND_AMP           = (1);
    public const int AILIA_ENVIRONMENT_BACKEND_CUDA          = (2);
    public const int AILIA_ENVIRONMENT_BACKEND_MPS           = (3);
    public const int AILIA_ENVIRONMENT_BACKEND_VULKAN        = (6);

    public const int AILIA_ENVIRONMENT_PROPERTY_NORMAL       = (0);
    /**
    * \~japanese
    * 省電力なGPU(内蔵GPUなど)を用いることを示す(MPS用)
    */
    public const int AILIA_ENVIRONMENT_PROPERTY_LOWPOWER     = (1);
    /**
    * \~japanese
    * FP16で動作することを示す
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
        */
        public Int32 type;
        /**
        * \~japanese
        * デバイス名(シングルトンで保持されており開放不要)(Marshal.PtrToStringAnsiでstringに変換可能)
        */
        public IntPtr name;
        /**
        * \~japanese
        * 環境のバックエンド (AILIA_ENVIRONMENT_BACKEND_*) (type== \ref AILIA_ENVIRONMENT_TYPE_GPU の場合有効)
        */
        public Int32 backend;
        /**
        * \~japanese
        * 環境の特性などを示す(AILIA_ENVIRONMENT_PROPERTY_* の論理和)
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
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaGetSelectedEnvironment(IntPtr net, ref IntPtr env, UInt32 version);

    /****************************************************************
    * メモリモードAPI
    **/

    /**
    * \~japanese
    * 中間バッファーの開放は行わない
    */
    public const Int32 AILIA_MEMORY_NO_OPTIMIZATION                        = (0);
    /**
    * \~japanese
    * 重みなどの定数となる中間バッファーを開放する
    */
    public const Int32 AILIA_MEMORY_REDUCE_CONSTANT                        = (1);
    /**
    * \~japanese
    * 入力指定のinitializerを変更不可にし、重みなどの定数となる中間バッファーを開放する
    */
    public const Int32 AILIA_MEMORY_REDUCE_CONSTANT_WITH_INPUT_INITIALIZER = (2);
    /**
    * \~japanese
    * 推論時の中間バッファーを開放する
    */
    public const Int32 AILIA_MEMORY_REDUCE_INTERSTAGE                      = (4);
    /**
    * \~japanese
    * 中間バッファーを共有して推論する。 \ref AILIA_MEMORY_REDUCE_INTERSTAGE と併用した場合、共有可能な中間バッファーは開放しない。
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
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaSetProfileMode(IntPtr net, UInt32 mode);

    /****************************************************************
    * 情報取得API
    **/

    /**
    * \~japanese
    * @brief ステータスコードを説明する文字列を返します
    * @return
    *   ステータスコードを説明する文字列
    * @details
    *   返値は解放する必要はありません。
    *   文字列の有効期間はailiaのライブラリをアンロードするまでです。
    *   取得したポイントから以下のように文字列に変換して下さい。
    *   @code
    *   Marshal.PtrToStringAnsi(Ailia.ailiaGetStatusString(status))
    *   @endcode
    */
    [DllImport(LIBRARY_NAME)]
    public static extern IntPtr ailiaGetStatusString(Int32 status);

    /**
    * \~japanese
    * @brief エラーの詳細を返します
    * @return
    *   エラー詳細
    * @details
    *   返値は解放する必要はありません。
    *   文字列の有効期間は次にailiaのAPIを呼ぶまでです。
    *   取得したポイントから以下のように文字列に変換して下さい。
    *   @code
    *   Marshal.PtrToStringAnsi(Ailia.ailiaGetErrorDetail(net))
    *   @endcode
    */
    [DllImport(LIBRARY_NAME)]
    public static extern IntPtr ailiaGetErrorDetail(IntPtr net);

    /**
    * \~japanese
    * @brief ライブラリバージョンを取得します
    * @return
    *   バージョン番号(Marshal.PtrToStringAnsiでstringに変換可能)
    * @details
    *   返値は解放する必要はありません。
    */
    [DllImport(LIBRARY_NAME)]
    public static extern IntPtr ailiaGetVersion();
}
