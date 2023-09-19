/* ailia Audio Unity Plugin Native Interface */
/* Copyright 2021 AXELL CORPORATION */

using System;
using System.Runtime.InteropServices;

public class AiliaAudio
{

    /****************************************************************
    * パラメータ定義
    **/

    public const Int32 AILIA_AUDIO_WIN_TYPE_HANN = (1);    /* 窓関数に hann 窓を使う */
    public const Int32 AILIA_AUDIO_WIN_TYPE_HAMMING = (2);    /* 窓関数に hamming 窓を使う */

    public const Int32 AILIA_AUDIO_STFT_CENTER_NONE = (0);    /* STFT の際、前後に padding を入れない */
    public const Int32 AILIA_AUDIO_STFT_CENTER_ENABLE = (1);    /* STFT の際、sample_n の前後に fft_n/2 の padding を入れる */
    public const Int32 AILIA_AUDIO_STFT_CENTER_SCIPY_DEFAULT = (2); /*STFT の際、sample_n の前後に fft_n/2 の padding(zero)を入れ、さらにhop_n処理単位になるように後方padding(zero)で合わせる*/

    public const Int32 AILIA_AUDIO_FFT_NORMALIZE_NONE = (0);    /* FFT の出力を正規化しない */
    public const Int32 AILIA_AUDIO_FFT_NORMALIZE_LIBROSA_COMPAT = (1);    /* FFT の出力を librosa 互換で正規化する */
    public const Int32 AILIA_AUDIO_FFT_NORMALIZE_PYTORCH_COMPAT = (1);    /* FFT の出力を PyTorch 互換で正規化する */
    public const Int32 AILIA_AUDIO_FFT_NORMALIZE_SCIPY_COMPAT = (2);    /* FFT の出力を SciPy 互換で正規化する */

    public const Int32 AILIA_AUDIO_MEL_NORMALIZE_NONE = (0);    /* MEL スペクトログラムの出力を正規化しない */
    public const Int32 AILIA_AUDIO_MEL_NORMALIZE_ENABLE = (1);    /* MEL スペクトログラムの出力を正規化する */

    public const Int32 AILIA_AUDIO_MEL_SCALE_FORMULA_HTK = (1);    /* MEL 尺度を HTK formula で求める (PyTorch 互換) */
    public const Int32 AILIA_AUDIO_MEL_SCALE_FORMULA_SLANYE = (0);    /* MEL 尺度を Slanye's formula で求める (librosa デフォルト互換) */

    public const Int32 AILIA_AUDIO_PHASE_FORM_COMPLEX = (1);    /* 位相を複素数形式で出力する (librosa デフォルト互換) */
    public const Int32 AILIA_AUDIO_PHASE_FORM_REAL = (0);    /* 位相を実数形式で出力する (PyTorch デフォルト互換) */

    public const Int32 AILIA_AUDIO_FILTFILT_PAD_NONE = (0);    /*ゼロ位相フィルタ処理の際、padding をしない*/
    public const Int32 AILIA_AUDIO_FILTFILT_PAD_ODD = (1);    /*ゼロ位相フィルタ処理の際、padding はodd*/
    public const Int32 AILIA_AUDIO_FILTFILT_PAD_EVEN = (2);    /*ゼロ位相フィルタ処理の際、padding はeven(reflect)*/
    public const Int32 AILIA_AUDIO_FILTFILT_PAD_CONSTANT = (3);    /*ゼロ位相フィルタ処理の際、padding は端値*/

    /* Native Binary 定義 */

#if (UNITY_IPHONE && !UNITY_EDITOR) || (UNITY_WEBGL && !UNITY_EDITOR)
        public const String LIBRARY_NAME="__Internal";
#else
#if (UNITY_ANDROID && !UNITY_EDITOR) || (UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX)
            public const String LIBRARY_NAME="ailia_audio";
#else
    public const String LIBRARY_NAME = "ailia_audio";
#endif
#endif

    /*************************************************************
    * 入力値を対数スケールに変換します。
    *   引数:
    *     dst - 出力データポインタ、float 型、長さ src_n
    *     src - 入力データポインタ、float 型、長さ src_n
    *     src_n - 計算対象の要素数
    *   返値:
    *     成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    *   解説:
    *     dst = log_e(1.0 + src) を計算します。
    **/
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaAudioLog1p(float[] dst, float[] src, int src_n);

