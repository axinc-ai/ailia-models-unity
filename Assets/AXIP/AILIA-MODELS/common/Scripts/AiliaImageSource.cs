using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using static ailiaSDK.AiliaImageUtil;

namespace ailiaSDK
{
	public class AiliaImageSource : MonoBehaviour
	{
		public Texture2D _texture;
		Color32[] color32sBuffer = new Color32[0];

		public bool IsPrepared { get; private set; }
		public int Width { get { return _texture.width; } }
		public int Height { get { return _texture.height; } }

		private void Awake()
		{
			_texture = new Texture2D(0, 0);
		}

		public void CreateSource(string URL)
		{
			StartCoroutine(GetTexture(URL));
		}

		IEnumerator GetTexture(string URL)
		{
			IsPrepared = false;
			UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(URL);

			yield return webRequest.SendWebRequest();

			if (webRequest.isNetworkError || webRequest.isHttpError)
			{
				Debug.Log(webRequest.error);
			}
			else
			{
				var downloadTexture = (webRequest.downloadHandler as DownloadHandlerTexture).texture;
				if (downloadTexture.format != TextureFormat.RGBA32)
				{
					_texture = new Texture2D(downloadTexture.width, downloadTexture.height, TextureFormat.RGBA32, false);
					_texture.SetPixels(downloadTexture.GetPixels());
					_texture.Apply();
				}
				else
				{
					_texture = downloadTexture;
				}
				IsPrepared = true;
			}
		}

		public Rect GetCropRect(Crop crop)
		{
			if (!IsPrepared) return Rect.zero;
			return AiliaImageUtil.GetCropRect(_texture, crop);
		}

		public Color32[] GetPixels32(Crop crop)
		{
			return GetPixels32(GetCropRect(crop));
		}

		public Color32[] GetPixels32(Rect cropRect)
		{
			if (!IsPrepared) return null;

			if (cropRect.xMax < 0) cropRect.xMax = 0;
			if (cropRect.yMax < 0) cropRect.yMax = 0;
			if (cropRect.xMax > Width) cropRect.xMax = Width;
			if (cropRect.yMax > Height) cropRect.yMax = Height;

			int length = (int)(cropRect.width * cropRect.height);
			if (color32sBuffer.Length != length)
			{
				color32sBuffer = new Color32[length];
			}

			var nativeArrayPixels = _texture.GetRawTextureData<Color32>();
			int yMin = (int)cropRect.yMin;
			int yMax = (int)cropRect.yMax;
			int xMin = (int)cropRect.xMin;
			int xMax = (int)cropRect.xMax;
			int destIndex = 0;
			for (int j = yMin; j < yMax; j++)
			{
				int start = xMin + j * _texture.width;
				int end = xMax + j * _texture.width;
				for (int i = start; i < end; i++)
				{
					color32sBuffer[destIndex] = nativeArrayPixels[i];
					destIndex++;
				}
			}
			return color32sBuffer;
		}
	}
}