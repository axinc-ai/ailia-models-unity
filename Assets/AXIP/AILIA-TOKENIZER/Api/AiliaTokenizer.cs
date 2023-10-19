/* ailia Tokenizer Unity Plugin Native Interface */
/* Copyright 2023 AXELL CORPORATION */

using UnityEngine;
using System.Collections;
using System.IO;
using System;
using System.Runtime.InteropServices;

public class AiliaTokenizer
{

    /* Native Binary 定義 */

    #if (UNITY_IPHONE && !UNITY_EDITOR) || (UNITY_WEBGL && !UNITY_EDITOR)
        public const String LIBRARY_NAME="__Internal";
    #else
        #if (UNITY_ANDROID && !UNITY_EDITOR) || (UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX)
            public const String LIBRARY_NAME="ailia_tokenizer";
        #else
            public const String LIBRARY_NAME="ailia_tokenizer";
        #endif
    #endif

    /****************************************************************
    * アルゴリズム定義
    **/

    /**
    * \~japanese
    * @def AILIA_TOKENIZER_TYPE_WHISPER
    * @brief Whisper向けのトークナイザ
    *
    * \~english
    * @def AILIA_TOKENIZER_TYPE_WHISPER
    * @brief Tokenizer for Whisper
    */
    public const Int32  AILIA_TOKENIZER_TYPE_WHISPER = (0);

    /**
    * \~japanese
    * @def AILIA_TOKENIZER_TYPE_CLIP
    * @brief Clip向けのトークナイザ
    *
    * \~english
    * @def AILIA_TOKENIZER_TYPE_CLIP
    * @brief Tokenizer for Clip
    */
    public const Int32  AILIA_TOKENIZER_TYPE_CLIP = (1);

    /**
    * \~japanese
    * @def AILIA_TOKENIZER_TYPE_XLM_ROBERTA
    * @brief XLM_ROBERTA向けのトークナイザ
    *
    * \~english
    * @def AILIA_TOKENIZER_TYPE_XLM_ROBERTA
    * @brief Tokenizer for XLM_ROBERTA
    */
    public const Int32 AILIA_TOKENIZER_TYPE_XLM_ROBERTA = (2);

    /**
    * \~japanese
    * @def AILIA_TOKENIZER_TYPE_MARIAN
    * @brief MARIAN向けのトークナイザ
    *
    * \~english
    * @def AILIA_TOKENIZER_TYPE_MARIAN
    * @brief Tokenizer for MARIAN
    */
    public const Int32 AILIA_TOKENIZER_TYPE_MARIAN = (3);

    /**
    * \~japanese
    * @def AILIA_TOKENIZER_TYPE_BERT_JAPANESE_WORDPIECE
    * @brief Japanese BERT向けのトークナイザ
    *
    * \~english
    * @def AILIA_TOKENIZER_TYPE_BERT_JAPANESE_WORDPIECE
    * @brief Tokenizer for Japanese BERT
    */
    public const Int32 AILIA_TOKENIZER_TYPE_BERT_JAPANESE_WORDPIECE = (4);

    /**
    * \~japanese
    * @def AILIA_TOKENIZER_TYPE_BERT_JAPANESE_CHARACTER
    * @brief Japanese BERT向けのトークナイザ
    *
    * \~english
    * @def AILIA_TOKENIZER_TYPE_BERT_JAPANESE_CHARACTER
    * @brief Tokenizer for Japanese BERT
    */
    public const Int32 AILIA_TOKENIZER_TYPE_BERT_JAPANESE_CHARACTER = (5);

    /**
    * \~japanese
    * @def AILIA_TOKENIZER_TYPE_T5
    * @brief T5向けのトークナイザ
    *
    * \~english
    * @def AILIA_TOKENIZER_TYPE_T5
    * @brief Tokenizer for T5
    */
    public const Int32 AILIA_TOKENIZER_TYPE_T5 = (6);

    /****************************************************************
    * フラグ定義
    **/