    /*************************************************************
    * 非負の入力値をデシベルスケールに変換します。
    *   引数:
    *     dst - 出力データポインタ、float 型、長さ src_n
    *     src - 入力データポインタ、float 型、要素数 src_n
    *     src_n - 計算対象の要素数
    *     top_db - 出力の最大値から出力下限の閾値までを定める値 (>= 0.0)、負数の場合は処理は閾値を設定しない
    *   返値:
    *     成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    *   解説:
    *     librosa.power_to_dbと互換性があります。
    **/
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaAudioConvertPowerToDB(float[] dst, float[] src, int src_n, float top_db);


    /*************************************************************
    * STFTで生成される時間フレーム長を取得します。
    *   引数:
    *     frame_n - フレーム長出力先ポインタ
    *     sample_n - STFTを適用するサンプル数
    *     fft_n - FFT点数
    *     hop_n - 窓のシフト数
    *     center - AILIA_AUDIO_STFT_CENTER_* のいずれか
    *   返値:
    *     成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    *   備考:
    *     STFT実行前のバッファサイズの決定に使用します。
    *     AILIA_AUDIO_STFT_CENTER_NONE の場合 hop_n ずつ区切り、sample_n の前後に padding を行いません。
    *     \ref AILIA_AUDIO_STFT_CENTER_ENABLE  の場合 sample_n の前後に fft_n/2 の padding(reflect) を行います。
    *     \ref AILIA_AUDIO_STFT_CENTER_SCIPY_DEFAULT の場合、sample_n の前後に fft_n/2 の padding(zero)を行い、hop_nの倍数になるようにpadding(zero)を行います。
    **/
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaAudioGetFrameLen(ref Int32 frame_n, int sample_n, int fft_n, int hop_n, int center);


    /************************************************************* 
    * ISTFTで生成されるサンプル数を取得します。
    *   引数:
    *     sample_n - サンプル数出力先ポインタ
    *     frame_n - STFTデータの時間フレーム長
    *     fft_n - FFT点数
    *     hop_n - 窓のシフト数
    *     center - AILIA_AUDIO_STFT_CENTER_* のいずれか
    *     返値:
    *       成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    *     備考:
    *       ISTFT実行前のバッファサイズの決定に使用します。
    *       \ref AILIA_AUDIO_STFT_CENTER_NONE  の場合 前後の切り捨てを行いません。
    *       \ref AILIA_AUDIO_STFT_CENTER_NONE  以外の場合 前後の切り捨てを行います。
    **/
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaAudioGetSampleLen(ref Int32 sample_n, int frame_n, int freq_n, int hop_n, int center);

    /*************************************************************
    * 窓関数の係数を取得します。
    *   引数:
    *     dst - 出力データのポインタ、float 型、要素数 window_n
    *     window_n - 窓の長さ（サンプル数）
    *     win_type - 窓関数の種類、AILIA_AUDIO_WIN_TYPE_* のいずれか
    *   返値:
    *     成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    *   備考:
    *     窓関数はhann窓とhamming窓のみ対応しています。
    **/
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaAudioGetWindow(float[] dst, int window_n, int win_type);


    /*************************************************************
    * FFTを実行します。
    *   引数:
    *     dst - 出力データのポインタ、float 型、外側から fft_n, 2(実部、虚部) 順のメモリレイアウト
    *     src - 入力データのポインタ、float 型、要素数 fft_n
    *     fft_n - FFT点数
    *   返値:
    *     成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    *   備考:
    *     FFT点数が2の累乗の場合、高速アルゴリズムで動作します。 
    *     出力データは実部と虚部の交互信号であり、長さは fft_n*2 です。
    **/
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaAudioFFT(float[] dst, float[] src, int fft_n);


    /************************************************************* 
    * IFFTを実行します。
    *   引数:
    *     dst - 出力データのポインタ、float 型、外側から fft_n, 2(実部、虚部) 順のメモリレイアウト
    *     src - 入力データのポインタ、float 型、外側から fft_n, 2(実部、虚部) 順のメモリレイアウト
    *     fft_n - FFT点数
    *   返値:
    *     成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    *   備考:
    *     FFT点数が2の累乗の場合、高速アルゴリズムで動作します。
    *     出力データは実部と虚部の交互信号であり、長さは fft_n*2 です。
    **/
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaAudioIFFT(float[] dst, float[] src, int fft_n);


