using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

namespace ailiaSDK
{
	public class AiliaModelsConst : MonoBehaviour
	{
		public enum AiliaModelTypes : int
		{
			[System.ComponentModel.Category("Image Classification"), System.ComponentModel.Description("vgg16")]
			vgg16,
			[System.ComponentModel.Category("Image Classification"), System.ComponentModel.Description("googlenet")]
			googlenet,
			[System.ComponentModel.Category("Image Classification"), System.ComponentModel.Description("resnet50")]
			resnet50,
			[System.ComponentModel.Category("Image Classification"), System.ComponentModel.Description("inceptionv3")]
			inceptionv3,
			[System.ComponentModel.Category("Image Classification"), System.ComponentModel.Description("mobilenetv2")]
			mobilenetv2,
			[System.ComponentModel.Category("Image Classification"), System.ComponentModel.Description("mobilenetv3")]
			mobilenetv3,
			[System.ComponentModel.Category("Image Classification"), System.ComponentModel.Description("partialconv")]
			partialconv,
			[System.ComponentModel.Category("Image Segmentation"), System.ComponentModel.Description("deeplabv3")]
			deeplabv3,
			[System.ComponentModel.Category("Image Segmentation"), System.ComponentModel.Description("hrnet_segmentation")]
			hrnet_segmentation,
			[System.ComponentModel.Category("Image Segmentation"), System.ComponentModel.Description("hair_segmentation")]
			hair_segmentation,
			[System.ComponentModel.Category("Image Manipulation"), System.ComponentModel.Description("srresnet")]
			srresnet,
			[System.ComponentModel.Category("Image Manipulation"), System.ComponentModel.Description("srresnet")]
			noise2noise,
			[System.ComponentModel.Category("Image Manipulation"), System.ComponentModel.Description("srresnet")]
			dewarpnet,
			[System.ComponentModel.Category("Object Detection"), System.ComponentModel.Description("yolov1-tiny")]
			yolov1_tiny,
			[System.ComponentModel.Category("Object Detection"), System.ComponentModel.Description("yolov1-face")]
			yolov1_face,
			[System.ComponentModel.Category("Object Detection"), System.ComponentModel.Description("yolov2")]
			yolov2,
			[System.ComponentModel.Category("Object Detection"), System.ComponentModel.Description("yolov3")]
			yolov3,
			[System.ComponentModel.Category("Object Detection"), System.ComponentModel.Description("yolov3-tiny")]
			yolov3_tiny,
			[System.ComponentModel.Category("Object Detection"), System.ComponentModel.Description("yolov3-face")]
			yolov3_face,
			[System.ComponentModel.Category("Object Detection"), System.ComponentModel.Description("yolov3-hand")]
			yolov3_hand,
			[System.ComponentModel.Category("Object Detection"), System.ComponentModel.Description("yolov4")]
			yolov4,
			[System.ComponentModel.Category("Object Detection"), System.ComponentModel.Description("yolov4-tiny")]
			yolov4_tiny,
			[System.ComponentModel.Category("Object Detection"), System.ComponentModel.Description("mobilenet_ssd")]
			mobilenet_ssd,
			[System.ComponentModel.Category("Object Detection"), System.ComponentModel.Description("maskrcnn")]
			maskrcnn,
			[System.ComponentModel.Category("Pose Estimation"), System.ComponentModel.Description("openpose")]
			openpose,
			[System.ComponentModel.Category("Pose Estimation"), System.ComponentModel.Description("lightweight-human-pose-estimation")]
			lightweight_human_pose_estimation,
			[System.ComponentModel.Category("Pose Estimation"), System.ComponentModel.Description("lightweight-human-pose-estimation-3d")]
			lightweight_human_pose_estimation_3d,
			[System.ComponentModel.Category("Gaze Estimation"), System.ComponentModel.Description("gazeml")]
			gazeml,
			[System.ComponentModel.Category("Face Recognization"), System.ComponentModel.Description("face_classification")]
			face_classification,
			[System.ComponentModel.Category("Face Recognization"), System.ComponentModel.Description("vggface2")]
			vggface2,
			[System.ComponentModel.Category("Face Recognization"), System.ComponentModel.Description("facial_feature")]
			facial_feature,
			[System.ComponentModel.Category("Face Recognization"), System.ComponentModel.Description("face_alignment")]
			face_alignment,
			[System.ComponentModel.Category("Face Recognization"), System.ComponentModel.Description("blazeface")]
			blazeface,
			[System.ComponentModel.Category("Face Recognization"), System.ComponentModel.Description("face_classification")]
			arcface,
			[System.ComponentModel.Category("Rotation Estimation"), System.ComponentModel.Description("rotnet")]
			rotnet,
			[System.ComponentModel.Category("Crowd Counting"), System.ComponentModel.Description("crowdcount-cascaded-mtl")]
			crowdcount_cascaded_mtl,
			[System.ComponentModel.Category("Depth Estimation"), System.ComponentModel.Description("monodepth2")]
			monodepth2,
			[System.ComponentModel.Category("Text Recognization"), System.ComponentModel.Description("etl")]
			etl,
			[System.ComponentModel.Category("Natural Language Processing"), System.ComponentModel.Description("bert")]
			bert
		}
		public static string[] MobilenetSSDPretrainedModel = new string[] { "mb1-ssd", "mb2-ssd-lite" };
		public static string[] Resnet50Model = new string[] { "resnet50.opt", "resnet50", "resnet50_pytorch" };
	}
	public static class AiliaModelExtensions
	{
		public static string GetCategory(this Enum value)
		{
			var field = value.GetType().GetField(value.ToString());
			var attribute = Attribute.GetCustomAttribute(field, typeof(CategoryAttribute)) as CategoryAttribute;

			if (attribute != null)
			{
				return attribute.Category;
			}
			return value.ToString();
		}

		public static string GetDescription(this Enum value)
		{
			var field = value.GetType().GetField(value.ToString());
			var attribute = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute;

			if (attribute != null)
			{
				return attribute.Description;
			}
			return value.ToString();
		}
	}
}