    /**
    * \~japanese
    * @def AILIA_TOKENIZER_FLAG_NONE
    * @brief フラグを設定しません
    *
    * \~english
    * @def AILIA_TOKENIZER_FLAG_NONE
    * @brief Default flag
    */
    public const Int32  AILIA_TOKENIZER_FLAG_NONE = (0);

    /**
    * \~japanese
    * @def AILIA_TOKENIZER_FLAG_UTF8_SAFE
    * @brief UTF8として有効な文字のみ出力します
    *
    * \~english
    * @def AILIA_TOKENIZER_FLAG_UTF8_SAFE
    * @brief Output only characters valid as UTF8
    */
    public const Int32  AILIA_TOKENIZER_FLAG_UTF8_SAFE = (1);

    /****************************************************************
    * Tokenizer API
    **/

    /**
    * \~japanese
    * @brief トークナイズオブジェクトを作成します。
    * @param net トークナイザオブジェクトポインタへのポインタ
    * @param type AILIA_TOKENIZER_TYPE_*
    * @param flag AILIA_TOKENIZER_FLAG_*の論理和
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    * @details
    *   トークナイズオブジェクトを作成します。
    *
    * \~english
    * @brief Creates a tokenizer instance.
    * @param net A pointer to the tokenizer instance pointer
    * @param type AILIA_TOKENIZER_TYPE_*
    * @param flag OR of AILIA_TOKENIZER_FLAG_*
    * @return
    *   If this function is successful, it returns  \ref AILIA_STATUS_SUCCESS , or an error code otherwise.
    * @details
    *   Creates a tokenizer instance.
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaTokenizerCreate(ref IntPtr net, int type, int flags);

    /**
    * \~japanese
    * @brief モデルファイルを読み込みます。
    * @param net トークナイザオブジェクトポインタへのポインタ
    * @param path SentencePieceのモデルファイルのパス
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    * @details
    *   SentencePieceのモデルファイルを読み込みます。AILIA_TOKENIZER_TYPE_XLM_ROBERTAもしくはAILIA_TOKENIZER_TYPE_MARIANの場合のみ必要です。
    *
    * \~english
    * @brief Open model file.
    * @param net A pointer to the tokenizer instance pointer
    * @param path Path for SentencePiece
    * @return
    *   If this function is successful, it returns  \ref AILIA_STATUS_SUCCESS , or an error code otherwise.
    * @details
    *   Open a model file for SentencePiece. This API only requires for AILIA_TOKENIZER_TYPE_XLM_ROBERTA or AILIA_TOKENIZER_TYPE_MARIAN.
    */
    #if (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN)
        [DllImport(LIBRARY_NAME, EntryPoint = "ailiaTokenizerOpenModelFileW", CharSet=CharSet.Unicode)]
        public static extern int ailiaTokenizerOpenModelFile(IntPtr net, string path);
    #else
        [DllImport(LIBRARY_NAME, EntryPoint = "ailiaTokenizerOpenModelFileA", CharSet=CharSet.Ansi)]
        public static extern int ailiaTokenizerOpenModelFile(IntPtr net, string ath);
    #endif

    /**
    * \~japanese
    * @brief 辞書ファイルを読み込みます。
    * @param net トークナイザオブジェクトポインタへのポインタ
    * @param path Mecabの辞書ファイルのパス
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    * @details
    *   Mecabの辞書ファイルを読み込みます。AILIA_TOKENIZER_TYPE_BERT_JAPANESE_XXXの場合のみ必要です。
    *
    * \~english
    * @brief Open dictionary file.
    * @param net A pointer to the tokenizer instance pointer
    * @param path Path for dictionary of Mecab
    * @return
    *   If this function is successful, it returns  \ref AILIA_STATUS_SUCCESS , or an error code otherwise.
    * @details
    *   Open a model file for Mecab. This API only requires for AILIA_TOKENIZER_TYPE_BERT_JAPANESE_XXX.
    */
    #if (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN)
        [DllImport(LIBRARY_NAME, EntryPoint = "ailiaTokenizerOpenDictionaryFileW", CharSet=CharSet.Unicode)]
        public static extern int ailiaTokenizerOpenDictionaryFile(IntPtr net, string path);
    #else
        [DllImport(LIBRARY_NAME, EntryPoint = "ailiaTokenizerOpenDictionaryFileA", CharSet=CharSet.Ansi)]
        public static extern int ailiaTokenizerOpenDictionaryFile(IntPtr net, string path);
    #endif