    /*************************************************************
    * 音響信号からスペクトログラムを生成します。
    *   引数:
    *     dst - 出力データのポインタ、float 型、外側から freq_n, frame_n, 2(複素数: 実部, 虚部) 順のメモリレイアウト
    *     src - 入力データのポインタ、float 型、要素数 sample_n
    *     sample_n - 入力データのサンプル数
    *     fft_n - FFT点数（2の累乗）
    *     hop_n - フレームのシフト数
    *     win_n - 窓関数の長さ
    *     win_type - 窓関数の種類、AILIA_AUDIO_WIN_TYPE_* のいずれか
    *     max_frame_n - 出力データの時間フレーム数の最大値
    *     center -  入力データの前後へのパディングの有無、AILIA_AUDIO_STFT_CENTER_* のいずれか
    *     power - スペクトログラムの乗数（>= 0.0） 0.0: 複素スペクトログラム、1.0: 振幅スペクトログラム、2.0: パワースペクトログラム、その他: 任意の累乗値の出力に相当
    *     norm_type - FFT後の正規化処理、AILIA_AUDIO_FFT_NORMALIZE_* のいずれか
    *   返値:
    *     成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    *   解説:
    *     時間フレームごとにFFT→正規化処理→累乗（振幅・パワーに変換）の順で処理を実行します。
    *   備考:
    *     時間フレームごとにFFT→正規化処理→累乗（振幅・パワーに変換）の順で処理を実行します。
    *     出力データは実部と虚部の交互信号であり、長さは(fft_n/2+1)*時間フレーム長*2です。
    *     powerが0.0以外の場合は虚部の値を全て0.0として出力します。
    **/
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaAudioGetSpectrogram(float[] dst, float[] src, int sample_n, int fft_n, int hop_n, int win_n, int win_type, int max_frame_n, int center, float power, int norm_type);


    /*************************************************************　
    * 複素スペクトログラムから音響信号を生成します。
    *   引数:
    *     dst - 出力データのポインタ、float 型、要素数 sample_n
    *     src - 入力データのポインタ、float 型、外側から freq_n, frame_n, 2(複素数: 実部, 虚部) 順のメモリレイアウト
    *     frame_n - 入力データの時間フレーム数
    *     freq_n - 周波数（fft_n/2+1）
    *     hop_n - フレームのシフト数
    *     win_n - 窓関数の長さ
    *     win_type: 窓関数の種類、AILIA_AUDIO_WIN_TYPE_* のいずれか
    *     max_sample_n - 出力データのサンプル数の最大値
    *     center -  入力データ生成時の前後へのパディングの有無、AILIA_AUDIO_STFT_CENTER_* のいずれか
    *     norm_type - 入力データ生成時の正規化処理、AILIA_AUDIO_FFT_NORMALIZE_* のいずれか
    *   返値:
    *     成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    *   備考:
    *   時間フレームごとにIFFTを行い、最後に正規化処理を実行します。
    *   複素スペクトログラムのみに対応しています。
    **/
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaAudioGetInverseSpectrogram(float[] dst, float[] src, int frame_n, int freq_n, int hop_n, int win_n, int win_type, int max_sample_n, int center, int norm_type);


    /*************************************************************
    * メルフィルタバンクの係数を計算します。
    *   引数:
    *     dst - 出力データのポインタ、float 型、外側から mel_n, freq_n  順のメモリレイアウト
    *     freq_n - 周波数のインデックス数
    *     f_min - 周波数の最小値
    *     f_max - 周波数の最大値
    *     mel_n - メル周波数のインデックス数（ < freq_n）
    *     sample_rate - サンプリング周波数
    *     mel_norm - 出力される係数の正規化の有無、AILIA_AUDIO_MEL_NORMALIZE_* のいずれか
    *     mel_formula - MEL尺度の形式、AILIA_AUDIO_MEL_SCALE_FORMULA_* のいずれか
    *   返値:
    *     成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    *   備考：
    *   　削除しました。
    **/
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaAudioGetFBMatrix(float[] dst, int freq_n, float f_min, float f_max, int mel_n, int sample_rate, int mel_norm, int mel_formula);


