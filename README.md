# ailia-models-unity

The collection of pre-trained, state-of-the-art models for Unity.

[<img src="Demo/colorization.png" width=512px>](/Assets/AXIP/AILIA-MODELS/ImageManipulation/)

# How to use

[ailia MODELS Unity tutorial](TUTORIAL.md)

# Supporting Models

## Audio processing

| Name | Detail | Exported From | Supported Ailia Version | Blog |
|:-----------|------------:|:------------:|:------------:|:------------:|
| [silero_vad](/Assets/AXIP/AILIA-MODELS/AudioProcessing/) | [Silero VAD](https://github.com/snakers4/silero-vad) | Pytorch | 1.2.15 and later | [JP](https://medium.com/axinc/silerovad-%E7%99%BA%E8%A9%B1%E5%8C%BA%E9%96%93%E3%82%92%E6%A4%9C%E5%87%BA%E3%81%99%E3%82%8B%E6%A9%9F%E6%A2%B0%E5%AD%A6%E7%BF%92%E3%83%A2%E3%83%87%E3%83%AB-2ad6cf395703) |
| [rvc](/Assets/AXIP/AILIA-MODELS/AudioProcessing/) | [Retrieval-based-Voice-Conversion-WebUI](https://github.com/RVC-Project/Retrieval-based-Voice-Conversion-WebUI) | Pytorch | 1.2.12 and later | [JP](https://medium.com/axinc/rvc-ai%E3%82%92%E4%BD%BF%E7%94%A8%E3%81%97%E3%81%9F%E3%83%9C%E3%82%A4%E3%82%B9%E3%83%81%E3%82%A7%E3%83%B3%E3%82%B8%E3%83%A3%E3%83%BC-64a813c7a0c4) |
| [whisper](/Assets/AXIP/AILIA-MODELS/AudioProcessing/) | [Whisper](https://github.com/openai/whisper) | Pytorch | 1.3.0 and later | [JP](https://medium.com/axinc/whisper-%E6%97%A5%E6%9C%AC%E8%AA%9E%E3%82%92%E5%90%AB%E3%82%8099%E8%A8%80%E8%AA%9E%E3%82%92%E8%AA%8D%E8%AD%98%E3%81%A7%E3%81%8D%E3%82%8B%E9%9F%B3%E5%A3%B0%E8%AA%8D%E8%AD%98%E3%83%A2%E3%83%87%E3%83%AB-b6e578f55c87) |

## Depth estimation

| Name | Detail | Exported From | Supported Ailia Version | Blog |
|:-----------|------------:|:------------:|:------------:|:------------:|
| [midas](/Assets/AXIP/AILIA-MODELS/DepthEstimation/) | [Towards Robust Monocular Depth Estimation:<br/> Mixing Datasets for Zero-shot Cross-dataset Transfer](https://github.com/intel-isl/MiDaS) | Pytorch | 1.2.4 and later | [EN](https://medium.com/axinc-ai/midas-a-machine-learning-model-for-depth-estimation-e96119cc1a3c) [JP](https://medium.com/axinc/midas-%E5%A5%A5%E8%A1%8C%E3%81%8D%E3%82%92%E6%8E%A8%E5%AE%9A%E3%81%99%E3%82%8B%E6%A9%9F%E6%A2%B0%E5%AD%A6%E7%BF%92%E3%83%A2%E3%83%87%E3%83%AB-71e65a041e0f) |

## Diffusion

| Name | Detail | Exported From | Supported Ailia Version | Blog |
|:-----------|------------:|:------------:|:------------:|:------------:|
| [latent-diffusion-inpainting](/Assets/AXIP/AILIA-MODELS/Diffusion/) | [Latent Diffusion - inpainting](https://github.com/CompVis/latent-diffusion) | Pytorch | 1.2.10 and later ||
| [latent-diffusion-superresolution](/Assets/AXIP/AILIA-MODELS/Diffusion/) | [Latent Diffusion - Super-resolution](https://github.com/CompVis/latent-diffusion) | Pytorch | 1.2.10 and later ||
| [stable-diffusion-txt2img](/Assets/AXIP/AILIA-MODELS/Diffusion//) | [Stable Diffusion](https://github.com/CompVis/stable-diffusion) | Pytorch | 1.2.14 and later ||

## Foundation

| Name | Detail | Exported From | Supported Ailia Version | Blog |
|:-----------|------------:|:------------:|:------------:|:------------:|
| [detic](/Assets/AXIP/AILIA-MODELS/Foundation/) | [Detecting Twenty-thousand Classes using Image-level Supervision](https://github.com/facebookresearch/Detic) | Pytorch | 1.2.14 and later | [EN](https://medium.com/p/49cba412b7d4) [JP](https://medium.com/axinc/detic-21k%E3%82%AF%E3%83%A9%E3%82%B9%E3%82%92%E9%AB%98%E7%B2%BE%E5%BA%A6%E3%81%AB%E3%82%BB%E3%82%B0%E3%83%A1%E3%83%B3%E3%83%86%E3%83%BC%E3%82%B7%E3%83%A7%E3%83%B3%E3%81%A7%E3%81%8D%E3%82%8B%E7%89%A9%E4%BD%93%E6%A4%9C%E5%87%BA%E3%83%A2%E3%83%87%E3%83%AB-1b8f777ee89a) |

## Generative adversarial networks

| Name | Detail | Exported From | Supported Ailia Version | Blog |
|:-----------|------------:|:------------:|:------------:|:------------:|
| [lipgan](/Assets/AXIP/AILIA-MODELS/GenerativeAdversarialNetworks/) | [LipGAN](https://github.com/Rudrabha/LipGAN) | Keras | 1.2.15 and later | [JP](https://medium.com/axinc/lipgan-%E3%83%AA%E3%83%83%E3%83%97%E3%82%B7%E3%83%B3%E3%82%AF%E5%8B%95%E7%94%BB%E3%82%92%E7%94%9F%E6%88%90%E3%81%99%E3%82%8B%E6%A9%9F%E6%A2%B0%E5%AD%A6%E7%BF%92%E3%83%A2%E3%83%87%E3%83%AB-57511508eaff) |
| [gfpgan](/Assets/AXIP/AILIA-MODELS/GenerativeAdversarialNetworks/) | [GFP-GAN: Towards Real-World Blind Face Restoration with Generative Facial Prior](https://github.com/TencentARC/GFPGAN) | Pytorch | 1.2.10 and later | [JP](https://medium.com/axinc/gfpgan-%E9%A1%94%E7%94%BB%E5%83%8F%E3%82%92%E9%AB%98%E7%94%BB%E8%B3%AA%E5%8C%96%E3%81%99%E3%82%8B%E6%A9%9F%E6%A2%B0%E5%AD%A6%E7%BF%92%E3%83%A2%E3%83%87%E3%83%AB-547acd717086) |

## Hand recognition

| Name | Detail | Exported From | Supported Ailia Version | Blog |
|:-----------|------------:|:------------:|:------------:|:------------:|
| [blazehand](/Assets/AXIP/AILIA-MODELS/HandDetection/) | [MediaPipePyTorch](https://github.com/zmurez/MediaPipePyTorch) | Pytorch | 1.2.5 and later | [EN](https://medium.com/axinc-ai/blazehand-a-machine-learning-model-for-detecting-hand-key-points-c3943b82739a) [JP](https://medium.com/axinc/blazehand-%E6%89%8B%E3%81%AE%E3%82%AD%E3%83%BC%E3%83%9D%E3%82%A4%E3%83%B3%E3%83%88%E3%82%92%E6%A4%9C%E5%87%BA%E3%81%99%E3%82%8B%E6%A9%9F%E6%A2%B0%E5%AD%A6%E7%BF%92%E3%83%A2%E3%83%87%E3%83%AB-e84e011ef7bc) |

## Image classification

| Name | Detail | Exported From | Supported Ailia Version | Blog |
|:-----------|------------:|:------------:|:------------:|:------------:|
| [googlenet](/Assets/AXIP/AILIA-MODELS/ImageClassification/) |[Going Deeper with Convolutions]( https://arxiv.org/abs/1409.4842 )|Pytorch| 1.2.0 and later||
| [resnet50](/Assets/AXIP/AILIA-MODELS/ImageClassification/) | [Deep Residual Learning for Image Recognition]( https://github.com/KaimingHe/deep-residual-networks) | Chainer | 1.2.0 and later ||
| [inceptionv3](/Assets/AXIP/AILIA-MODELS/ImageClassification/)|[Rethinking the Inception Architecture for Computer Vision](http://arxiv.org/abs/1512.00567)|Pytorch| 1.2.0 and later | [JP](https://medium.com/axinc/ailia-sdk-%E3%83%A2%E3%83%87%E3%83%AB%E7%B4%B9%E4%BB%8B-inceptionv3-b39dd43f285d) |
| [mobilenetv2](/Assets/AXIP/AILIA-MODELS/ImageClassification/)|[PyTorch Implemention of MobileNet V2](https://github.com/d-li14/mobilenetv2.pytorch)|Pytorch| 1.2.0 and later ||
| [mobilenetv3](/Assets/AXIP/AILIA-MODELS/ImageClassification/)|[PyTorch Implemention of MobileNet V3](https://github.com/d-li14/mobilenetv3.pytorch)|Pytorch| 1.2.1 and later ||
| [partialconv](/Assets/AXIP/AILIA-MODELS/ImageClassification/)|[Partial Convolution Layer for Padding and Image Inpainting](https://github.com/NVIDIA/partialconv)|Pytorch| 1.2.0 and later ||

## Image deformation

| Name | Detail | Exported From | Supported Ailia Version | Blog |
|:-----------|------------:|:------------:|:------------:|:------------:|
| [dewarpnet](/Assets/AXIP/AILIA-MODELS/ImageDeformation/) | [DewarpNet: Single-Image Document Unwarping With Stacked 3D and 2D Regression Networks](https://github.com/cvlab-stonybrook/DewarpNet) | Pytorch | 1.2.1 and later ||

## Image segmentation

| Name | Detail | Exported From | Supported Ailia Version | Blog |
|:-----------|------------:|:------------:|:------------:|:------------:|
| [deeplabv3](/Assets/AXIP/AILIA-MODELS/ImageSegmentation/) | [Xception65 for backbone network of DeepLab v3+](https://github.com/tensorflow/models/tree/master/research/deeplab) | Chainer | 1.2.0 and later ||
| [hrnet_segmentation](/Assets/AXIP/AILIA-MODELS/ImageSegmentation/) | [High-resolution networks (HRNets) for Semantic Segmentation](https://github.com/HRNet/HRNet-Semantic-Segmentation) | Pytorch | 1.2.1 and later ||
| [hair_segmentation](/Assets/AXIP/AILIA-MODELS/ImageSegmentation/) | [hair segmentation in mobile device](https://github.com/thangtran480/hair-segmentation) | Keras | 1.2.1 and later ||
| [pspnet-hair-segmentation](/Assets/AXIP/AILIA-MODELS/ImageSegmentation/) | [pytorch-hair-segmentation](https://github.com/YBIGTA/pytorch-hair-segmentation) | Pytorch | 1.2.2 and later ||
| [U2net](/Assets/AXIP/AILIA-MODELS/ImageSegmentation/) | [U2-Net: Going Deeper with Nested U-Structure for Salient Object Detection](https://github.com/xuebinqin/U-2-Net) | Pytorch | 1.2.2 and later | [EN](https://medium.com/axinc-ai/u2net-a-machine-learning-model-that-performs-object-cropping-in-a-single-shot-48adfc158483) [JP](https://medium.com/axinc/u2net-%E3%82%B7%E3%83%B3%E3%82%B0%E3%83%AB%E3%82%B7%E3%83%A7%E3%83%83%E3%83%88%E3%81%A7%E7%89%A9%E4%BD%93%E3%81%AE%E5%88%87%E3%82%8A%E6%8A%9C%E3%81%8D%E3%82%92%E8%A1%8C%E3%81%86%E6%A9%9F%E6%A2%B0%E5%AD%A6%E7%BF%92%E3%83%A2%E3%83%87%E3%83%AB-e346f2787cdb) |

## Image manipulation

| Name | Detail | Exported From | Supported Ailia Version | Blog |
|:-----------|------------:|:------------:|:------------:|:------------:|
| [noise2noise](/Assets/AXIP/AILIA-MODELS/ImageManipulation/) | [Learning Image Restoration without Clean Data](https://github.com/joeylitalien/noise2noise-pytorch) | Pytorch | 1.2.0 and later ||
| [illnet](/Assets/AXIP/AILIA-MODELS/ImageManipulation/) | [Document Rectification and Illumination Correction using a Patch-based CNN](https://github.com/xiaoyu258/DocProj) | Pytorch | 1.2.2 and later ||
| [colorization](/Assets/AXIP/AILIA-MODELS/ImageManipulation/) | [Colorful Image Colorization](https://github.com/richzhang/colorization) | Pytorch | 1.2.2 and later | [EN](https://medium.com/axinc-ai/colorization-a-machine-learning-model-for-colorizing-black-and-white-images-829e35e4f91c) [JP](https://medium.com/axinc/colorization-%E7%99%BD%E9%BB%92%E7%94%BB%E5%83%8F%E3%82%92%E3%82%AB%E3%83%A9%E3%83%BC%E5%8C%96%E3%81%99%E3%82%8B%E6%A9%9F%E6%A2%B0%E5%AD%A6%E7%BF%92%E3%83%A2%E3%83%87%E3%83%AB-177d3fd52e40) |

## Object detection

| Name | Detail | Exported From | Supported Ailia Version | Blog |
|:-----------|------------:|:------------:|:------------:|:------------:|
| [yolov1-tiny](/Assets/AXIP/AILIA-MODELS/ObjectDetection/) | [YOLO: Real-Time Object Detection](https://pjreddie.com/darknet/yolov1/) | Darknet | 1.1.0 and later ||
| [yolov1-face](/Assets/AXIP/AILIA-MODELS/ObjectDetection/) | [YOLO-Face-detection](https://github.com/dannyblueliu/YOLO-Face-detection/) | Darknet | 1.1.0 and later ||
| [yolov2](/Assets/AXIP/AILIA-MODELS/ObjectDetection/) | [YOLO: Real-Time Object Detection](https://pjreddie.com/darknet/yolo/) | Pytorch | 1.2.0 and later ||
| [yolov2-tiny](/Assets/AXIP/AILIA-MODELS/ObjectDetection/) | [YOLO: Real-Time Object Detection](https://pjreddie.com/darknet/yolo/) | Pytorch | 1.2.0 and later ||
| [yolov3](/Assets/AXIP/AILIA-MODELS/ObjectDetection/) | [YOLO: Real-Time Object Detection](https://pjreddie.com/darknet/yolo/) | ONNX Runtime | 1.2.1 and later | [EN](https://medium.com/axinc-ai/yolov3-a-machine-learning-model-to-detect-the-position-and-type-of-an-object-60f1c18f8107) [JP](https://medium.com/axinc/yolov3-66c9b998c096) |
| [yolov3-tiny](/Assets/AXIP/AILIA-MODELS/ObjectDetection/) | [YOLO: Real-Time Object Detection](https://pjreddie.com/darknet/yolo/) | ONNX Runtime | 1.2.1 and later ||
| [yolov3-face](/Assets/AXIP/AILIA-MODELS/ObjectDetection/) | [Face detection using keras-yolov3](https://github.com/axinc-ai/yolov3-face) | Keras | 1.2.1 and later ||
| [yolov3-hand](/Assets/AXIP/AILIA-MODELS/ObjectDetection/) | [Hand detection branch of Face detection using keras-yolov3](https://github.com/axinc-ai/yolov3-face/tree/hand_detection) | Keras | 1.2.1 and later ||
| [yolov4](/Assets/AXIP/AILIA-MODELS/ObjectDetection/) | [YOLO: Real-Time Object Detection](https://pjreddie.com/darknet/yolo/) | Pytorch | 1.2.7 and later ||
| [yolov4-tiny](/Assets/AXIP/AILIA-MODELS/ObjectDetection/) | [YOLO: Real-Time Object Detection](https://pjreddie.com/darknet/yolo/) | Pytorch | 1.2.7 and later ||
| [yolox](/Assets/AXIP/AILIA-MODELS/ObjectDetection/) | [YOLOX](https://github.com/Megvii-BaseDetection/YOLOX) | Pytorch | 1.2.9 and later | [EN](https://medium.com/axinc-ai/yolox-object-detection-model-exceeding-yolov5-d6cea6d3c4bc) [JP](https://medium.com/axinc/yolox-yolov5%E3%82%92%E8%B6%85%E3%81%88%E3%82%8B%E7%89%A9%E4%BD%93%E6%A4%9C%E5%87%BA%E3%83%A2%E3%83%87%E3%83%AB-e9706e15fef2) |
| [mobilenet_ssd](/Assets/AXIP/AILIA-MODELS/ObjectDetection/) | [MobileNetV1, MobileNetV2, VGG based SSD/SSD-lite implementation in Pytorch](https://github.com/qfgaohao/pytorch-ssd) | Pytorch | 1.2.1 and later ||

## Pose estimation

| Name | Detail | Exported From | Supported Ailia Version | Blog |
|:-----------|------------:|:------------:|:------------:|:------------:|
| [lightweight-human-pose-estimation](/Assets/AXIP/AILIA-MODELS/PoseEstimation/) | [Fast and accurate human pose estimation in PyTorch. Contains implementation of "Real-time 2D Multi-Person Pose Estimation on CPU: Lightweight OpenPose" paper.](https://github.com/Daniil-Osokin/lightweight-human-pose-estimation.pytorch) | Pytorch | 1.2.1 and later | [EN](https://medium.com/axinc-ai/lightweighthumanpose-a-machine-learning-model-for-fast-multi-person-skeleton-detection-631c042bed50) [JP](https://medium.com/axinc/lightweighthumanpose-%E9%AB%98%E9%80%9F%E3%81%AB%E8%A4%87%E6%95%B0%E4%BA%BA%E3%81%AE%E9%AA%A8%E6%A0%BC%E3%82%92%E6%A4%9C%E5%87%BA%E3%81%99%E3%82%8B%E6%A9%9F%E6%A2%B0%E5%AD%A6%E7%BF%92%E3%83%A2%E3%83%87%E3%83%AB-bc34d420e6e2) |
| [blazepose-fullbody](/Assets/AXIP/AILIA-MODELS/PoseEstimation/) | [MediaPipe](https://google.github.io/mediapipe/solutions/models.html#pose) | TensorFlow Lite | 1.2.5 and later ||
| [pose_resnet](/Assets/AXIP/AILIA-MODELS/PoseEstimation/) | [Simple Baselines for Human Pose Estimation and Tracking](https://github.com/microsoft/human-pose-estimation.pytorch) | Pytorch | 1.2.1 and later | [EN](https://medium.com/axinc-ai/poseresnet-a-top-down-machine-learning-model-for-skeletal-detection-9454f391ae4d) [JP](https://medium.com/axinc/poseresnet-%E3%83%88%E3%83%83%E3%83%97%E3%83%80%E3%82%A6%E3%83%B3%E3%81%A7%E9%AA%A8%E6%A0%BC%E6%A4%9C%E5%87%BA%E3%82%92%E8%A1%8C%E3%81%86%E6%A9%9F%E6%A2%B0%E5%AD%A6%E7%BF%92%E3%83%A2%E3%83%87%E3%83%AB-9e0d20396d1e) |

## Style transfer

| Name | Detail | Exported From | Supported Ailia Version | Blog |
|:-----------|------------:|:------------:|:------------:|:------------:|
| [adain](/Assets/AXIP/AILIA-MODELS/StyleTransfer/) | [Arbitrary Style Transfer in Real-time with Adaptive Instance Normalization](https://github.com/naoto0804/pytorch-AdaIN)| Pytorch | 1.2.1 and later | [EN](https://medium.com/axinc-ai/adain-a-machine-learning-model-for-style-transfer-341b242c554b) [JP](https://medium.com/axinc/adain-%E7%94%BB%E5%83%8F%E3%81%AE%E3%82%B9%E3%82%BF%E3%82%A4%E3%83%AB%E3%82%92%E5%A4%89%E6%8F%9B%E3%81%99%E3%82%8B%E6%A9%9F%E6%A2%B0%E5%AD%A6%E7%BF%92%E3%83%A2%E3%83%87%E3%83%AB-2443feba832b) |

## Super resolution

| Name | Detail | Exported From | Supported Ailia Version | Blog |
|:-----------|------------:|:------------:|:------------:|:------------:|
| [srresnet](/Assets/AXIP/AILIA-MODELS/SuperResolution/) | [Photo-Realistic Single Image Super-Resolution Using a Generative Adversarial Network](https://github.com/twtygqyy/pytorch-SRResNet) | Pytorch | 1.2.0 and later | [EN](https://medium.com/axinc-ai/srresnet-a-machine-learning-model-to-increase-image-resolution-9efc478f2674) [JP](https://medium.com/axinc/srresnet-%E7%94%BB%E5%83%8F%E3%82%92%E9%AB%98%E5%93%81%E8%B3%AA%E3%81%AB%E6%8B%A1%E5%A4%A7%E3%81%99%E3%82%8B%E6%A9%9F%E6%A2%B0%E5%AD%A6%E7%BF%92%E3%83%A2%E3%83%87%E3%83%AB-9e35b9a90586) |
| [real-esrgan](/Assets/AXIP/AILIA-MODELS/SuperResolution/) | [Real-ESRGAN](https://github.com/xinntao/Real-ESRGAN) | Pytorch | 1.2.9 and later |

## Text recognition

| Name | Detail | Exported From | Supported Ailia Version | Blog |
|:-----------|------------:|:------------:|:------------:|:------------:|
| [paddleocr](/Assets/AXIP/AILIA-MODELS/TextRecognition/) | [PaddleOCR : Awesome multilingual OCR toolkits based on PaddlePaddle](https://github.com/PaddlePaddle/PaddleOCR) | Pytorch | 1.2.6 and later | [EN](https://medium.com/axinc-ai/paddleocr-the-latest-lightweight-ocr-system-a13171d7ea3e) [JP](https://medium.com/axinc/paddleocr-%E6%9C%80%E6%96%B0%E3%81%AE%E8%BB%BD%E9%87%8Focr%E3%82%B7%E3%82%B9%E3%83%86%E3%83%A0-8744205f3703) |

## Text to Speech

| Name | Detail | Exported From | Supported Ailia Version | Blog |
|:-----------|------------:|:------------:|:------------:|:------------:|
| [tacotron2](/Assets/AXIP/AILIA-MODELS/TextToSpeech/) | [Tacotron2](https://github.com/NVIDIA/tacotron2) | Pytorch | 1.2.15 and later | [JP](https://medium.com/axinc/tacotron2-%E6%B3%A2%E5%BD%A2%E5%A4%89%E6%8F%9B%E3%82%92ai%E3%81%A7%E8%A1%8C%E3%81%86%E9%AB%98%E5%93%81%E8%B3%AA%E3%81%AA%E9%9F%B3%E5%A3%B0%E5%90%88%E6%88%90%E3%83%A2%E3%83%87%E3%83%AB-bc592217a399) |
| [gpt-sovits](/Assets/AXIP/AILIA-MODELS/TextToSpeech/) | [GPT-SoVITS](https://github.com/RVC-Boss/GPT-SoVITS) | Pytorch | 1.4.0 and later | [JP](https://medium.com/axinc/gpt-sovits-%E3%83%95%E3%82%A1%E3%82%A4%E3%83%B3%E3%83%81%E3%83%A5%E3%83%BC%E3%83%8B%E3%83%B3%E3%82%B0%E3%81%A7%E3%81%8D%E3%82%8B0%E3%82%B7%E3%83%A7%E3%83%83%E3%83%88%E3%81%AE%E9%9F%B3%E5%A3%B0%E5%90%88%E6%88%90%E3%83%A2%E3%83%87%E3%83%AB-2212eeb5ad20) |

## Face detection

| Name | Detail | Exported From | Supported Ailia Version | Blog |
|:-----------|------------:|:------------:|:------------:|:------------:|
|[blazeface](/Assets/AXIP/AILIA-MODELS/FaceDetection/) | [BlazeFace-PyTorch](https://github.com/hollance/BlazeFace-PyTorch) | Pytorch | 1.2.1 and later | [EN](https://medium.com/axinc-ai/blazeface-a-machine-learning-model-for-fast-detection-of-face-positions-and-key-points-5dcfb9429d72) [JP](https://medium.com/axinc/blazeface-%E9%A1%94%E3%81%AE%E4%BD%8D%E7%BD%AE%E3%81%A8%E3%82%AD%E3%83%BC%E3%83%9D%E3%82%A4%E3%83%B3%E3%83%88%E3%82%92%E9%AB%98%E9%80%9F%E3%81%AB%E6%A4%9C%E5%87%BA%E3%81%99%E3%82%8B%E6%A9%9F%E6%A2%B0%E5%AD%A6%E7%BF%92%E3%83%A2%E3%83%87%E3%83%AB-e851c348a32b) |
|[facemesh](/Assets/AXIP/AILIA-MODELS/FaceDetection/) | [facemesh.pytorch](https://github.com/thepowerfuldeez/facemesh.pytorch) | Pytorch | 1.2.2 and later | [EN](https://medium.com/axinc-ai/facemesh-detecting-key-points-on-faces-in-real-time-977c03f1bab) [JP](https://medium.com/axinc/facemesh-%E3%83%AA%E3%82%A2%E3%83%AB%E3%82%BF%E3%82%A4%E3%83%A0%E3%81%A7%E9%A1%94%E3%81%AE%E3%82%AD%E3%83%BC%E3%83%9D%E3%82%A4%E3%83%B3%E3%83%88%E3%82%92%E6%A4%9C%E5%87%BA%E3%81%99%E3%82%8B%E6%A9%9F%E6%A2%B0%E5%AD%A6%E7%BF%92%E3%83%A2%E3%83%87%E3%83%AB-bf223a50b7d6) |
|[facemesh_v2](/Assets/AXIP/AILIA-MODELS/FaceDetection/) | [MediaPipe Face landmark detection](https://developers.google.com/mediapipe/solutions/vision/face_landmarker)| Pytorch | 1.2.9 and later | [JP](https://medium.com/axinc/facemeshv2-blendshape%E3%82%82%E8%A8%88%E7%AE%97%E5%8F%AF%E8%83%BD%E3%81%AA%E9%A1%94%E3%81%AE%E3%82%AD%E3%83%BC%E3%83%9D%E3%82%A4%E3%83%B3%E3%83%88%E6%A4%9C%E5%87%BA%E3%83%A2%E3%83%87%E3%83%AB-3198898dccdd) |
| [retinaface](/Assets/AXIP/AILIA-MODELS/FaceDetection/) | [RetinaFace: Single-stage Dense Face Localisation in the Wild.](https://github.com/biubug6/Pytorch_Retinaface) | Pytorch | 1.2.5 and later | [JP](https://medium.com/axinc/retinaface-%E9%AB%98%E8%A7%A3%E5%83%8F%E5%BA%A6%E3%81%AB%E5%AF%BE%E5%BF%9C%E3%81%97%E3%81%9F%E9%A1%94%E6%A4%9C%E5%87%BA%E3%83%A2%E3%83%87%E3%83%AB-37d0807581ce) |

## Face identification

| Name | Detail | Exported From | Supported Ailia Version | Blog |
|:-----------|------------:|:------------:|:------------:|:------------:|
|[vggface2](/Assets/AXIP/AILIA-MODELS/FaceIdentification/) | [VGGFace2 Dataset for Face Recognition](https://github.com/ox-vgg/vgg_face2) | Caffe | 1.1.0 and later ||
|[arcface](/Assets/AXIP/AILIA-MODELS/FaceIdentification/) | [pytorch implement of arcface](https://github.com/ronghuaiyang/arcface-pytorch) | Pytorch | 1.2.1 and later | [EN](https://medium.com/axinc-ai/arcface-a-machine-learning-model-for-face-recognition-5f743cdac6fa) [JP](https://medium.com/axinc/arcface-%E9%A1%94%E8%AA%8D%E8%A8%BC%E3%82%92%E8%A1%8C%E3%81%86%E6%A9%9F%E6%A2%B0%E5%AD%A6%E7%BF%92%E3%83%A2%E3%83%87%E3%83%AB-cbb0e127bd0a) |

## Person ReID

| Name | Detail | Exported From | Supported Ailia Version | Blog |
|:-----------|------------:|:------------:|:------------:|:------------:|
|[person_reid_baseline_pytorch](/Assets/AXIP/AILIA-MODELS/FaceIdentification/) | [UTS-Person-reID-Practical](https://github.com/layumi/Person_reID_baseline_pytorch) | Pytorch | 1.2.6 and later ||

## Natural Language Processing

| Name | Detail | Exported From | Supported Ailia Version | Blog |
|:-----------|------------:|:------------:|:------------:|:------------:|
|[sentence_transformers_japanese](/Assets/AXIP/AILIA-MODELS/NaturalLanguageProcessing/) | [sentence transformers](https://huggingface.co/sentence-transformers/paraphrase-multilingual-mpnet-base-v2) | Pytorch | 1.2.7 and later | [JP](https://medium.com/axinc/sentencetransformer-%E3%83%86%E3%82%AD%E3%82%B9%E3%83%88%E3%81%8B%E3%82%89embedding%E3%82%92%E5%8F%96%E5%BE%97%E3%81%99%E3%82%8B%E8%A8%80%E8%AA%9E%E5%87%A6%E7%90%86%E3%83%A2%E3%83%87%E3%83%AB-b7d2a9bb2c31) |
|[multilingual-e5](/Assets/AXIP/AILIA-MODELS/NaturalLanguageProcessing/) | [multilingual-e5-base](https://huggingface.co/intfloat/multilingual-e5-base) | Pytorch | 1.2.15 and later | [JP](https://medium.com/axinc/multilingual-e5-%E5%A4%9A%E8%A8%80%E8%AA%9E%E3%81%AE%E3%83%86%E3%82%AD%E3%82%B9%E3%83%88%E3%82%92embedding%E3%81%99%E3%82%8B%E6%A9%9F%E6%A2%B0%E5%AD%A6%E7%BF%92%E3%83%A2%E3%83%87%E3%83%AB-71f1dec7c4f0) |
|[fugumt-en-ja](/Assets/AXIP/AILIA-MODELS/NaturalLanguageProcessing/) | [Fugu-Machine Translator](https://github.com/s-taka/fugumt)   | Pytorch | 1.2.9 and later | [JP](https://medium.com/axinc/fugumt-%E8%8B%B1%E8%AA%9E%E3%81%8B%E3%82%89%E6%97%A5%E6%9C%AC%E8%AA%9E%E3%81%B8%E3%81%AE%E7%BF%BB%E8%A8%B3%E3%82%92%E8%A1%8C%E3%81%86%E6%A9%9F%E6%A2%B0%E5%AD%A6%E7%BF%92%E3%83%A2%E3%83%87%E3%83%AB-46b839c1b4ae) |
|[fugumt-ja-en](/Assets/AXIP/AILIA-MODELS/NaturalLanguageProcessing/) | [Fugu-Machine Translator](https://github.com/s-taka/fugumt)   | Pytorch | 1.2.10 abd later |

# Document

[ailia SDK Tutorial (Unity)](https://medium.com/axinc-ai/ailia-sdk-tutorial-unity-54f2a8155b8f)

[ailia Unity API](https://axinc-ai.github.io/ailia-sdk/api/unity/en/)

# Other languages

[python version](https://github.com/axinc-ai/ailia-models)

[c++ version](https://github.com/axinc-ai/ailia-models-cpp)
