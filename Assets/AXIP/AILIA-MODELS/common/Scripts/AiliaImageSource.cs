using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using static ailiaSDK.AiliaImageUtil;

namespace ailiaSDK
{
	public class AiliaImageSource : MonoBehaviour
	{
		Texture2D _texture;
		Color32[] color32sBuffer = new Color32[0];
		Texture2D textureBuffer;

		public bool IsPrepared { get; private set; }
		public int Width { get { return _texture.width; } }
		public int Height { get { return _texture.height; } }

		private void Awake()
		{
			_texture = new Texture2D(0, 0, TextureFormat.RGBA32, false);
			textureBuffer = new Texture2D(0, 0, TextureFormat.RGBA32, false);
		}

		public void CreateSource(string URL)
		{
			StartCoroutine(GetTexture(URL));
		}

		public void CreateSource(Texture2D texture)
		{
			if (texture.format == TextureFormat.RGBA32)
			{
				_texture = texture;
			}
			else
			{
				_texture = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
				_texture.SetPixels32(texture.GetPixels32());
				_texture.Apply();
			}
			IsPrepared = true;
		}

		public void Resize(int width, int height)
		{
			_texture = ResizeTexture(_texture, width, height);
		}

		IEnumerator GetTexture(string URL)
		{
			IsPrepared = false;
			if (URL.StartsWith("file://"))
			{
				GetTextureDisk(URL.Remove(0, 7));
			}
			else
			{
				yield return GetTextureWWW(URL);
			}
		}

		IEnumerator GetTextureWWW(string URL)
		{
			UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(URL);
			yield return webRequest.SendWebRequest();

			if (webRequest.isNetworkError || webRequest.isHttpError)
			{
				Debug.LogError(webRequest.error, this);
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

		void GetTextureDisk(string filePath)
		{
			if (!File.Exists(filePath))
			{
				Debug.Log("file not exists \"" + filePath + "\"");
				return;
			}

			Texture2D loadTex = new Texture2D(0, 0);
			using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
			{
				var data = new byte[fs.Length];
				fs.Read(data, 0, data.Length);
				if (!loadTex.LoadImage(data))
				{
					Debug.Log("unkown file \"" + filePath + "\"");
					return;
				}
			}
			_texture = new Texture2D(loadTex.width, loadTex.height, TextureFormat.RGBA32, false);
			_texture.SetPixels32(loadTex.GetPixels32());
			_texture.Apply();
			IsPrepared = true;
		}

		public Rect GetCropRect(Crop crop)
		{
			if (!IsPrepared) return Rect.zero;
			return AiliaImageUtil.GetCropRect(_texture, crop);
		}

		public Color32[] GetPixels32(Crop crop, bool upsideDown = false)
		{
			return GetPixels32(GetCropRect(crop), upsideDown);
		}

		public Color32[] GetPixels32(Rect cropRect, bool upsideDown = false)
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

			if (upsideDown)
			{
				for (int j = yMax - 1; j >= yMin; j--)
				{
					int start = xMin + j * _texture.width;
					int end = xMax + j * _texture.width;
					for (int i = start; i < end; i++)
					{
						color32sBuffer[destIndex] = nativeArrayPixels[i];
						destIndex++;
					}
				}
			}
			else
			{
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
			}
			return color32sBuffer;
		}

		public Texture2D GetTexture(Crop crop)
		{
			return GetTexture(GetCropRect(crop));
		}
		public Texture2D GetTexture(Rect cropRect)
		{
			if (!IsPrepared) return null;

			if (cropRect.xMax < 0) cropRect.xMax = 0;
			if (cropRect.yMax < 0) cropRect.yMax = 0;
			if (cropRect.xMax > Width) cropRect.xMax = Width;
			if (cropRect.yMax > Height) cropRect.yMax = Height;

			int length = (int)(cropRect.width * cropRect.height);

			if (textureBuffer.width != cropRect.width || textureBuffer.width != cropRect.height)
			{
				textureBuffer = new Texture2D((int)cropRect.width, (int)cropRect.height, TextureFormat.RGBA32, false);
			}

			Graphics.CopyTexture(
				src: _texture,
				srcElement: 0,
				srcMip: 0,
				srcX: (int)cropRect.xMin,
				srcY: (int)cropRect.yMin,
				srcWidth: (int)cropRect.width,
				srcHeight: (int)cropRect.height,
				dst: textureBuffer,
				dstElement: 0,
				dstMip: 0,
				dstX: 0,
				dstY: 0);

			return textureBuffer;
		}
	}
}
