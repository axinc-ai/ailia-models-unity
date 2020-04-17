# ailia-models-unity

The collection of pre-trained, state-of-the-art models for Unity.

[ailia models (Python version)](https://github.com/axinc-ai/ailia-models)

## About ailia SDK

ailia SDK is a cross-platform high speed inference SDK. The ailia SDK provides a consistent C++ API on Windows, Mac, Linux, iOS, Android and Jetson. It supports Unity, Python and JNI for efficient AI implementation. The ailia SDK makes great use of the GPU from Vulkan and Metal to serve accelerated computing.

You can download a free evaluation version that allows you to evaluate the ailia SDK for 30 days. Please download from the trial link below.

https://ailia.jp/en/

## Notice

This repository does not include ailia libraries.

So you must get license and import ailia libraries to Plugin folder.

## Develop Environment

- Windows, Mac
- Unity 2017.4.30f1

## Target Environment

- Windows, Mac, iOS, Android, Linux

# Supporting Models

We are now converting to C#. Please wait to complete conversion.

## Image classification

| Name | Detail | Exported From | Supported Ailia Version |
|:-----------|------------:|:------------:|:------------:|
| [resnet50](/Assets/AXIP/AILIA-MODELS/resnet50/) | [Deep Residual Learning for Image Recognition]( https://github.com/KaimingHe/deep-residual-networks) | Chainer | 1.2.0 and later |

## Object detection

| Name | Detail | Exported From | Supported Ailia Version |
|:-----------|------------:|:------------:|:------------:|
| [yolov3-tiny](/Assets/AXIP/AILIA-MODELS/yolov3-tiny/) | [YOLO: Real-Time Object Detection](https://pjreddie.com/darknet/yolo/) | ONNX Runtime | 1.2.1 and later |
| [yolov3-face](/Assets/AXIP/AILIA-MODELS/yolov3-face/) | [Face detection using keras-yolov3](https://github.com/axinc-ai/yolov3-face) | Keras | 1.2.1 and later |

## Pose estimation

| Name | Detail | Exported From | Supported Ailia Version |
|:-----------|------------:|:------------:|:------------:|
|[lightweight-human-pose-estimation](/Assets/AXIP/AILIA-MODELS/lightweight-human-pose-estimation/) | [Fast and accurate human pose estimation in PyTorch. Contains implementation of "Real-time 2D Multi-Person Pose Estimation on CPU: Lightweight OpenPose" paper.](https://github.com/Daniil-Osokin/lightweight-human-pose-estimation.pytorch) | Pytorch | 1.2.1 and later |

## Face recognization

| Name | Detail | Exported From | Supported Ailia Version |
|:-----------|------------:|:------------:|:------------:|
|[face_classification](/Assets/AXIP/AILIA-MODELS/yolov3-face) | [Real-time face detection and emotion/gender classification](https://github.com/oarriaga/face_classification) | Keras | 1.1.0 and later |
|[vggface2](/Assets/AXIP/AILIA-MODELS/vggface2) | [VGGFace2 Dataset for Face Recognition](https://github.com/ox-vgg/vgg_face2) | Caffe | 1.1.0 and later |