    /*************************************************************
    * 音響信号からメルスペクトログラムを生成します。
    *   引数:
    *     dst - 出力データのポインタ、float 型、外側から mel_n, frame_n 順のメモリレイアウト
    *     src - 入力データのポインタ、float 型、モノラル PCM データ
    *     sample_n - 入力データのサンプル数
    *     sample_rate - サンプリング周波数
    *     fft_n - FFT点数
    *     hop_n - フレームのシフト数
    *     win_n - 1フレームに含むサンプル数
    *     win_type - 窓関数の種類、AILIA_AUDIO_WIN_TYPE_* のいずれか
    *     max_frame_n - 出力データの時間フレーム数の最大値
    *     center -  入力データの前後へのパディングの有無、AILIA_AUDIO_STFT_CENTER_* のいずれか
    *     power - スペクトログラムの乗数（ > 0.0）1.0: 振幅スペクトログラム、2.0: パワースペクトログラム、その他: 任意の累乗値の出力に相当
    *     fft_norm_type - FFT後の正規化処理、AILIA_AUDIO_FFT_NORMALIZE_* のいずれか
    *     f_min - 周波数の最小値
    *     f_max - 周波数の最大値
    *     mel_n - メル周波数のインデックス数（ < freq_n）
    *     mel_norm_type - MELスペクトログラムの正規化の有無、AILIA_AUDIO_MEL_NORMALIZE_* のいずれか
    *     mel_formula - MEL尺度の形式、AILIA_AUDIO_MEL_SCALE_FORMULA_* のいずれか
    *   返値:
    *     成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    *   備考:
    *     時間フレームごとにFFT(STFT)→正規化処理→累乗（振幅・パワーに変換→メルフィルタバンクの係数取得→メル尺度への変換 の順で処理を実行します。
    *     出力データは実数の信号であり、長さはmel_n*時間フレーム長です。
    **/
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaAudioGetMelSpectrogram(float[] dst, float[] src, int sample_n, int sample_rate, int fft_n, int hop_n, int win_n, int win_type, int max_frame_n, int center, float power, int fft_norm_type, float f_min, float f_max, int mel_n, int mel_norm_type, int mel_formula);


    /*************************************************************
    * スペクトログラムから振幅と位相を計算します。
    *   引数:
    *     dst_mag - 振幅の出力先ポインタ、外側から freq_n, frame_n 順のメモリレイアウト
    *     dst_phase - 位相の出力先ポインタ、外側から freq_n, frame_n, 2(実部、虚部) 順のメモリレイアウト
    *     src - 入力データのポインタ、frame_n, freq_n, 2(実部、虚部) 順のメモリレイアウト
    *     freq_n - 周波数のインデックス数
    *     frame_n - 時間フレームの数
    *     power - 振幅スペクトルの乗数 ( > 0.0)、1.0: 振幅スペクトログラム、2.0: パワースペクトログラムに相当
    *     phase_form - 位相の出力形式、AILIA_AUDIO_PHASE_FORM_* のいずれか
    *   返値:
    *     成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    *   備考:
    *     librosaのデフォルト値と互換の条件: phase_form = AILIA_AUDIO_PHASE_FORM_COMPLEX, power = 1.0
    *     PyTorchのデフォルト値と互換の条件: phase_form = AILIA_AUDIO_PHASE_FORM_REAL, power = 1.0
    *     phase_formによってdst_phaseの出力が変わります。
    *       - AILIA_AUDIO_PHASE_FORM_COMPLEX : 実部と虚部の交互信号、サイズは freq_n * frame_n * 2
    *       - AILIA_AUDIO_PHASE_FORM_REAL : 実部のみの信号、サイズは freq_n * frame_n
    **/
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaAudioMagPhase(float[] dst_mag, float[] dst_phase, float[] src, int freq_n, int frame_n, float power, int phase_form);


    /*************************************************************
    * 実数の信号に対して標準化を実行します。
    *   引数:
    *     dst - 出力データのポインタ、float 型、要素数 src_n
    *     src - 入力データのポインタ、float 型、要素数 src_n
    *     src_n - 入力データの要素数
    *   返値:
    *     成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    *   解説:
    *     入力データの平均0、分散1になるよう標準化を行う。
    *     dst = (src - mean(src)) / std(src)
    **/
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaAudioStandardize(float[] dst, float[] src, int src_n);


    /*************************************************************
    * 複素数のノルムを算出します。
    *   引数:
    *     dst - 出力データのポインタ、float 型、要素数 src_n
    *     src - 入力データのポインタ、float 型、外側から src_n, 2(実部、虚部) 順のメモリレイアウト
    *     src_n - 入力データの要素数
    *     power - 累乗値( > 0.0 )、1.0で複素数絶対値に相当
    *   返値:
    *     成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    *   解説:
    *     入力データのノルムを算出します。
    *     src_cmp = src[0] + src[1] i において
    *     tmp_dst = pow(src[0],2.0) + pow(src[1],2.0)
    *     dst[0] = pow(tmp_dst,0.5*power);
    **/
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaAudioComplexNorm(float[] dst, float[] src, int src_n, float power);


