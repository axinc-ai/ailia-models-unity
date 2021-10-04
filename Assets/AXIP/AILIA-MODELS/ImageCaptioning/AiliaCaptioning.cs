using ailiaSDK;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

struct JsonVocabulary
{
    public string[] vocab;
}

public class AiliaCaptioning : IDisposable
{
    private AiliaDownload ailiaDownload;

    private AiliaModel ailiaCaptioningFeatures = new AiliaModel();
    private AiliaModel ailiaCaptioningInference = new AiliaModel();

    private string[] jsonDict;

    public bool gpuMode { get; private set; }

    public AiliaCaptioning(AiliaDownload ailiaDownload = null)
    {
        this.ailiaDownload = ailiaDownload ?? new AiliaDownload();
    }

    public IEnumerator Initialize(bool gpuMode, Action<bool> onCompleted)
    {
        this.gpuMode = gpuMode;
        string assetPath = Application.temporaryCachePath;
        string folderPath = "image_captioning_pytorch";
        string featuresModelName = "model_feat";
        string inferenceModelName = "model_fc";

        // captioning feature extraction
        if (gpuMode)
        {
            ailiaCaptioningFeatures.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
        }

        yield return ailiaDownload.DownloadWithProgressFromURL(new ModelDownloadURL[] {
            new ModelDownloadURL() { folder_path = folderPath, file_name = $"{featuresModelName}.onnx.prototxt" },
            new ModelDownloadURL() { folder_path = folderPath, file_name = $"{featuresModelName}.onnx" },
            new ModelDownloadURL() { folder_path = folderPath, file_name = $"{inferenceModelName}.onnx.prototxt" },
            new ModelDownloadURL() { folder_path = folderPath, file_name = $"{inferenceModelName}.onnx" }
        }.ToList(), () =>
        {
            bool status = false;

            status = ailiaCaptioningFeatures.OpenFile($"{assetPath}/{featuresModelName}.onnx.prototxt", $"{assetPath}/{featuresModelName}.onnx");

            if (status == false)
            {
                Debug.LogError($"Could not load model {featuresModelName} at '{assetPath}': {ailiaCaptioningFeatures.GetErrorDetail()}");
                onCompleted(false);
                return;
            }

            status = ailiaCaptioningInference.OpenFile($"{assetPath}/{inferenceModelName}.onnx.prototxt", $"{assetPath}/{inferenceModelName}.onnx");

            if (status == false)
            {
                Debug.LogError($"Could not load model {inferenceModelName} at '{assetPath}': {ailiaCaptioningInference.GetErrorDetail()}");
                onCompleted(false);
                return;
            }

            try
            {
                string dictJSON = File.ReadAllText(Application.streamingAssetsPath + "/AILIA/captioning_vocab.json");
                jsonDict = JsonUtility.FromJson<JsonVocabulary>("{ \"vocab\": " + dictJSON + " }").vocab;
            }
            catch (Exception e)
            {
                Debug.LogError($"Could not load the captioning vocabulary from JSON file: {e.Message}");
                onCompleted(false);
                return;
            }

            onCompleted(true);
            return;
        });
    }

    private string RunCaptioningInferenceFromFeatures(float[] features)
    {
        bool status;
        string assetPath = Application.streamingAssetsPath + "/AILIA";
        
        int inputBlobIndex = ailiaCaptioningInference.FindBlobIndexByName("fc_feats");

        status = ailiaCaptioningInference.SetInputBlobData(features, inputBlobIndex);

        if (status == false)
        {
            Debug.LogError("Could not set input blob data");
            return "";
        }

        bool result = ailiaCaptioningInference.Update();

        if (result == false)
        {
            Debug.Log(ailiaCaptioningInference.GetErrorDetail());
        }

        Debug.Log("[F] RAN TO COMPLETION, result: " + result);

        int outputBlobIndex = ailiaCaptioningInference.FindBlobIndexByName("seq");
        float[] outputArray = new float[20];

        status = ailiaCaptioningInference.GetBlobData(outputArray, outputBlobIndex);

        if (status == false)
        {
            Debug.LogError("Could not get output blob data " + outputBlobIndex);
            Debug.LogError(ailiaCaptioningInference.GetErrorDetail());
            return "";
        }

        Debug.Log(String.Join(" ", outputArray));
        int firstNullWordIndex = Array.FindIndex(outputArray, i => i == 0);
        firstNullWordIndex = (firstNullWordIndex < 0 ? outputArray.Length : firstNullWordIndex);
        return String.Join(" ", outputArray.Take(firstNullWordIndex).Select(i => (i > 0 ? jsonDict[((int)i) - 1] : "")).ToArray());
    }

