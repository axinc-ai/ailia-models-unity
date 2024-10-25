using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using System.Threading;
using System.Threading.Tasks;

using ailia;
using ailiaVoice;

namespace ailiaSDK{

public class AiliaVoiceSample : MonoBehaviour
{
	// Model list
	public enum TextToSpeechSampleModels
	{
		tacotron2_english,
		gpt_sovits_japanese,
		gpt_sovits_english
	}

	// Settings
	public TextToSpeechSampleModels modelType = TextToSpeechSampleModels.gpt_sovits_japanese;
	public GameObject UICanvas = null;

	public AudioClip clip;
	public AudioClip ref_clip;
	public AudioSource audioSource;
	public GameObject processing;
	public bool gpu_mode = false;
	public InputField input_field;
	private string queue_text = "";
	private bool initialized = false;
	private AiliaVoiceModel voice = new AiliaVoiceModel();
	private string before_ref_clip_name = "";
	private bool model_downloading = false;

	// model download
	private AiliaDownload ailia_download = new AiliaDownload();


	private bool isProcessing = false;

	// Start is called before the first frame update
	void Start()
	{
			AiliaLicense.CheckAndDownloadLicense();
		UISetup();
		LoadModel();
	}

	void UISetup()
	{
		Debug.Assert (UICanvas != null, "UICanvas is null");

		Text label_text = UICanvas.transform.Find("LabelText").GetComponent<Text>();
		label_text.text = "";

		Text mode_text = UICanvas.transform.Find("ModeLabel").GetComponent<Text>();
		mode_text.text = "ailia Voice Synthesis Sample";

		UICanvas.transform.Find("RawImage").GetComponent<RawImage>().gameObject.SetActive(false);
	}

	void LoadModel(){
		if (initialized){
			return;
		}

		int env_id = voice.GetEnvironmentId(gpu_mode);		
		bool status = voice.Create(env_id, AiliaVoice.AILIA_VOICE_FLAG_NONE);
		if (status == false){
			Debug.Log("Create failed");
			return;
		}

		string asset_path=Application.temporaryCachePath;
		string path = asset_path+"/";

		var urlList = new List<ModelDownloadURL>();

		if (modelType == TextToSpeechSampleModels.gpt_sovits_japanese || modelType == TextToSpeechSampleModels.gpt_sovits_english){
			urlList.Add(new ModelDownloadURL() { folder_path = "open_jtalk/open_jtalk_dic_utf_8-1.11", file_name = "char.bin" });
			urlList.Add(new ModelDownloadURL() { folder_path = "open_jtalk/open_jtalk_dic_utf_8-1.11", file_name = "COPYING" });
			urlList.Add(new ModelDownloadURL() { folder_path = "open_jtalk/open_jtalk_dic_utf_8-1.11", file_name = "left-id.def" });
			urlList.Add(new ModelDownloadURL() { folder_path = "open_jtalk/open_jtalk_dic_utf_8-1.11", file_name = "matrix.bin" });
			urlList.Add(new ModelDownloadURL() { folder_path = "open_jtalk/open_jtalk_dic_utf_8-1.11", file_name = "pos-id.def" });
			urlList.Add(new ModelDownloadURL() { folder_path = "open_jtalk/open_jtalk_dic_utf_8-1.11", file_name = "rewrite.def" });
			urlList.Add(new ModelDownloadURL() { folder_path = "open_jtalk/open_jtalk_dic_utf_8-1.11", file_name = "right-id.def" });
			urlList.Add(new ModelDownloadURL() { folder_path = "open_jtalk/open_jtalk_dic_utf_8-1.11", file_name = "sys.dic" });
			urlList.Add(new ModelDownloadURL() { folder_path = "open_jtalk/open_jtalk_dic_utf_8-1.11", file_name = "unk.dic" });
		}
		if (modelType == TextToSpeechSampleModels.gpt_sovits_english){
			urlList.Add(new ModelDownloadURL() { folder_path = "g2p_en", file_name = "averaged_perceptron_tagger_classes.txt" });
			urlList.Add(new ModelDownloadURL() { folder_path = "g2p_en", file_name = "averaged_perceptron_tagger_tagdict.txt" });
			urlList.Add(new ModelDownloadURL() { folder_path = "g2p_en", file_name = "averaged_perceptron_tagger_weights.txt" });
			urlList.Add(new ModelDownloadURL() { folder_path = "g2p_en", file_name = "cmudict" });
			urlList.Add(new ModelDownloadURL() { folder_path = "g2p_en", file_name = "g2p_decoder.onnx" });
			urlList.Add(new ModelDownloadURL() { folder_path = "g2p_en", file_name = "g2p_encoder.onnx" });
			urlList.Add(new ModelDownloadURL() { folder_path = "g2p_en", file_name = "homographs.en" });
		}
		if (modelType == TextToSpeechSampleModels.tacotron2_english){
			urlList.Add(new ModelDownloadURL() { folder_path = "tacotron2", file_name = "encoder.onnx", local_name = "nivdia_encoder.onnx" });
			urlList.Add(new ModelDownloadURL() { folder_path = "tacotron2", file_name = "decoder_iter.onnx", local_name = "nivdia_decoder_iter.onnx" });
			urlList.Add(new ModelDownloadURL() { folder_path = "tacotron2", file_name = "postnet.onnx", local_name = "nivdia_postnet.onnx" });
			urlList.Add(new ModelDownloadURL() { folder_path = "tacotron2", file_name = "waveglow.onnx", local_name = "nivdia_waveglow.onnx" });
		}
		if (modelType == TextToSpeechSampleModels.gpt_sovits_japanese || modelType == TextToSpeechSampleModels.gpt_sovits_english){
			urlList.Add(new ModelDownloadURL() { folder_path = "gpt-sovits", file_name = "t2s_encoder.onnx" });
			urlList.Add(new ModelDownloadURL() { folder_path = "gpt-sovits", file_name = "t2s_fsdec.onnx" });
			urlList.Add(new ModelDownloadURL() { folder_path = "gpt-sovits", file_name = "t2s_sdec.opt3.onnx" });
			urlList.Add(new ModelDownloadURL() { folder_path = "gpt-sovits", file_name = "vits.onnx" });
			urlList.Add(new ModelDownloadURL() { folder_path = "gpt-sovits", file_name = "cnhubert.onnx" });
		}

		AiliaDownload ailia_download = new AiliaDownload();
		ailia_download.DownloaderProgressPanel = UICanvas.transform.Find("DownloaderProgressPanel").gameObject;

		StartCoroutine(ailia_download.DownloadWithProgressFromURL(urlList, () =>
		{
			if (modelType == TextToSpeechSampleModels.gpt_sovits_japanese || modelType == TextToSpeechSampleModels.gpt_sovits_english){
				status = voice.OpenDictionary(path, AiliaVoice.AILIA_VOICE_DICTIONARY_TYPE_OPEN_JTALK);
				if (status == false){
					Debug.Log("OpenDictionary failed");
					return;
				}
			}
			if (modelType == TextToSpeechSampleModels.gpt_sovits_english){
				status = voice.OpenDictionary(path, AiliaVoice.AILIA_VOICE_DICTIONARY_TYPE_G2P_EN);
				if (status == false){
					Debug.Log("OpenDictionary failed");
					return;
				}
			}

			switch(modelType){
			case TextToSpeechSampleModels.tacotron2_english:
				Debug.Log(path+"nivdia_encoder.onnx");
				status = voice.OpenModel(path+"nivdia_encoder.onnx", path+"nivdia_decoder_iter.onnx", path+"nivdia_postnet.onnx", path+"nivdia_waveglow.onnx", null, AiliaVoice.AILIA_VOICE_MODEL_TYPE_TACOTRON2, AiliaVoice.AILIA_VOICE_CLEANER_TYPE_BASIC);
				break;
			case TextToSpeechSampleModels.gpt_sovits_japanese:
			case TextToSpeechSampleModels.gpt_sovits_english:
				status = voice.OpenModel(path+"t2s_encoder.onnx", path+"t2s_fsdec.onnx", path+"t2s_sdec.opt3.onnx", path+"vits.onnx", path+"cnhubert.onnx", AiliaVoice.AILIA_VOICE_MODEL_TYPE_GPT_SOVITS, AiliaVoice.AILIA_VOICE_CLEANER_TYPE_BASIC);
				break;
			}
			if (status == false){
				Debug.Log("OpenModel failed");
				return;
			}
			initialized = true;
			before_ref_clip_name = "";
		}));
	}

