using ailiaSDK;
using Assets.Scripts;
using NWaves.Filters.Base;
using NWaves.Signals;
using NWaves.Transforms;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

struct JsonWave
{
    public float[] samples;
}

struct HyperParameters
{
    public static int n_fft = 2048; // fft points (samples)
    public static float power = 1.5f; // Exponent for amplifying the predicted magnitude
    public static int n_iter = 50; // Number of inversion iterations
    public static float preemphasis = .97f;
    public static float max_db = 100;
    public static float ref_db = 20;
    public static int sr = 22050; // Sampling rate
    public static float frame_shift = 0.0125f; // seconds
    public static float frame_length = 0.05f; // seconds
    public static int hop_length = Mathf.RoundToInt(sr * frame_shift); // samples. =276.
    public static int win_length = Mathf.RoundToInt(sr * frame_length); // samples. =1102.
}

class AiliaTextToSpeechThreadedJob : ThreadedJob
{
    public float[] samples { get; private set; }

    private AiliaTts ailiaTts;
    private string textToSpeak;

    public AiliaTextToSpeechThreadedJob(AiliaTts ailiaTts, string textToSpeak)
    {
        this.ailiaTts = ailiaTts;
        this.textToSpeak = textToSpeak;
    }

    protected override void ThreadFunction()
    {
        samples = ailiaTts.RunTTSInference(textToSpeak);
    }
}

public class AiliaTts : MonoBehaviour, IDisposable
{
    private AiliaDownload ailiaDownload;
    private AiliaModel ailiaText2Mel = new AiliaModel();
    private AiliaModel ailiaSsr = new AiliaModel();

    public bool gpuMode { get; private set; }

    static readonly string validCharacters = "PE abcdefghijklmnopqrstuvwxyz'.?";
    static readonly int endOfSentenceIndex = validCharacters.IndexOf("E");
    static readonly int maxText2MelIterations = 210;
    static readonly uint melSpectrogramSize = 80;

    public AiliaTts(AiliaDownload ailiaDownload)
    {
        this.ailiaDownload = ailiaDownload ?? new AiliaDownload();
    }

    public IEnumerator Initialize(bool gpuMode, Action<bool> onCompleted)
    {
        this.gpuMode = gpuMode;
        string assetPath = Application.temporaryCachePath;
        string folderPath = "pytorch-dc-tts";
        string text2MelModelName = "text2mel";
        string ssrnModelName = "ssrn";

        // captioning feature extraction
        if (gpuMode)
        {
            ailiaText2Mel.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
        }

        yield return ailiaDownload.DownloadWithProgressFromURL(new ModelDownloadURL[] {
            new ModelDownloadURL() { folder_path = folderPath, file_name = $"{text2MelModelName}.onnx.prototxt" },
            new ModelDownloadURL() { folder_path = folderPath, file_name = $"{text2MelModelName}.onnx" },
            new ModelDownloadURL() { folder_path = folderPath, file_name = $"{ssrnModelName}.onnx.prototxt" },
            new ModelDownloadURL() { folder_path = folderPath, file_name = $"{ssrnModelName}.onnx" }
        }.ToList(), () =>
        {
            bool status = false;

            status = ailiaText2Mel.OpenFile($"{assetPath}/{text2MelModelName}.onnx.prototxt", $"{assetPath}/{text2MelModelName}.onnx");

            if (status == false)
            {
                Debug.LogError($"Could not load model {text2MelModelName} at '{assetPath}': {ailiaText2Mel.GetErrorDetail()}");
                onCompleted(false);
                return;
            }

            status = ailiaSsr.OpenFile($"{assetPath}/{ssrnModelName}.onnx.prototxt", $"{assetPath}/{ssrnModelName}.onnx");

            if (status == false)
            {
                Debug.LogError($"Could not load model {ssrnModelName} at '{assetPath}': {ailiaSsr.GetErrorDetail()}");
                onCompleted(false);
                return;
            }

            onCompleted(true);
            return;
        });
    }

    private string Preprocess(string sentence)
    {
        StringBuilder builder = new StringBuilder();

        foreach (var c in sentence.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(c);
            }
        }