    private float[] inputArray;
    private uint channelCount = 3;
    private uint featureCount = 2048;
    private uint inputWidth;
    private uint inputHeight;
    public void PreprocessTexture(Texture2D texture)
    {
        int maxInputWidth = 640;

        if (texture.width > maxInputWidth)
        {
            RenderTexture previousActiveTexture = RenderTexture.active;
            int newWidth = maxInputWidth;
            int newHeight = ((int)texture.height * maxInputWidth / texture.width);
            Vector2 scale = new Vector2((float)newWidth / texture.width, (float)newHeight / texture.height);
            Texture2D resizedTexture = new Texture2D(newWidth, newHeight, texture.format, false);
            RenderTexture.active = new RenderTexture(resizedTexture.width, resizedTexture.height, 32);
            Graphics.Blit(texture, RenderTexture.active, Vector2.one, Vector2.zero);
            resizedTexture.ReadPixels(new Rect { x = 0, y = 0, width = resizedTexture.width, height = resizedTexture.height }, 0, 0);
            resizedTexture.Apply();
            RenderTexture.active = previousActiveTexture;

            texture = resizedTexture;
        }


        inputWidth = ((uint)texture.width);
        inputHeight = ((uint)texture.height);
        
        float[] normalizedMean = new float[] { 0.485f, 0.456f, 0.406f };
        float[] normalizedStandardDeviation = new float[] { 0.229f, 0.224f, 0.225f };
        Color32[] colorData = texture.GetPixels32();

        Debug.Log("SIZE " + inputWidth + " " + inputHeight);

        inputArray = new float[inputWidth * inputHeight * channelCount];
        
        for (int heightIndex = 0; heightIndex < inputHeight; heightIndex++)
        {
            for (int widthIndex = 0; widthIndex < inputWidth; widthIndex++)
            {
                Color32 value = colorData[heightIndex * inputWidth + widthIndex];

                inputArray[((0 * inputHeight + heightIndex) * inputWidth) + widthIndex] = (((float)value.r) / 255 - normalizedMean[0]) / normalizedStandardDeviation[0];
                inputArray[((1 * inputHeight + heightIndex) * inputWidth) + widthIndex] = (((float)value.g) / 255 - normalizedMean[1]) / normalizedStandardDeviation[1];
                inputArray[((2 * inputHeight + heightIndex) * inputWidth) + widthIndex] = (((float)value.b) / 255 - normalizedMean[2]) / normalizedStandardDeviation[2];
            }
        }
    }

    public string InferCaptionFromFrame()
    {
        bool status;
        float[] outputArray = new float[featureCount];

        int inputBlobIndex = ailiaCaptioningFeatures.FindBlobIndexByName("img");

        status = ailiaCaptioningFeatures.SetInputBlobShape(
            new Ailia.AILIAShape
            {
                x = inputWidth,
                y = inputHeight,
                z = channelCount,
                w = 1,
                dim = 3
            },
            inputBlobIndex
        );

        if (status == false)
        {
            Debug.LogError("Could not set input blob shape");
            return "";
        }

        status = ailiaCaptioningFeatures.SetInputBlobData(inputArray, inputBlobIndex);

        if (status == false)
        {
            Debug.LogError("Could not set input blob data");
            return "";
        }

        Debug.Log($"INPUT\n{inputArray[0]} {inputArray[1]} {inputArray[2]} {inputArray[3]} {inputArray[4]}");

        bool result = ailiaCaptioningFeatures.Update();

        if (result == false)
        {
            Debug.Log(ailiaCaptioningFeatures.GetErrorDetail());
        }

        Debug.Log("RAN TO COMPLETION, result: " + result);

        int outputBlobIndex = ailiaCaptioningFeatures.FindBlobIndexByName("fc");

        status = ailiaCaptioningFeatures.GetBlobData(outputArray, outputBlobIndex);

        if (status == false)
        {
            Debug.LogError("Could not get output blob data " + outputBlobIndex);
            Debug.LogError(ailiaCaptioningFeatures.GetErrorDetail());
            return "";
        }

        return RunCaptioningInferenceFromFeatures(outputArray);
    }

#region IDisposable Support
    private bool disposedValue = false; // To detect redundant calls

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                ailiaCaptioningFeatures.Close();
                ailiaCaptioningInference.Close();
                ailiaCaptioningFeatures = null;
                ailiaCaptioningInference = null;
            }

            disposedValue = true;
        }
    }

    // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
    ~AiliaCaptioning()
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