	private void Infer(string text){
		if (modelType == TextToSpeechSampleModels.gpt_sovits_japanese || modelType == TextToSpeechSampleModels.gpt_sovits_english){
			if (ref_clip.name != before_ref_clip_name){
				string label = "水をマレーシアから買わなくてはならない。";
				Debug.Log("Label : " + label);
				string ref_text = voice.G2P(label, AiliaVoice.AILIA_VOICE_TEXT_POST_PROCESS_APPEND_PUNCTUATION);
				voice.SetReference(ref_clip, ref_text);
				before_ref_clip_name = ref_clip.name;
			}
		}
		if (modelType == TextToSpeechSampleModels.gpt_sovits_japanese){
			text = voice.G2P(text, AiliaVoice.AILIA_VOICE_G2P_TYPE_GPT_SOVITS_JA);
		}
		if (modelType == TextToSpeechSampleModels.gpt_sovits_english){
			text = voice.G2P(text, AiliaVoice.AILIA_VOICE_G2P_TYPE_GPT_SOVITS_EN);
		}

		Debug.Log("Features : "+ text);

		var context = SynchronizationContext.Current;

		Task.Run(async () =>
		{
			isProcessing = true;

			bool status = voice.Inference(text);
			if (status == false){
				Debug.Log("Inference error");
				return;
			}

			context.Post(state =>
			{
				clip = voice.GetAudioClip();
				if (status == null){
					Debug.Log("Inference failed");
					return;
				}

				Debug.Log("Samples : " + clip.samples);
				Debug.Log("Channels : " + clip.channels);
				Debug.Log("SamplingRate : " + clip.frequency);
				
				audioSource.clip = clip;
				audioSource.Play();

				isProcessing = false;
			}, null);			
		});//.Wait();
	}

	void OnDisable(){
		Debug.Log("OnDisable");
		voice.Close();
		initialized = false;
	}

	void OnApplicationQuit(){
		Debug.Log("OnApplicationQuit");
		voice.Close();
		initialized = false;
	}

	// Update is called once per frame
	void Update()
	{
		if (isProcessing){
			processing.SetActive(true);
			return;
		}else{
			processing.SetActive(false);
		}
		if (queue_text != ""){
			if (initialized){
				Infer(queue_text);
				queue_text = "";
			}		
		}
	}

	public void Speak(){
		queue_text = input_field.text;
		Debug.Log("Queue : " + queue_text);
	}

	public void Replay(){
		audioSource.Play();
	}
}

}
