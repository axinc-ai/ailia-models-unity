using UnityEngine;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer))]
public class AiliaVideoSource : MonoBehaviour
{
	public enum Crop
	{
		Center
	}

	VideoPlayer videoPlayer;
	Texture2D _texture;
	Color32[] color32sBuffer = new Color32[0];

	public bool IsPrepared { get { return videoPlayer.isPrepared; } }
	public bool IsPlaying {  get { return videoPlayer.isPlaying; } }
	public uint Width { get { return videoPlayer.width; } }
	public uint Height { get { return videoPlayer.height; } }

	private void Awake()
	{
		videoPlayer = gameObject.GetComponent<VideoPlayer>();
		_texture = new Texture2D(0, 0);
	}

	public void CreateVideoSource(string URL, RenderTexture targetTexture = null, VideoPlayer.EventHandler prepareCompleteEvent = null)
	{
		if(videoPlayer != null) videoPlayer.Stop();

		videoPlayer.playOnAwake = false;
		videoPlayer.renderMode = targetTexture == null ? VideoRenderMode.APIOnly : VideoRenderMode.RenderTexture;
		videoPlayer.source = VideoSource.Url;
		videoPlayer.url = URL;
		videoPlayer.targetTexture = targetTexture;
		videoPlayer.aspectRatio = VideoAspectRatio.Stretch;
		if(prepareCompleteEvent != null)
		{
			videoPlayer.prepareCompleted += prepareCompleteEvent;
		}
		videoPlayer.Prepare();
	}

	public Rect GetCropRect(Crop crop)
	{
		if (!videoPlayer.isPrepared) return Rect.zero;

		Rect rect = new Rect();
		float shortside = videoPlayer.width < videoPlayer.height ? videoPlayer.width : videoPlayer.height;
		switch (crop)
		{
			case Crop.Center:
				rect = new Rect((videoPlayer.width - shortside) * 0.5f, (videoPlayer.height - shortside) * 0.5f, shortside, shortside);
				break;
			default:
				break;
		}
		return rect;
	}

	public Color32[] GetPixels32(Crop crop)
	{
		if (!videoPlayer.isPrepared) return null;
		return GetPixels32(GetCropRect(crop));
	}

	public Color32[] GetPixels32(Rect cropRect)
	{
		if (!videoPlayer.isPrepared) return null;

		RenderTexture rTexture = videoPlayer.texture as RenderTexture;
		if (cropRect.xMax < 0) cropRect.xMax = 0;
		if (cropRect.yMax < 0) cropRect.yMax = 0;
		if (cropRect.xMax > rTexture.width) cropRect.xMax = rTexture.width;
		if (cropRect.yMax > rTexture.height) cropRect.yMax = rTexture.height;

		if (cropRect.width != _texture.width || cropRect.height != _texture.height)
		{
			_texture = new Texture2D((int)cropRect.width, (int)cropRect.height, TextureFormat.RGBA32, false);
		}
		RenderTexture rt = RenderTexture.active;
		RenderTexture.active = rTexture;
		_texture.ReadPixels(cropRect, 0, 0);
		_texture.Apply();
		RenderTexture.active = rt;

		int length = _texture.width * _texture.height;
		if (color32sBuffer.Length != length)
		{
			color32sBuffer = new Color32[length];
		}

		var nativeArrayPixels = _texture.GetRawTextureData<Color32>();
		nativeArrayPixels.CopyTo(color32sBuffer);
		return color32sBuffer;
	}
	
	public bool Play()
	{
		if (!videoPlayer.isPrepared) return false;
		videoPlayer.Play();
		return true;
	}

	public void PlayOnReady()
	{
		if (!Play())
		{
			videoPlayer.prepareCompleted += (vp) => { vp.Play(); };
		}
	}

	public void Stop()
	{
		videoPlayer.Stop();
	}
	public void Pause()
	{
		videoPlayer.Pause();
	}

	public bool StepForward()
	{
		if (!videoPlayer.isPrepared) return false;
		videoPlayer.StepForward();
		return true;
	}
}