    /*************************************************************
    * 実数STFT結果をメル尺度に変換する
    *   引数:
    *     dst - 出力データのポインタ、float 型、外側から mel_n, frame_n 順のメモリレイアウト
    *     src - 入力データのポインタ、float 型、外側から freq_n, frame_n 順のメモリレイアウト
    *     fb_mtrx - メルフィルタバンク、float 型、外側から mel_n, freq_n  順のメモリレイアウト
    *     freq_n - 周波数のインデックス数
    *     frame_n - 入力データの時間フレームの数
    *     mel_n - メル周波数のインデックス数
    *   返値:
    *      成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    *   解説:
    *     入力された実数スペクトログラムをメル尺度に変換します
    *     fb_mtrxには ailiaAudioGetFBMatrix() で取得した係数を与える事が出来ます
    **/
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaAudioConvertToMel(float[] dst, float[] src, float[] fb_mtrx, int freq_n, int frame_n, int mel_n);


    /*************************************************************
    * 実数スペクトログラム/メルスペクトログラムの時間フレーム数を調整します。
    *   引数:
    *     dst - 出力データのポインタ、freq_n, dst_frame_n 順のメモリレイアウト
    *     src - 入力データのポインタ、freq_n, src_frame_n 順のメモリレイアウト
    *     freq_n - 周波数のインデックス数
    *     dst_frame_n - 出力データの時間フレームの数
    *     src_frame_n  - 入力データの時間フレームの数
    *     pad_data - パディング（dst_frame_n > src_frame_n の場合に使用）
    *   返値:
    *     成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    *   解説:
    *     dst_frame_n > src_frame_n : 不足する時間フレームのデータを pad_data のデータで埋める。
    *     dst_frame_n <= src_frame_n : 先頭から dst_frame_n のデータのみを切り出す。
    **/
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaAudioFixFrameLen(float[] dst, float[] src, int freq_n, int dst_frame_n, int src_frame_n, float pad_data);


    /*************************************************************
    * 信号をリサンプルします
    *   引数:
    *     dst - 出力データのポインタ、float 型、要素数 dst_n
    *     src - 入力データのポインタ、float 型、要素数 src_n
    *     dst_sample_rate - 変換後のサンプリングレート
    *     dst_n - データ出力先の確保要素数（dst_n >= max_resample_n）
    *     src_sample_rate - 入力データのサンプリングレート
    *     src_n - 入力データの要素数
    *   返値:
    *     成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    *   解説:
    *     最大出力数max_resample_nは ailiaAudioGetResampleLen() で取得できます。
    *     dst_n <  max_resample_n : 先頭からdst_nに入る部分のみ出力
    *     dst_n >= max_resample_n : 出力要素数はmax_resample_n
    **/
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaAudioResample(float[] dst, float[] src, int dst_sample_rate, int dst_n, int src_sample_rate, int src_n);


    /*************************************************************
    * リサンプル後のサンプル数を計算します
    *   引数:
    *     dst_sample_n - リサンプル後サンプル数出力先ポインタ
    *     dst_sample_rate - 変換後のサンプリングレート
    *     src_sample_n - 入力データのサンプル数
    *     src_sample_rate - 入力データのサンプリングレート
    *   返値:
    *     成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    *   解説:
    **/
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaAudioGetResampleLen(ref Int32 dst_sample_n, int dst_sample_rate, int src_sample_n, int src_sample_rate);


