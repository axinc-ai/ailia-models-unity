using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class TexturePreprocessor
{
    private static Material blitMaterial;
    static readonly int _VertTransform = Shader.PropertyToID("_VertTransform");
    static readonly int _UVRect = Shader.PropertyToID("_UVRect");
    public static readonly Matrix4x4 PUSH_MATRIX = Matrix4x4.Translate(new Vector3(0.5f, 0.5f, 0));
    public static readonly Matrix4x4 POP_MATRIX = Matrix4x4.Translate(new Vector3(-0.5f, -0.5f, 0));

    public static Texture2D PreprocessTexture(Texture2D texture, RenderTexture buffer, Vector2 outputDimensions, Matrix4x4? cropMatrix = null, bool? fill = false)
    {
        int width = (int) outputDimensions.x;
        int height = (int) outputDimensions.y;

        Graphics.Blit(Texture2D.blackTexture, buffer);

        if (blitMaterial == null)
        {
            blitMaterial = new Material(Shader.Find("Hidden/TFLite/Resize"));
        }

        blitMaterial.SetMatrix(_VertTransform, (cropMatrix ?? PUSH_MATRIX * Matrix4x4.Translate(Vector3.zero) * POP_MATRIX));

        float srcAspect = (float) texture.width / texture.height;
        float dstAspect = outputDimensions.x / outputDimensions.y;
        
        if (fill == null)
        {
            blitMaterial.SetVector(_UVRect, new Vector4(1, 1, 0, 0));
        }
        else
        {
            if ((srcAspect > dstAspect) ^ fill.Value)
            {
                float ratio = srcAspect / dstAspect;
                blitMaterial.SetVector(_UVRect, new Vector4(1, ratio, 0, (1 - ratio) / 2));
            }
            else
            {
                float ratio = dstAspect / srcAspect;
                blitMaterial.SetVector(_UVRect, new Vector4(ratio, 1, (1 - ratio) / 2, 0));
            }
        }

        Graphics.Blit(texture, buffer, blitMaterial, 0);

        Texture2D outputTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = buffer;
        outputTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        outputTexture.Apply();
        RenderTexture.active = previous;

#if UNITY_EDITOR
		// Encode texture into PNG
		// For testing purposes, also write to a file in the project folder
		//byte[] bytes = outputTexture.EncodeToPNG();
		//File.WriteAllBytes(Application.dataPath + $"/../TexturePreprocessor_{outputDimensions.x}x{outputDimensions.y}.png", bytes);
#endif

		return outputTexture;
    }
}
