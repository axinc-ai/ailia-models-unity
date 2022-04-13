/* AILIA Unity Plugin Native Interface */
/* Copyright 2018-2021 AXELL CORPORATION */
/* Updated August 10, 2021 */

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

    public const Int32  AILIA_STATUS_SUCCESS                 =(   0);  /* 成功 */
    public const Int32  AILIA_STATUS_INVALID_ARGUMENT        =(  -1);  /* 引数が不正 */
    public const Int32  AILIA_STATUS_ERROR_FILE_API          =(  -2);  /* ファイルアクセスに失敗した */
    public const Int32  AILIA_STATUS_INVALID_VERSION         =(  -3);  /* ストリームバージョンか構造体バージョンが不正 */
    public const Int32  AILIA_STATUS_BROKEN                  =(  -4);  /* 壊れたファイルが渡された */
    public const Int32  AILIA_STATUS_MEMORY_INSUFFICIENT     =(  -5);  /* メモリが不足している */
    public const Int32  AILIA_STATUS_THREAD_ERROR            =(  -6);  /* スレッドの作成に失敗した */
    public const Int32  AILIA_STATUS_INVALID_STATE           =(  -7);  /* デコーダの内部状態が不正 */
    public const Int32  AILIA_STATUS_UNSUPPORT_NET           =(  -9);  /* 非対応のネットワーク */
    public const Int32  AILIA_STATUS_INVALID_LAYER           =( -10);  /* レイヤーの重み、入力形状などが不正 */
    public const Int32  AILIA_STATUS_INVALID_PARAMINFO       =( -11);  /* パラメーターファイルの内容が不正 */
    public const Int32  AILIA_STATUS_NOT_FOUND               =( -12);  /* 指定した要素が見つからなかった */
    public const Int32  AILIA_STATUS_GPU_UNSUPPORT_LAYER     =( -13);  /* GPUで未対応のレイヤーパラメーターが与えられた */
    public const Int32  AILIA_STATUS_GPU_ERROR               =( -14);  /* GPU上での処理中にエラー */
    public const Int32  AILIA_STATUS_UNIMPLEMENTED           =( -15);  /* 未実装 */
    public const Int32  AILIA_STATUS_PERMISSION_DENIED       =( -16);  /* 許可されていない操作 */
    public const Int32  AILIA_STATUS_EXPIRED                 =( -17);  /* モデルの有効期限切れ */
    public const Int32  AILIA_STATUS_UNSETTLED_SHAPE         =( -18);  /* 形状が未確定 */
    public const Int32  AILIA_STATUS_DATA_HIDDEN             =( -19);  /* アプリケーションからは取得できない情報だった */
    public const Int32  AILIA_STATUS_DATA_REMOVED            =AILIA_STATUS_DATA_HIDDEN;  /* 最適化などにより削除された */
    public const Int32  AILIA_STATUS_LICENSE_NOT_FOUND       =( -20);  /* 有効なライセンスが見つからない */
    public const Int32  AILIA_STATUS_LICENSE_BROKEN          =( -21);  /* ライセンスが壊れている */
    public const Int32  AILIA_STATUS_LICENSE_EXPIRED         =( -22);  /* ライセンスの有効期限切れ */
    public const Int32  AILIA_STATUS_NDIMENSION_SHAPE        =( -23);  /* 形状が5次元以上であることを示す */
    public const Int32  AILIA_STATUS_OTHER_ERROR             =(-128);  /* 不明なエラー */

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
    public ailiaCallbackOpen  fopen;     /* ユーザ定義fopen関数 */
    public ailiaCallbackSeek  fseek;     /* ユーザ定義fseek関数 */
    public ailiaCallbackTell  ftell;     /* ユーザ定義ftell関数 */
    public ailiaCallbackRead  fread;     /* ユーザ定義fread関数 */
    public ailiaCallbackSize  fsize;     /* ユーザ定義fsize関数 */
    public ailiaCallbackClose fclose;    /* ユーザ定義fclose関数 */
    }

    public const Int32 AILIA_FILE_CALLBACK_VERSION           = 1;

    /****************************************************************
    * 形状情報
    **/

    public const Int32  AILIA_SHAPE_VERSION = (1);

    [StructLayout(LayoutKind.Sequential)]
    public class AILIAShape {
        public UInt32 x;    // X軸のサイズ
        public UInt32 y;    // Y軸のサイズ
        public UInt32 z;    // Z軸のサイズ
        public UInt32 w;    // W軸のサイズ
        public UInt32 dim;  // 次元情報
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
    *  ネットワークオブジェクトを作成します。
    *    引数:
    *      net        - ネットワークオブジェクトポインタへのポインタ
    *      env_id     - 計算に利用する環境のID(ailiaGetEnvironmentで取得) AILIA_ENVIRONMENT_ID_AUTOにした場合は自動で選択する
    *      num_thread - スレッド数、AILIA_MULTITHREAD_AUTOで自動
    *    返値:
    *      成功した場合はAILIA_STATUS_SUCCESS、そうでなければエラーコードを返す。
    *   解説:
    *     ネットワークオブジェクトを作成します。
    *     推論実行環境を自動にした場合はCPUモードになり、BLASが利用できる場合はBLASを利用します。
    *     なお、BLASを利用する場合num_threadは無視される場合があります。
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaCreate(ref IntPtr net, int env_id, int num_thread);

    /**
    *  ネットワークオブジェクトを初期化します。(ファイルから読み込み)
    *    引数:
    *      net          - ネットワークオブジェクトポインタ
    *      path         - ailiaファイルのパス名(MBSC or UTF16)
    *    返値:
    *      成功した場合はAILIA_STATUS_SUCCESS、そうでなければエラーコードを返す。
    *   解説:
    *      ファイルから読み込みネットワークオブジェクトを初期化します。
    */

#if (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN)
    [DllImport(LIBRARY_NAME, EntryPoint = "ailiaOpenStreamFileW", CharSet=CharSet.Unicode)]
    public static extern int ailiaOpenStreamFile(IntPtr net, string path);
#else
    [DllImport(LIBRARY_NAME, EntryPoint = "ailiaOpenStreamFileA", CharSet=CharSet.Ansi)]
    public static extern int ailiaOpenStreamFile(IntPtr net, string path);
#endif

    /**
    *  ネットワークオブジェクトに重み係数を読み込みます
    *    引数:
    *      net          - ネットワークオブジェクトポインタ
    *      path         - protobuf/onnxファイルのパス名(MBSC or UTF16)
    *    返値:
    *      成功した場合はAILIA_STATUS_SUCCESS、そうでなければエラーコードを返す。
    *   解説:
    *      ファイルから読み込みネットワークオブジェクトに重みを読み込みます
    */

#if (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN)
    [DllImport(LIBRARY_NAME, EntryPoint = "ailiaOpenWeightFileW", CharSet=CharSet.Unicode)]
    public static extern int ailiaOpenWeightFile(IntPtr net, string path);
#else
    [DllImport(LIBRARY_NAME, EntryPoint = "ailiaOpenWeightFileA", CharSet=CharSet.Ansi)]
    public static extern int ailiaOpenWeightFile(IntPtr net, string path);
#endif

    /**
    *  ネットワークオブジェクトを初期化します。(ユーザ定義ファイルアクセスコールバック)
    *    引数:
    *      net              - ネットワークオブジェクトポインタ
    *      fopen_args       - AILIA_USER_API_FOPENに通知される引数ポインタ
    *      callback         - ユーザ定義ファイルアクセスコールバック構造体
    *      version          - ファイルアクセスコールバック構造体のバージョン(AILIA_FILE_CALLBACK_VERSION)
    *    返値:
    *      成功した場合はAILIA_STATUS_SUCCESS、そうでなければエラーコードを返す。
    *   解説:
    *      ファイルから読み込み、ネットワークオブジェクトを初期化します。
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaOpenStreamEx(IntPtr net, IntPtr fopen_args, ailiaFileCallback callback, Int32 version);

    /**
    *  ネットワークオブジェクトに重み係数を読み込みます。(ユーザ定義ファイルアクセスコールバック)
    *    引数:
    *      net          - ネットワークオブジェクトポインタ
    *      fopen_args   - AILIA_USER_API_FOPENに通知される引数ポインタ
    *      callback     - ユーザ定義ファイルアクセスコールバック構造体
    *      version      - ファイルアクセスコールバック構造体のバージョン(AILIA_FILE_CALLBACK_VERSION)
    *    返値:
    *      成功した場合はAILIA_STATUS_SUCCESS、そうでなければエラーコードを返す。
    *   解説:
    *      ファイルからネットワークオブジェクトに重みを読み込みます。
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaOpenWeightEx(IntPtr net, IntPtr fopen_args, ailiaFileCallback callback, Int32 version);

    /**
    *  ネットワークオブジェクトを初期化します。(メモリから読み込み)
    *    引数:
    *      net          - ネットワークオブジェクトポインタ
    *      buf          - prototxtファイルのデータへのポインター
    *      buf_size     - prototxtファイルのデータサイズ
    *    返値:
    *      成功した場合はAILIA_STATUS_SUCCESS、そうでなければエラーコードを返す。
    *   解説:
    *      メモリから読み込み、ネットワークオブジェクトを初期化します。
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaOpenStreamMem(IntPtr net, byte[] buf, UInt32 buf_size);

    /**
    *  ネットワークオブジェクトに重み係数を読み込みます。(メモリから読み込み)
    *    引数:
    *      net          - ネットワークオブジェクトポインタ
    *      buf          - protobuf/onnxフファイルのデータへのポインター
    *      buf_size     - protobuf/onnxフファイルのデータサイズ
    *    返値:
    *      成功した場合はAILIA_STATUS_SUCCESS、そうでなければエラーコードを返す。
    *   解説:
    *      ファイルからネットワークオブジェクトに重みを読み込みます。
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaOpenWeightMem(IntPtr net, byte[] buf, UInt32 buf_size);

    /**
    *  ネットワークオブジェクトを破棄します。
    *    引数:
    *      net - ネットワークオブジェクトポインタ
    */
    [DllImport(LIBRARY_NAME)]
    public static extern void ailiaDestroy(IntPtr net);

    /**
    *  推論時の入力データの形状を変更します。
    *    引数:
    *      net      - ネットワークオブジェクトポインタ
    *      shape    - 入力データの形状情報
    *      version  - AILIA_SHAPE_VERSION
    *    返値:
    *      成功した場合はAILIA_STATUS_SUCCESS、そうでなければエラーコードを返す。
    *   解説:
    *      prototxtで定義されている入力形状を変更します。
    *      prototxtに記述されているランクと同じにする必要があります。
    *      なお、重み係数の形状が入力形状に依存しているなどによりエラーが返る場合があります。
    *      prototxtで定義されているランクが5次元以上の場合はailiaSetInputShapeNDを利用してください。
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaSetInputShape(IntPtr net, [In] AILIAShape shape, UInt32 version);

    /**
    *  推論時の入力データの形状を変更します。
    *    引数:
    *      net       - ネットワークオブジェクトポインタ
    *      shape     - 入力データの各次元の大きさの配列(dim-1, dim-2, ... ,1, 0)
    *      dim       - shapeの次元
    *    返値:
    *      成功した場合はAILIA_STATUS_SUCCESS、そうでなければエラーコードを返す。
    *   解説:
    *      prototxtで定義されている入力形状を変更します。
    *      prototxtに記述されているランクと同じにする必要があります。
    *      なお、重み係数の形状が入力形状に依存しているなどによりエラーが返る場合があります。
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaSetInputShapeND(IntPtr net, UInt32 [] shape, UInt32 dim);

    /**
    *  推論時の入力データの形状を取得します。
    *    引数:
    *      net      - ネットワークオブジェクトポインタ
    *      shape    - 入力データの形状情報
    *    返値:
    *      成功した場合はAILIA_STATUS_SUCCESS、5次元以上の場合AILIA_STATUS_NDIMENSION_SHAPE、
    *      形状の一部が未確定の場合AILIA_STATUS_UNSETTLED_SHAPE、それ以外のエラーの場合はエラーコードを返す。
    *
    *      形状が5次元以上の場合はailiaGetInputDim、ailiaGetInputShapeNDを利用してください。
    *      形状の一部が未確定の場合、該当する次元の値は0となり、それ以外の次元の値は有効な値が格納されます。
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaGetInputShape(IntPtr net, [In,Out] AILIAShape shape, UInt32 version);

    /**
    *  推論時の入力データの次元を取得します。
    *    引数:
    *      net       - ネットワークオブジェクトポインタ
    *      dim       - 入力データの次元の格納先
    *    返値:
    *      成功した場合はAILIA_STATUS_SUCCESS、それ以外のエラーの場合はエラーコードを返す。
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaGetInputDim(IntPtr net, ref UInt32 dim);

    /**
    *  推論時の入力データの形状を取得します。
    *    引数:
    *      net       - ネットワークオブジェクトポインタ
    *      shape     - 入力データの各次元の大きさの格納先配列(dim-1, dim-2, ... ,1, 0順で格納)
    *      dim       - shapeの次元
    *    返値:
    *      成功した場合はAILIA_STATUS_SUCCESS、形状の一部が未確定の場合AILIA_STATUS_UNSETTLED_SHAPE、
    *      それ以外のエラーの場合はエラーコードを返す。
    *
    *      形状の一部が未確定の場合、該当する次元の値は0となり、それ以外の次元の値は有効な値が格納されます。
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaGetInputShapeND(IntPtr net, UInt32[] shape, UInt32 dim);

    /**
    *  推論・学習時の出力データの形状を取得します。
    *    引数:
    *      net      - ネットワークオブジェクトポインタ
    *      shape    - 出力データの形状情報
    *    返値:
    *      成功した場合はAILIA_STATUS_SUCCESS、5次元以上の場合AILIA_STATUS_NDIMENSION_SHAPE、
    *      それ以外のエラーの場合エラーコードを返す。
    *      形状が5次元以上の場合はailiaGetOutputDim、ailiaGetOutputShapeNDを利用してください。
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaGetOutputShape(IntPtr net, [In,Out] AILIAShape shape, UInt32 version);

    /**
    *  推論時の出力データの次元を取得します。
    *    引数:
    *      net       - ネットワークオブジェクトポインタ
    *      dim       - 出力データの次元の格納先
    *    返値:
    *      成功した場合はAILIA_STATUS_SUCCESS、それ以外のエラーの場合はエラーコードを返す。
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaGetOutputDim(IntPtr net, ref UInt32 dim);

    /**
    *  推論時の出力データの形状を取得します。
    *    引数:
    *      net       - ネットワークオブジェクトポインタ
    *      shape     - 出力データの各次元の大きさの格納先配列(dim-1, dim-2, ... ,1, 0順で格納)
    *      dim       - shapeの次元
    *    返値:
    *      成功した場合はAILIA_STATUS_SUCCESS、それ以外のエラーの場合はエラーコードを返す。
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaGetOutputShapeND(IntPtr net, UInt32 [] shape, UInt32 dim);

    /**
    *  推論を行い推論結果を取得します。
    *    引数:
    *      net                         - ネットワークオブジェクトポインタ
    *      dest                        - 推論結果書き出し先バッファ X,Y,Z,Wの順でnumeric型で格納 サイズはネットファイルのoutputSizeとなる
    *      dest_size                   - 推論結果書き出し先バッファのbyte数
    *      src                         - 推論データ X,Y,Z,Wの順でnumeric型で格納 サイズはネットファイルのinputSizeとなる
    *      src_size                    - 推論データのbyte数
    *    返値:
    *      成功した場合はAILIA_STATUS_SUCCESS、そうでなければエラーコードを返す。
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaPredict(IntPtr net, IntPtr dest, UInt32 dest_size, IntPtr src, UInt32 src_size);

    /****************************************************************
    * 状態取得API
    **/

    /**
    *  推論時の内部データ(Blob)の数を取得します。
    *    引数:
    *      net          - ネットワークオブジェクトポインタ
    *      blob_count   - blobの数の格納先
    *    返値:
    *      成功した場合はAILIA_STATUS_SUCCESS、そうでなければエラーコードを返す。
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaGetBlobCount(IntPtr net, ref UInt32 blob_count);

    /**
    *  推論時の内部データ(Blob)の形状を取得します。
    *    引数:
    *      net          - ネットワークオブジェクトポインタ
    *      shape        - 入力データの形状情報
    *      blob_idx     - blobのインデックス (0～ailiaGetBlobCount()-1)
    *      version      - AILIA_SHAPE_VERSION
    *    返値:
    *      成功した場合はAILIA_STATUS_SUCCESS、そうでなければエラーコードを返す。
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaGetBlobShape(IntPtr net, [In,Out] AILIAShape shape, UInt32 blob_idx, UInt32 version);

    /**
    *  推論時の内部データ(Blob)の次元を取得します。
    *    引数:
    *      net        - ネットワークオブジェクトポインタ
    *      dim        - blobの次元の格納先
    *      blob_idx   - blobのインデックス (0～ailiaGetBlobCount()-1)
    *    返値:
    *      成功した場合はAILIA_STATUS_SUCCESS、それ以外のエラーの場合はエラーコードを返す。
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaGetBlobDim(IntPtr net, ref UInt32 dim, UInt32 blob_idx);

    /**
    *  推論時の内部データ(Blob)の形状を取得します。
    *    引数:
    *      net        - ネットワークオブジェクトポインタ
    *      shape      - blobの各次元の大きさの格納先配列(dim-1, dim-2, ... ,1, 0順で格納)
    *      dim        - shapeの次元
    *      blob_idx   - blobのインデックス (0～ailiaGetBlobCount()-1)
    *    返値:
    *      成功した場合はAILIA_STATUS_SUCCESS、それ以外のエラーの場合はエラーコードを返す。
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaGetBlobShapeND(IntPtr net, UInt32[] shape, UInt32 dim, UInt32 blob_idx);

    /**
    *  推論時の内部データ(Blob)を取得します。
    *    引数:
    *      net          - ネットワークオブジェクトポインタ
    *      dest         - 推論結果書き出し先バッファ X,Y,Z,Wの順でnumeric型で格納
    *      dest_size    - 推論結果書き出し先バッファのbyte数
    *      blob_idx     - blobのインデックス (0～ailiaGetBlobCount()-1)
    *    返値:
    *      成功した場合はAILIA_STATUS_SUCCESS、そうでなければエラーコードを返す。
    *    解説:
    *      ailiaPredictを一度も実行していない場合はAILIA_STATUS_INVALID_STATEが返ります。
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaGetBlobData(IntPtr net, IntPtr dest, UInt32 dest_size, UInt32 blob_idx);

    /**
    *  推論時の内部データ(Blob)のインデックスを名前で探し取得します。
    *    引数:
    *      net          - ネットワークオブジェクトポインタ
    *      blob_idx     - blobのインデックス (0~ailiaGetBlobCount()-1)
    *      name         - 検索するBlob名
    *    返値:
    *      成功した場合はAILIA_STATUS_SUCCESS、そうでなければエラーコードを返す。
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaFindBlobIndexByName(IntPtr net, ref UInt32 blob_idx, string name);

    /**
    *  内部データ(Blob)の名前ための必要なバッファーサイズを出力します。
    *    引数:
    *      net              - ネットワークオブジェクトポインタ
    *      blob_idx         - blobのインデックス (0~ailiaGetBlobCount()-1)
    *      buffer_size      - 出力：Blob名を出力するのに必要なバッファーサイズ(終端null文字分を含む)
    *    返値:
    *      成功した場合はAILIA_STATUS_SUCCESS、そうでなければエラーコードを返す。
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaGetBlobNameLengthByIndex(IntPtr net, UInt32 blob_idx, ref UInt32 buffer_size);

    /**
    *  推論時の内部データ(Blob)の名前をインデックスで探し取得します。
    *    引数:
    *      net          - ネットワークオブジェクトポインタ
    *      buffer       - Blob名の出力先バッファ
    *      buffer_size  - バッファのサイズ(終端null文字分を含む)
    *      blob_idx     - 検索するblobのインデックス
    *    返値:
    *      成功した場合はAILIA_STATUS_SUCCESS、そうでなければエラーコードを返す。
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaFindBlobNameByIndex(IntPtr net, IntPtr buffer, UInt32 buffer_size, UInt32 blob_idx);

    /**
    *  ネットワークSummary用に必要なバッファーサイズを出力します。
    *    引数:
    *      net          - ネットワークオブジェクトポインタ
    *      buffer_size  - バッファーのサイズの格納先(終端null文字分を含む)
    *    返値:
    *      成功した場合はAILIA_STATUS_SUCCESS、そうでなければエラーコードを返す。
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaGetSummaryLength(IntPtr net, ref UInt32 buffer_size);

    /**
    *  各Blobの名前とシェイプを表示します。
    *    引数:
    *      net          - ネットワークオブジェクトポインタ
    *      buffer       - サマリーの出力先
    *      buffer_size  - 出力バッファーのサイズ(終端null文字分を含む)
    *                     ailiaGetSummaryLengthで取得した値を設定してください。
    *    返値:
    *      成功した場合はAILIA_STATUS_SUCCESS、そうでなければエラーコードを返す。
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaSummary(IntPtr  net, IntPtr buffer, UInt32 buffer_size);

    /****************************************************************
    * 複数入力指定・推論API
    **/

    /**
    *  入力データ(Blob)の数を取得します。
    *    引数:
    *      net                - ネットワークオブジェクトポインタ
    *      input_blob_count   - 入力blobの数の格納先
    *    返値:
    *      成功した場合はAILIA_STATUS_SUCCESS、そうでなければエラーコードを返す。
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaGetInputBlobCount(IntPtr net, ref UInt32 input_blob_count);

    /**
    *  入力データ(Blob)のインデックスを取得します
    *    引数:
    *      net              - ネットワークオブジェクトポインタ
    *      blob_idx         - blobのインデックス(0～ailiaGetBlobCount()-1)
    *      input_blob_idx   - 入力blob内でのインデックス(0～ailiaGetInputBlobCount()-1)
    *    返値:
    *      成功した場合はAILIA_STATUS_SUCCESS、そうでなければエラーコードを返す。
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaGetBlobIndexByInputIndex(IntPtr net, ref UInt32 blob_idx, UInt32 input_blob_idx);

    /**
    *  指定したBlobに入力データを与えます。
    *    引数:
    *      net          - ネットワークオブジェクトポインタ
    *      src          - 推論データ X,Y,Z,Wの順でnumeric型で格納 サイズはネットファイルのinputSizeとなる
    *      src_size     - 推論データサイズ
    *      blob_idx     - 入力するblobのインデックス
    *    返値:
    *      成功した場合はAILIA_STATUS_SUCCESS、そうでなければエラーコードを返す。
    *    解説:
    *      複数入力があるネットワークなどで入力を指定する場合に用います。
    *      blob_idxは入力レイヤーのblob以外のものを指定した場合AILIA_STATUS_INVALID_ARGUMENTが返ります。
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaSetInputBlobData(IntPtr net, IntPtr src, UInt32 src_size, UInt32 blob_idx);

    /**
    *  指定したBlobの形状を変更します。
    *    引数:
    *      net      - ネットワークオブジェクトポインタ
    *      shape    - 入力データの形状情報
    *      blob_idx - 変更するblobのインデックス
    *      version  - AILIA_SHAPE_VERSION
    *    返値:
    *      成功した場合はAILIA_STATUS_SUCCESS、そうでなければエラーコードを返す。
    *    解説:
    *      複数入力があるネットワークなどで入力形状を変更する場合に用います。
    *      blob_idxは入力レイヤーのblob以外のものを指定した場合AILIA_STATUS_INVALID_ARGUMENTが返ります。
    *      その他の注意点はailiaSetInputShapeの解説を参照してください。
    *      入力形状のランクが5次元以上の場合はailiaSetInputBlobShapeNDを利用してください。
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaSetInputBlobShape(IntPtr net, [In] AILIAShape shape, UInt32 blob_idx,UInt32 version);

    /**
    *  指定したBlobの形状を変更します。
    *    引数:
    *      net      - ネットワークオブジェクトポインタ
    *      shape    - 入力データの各次元の大きさの配列(dim-1, dim-2, ... ,1, 0)
    *      dim      - shapeの次元
    *      blob_idx - 変更するblobのインデックス
    *    返値:
    *      成功した場合はAILIA_STATUS_SUCCESS、そうでなければエラーコードを返す。
    *    解説:
    *      複数入力があるネットワークなどで入力形状を変更する場合に用います。
    *      blob_idxは入力レイヤーのblob以外のものを指定した場合AILIA_STATUS_INVALID_ARGUMENTが返ります。
    *      その他の注意点はailiaSetInputShapeNDの解説を参照してください。
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaSetInputBlobShapeND(IntPtr net, UInt32[] shape, UInt32 dim, UInt32 blob_idx);

    /**
    *  事前に指定した入力データで推論を行います。
    *    引数:
    *      net  - ネットワークオブジェクトポインタ
    *    返値:
    *      成功した場合はAILIA_STATUS_SUCCESS、そうでなければエラーコードを返す。
    *    解説:
    *      ailiaSetInputBlobDataを用いて入力を与えた場合などに用います。
    *      推論結果はailiaGetBlobDataで取得してください。
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaUpdate(IntPtr net);

    /**
    *  出力データ(Blob)の数を取得します。
    *    引数:
    *      net                 - ネットワークオブジェクトポインタ
    *      output_blob_count   - 出力blobの数の格納先
    *    返値:
    *      成功した場合はAILIA_STATUS_SUCCESS、そうでなければエラーコードを返す。
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaGetOutputBlobCount(IntPtr net, ref UInt32 output_blob_count);

    /**
    *  出力データ(Blob)のインデックスを取得します
    *    引数:
    *      net               - ネットワークオブジェクトポインタ
    *      blob_idx          - blobのインデックス(0～ailiaGetBlobCount()-1)
    *      output_blob_idx   - 出力blob内でのインデックス(0～ailiaGetOutputBlobCount()-1)
    *    返値:
    *      成功した場合はAILIA_STATUS_SUCCESS、そうでなければエラーコードを返す。
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
    public const int AILIA_ENVIRONMENT_PROPERTY_LOWPOWER     = (1); // 省電力なGPU(内蔵GPUなど)を用いることを示す(MPS用)
    public const int AILIA_ENVIRONMENT_PROPERTY_FP16         = (2); // FP16で動作することを示す

    [StructLayout(LayoutKind.Sequential)]
    public class AILIAEnvironment {
        public Int32 id;        // 環境を識別するID
        public Int32 type;      // 環境の種別(AILIA_ENVIRONMENT_TYPE_CPU or BLAS or GPU)
        public IntPtr name;     // デバイス名(シングルトンで保持されており開放不要)(Marshal.PtrToStringAnsiでstringに変換可能)
        public Int32 backend;   // 環境のバックエンド (AILIA_ENVIRONMENT_BACKEND_*) (type==AILIA_ENVIRONMENT_TYPE_GPUの場合有効)
        public Int32 props;     // 環境の特性などを示す(AILIA_ENVIRONMENT_PROPERTY_*)
    }

    /**
    *  一時キャッシュディレクトリを指定します
    *    引数:
    *      cache_dir    - 一時キャッシュディレクトリ
    *    返値:
    *      成功した場合はAILIA_STATUS_SUCCESS、そうでなければエラーコードを返す。
    *    解説:
    *      指定したキャッシュディレクトリは推論実行環境毎に最適化したマシンコードを生成して保存するためにシステムが利用します。
    *      ailia の実行開始時に一度だけ呼び出してください。二回目以降の呼び出しに対しては無視して成功を返します。
    *      複数スレッドから呼び出された場合も内部で排他制御しているので特に問題は発生しません。
    *      Vulkan の shader cache 機能など、この API を呼ぶまで利用できないものがあります。
    *      cache_dirにはContext.getCacheDir()で取得したファイルパスを指定してください。
    */
#if (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN)
    [DllImport(LIBRARY_NAME, EntryPoint = "ailiaSetTemporaryCachePathW", CharSet=CharSet.Unicode)]
    public static extern int ailiaSetTemporaryCachePath(string path);
#else
    [DllImport(LIBRARY_NAME, EntryPoint = "ailiaSetTemporaryCachePathA", CharSet=CharSet.Ansi)]
    public static extern int ailiaSetTemporaryCachePath(string path);
#endif

    /**
    *  利用可能な計算環境(CPU, GPU)の数を取得します
    *    引数:
    *      env_count     - 環境情報の数の格納先
    *    返値:
    *      利用可能な環境の件数
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaGetEnvironmentCount(ref Int32 env_count);

    /**
    *  計算環境の一覧を取得します
    *    引数:
    *      env          - 環境情報の格納先(AILIANetworkインスタンスを破棄するまで有効)
    *      env_idx      - 環境情報のインデックス(0~ailiaGetEnvironmentCount()-1)
    *      version      - AILIA_ENVIRONMENT_VERSION
    *    返値:
    *      成功した場合はAILIA_STATUS_SUCCESS、そうでなければエラーコードを返す。
    *    解説:
    *      取得したポインタから以下のように構造体に変換して下さい。
    *        System.IntPtr env_ptr=System.IntPtr.Zero;
    *        Ailia.ailiaGetEnvironment(ref env_ptr,(uint)environment,Ailia.AILIA_ENVIRONMENT_VERSION);
    *        Ailia.AILIAEnvironment env=(Ailia.AILIAEnvironment)Marshal.PtrToStructure(env_ptr,typeof(Ailia.AILIAEnvironment));
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaGetEnvironment(ref IntPtr env, UInt32 env_idx, UInt32 version);

    /**
    *  選択された計算環境を取得します
    *    引数:
    *      net          - ネットワークオブジェクトポインタ
    *      env          - 計算環境情報の格納先(AILIANetworkインスタンスを破棄するまで有効)
    *      version      - AILIA_ENVIRONMENT_VERSION
    *    返値:
    *      成功した場合はAILIA_STATUS_SUCCESS、そうでなければエラーコードを返す。
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaGetSelectedEnvironment(IntPtr net, ref IntPtr env, UInt32 version);

    /****************************************************************
    * メモリモードAPI
    **/

    public const Int32 AILIA_MEMORY_NO_OPTIMIZATION                        = (0); // 中間バッファーの開放は行わない
    public const Int32 AILIA_MEMORY_REDUCE_CONSTANT                        = (1); // 重みなどの定数となる中間バッファーを開放する
    public const Int32 AILIA_MEMORY_REDUCE_CONSTANT_WITH_INPUT_INITIALIZER = (2); // 入力指定のinitializerを変更不可にし、重みなどの定数となる中間バッファーを開放する
    public const Int32 AILIA_MEMORY_REDUCE_INTERSTAGE                      = (4); // 推論時の中間バッファーを開放する
    public const Int32 AILIA_MEMORY_REUSE_INTERSTAGE                       = (8); // 中間バッファーを共有して推論する。AILIA_MEMORY_REDUCE_INTERSTAGEと併用した場合、共有可能な中間バッファーは開放しない。

    public const Int32 AILIA_MEMORY_OPTIMAIZE_DEFAULT = (AILIA_MEMORY_REDUCE_CONSTANT);

    /**
    *  推論時のメモリの使用方針を設定します
    *    引数:
    *      net        - ネットワークオブジェクトポインタ
    *      mode       - メモリモード(論理和で複数指定可) AILIA_MEMORY_XXX (デフォルト:AILIA_MEMORY_REDUCE_CONSTANT)
    *    返値:
    *      成功した場合はAILIA_STATUS_SUCCESS、そうでなければエラーコードを返す。
    *    解説:
    *      メモリの使用方針を変更します。AILIA_MEMORY_NO_OPTIMIZATION以外を指定した場合は、
    *      推論時に確保する中間バッファーを開放するため、推論時のメモリ使用量を削減することができます。
    *      ailiaCreateの直後に指定する必要があります。ailiaOpenを呼び出した後は変更することができません。
    *      なお、中間バッファーを開放するように指定した場合、該当するBlobに対し、ailiaGetBlobDataを呼び出すと
    *      AILIA_STATUS_DATA_HIDDENエラーが返ります。
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaSetMemoryMode(IntPtr net, UInt32 mode);

    /****************************************************************
    * 情報取得API
    **/

    /**
    * エラーの詳細を返します
    * 返値:
    *   エラー詳細
    * 解説:
    *   返値は解放する必要はありません。
    *   文字列の有効期間は次にailiaのAPIを呼ぶまでです。
    *   取得したポイントから以下のように文字列に変換して下さい。
    *   Marshal.PtrToStringAnsi(Ailia.ailiaGetErrorDetail(net))
    */
    [DllImport(LIBRARY_NAME)]
    public static extern IntPtr ailiaGetErrorDetail(IntPtr net);

    /**
    * ライブラリバージョンを取得します
    * 返値:
    *   バージョン番号(Marshal.PtrToStringAnsiでstringに変換可能)
    * 解説:
    *   返値は解放する必要はありません。
    */
    [DllImport(LIBRARY_NAME)]
    public static extern IntPtr ailiaGetVersion();

}