        return builder.ToString().ToLower().Replace($"[^{validCharacters}]", " ").Replace("[ ]+", " ").Trim() + "E";
    }

    float[] convertSentenceToIndices(string sentence)
    {
        float[] indices = new float[sentence.Length];

        for (int i = 0; i < sentence.Length; ++i)
        {
            indices[i] = validCharacters.IndexOf(sentence[i]);
        }

        return indices;
    }

    public float[] RunTTSInference(string inputSentence)
    {
        float[] inputArray = convertSentenceToIndices(Preprocess(inputSentence));
        float[] melSpectrogram = RunText2MelInference(inputArray);
        float[] superMelSpectrogram = RunSSRInference(melSpectrogram);

        Debug.Log(superMelSpectrogram.Length);

        float[] rawAudioSamples = DecodeSRMelSpectrogram(superMelSpectrogram);

        Debug.Log("TTS inference done");

        return rawAudioSamples;
    }

    private float[] DecodeSRMelSpectrogram(float[] superMelSpectrogram)
    {
        int samplesSize = superMelSpectrogram.Length / (1 + HyperParameters.n_fft / 2);
        float[] inputArray = new float[superMelSpectrogram.Length];
        
        for (int i = 0; i < inputArray.Length; ++i)
        {
            float exp = (Mathf.Clamp(superMelSpectrogram[i], 0, 1) * HyperParameters.max_db) - HyperParameters.max_db + HyperParameters.ref_db;
            inputArray[i] = Mathf.Pow(10, 0.05f * exp);
        }

        Stft stft = new Stft(HyperParameters.win_length, HyperParameters.hop_length, NWaves.Windows.WindowTypes.Hann, HyperParameters.n_fft);
        List<float[]> signal = inputArray
            .Select((sample, idx) => (sample, idx))
            .GroupBy(t => t.idx % samplesSize)
            .Select(g => g.Select(t => Mathf.Pow(t.sample, HyperParameters.power)).ToArray())
            .ToList();

        var griffinLim = new NWaves.Operations.GriffinLimReconstructor(signal, stft, 1);
        float[] reconstructed = griffinLim.Reconstruct(HyperParameters.n_iter);

        var filter = new FirFilter(new float[] { -HyperParameters.preemphasis });

        reconstructed = filter.ProcessAllSamples(reconstructed);

        return reconstructed;
    }

    private float[] RunText2MelInference(float[] inputArray)
    {
        bool status;

        int sentenceInputBlobIndex = ailiaText2Mel.FindBlobIndexByName("input.1");
        int spectrogramInputBlobIndex = ailiaText2Mel.FindBlobIndexByName("input.2");
        int spectrogramOutputBlobIndex = ailiaText2Mel.FindBlobIndexByName("output.2");
        int attentionOutputBlobIndex = ailiaText2Mel.FindBlobIndexByName("output.3");

        Ailia.AILIAShape sentenceShape = new Ailia.AILIAShape
        {
            x = (uint)inputArray.Length,
            y = 1,
            z = 1,
            w = 1,
            dim = 2
        };
        Ailia.AILIAShape spectrogramShape = new Ailia.AILIAShape
        {
            x = 1,
            y = melSpectrogramSize,
            z = 1,
            w = 1,
            dim = 3
        };
        List<float> spectrogram = new List<float>((int) spectrogramShape.y);
        spectrogram.AddRange(Enumerable.Repeat(0f, ((int) spectrogramShape.y)));

        status = ailiaText2Mel.SetInputBlobShape(sentenceShape, sentenceInputBlobIndex);

        if (status == false)
        {
            Debug.LogError($"Could not set sentence input blob shape");
            Debug.LogError(ailiaText2Mel.GetErrorDetail());
            return new float[0];
        }

        status = ailiaText2Mel.SetInputBlobData(inputArray, sentenceInputBlobIndex);

        if (status == false)
        {
            Debug.LogError("Could not set sentence input blob data");
            Debug.LogError(ailiaText2Mel.GetErrorDetail());
            return new float[0];
        }

        for (int i = 0; i < maxText2MelIterations; ++i)
        {
            status = ailiaText2Mel.SetInputBlobShape(spectrogramShape, spectrogramInputBlobIndex);

            if (status == false)
            {
                Debug.LogError($"Could not set spectrogram input blob shape");
                return new float[0];
            }

            status = ailiaText2Mel.SetInputBlobData(spectrogram.ToArray(), spectrogramInputBlobIndex);

            if (status == false)
            {
                Debug.LogError("Could not set spectrogram input blob data");
                Debug.LogError(ailiaText2Mel.GetErrorDetail());
                return new float[0];
            }

            status = ailiaText2Mel.Update();

            if (status == false)
            {
                Debug.LogError("Error while running text2mel inference");
                Debug.LogError(ailiaText2Mel.GetErrorDetail());
                return new float[0];
            }


            Ailia.AILIAShape spectrogramOutputShape = ailiaText2Mel.GetBlobShape(spectrogramOutputBlobIndex);
            float[] spectrogramOutputArray = new float[spectrogramOutputShape.x * spectrogramOutputShape.y * spectrogramOutputShape.z];

            status = ailiaText2Mel.GetBlobData(spectrogramOutputArray, spectrogramOutputBlobIndex);

            if (status == false)
            {
                Debug.LogError("Could not get spectrogram output blob data ");
                Debug.LogError(ailiaText2Mel.GetErrorDetail());
                return new float[0];
            }

            spectrogram.Clear();

            for (int k = 0; k < spectrogramOutputShape.y; ++k)
            {
                for (int j = 0; j < spectrogramOutputShape.x + 1; ++j)
                {
                    spectrogram.Add(j == 0 ? 0 : spectrogramOutputArray[k * ((int) spectrogramOutputShape.x) + (j - 1)]);
                }
            }

            spectrogramShape.x = spectrogramOutputShape.x + 1;


            Ailia.AILIAShape attentionShape = ailiaText2Mel.GetBlobShape(attentionOutputBlobIndex);
            float[] attentionArray = new float[attentionShape.x * attentionShape.y * attentionShape.z];

            status = ailiaText2Mel.GetBlobData(attentionArray, attentionOutputBlobIndex);

            if (status == false)
            {
                Debug.LogError("Could not get attention output blob data ");
                Debug.LogError(ailiaText2Mel.GetErrorDetail());
                return new float[0];
            }

            int maxIndex = 0;
            float maxAttention = -1;

            for (int j = 0; j < attentionShape.y; ++j)
            {
                float attention = attentionArray[(j + 1) * attentionShape.x - 1];
            
                if (attention > maxAttention)
                {
                    maxIndex = j;
                    maxAttention = attention;
                }
            }

            if (inputArray[maxIndex] == endOfSentenceIndex)
            {
                Debug.Log($"End of text2mel inference at {i}");
                break;
            }
        }

        Debug.Log("DONE");

        return spectrogram.ToArray();
    }

    private float[] RunSSRInference(float[] inputArray)
    {
        bool status;

        int inputBlobIndex = ailiaSsr.FindBlobIndexByName("input.1");
        int outputBlobIndex = ailiaSsr.FindBlobIndexByName("output.2");
        Ailia.AILIAShape inputShape = new Ailia.AILIAShape
        {
            x = ((uint) inputArray.Length) / melSpectrogramSize,
            y = melSpectrogramSize,
            z = 1,
            w = 1,
            dim = 3
        };

        status = ailiaSsr.SetInputBlobShape(inputShape, inputBlobIndex);

        if (status == false)
        {
            Debug.LogError($"Could not set input blob shape");
            Debug.LogError(ailiaSsr.GetErrorDetail());
            return new float[0];
        }

        status = ailiaSsr.SetInputBlobData(inputArray, inputBlobIndex);

        if (status == false)
        {
            Debug.LogError("Could not set input blob data");
            Debug.LogError(ailiaSsr.GetErrorDetail());
            return new float[0];
        }

        status = ailiaSsr.Update();

        if (status == false)
        {
            Debug.LogError("Error while running ssrn inference");
            Debug.LogError(ailiaSsr.GetErrorDetail());
            return new float[0];
        }

        Ailia.AILIAShape outputShape = ailiaSsr.GetBlobShape(outputBlobIndex);
        float[] outputArray = new float[outputShape.x * outputShape.y * outputShape.z];

        status = ailiaSsr.GetBlobData(outputArray, outputBlobIndex);

        if (status == false)
        {
            Debug.LogError("Could not get output blob data ");
            Debug.LogError(ailiaSsr.GetErrorDetail());
            return new float[0];
        }

        return outputArray;
    }

    #region IDisposable Support
    private bool disposedValue = false; // To detect redundant calls

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                ailiaText2Mel.Close();
                ailiaSsr.Close();
                ailiaText2Mel = null;
                ailiaSsr = null;
            }

            disposedValue = true;
        }
    }

    // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
    ~AiliaTts()
    {
        // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        Dispose(false);
    }

    // This code added to correctly implement the disposable pattern.
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        Dispose(true);
        // TODO: uncomment the following line if the finalizer is overridden above.
        GC.SuppressFinalize(this);
    }
    #endregion
}
