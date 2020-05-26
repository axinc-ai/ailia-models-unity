using UnityEngine;

namespace ailiaSDK
{
	public class AiliaImageUtil
	{
		public enum Crop
		{
			Center,
			No
		}

		public static Rect GetCropRect(Texture texture, Crop crop)
		{
			Rect rect = new Rect();
			float shortside = texture.width < texture.height ? texture.width : texture.height;
			switch (crop)
			{
				case Crop.Center:
					rect = new Rect((texture.width - shortside) * 0.5f, (texture.height - shortside) * 0.5f, shortside, shortside);
					break;
				case Crop.No:
					rect = new Rect(0, 0, texture.width, texture.height);
					break;
				default:
					break;
			}
			return rect;
		}

		public static Rect GetCropRect(int sourceWidth, int sourceHeight, Crop crop)
		{
			Rect rect = new Rect();
			float shortside = sourceWidth < sourceHeight ? sourceWidth : sourceHeight;
			switch (crop)
			{
				case Crop.Center:
					rect = new Rect((sourceWidth - shortside) * 0.5f, (sourceHeight - shortside) * 0.5f, shortside, shortside);
					break;
				case Crop.No:
					rect = new Rect(0, 0, sourceWidth, sourceHeight);
					break;
				default:
					break;
			}
			return rect;
		}

		public static Color32[] GetPixels32(Texture2D texture, Rect cropRect, bool upsideDown = false)
		{
			if (cropRect.xMax < 0) cropRect.xMax = 0;
			if (cropRect.yMax < 0) cropRect.yMax = 0;
			if (cropRect.xMax > texture.width) cropRect.xMax = texture.width;
			if (cropRect.yMax > texture.height) cropRect.yMax = texture.height;

			int length = (int)(cropRect.width * cropRect.height);
			var color32sBuffer = new Color32[length];

			var nativeArrayPixels = texture.GetRawTextureData<Color32>();
			int yMin = (int)cropRect.yMin;
			int yMax = (int)cropRect.yMax;
			int xMin = (int)cropRect.xMin;
			int xMax = (int)cropRect.xMax;
			int destIndex = 0;
			if (upsideDown)
			{
				for (int j = yMax - 1; j >= yMin; j--)
				{
					int start = xMin + j * texture.width;
					int end = xMax + j * texture.width;
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
					int start = xMin + j * texture.width;
					int end = xMax + j * texture.width;
					for (int i = start; i < end; i++)
					{
						color32sBuffer[destIndex] = nativeArrayPixels[i];
						destIndex++;
					}
				}
			}
			return color32sBuffer;
		}

		public static Texture2D ResizeTexture(Texture2D texture, int width, int height)
		{
			var rt = RenderTexture.GetTemporary(width, height);
			Graphics.Blit(texture, rt);

			var currentRT = RenderTexture.active;
			RenderTexture.active = rt;
			var result = new Texture2D(width, height, texture.format, false);
			result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
			result.Apply();

			RenderTexture.active = currentRT;
			RenderTexture.ReleaseTemporary(rt);

			return result;
		}

		public static Color32[] CreatePalette(int num, byte alpha = 255)
		{
			Color32[] palette = new Color32[num];

			for (int j = 0; j < palette.Length; j++)
			{
				int lab = j;
				int i = 0;
				while (lab != 0)
				{
					palette[j].r |= (byte)(((lab >> 0) & 1) << (7 - i));
					palette[j].g |= (byte)(((lab >> 1) & 1) << (7 - i));
					palette[j].b |= (byte)(((lab >> 2) & 1) << (7 - i));
					i += 1;
					lab >>= 3;
				}
				palette[j].a = alpha;
			}
			return palette;
		}
	}
}