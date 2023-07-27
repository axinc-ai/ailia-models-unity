# ailia-models-unity

The collection of pre-trained, state-of-the-art models for Unity.

[<img src="Demo/colorization.png" width=512px>](/Assets/AXIP/AILIA-MODELS/ImageManipulation/)

# How to use

[ailia MODELS Unity tutorial](TUTORIAL.md)

# Supporting Models

## Audio processing

| Name | Detail | Exported From | Supported Ailia Version |
|:-----------|------------:|:------------:|:------------:|
| [silero_vad](/Assets/AXIP/AILIA-MODELS/AudioProcessing/) | [Silero VAD](https://github.com/snakers4/silero-vad) | Pytorch | 1.2.15 and later |

## Depth estimation

| Name | Detail | Exported From | Supported Ailia Version |
|:-----------|------------:|:------------:|:------------:|
| [midas](/Assets/AXIP/AILIA-MODELS/DepthEstimation/) | [Towards Robust Monocular Depth Estimation:<br/> Mixing Datasets for Zero-shot Cross-dataset Transfer](https://github.com/intel-isl/MiDaS) | Pytorch | 1.2.4 and later |

## Foundation

| Name | Detail | Exported From | Supported Ailia Version |
|:-----------|------------:|:------------:|:------------:|
| [detic](/Assets/AXIP/AILIA-MODELS/Foundation/) | [Detecting Twenty-thousand Classes using Image-level Supervision](https://github.com/facebookresearch/Detic) | Pytorch | 1.2.14 and later |

## Hand recognition

| Name | Detail | Exported From | Supported Ailia Version |
|:-----------|------------:|:------------:|:------------:|
| [blazehand](/Assets/AXIP/AILIA-MODELS/HandDetection/) | [MediaPipePyTorch](https://github.com/zmurez/MediaPipePyTorch) | Pytorch | 1.2.5 and later |

## Image classification

| Name | Detail | Exported From | Supported Ailia Version |
|:-----------|------------:|:------------:|:------------:|
| [googlenet](/Assets/AXIP/AILIA-MODELS/ImageClassification/) |[Going Deeper with Convolutions]( https://arxiv.org/abs/1409.4842 )|Pytorch| 1.2.0 and later|
| [resnet50](/Assets/AXIP/AILIA-MODELS/ImageClassification/) | [Deep Residual Learning for Image Recognition]( https://github.com/KaimingHe/deep-residual-networks) | Chainer | 1.2.0 and later |
| [inceptionv3](/Assets/AXIP/AILIA-MODELS/ImageClassification/)|[Rethinking the Inception Architecture for Computer Vision](http://arxiv.org/abs/1512.00567)|Pytorch| 1.2.0 and later |
| [mobilenetv2](/Assets/AXIP/AILIA-MODELS/ImageClassification/)|[PyTorch Implemention of MobileNet V2](https://github.com/d-li14/mobilenetv2.pytorch)|Pytorch| 1.2.0 and later |
| [mobilenetv3](/Assets/AXIP/AILIA-MODELS/ImageClassification/)|[PyTorch Implemention of MobileNet V3](https://github.com/d-li14/mobilenetv3.pytorch)|Pytorch| 1.2.1 and later |
| [partialconv](/Assets/AXIP/AILIA-MODELS/ImageClassification/)|[Partial Convolution Layer for Padding and Image Inpainting](https://github.com/NVIDIA/partialconv)|Pytorch| 1.2.0 and later |

## Image deformation

| Name | Detail | Exported From | Supported Ailia Version |
|:-----------|------------:|:------------:|:------------:|
| [dewarpnet](/Assets/AXIP/AILIA-MODELS/ImageDeformation/) | [DewarpNet: Single-Image Document Unwarping With Stacked 3D and 2D Regression Networks](https://github.com/cvlab-stonybrook/DewarpNet) | Pytorch | 1.2.1 and later |

## Image segmentation

| Name | Detail | Exported From | Supported Ailia Version |
|:-----------|------------:|:------------:|:------------:|
| [deeplabv3](/Assets/AXIP/AILIA-MODELS/ImageSegmentation/) | [Xception65 for backbone network of DeepLab v3+](https://github.com/tensorflow/models/tree/master/research/deeplab) | Chainer | 1.2.0 and later |
| [hrnet_segmentation](/Assets/AXIP/AILIA-MODELS/ImageSegmentation/) | [High-resolution networks (HRNets) for Semantic Segmentation](https://github.com/HRNet/HRNet-Semantic-Segmentation) | Pytorch | 1.2.1 and later |
| [hair_segmentation](/Assets/AXIP/AILIA-MODELS/ImageSegmentation/) | [hair segmentation in mobile device](https://github.com/thangtran480/hair-segmentation) | Keras | 1.2.1 and later |
| [pspnet-hair-segmentation](/Assets/AXIP/AILIA-MODELS/ImageSegmentation/) | [pytorch-hair-segmentation](https://github.com/YBIGTA/pytorch-hair-segmentation) | Pytorch | 1.2.2 and later |
| [U2net](/Assets/AXIP/AILIA-MODELS/ImageSegmentation/) | [U2-Net: Going Deeper with Nested U-Structure for Salient Object Detection](https://github.com/xuebinqin/U-2-Net) | Pytorch | 1.2.2 and later |

## Image manipulation

| Name | Detail | Exported From | Supported Ailia Version |
|:-----------|------------:|:------------:|:------------:|
| [noise2noise](/Assets/AXIP/AILIA-MODELS/ImageManipulation/) | [Learning Image Restoration without Clean Data](https://github.com/joeylitalien/noise2noise-pytorch) | Pytorch | 1.2.0 and later |
| [illnet](/Assets/AXIP/AILIA-MODELS/ImageManipulation/) | [Document Rectification and Illumination Correction using a Patch-based CNN](https://github.com/xiaoyu258/DocProj) | Pytorch | 1.2.2 and later |
| [colorization](/Assets/AXIP/AILIA-MODELS/ImageManipulation/) | [Colorful Image Colorization](https://github.com/richzhang/colorization) | Pytorch | 1.2.2 and later |

## Object detection

| Name | Detail | Exported From | Supported Ailia Version |
|:-----------|------------:|:------------:|:------------:|
| [yolov1-tiny](/Assets/AXIP/AILIA-MODELS/ObjectDetection/) | [YOLO: Real-Time Object Detection](https://pjreddie.com/darknet/yolov1/) | Darknet | 1.1.0 and later |
| [yolov1-face](/Assets/AXIP/AILIA-MODELS/ObjectDetection/) | [YOLO-Face-detection](https://github.com/dannyblueliu/YOLO-Face-detection/) | Darknet | 1.1.0 and later |
| [yolov2](/Assets/AXIP/AILIA-MODELS/ObjectDetection/) | [YOLO: Real-Time Object Detection](https://pjreddie.com/darknet/yolo/) | Pytorch | 1.2.0 and later |
| [yolov2-tiny](/Assets/AXIP/AILIA-MODELS/ObjectDetection/) | [YOLO: Real-Time Object Detection](https://pjreddie.com/darknet/yolo/) | Pytorch | 1.2.0 and later |
| [yolov3](/Assets/AXIP/AILIA-MODELS/ObjectDetection/) | [YOLO: Real-Time Object Detection](https://pjreddie.com/darknet/yolo/) | ONNX Runtime | 1.2.1 and later |
| [yolov3-tiny](/Assets/AXIP/AILIA-MODELS/ObjectDetection/) | [YOLO: Real-Time Object Detection](https://pjreddie.com/darknet/yolo/) | ONNX Runtime | 1.2.1 and later |
| [yolov3-face](/Assets/AXIP/AILIA-MODELS/ObjectDetection/) | [Face detection using keras-yolov3](https://github.com/axinc-ai/yolov3-face) | Keras | 1.2.1 and later |
| [yolov3-hand](/Assets/AXIP/AILIA-MODELS/ObjectDetection/) | [Hand detection branch of Face detection using keras-yolov3](https://github.com/axinc-ai/yolov3-face/tree/hand_detection) | Keras | 1.2.1 and later |
| [yolov4](/Assets/AXIP/AILIA-MODELS/ObjectDetection/) | [YOLO: Real-Time Object Detection](https://pjreddie.com/darknet/yolo/) | Pytorch | 1.2.7 and later |
| [yolov4-tiny](/Assets/AXIP/AILIA-MODELS/ObjectDetection/) | [YOLO: Real-Time Object Detection](https://pjreddie.com/darknet/yolo/) | Pytorch | 1.2.7 and later |
| [yolox](/Assets/AXIP/AILIA-MODELS/ObjectDetection/) | [YOLOX](https://github.com/Megvii-BaseDetection/YOLOX) | Pytorch | 1.2.9 and later |
| [mobilenet_ssd](/Assets/AXIP/AILIA-MODELS/ObjectDetection/) | [MobileNetV1, MobileNetV2, VGG based SSD/SSD-lite implementation in Pytorch](https://github.com/qfgaohao/pytorch-ssd) | Pytorch | 1.2.1 and later |

## Pose estimation

| Name | Detail | Exported From | Supported Ailia Version |
|:-----------|------------:|:------------:|:------------:|
| [lightweight-human-pose-estimation](/Assets/AXIP/AILIA-MODELS/PoseEstimation/) | [Fast and accurate human pose estimation in PyTorch. Contains implementation of "Real-time 2D Multi-Person Pose Estimation on CPU: Lightweight OpenPose" paper.](https://github.com/Daniil-Osokin/lightweight-human-pose-estimation.pytorch) | Pytorch | 1.2.1 and later |
| [blazepose-fullbody](/Assets/AXIP/AILIA-MODELS/PoseEstimation/) | [MediaPipe](https://google.github.io/mediapipe/solutions/models.html#pose) | TensorFlow Lite | 1.2.5 and later |

## Style transfer

| Name | Detail | Exported From | Supported Ailia Version |
|:-----------|------------:|:------------:|:------------:|
| [adain](/Assets/AXIP/AILIA-MODELS/StyleTransfer/) | [Arbitrary Style Transfer in Real-time with Adaptive Instance Normalization](https://github.com/naoto0804/pytorch-AdaIN)| Pytorch | 1.2.1 and later |

## Super resolution

| Name | Detail | Exported From | Supported Ailia Version |
|:-----------|------------:|:------------:|:------------:|
| [srresnet](/Assets/AXIP/AILIA-MODELS/SuperResolution/) | [Photo-Realistic Single Image Super-Resolution Using a Generative Adversarial Network](https://github.com/twtygqyy/pytorch-SRResNet) | Pytorch | 1.2.0 and later |
| [real-esrgan](/Assets/AXIP/AILIA-MODELS/SuperResolution/) | [Real-ESRGAN](https://github.com/xinntao/Real-ESRGAN) | Pytorch | 1.2.9 and later |

## Face detection

| Name | Detail | Exported From | Supported Ailia Version |
|:-----------|------------:|:------------:|:------------:|
|[blazeface](/Assets/AXIP/AILIA-MODELS/FaceDetection/) | [BlazeFace-PyTorch](https://github.com/hollance/BlazeFace-PyTorch) | Pytorch | 1.2.1 and later |
|[facemesh](/Assets/AXIP/AILIA-MODELS/FaceDetection/) | [facemesh.pytorch](https://github.com/thepowerfuldeez/facemesh.pytorch) | Pytorch | 1.2.2 and later |

## Face identification

| Name | Detail | Exported From | Supported Ailia Version |
|:-----------|------------:|:------------:|:------------:|
|[vggface2](/Assets/AXIP/AILIA-MODELS/FaceIdentification/) | [VGGFace2 Dataset for Face Recognition](https://github.com/ox-vgg/vgg_face2) | Caffe | 1.1.0 and later |
|[arcface](/Assets/AXIP/AILIA-MODELS/FaceIdentification/) | [pytorch implement of arcface](https://github.com/ronghuaiyang/arcface-pytorch) | Pytorch | 1.2.1 and later |

## Person ReID

| Name | Detail | Exported From | Supported Ailia Version |
|:-----------|------------:|:------------:|:------------:|
|[person_reid_baseline_pytorch](/Assets/AXIP/AILIA-MODELS/FaceIdentification/) | [UTS-Person-reID-Practical](https://github.com/layumi/Person_reID_baseline_pytorch) | Pytorch | 1.2.6 and later |

# Document

[ailia SDK Tutorial (Unity)](https://medium.com/axinc-ai/ailia-sdk-tutorial-unity-54f2a8155b8f)

[ailia Unity API](https://axinc-ai.github.io/ailia-sdk/api/unity/en/)

# Other languages

[python version](https://github.com/axinc-ai/ailia-models)

[c++ version](https://github.com/axinc-ai/ailia-models-cpp)
