/* ailia.tokenizer model class */
/* Copyright 2023 AXELL CORPORATION */

using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Threading;
using System.Runtime.InteropServices;

public class AiliaTokenizerModel : IDisposable
{
	// instance
	IntPtr net = IntPtr.Zero;

	/****************************************************************
	 * モデル
	 */

	/**
	* \~japanese
	* @brief インスタンスを作成します。
	* @param type           タイプ（AiliaTokenizer.AILIA_TOKENIZER_TYPE_*)
	* @param flag           フラグの論理和（AiliaSpeech.AILIA_TOKENIZER_FLAG_*)
	* @return
	*   成功した場合はtrue、失敗した場合はfalseを返す。
	*   
	* \~english
	* @brief   Create a instance.
	* @param type           Type (AiliaSpeech.AILIA_TOKENIZER_TYPE_*)
	* @param flag           OR of flags (AiliaSpeech.AILIA_TOKENIZER_FLAG_*)
	* @return
	*   If this function is successful, it returns  true  , or  false  otherwise.
	*/
	public bool Create(int type, int flag){
		if (net != null){
			Close();
		}

		int status = AiliaTokenizer.ailiaTokenizerCreate(ref net, type, flag);
		if (status != 0){
			return false;
		}

		return true;
	}

	/**
	* \~japanese
	* @brief モデルファイルを開きます。
	* @param model_path          モデルファイルへのパス。(nullの場合は読み込まない)
	* @param dictionary_path     辞書ファイルへのパス。(nullの場合は読み込まない)
	* @param vocab_path          Vocabファイルへのパス。(nullの場合は読み込まない)
	* @return
	*   成功した場合はtrue、失敗した場合はfalseを返す。
	*   
	* \~english
	* @brief   Open a model.
	* @param model_path          Path for model (don't load if null)
	* @param dictionary_path     Path for dictionary (don't load if null)
	* @param vocab_path          Path for vocab (don't load if null)
	* @return
	*   If this function is successful, it returns  true  , or  false  otherwise.
	*/
	public bool Open(string model_path = null, string dictionary_path = null, string vocab_path = null){
		if (net == null){
			return false;
		}

		int status = 0;
		
		if (model_path != null){
			status = AiliaTokenizer.ailiaTokenizerOpenModelFile(net, model_path);
			if (status != 0){
				return false;
			}
		}
		if (dictionary_path != null){
			status = AiliaTokenizer.ailiaTokenizerOpenDictionaryFile(net, dictionary_path);
			if (status != 0){
				return false;
			}
		}
		if (vocab_path != null){
			status = AiliaTokenizer.ailiaTokenizerOpenVocabFile(net, vocab_path);
			if (status != 0){
				return false;
			}
		}

		return true;
	}

	/****************************************************************
	 * 開放する
	 */
	/**
	* \~japanese
	* @brief インスタンスを破棄します。
	* @details
	*   インスタンスを破棄し、初期化します。
	*   
	*  \~english
	* @brief   Destroys instance
	* @details
	*   Destroys and initializes the instance.
	*/
	public virtual void Close()
	{
		if (net != IntPtr.Zero){
			AiliaTokenizer.ailiaTokenizerDestroy(net);
			net = IntPtr.Zero;
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

	~AiliaTokenizerModel(){
		Dispose(false);
	}

	/****************************************************************
	 * エンコードとデコード
	 */

	/**
	* \~japanese
	* @brief エンコードを実行します。
	* @param utf8    入力文字列
	* @return
	*   成功した場合はトークン列、失敗した場合は空配列を返す。
	*   
	* \~english
	* @brief   Perform encode
	* @param utf8    Input string
	* @return
	*   If this function is successful, it returns array of tokens  , or  empty array  otherwise.
	*/
	public int[] Encode(string utf8)
	{
		byte[] text = System.Text.Encoding.UTF8.GetBytes(utf8+"\u0000");
        GCHandle handle = GCHandle.Alloc(text, GCHandleType.Pinned);
        IntPtr input = handle.AddrOfPinnedObject();
		int status = AiliaTokenizer.ailiaTokenizerEncode(net, input);
		handle.Free();
		if (status != 0){
			return new int[0];
		}
		uint count = 0;
		status = AiliaTokenizer.ailiaTokenizerGetTokenCount(net, ref count);
		if (status != 0){
			return new int[0];
		}
		int[] tokens = new int [count];
        handle = GCHandle.Alloc(tokens, GCHandleType.Pinned);
        IntPtr output = handle.AddrOfPinnedObject();
		status = AiliaTokenizer.ailiaTokenizerGetTokens(net, output, count);
		handle.Free();
		if (status != 0){
			return new int[0];
		}
		return tokens;
	}

	/**
	* \~japanese
	* @brief デコードを実行します。
	* @pram tokens   入力トークン
	* @return
	*   成功した場合は文字列、失敗した場合は空文字列を返す。
	*   
	* \~english
	* @brief   Perform decode
	* @pram tokens   Input tokens
	* @return
	*   If this function is successful, it returns  string  , or  empty string  otherwise.
	*/
	public string Decode(int[] tokens)
	{
		uint count = (uint)tokens.Length;
        GCHandle handle = GCHandle.Alloc(tokens, GCHandleType.Pinned);
        IntPtr input = handle.AddrOfPinnedObject();
		int status = AiliaTokenizer.ailiaTokenizerDecode(net, input, count);
		handle.Free();
		if (status != 0){
			return "";
		}
		uint len = 0;
		status = AiliaTokenizer.ailiaTokenizerGetTextLength(net, ref len);
		if (status != 0){
			return "";
		}
		byte[] text = new byte [len];
        handle = GCHandle.Alloc(text, GCHandleType.Pinned);
        IntPtr output = handle.AddrOfPinnedObject();
		status = AiliaTokenizer.ailiaTokenizerGetText(net, output, len);
		handle.Free();
		if (status != 0){
			return "";
		}
		return System.Text.Encoding.UTF8.GetString(text);
	}
}