    /*************************************************************
    * 信号にフィルタ処理を適用します
    *   引数:
    *     dst - 出力データのポインタ、float 型、要素数 dst_n
    *     src - 入力データのポインタ、float 型、要素数 src_n
    *     n_coef - フィルタ分子係数のポインタ、float 型、要素数 n_coef_n
    *     d_coef - フィルタ分母係数のポインタ、float 型、要素数 d_coef_n
    *     zi - 遅延状態のポインタ、float 型、要素数 zi_n (zi_n = max(n_coef_n,d_coef_n)-1)、nullptrを許容
    *     dst_n - データ出力先の確保要素数（dst_n >= src_n）
    *     src_n - 入力データの要素数
    *     n_coef_n - フィルタ分子係数の要素数
    *     d_coef_n - フィルタ分母係数の要素数
    *     zi_n - 遅延状態の要素数 (zi_n >= max(n_coef_n,d_coef_n)-1)
    *   返値:
    *     成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    *   解説:
    *     dstへの出力数はmin(dst_m,src_n)となります。
    *     ziへは初期遅延状態を渡します。処理後には最終遅延状態に上書きされます。
    *     zi_nはmax(n_coef_n,d_coef_n)-1が必要となります。不足の場合、不足分は0でパディングし、最終遅延状態は返しません。
    *     ziにnullptrを与えた場合は、初期遅延状態を0とします。最終遅延状態も返しません。zi_nは無視されます。
    *     n_coef_nとd_coef_nは大きいほうを基準とし、不足分は0でパディングします。
    **/
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaAudioLinerFilter(float[] dst, float[] src, float[] n_coef, float[] d_coef, float[] zi, int dst_n, int src_n, int n_coef_n, int d_coef_n, int zi_n);


    /*************************************************************
    * フィルタ処理用の初期遅延係数を算出します
    *   引数:
    *     dst_zi 出力する初期遅延状態のポインタ、float 型、要素数 dst_n (dst_n >= max(n_coef_n,d_coef_n)-1)
    *     n_coef フィルタ分子係数のポインタ、float 型、要素数 n_coef_n
    *     d_coef フィルタ分母係数のポインタ、float 型、要素数 d_coef_n
    *     dst_n 出力先の確保要素数 (dst_n >= max(n_coef_n,d_coef_n)-1)
    *     n_coef_n フィルタ分子係数の要素数
    *     d_coef_n フィルタ分母係数の要素数
    *   返値:
    *     成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    *   解説:
    *     一般に、得られた係数に入力信号の先頭を乗じたものを、初期遅延状態として ailiaAudioLinerFilter() に与えます。
    *     dst_nはmax(n_coef_n,d_coef_n)-1が必要となります。
    *     不足の場合は、確保分だけ出力します。
    *     超える部分は、0で埋めます。
    *     n_coef_nとd_coef_nは大きいほうを基準とし、不足分は0でパディングします。
    **/
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaAudioGetLinerFilterZiCoef(float[] dst_zi, float[] n_coef, float[] d_coef, int dst_n, int n_coef_n, int d_coef_n);


    /*************************************************************
    * 信号にゼロ位相フィルタ処理を適用します
    *   引数:
    *     dst - 出力データのポインタ、float 型、要素数 dst_n
    *     src - 入力データのポインタ、float 型、要素数 src_n
    *     n_coef - フィルタ分子係数のポインタ、float 型、要素数 n_coef_n
    *     d_coef - フィルタ分母係数のポインタ、float 型、要素数 d_coef_n
    *     dst_n - データ出力先の確保要素数（dst_n >= src_n）
    *     src_n - 入力データの要素数
    *     n_coef_n - フィルタ分子係数の要素数
    *     d_coef_n - フィルタ分母係数の要素数
    *     pad_type - 入力信号に対する両端パディング処理方法、	AILIA_AUDIO_FILTFILT_PAD_* のいずれか
    *     pad_len - 入力信号に対する両端パディング数
    *   返値:
    *     成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    *   解説:
    *     dstへの出力数はmin(dst_m,src_n)となります。
    *     n_coef_nとd_coef_nは大きいほうを基準とし、不足分は0でパディングします。
    **/
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaAudioFilterFilter(float[] dst, float[] src, float[] n_coef, float[] d_coef, int dst_n, int src_n, int n_coef_n, int d_coef_n, int pad_type, int pad_len);

    /*************************************************************
    * 信号の入力前後の無音域を除いた領域を検出します
    *   引数:
    *   dst_start_pos - 有音域の先頭サンプル位置出力先ポインタ、int 型
    *   dst_length - 有音域の長さ出力先ポインタ、int 型
    *   src - 入力データのポインタ、float 型、要素数 sample_n
    *   sample_n - 入力データのサンプル数
    *   win_n - 1フレームに含むサンプル数
    *   hop_n - フレームのシフト数
    *   thr_db - 有音を判断するdB (thr_db > 0)
    *   返値:
    *     成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    *   解説:
    *     全域が無音の場合、*dst_start_pos = -1,*dst_length = 0となります。
    **/
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaAudioGetNonSilentPos(ref Int32 dst_start_pos, ref Int32 dst_length, float[] src, int sample_n, int win_n, int hop_n, float thr_db);


}