    /**
    * \~japanese
    * @brief 単語ファイルを読み込みます。
    * @param net トークナイザオブジェクトポインタへのポインタ
    * @param path 単語ファイルのパス
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    * @details
    *   単語ファイルを読み込みます。AILIA_TOKENIZER_TYPE_BERT_JAPANESE_XXXの場合のみ必要です。
    *
    * \~english
    * @brief Open vocab file.
    * @param net A pointer to the tokenizer instance pointer
    * @param path Path for Vocab file
    * @return
    *   If this function is successful, it returns  \ref AILIA_STATUS_SUCCESS , or an error code otherwise.
    * @details
    *   Open a vocab file. This API only requires for AILIA_TOKENIZER_TYPE_BERT_JAPANESE_XXX.
    */
    #if (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN)
        [DllImport(LIBRARY_NAME, EntryPoint = "ailiaTokenizerOpenVocabFileW", CharSet=CharSet.Unicode)]
        public static extern int ailiaTokenizerOpenVocabFile(IntPtr net, string path);
    #else
        [DllImport(LIBRARY_NAME, EntryPoint = "ailiaTokenizerOpenVocabFileA", CharSet=CharSet.Ansi)]
        public static extern int ailiaTokenizerOpenVocabFile(IntPtr net, string path);
    #endif

    /**
    * \~japanese
    * @brief エンコードを行います。
    * @param net トークナイザオブジェクトポインタ
    * @param text エンコードするテキスト(UTF8)
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    * @details
    *   認識した結果はailiaTokenizerGetTokens APIで取得します。
    *
    * \~english
    * @brief Perform encode
    * @param net A tokenizer instance pointer
    * @param text Text for encode (UTF8)
    * @return
    *   If this function is successful, it returns  \ref AILIA_STATUS_SUCCESS , or an error code otherwise.
    * @details
    *   Get the encoded result with ailiaTokenizerGetTokens API.
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaTokenizerEncode(IntPtr net, IntPtr utf8);
        
    /**
    * \~japanese
    * @brief トークンの数を取得します。
    * @param net   トークナイザオブジェクトポインタ
    * @param count  オブジェクト数
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    *
    * \~english
    * @brief Gets the number of tokens.
    * @param net   A tokenizer instance pointer
    * @param count  The number of objects
    * @return
    *   If this function is successful, it returns  \ref AILIA_STATUS_SUCCESS , or an error code otherwise.
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaTokenizerGetTokenCount(IntPtr net, ref uint count);

    /**
    * \~japanese
    * @brief トークンを取得します。
    * @param net   トークナイザオブジェクトポインタ
    * @param tokens トークン
    * @param count  格納先トークン数
    * @param 
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    * @details
    *   ailiaTokenizerEncode() を一度も実行していない場合は \ref AILIA_STATUS_INVALID_STATE が返ります。
    *
    * \~english
    * @brief Gets the detection results.
    * @param net   A tokenizer instance pointer
    * @param tokens Token
    * @param count  Token count
    * @return
    *   If this function is successful, it returns  \ref AILIA_STATUS_SUCCESS , or an error code otherwise.
    * @details
    *   If  ailiaTokenizerEncode()  is not run at all, the function returns  \ref AILIA_STATUS_INVALID_STATE .
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaTokenizerGetTokens(IntPtr net, IntPtr tokens, uint count);

    /**
    * \~japanese
    * @brief デコードを行います。
    * @param net トークナイザオブジェクトポインタ
    * @param tokens デコードするトークン
    * @param token_count トークンの数
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    * @details
    *   認識した結果はailiaTokenizerGetText APIで取得します。
    *
    * \~english
    * @brief Perform encode
    * @param net A tokenizer instance pointer
    * @param tokens Tokens for decode
    * @param token_count The number of tokens
    * @return
    *   If this function is successful, it returns  \ref AILIA_STATUS_SUCCESS , or an error code otherwise.
    * @details
    *   Get the decoded result with ailiaTokenizerGetText API.
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaTokenizerDecode(IntPtr net, IntPtr tokens, uint token_count);

    /**
    * \~japanese
    * @brief テキストの長さを取得します。(NULL文字含む)
    * @param net   トークナイザオブジェクトポインタ
    * @param len  テキストの長さ
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    *
    * \~english
    * @brief Gets the size of text. (Include null)
    * @param net   A tokenizer instance pointer
    * @param len  The length of text
    * @return
    *   If this function is successful, it returns  \ref AILIA_STATUS_SUCCESS , or an error code otherwise.
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaTokenizerGetTextLength(IntPtr net, ref uint len);

    /**
    * \~japanese
    * @brief テキストを取得します。
    * @param net   トークナイザオブジェクトポインタ
    * @param text  テキスト(UTF8)
    * @param len バッファサイズ
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    * @details
    *   ailiaTokenizerDecode() を一度も実行していない場合は \ref AILIA_STATUS_INVALID_STATE が返ります。
    *
    * \~english
    * @brief Gets the decoded text.
    * @param net   A tokenizer instance pointer
    * @param text  Text(UTF8)
    * @param len  Buffer size
    * @return
    *   If this function is successful, it returns  \ref AILIA_STATUS_SUCCESS , or an error code otherwise.
    * @details
    *   If  ailiaTokenizerDecode()  is not run at all, the function returns  \ref AILIA_STATUS_INVALID_STATE .
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaTokenizerGetText(IntPtr net, IntPtr text, uint len);

    /**
    * \~japanese
    * @brief トークナイズオブジェクトを破棄します。
    * @param net トークナイザオブジェクトポインタ
    *
    * \~english
    * @brief It destroys the tokenizer instance.
    * @param net A tokenizer instance pointer
    */
    [DllImport(LIBRARY_NAME)]
    public static extern void ailiaTokenizerDestroy(IntPtr net);

    /****************************************************************
    * Utility API
    **/

    /**
    * \~japanese
    * @brief UTF8の文字をUTF32の文字に変換します。
    * @param utf32   UTF32の文字
    * @param processed_byte 消費したバイト数(UTF8)
    * @param utf8  UTF8の文字
    * @param utf8_len   バッファサイズ
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    *
    * \~english
    * @brief Convert UTF8 character to UTF32 character.
    * @param utf32   UTF32の文字
    * @param processed_byte Processed bytes on UTF8
    * @param utf8  UTF8の文字
    * @param utf8_len   Buffer Size
    * @return
    *   If this function is successful, it returns  \ref AILIA_STATUS_SUCCESS , or an error code otherwise.
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaTokenizerUtf8ToUtf32(ref uint utf32, ref uint processed_byte, IntPtr utf8, uint utf8_len);

    /**
    * \~japanese
    * @brief UTF32の文字をUTF8の文字に変換します。
    * @param utf8   UTF8の文字(4byte以上必要)
    * @param processed_byte 書き込んだ文字数(UTF8)
    * @param utf32  UTF32の文字
    * @return
    *   成功した場合は \ref AILIA_STATUS_SUCCESS 、そうでなければエラーコードを返す。
    *
    * \~english
    * @brief Convert UTF32 character to UTF8 character.
    * @param utf8   UTF8 character(Require greater than 4byte)
    * @param processed_byte Processed bytes on UTF8
    * @param utf32  UTF32 character
    * @return
    *   If this function is successful, it returns  \ref AILIA_STATUS_SUCCESS , or an error code otherwise.
    */
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaTokenizerUtf32ToUtf8(IntPtr utf8, ref uint processed_byte, uint utf32);
}
