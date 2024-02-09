using ailiaSDK;
using System;
using System.Collections.Generic;
using UnityEngine;
using static ailiaSDK.AiliaImageUtil;

public class AiliaPoseResnet
{
    private AiliaDetectorModel ailiaDetection = new AiliaDetectorModel();
    private AiliaModel ailiaPoseEstimation = new AiliaModel();

    string[] classifierLabel = AiliaClassifierLabel.COCO_CATEGORY;
    float threshold = 0.4f;
    float iou = 0.45f;
    uint category_n = 80;

    private static readonly int RESNET_INPUT_WIDTH = 192;
    private static readonly int RESNET_INPUT_HEIGHT = 256;
    private static readonly int RESNET_OUTPUT_JOINT_COUNT = 17;
    private static readonly int RESNET_OUTPUT_WIDTH = 48;
    private static readonly int RESNET_OUTPUT_HEIGHT = 64;



    public AiliaPoseResnet(bool gpuMode, string assetPath)
    {
        bool status;

        if (gpuMode)
        {
            ailiaDetection.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
            ailiaPoseEstimation.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
        }

        ailiaDetection.Settings(
            AiliaFormat.AILIA_NETWORK_IMAGE_FORMAT_RGB,
            AiliaFormat.AILIA_NETWORK_IMAGE_CHANNEL_FIRST,
            AiliaFormat.AILIA_NETWORK_IMAGE_RANGE_UNSIGNED_FP32,
            AiliaDetector.AILIA_DETECTOR_ALGORITHM_YOLOV3,
            category_n,
            AiliaDetector.AILIA_DETECTOR_FLAG_NORMAL
        );
        string modelName = "yolov3";
        status = ailiaDetection.OpenFile($"{assetPath}/{modelName}.opt2.onnx.prototxt", $"{assetPath}/{modelName}.opt2.onnx");
        if (status == false)
        {
            string message = $"Could not load model {modelName}";
            Debug.LogError(message);
            throw new Exception(message);
        }
        Debug.Log($"Model loaded {modelName}");

        modelName = "pose_resnet_50_256x192";
        status = ailiaPoseEstimation.OpenFile($"{assetPath}/{modelName}.onnx.prototxt", $"{assetPath}/{modelName}.onnx");
        if (status == false)
        {
            string message = $"Could not load model {modelName}";
            Debug.LogError(message);
            throw new Exception(message);
        }
        Debug.Log($"Model loaded {modelName}");
    }

    public List<AiliaPoseEstimator.AILIAPoseEstimatorObjectPose> RunPoseEstimation(Color32[] camera, int tex_width, int tex_height)
    {
        // object detection
        List<AiliaDetector.AILIADetectorObject> detectionList = ailiaDetection.ComputeFromImageB2T(camera, tex_width, tex_height, threshold, iou);

        // pose estimation        
        List<AiliaPoseEstimator.AILIAPoseEstimatorObjectPose> result_list = new List<AiliaPoseEstimator.AILIAPoseEstimatorObjectPose>();

        foreach (AiliaDetector.AILIADetectorObject obj in detectionList)
        {
            if (classifierLabel[obj.category] != "person")
            {
                continue;
            }
            int x1 = (int)(obj.x * tex_width);
			int y1 = (int)(obj.y * tex_height);
			int x2 = (int)((obj.x + obj.w) * tex_width);
			int y2 = (int)((obj.y + obj.h) * tex_height);
            int[] p = keep_aspect(x1, y1, x2, y2, tex_width, tex_height, RESNET_INPUT_WIDTH, RESNET_INPUT_HEIGHT);
            float[] input = preprocessTexture(p, camera, tex_width, tex_height);
            float[] output = new float[RESNET_OUTPUT_JOINT_COUNT * RESNET_OUTPUT_WIDTH * RESNET_OUTPUT_HEIGHT];
            ailiaPoseEstimation.Predict(output, input);
            result_list.Add(postProcessOutput(output, tex_width, tex_height, p));
        }
        return result_list;
    }

    private int[] keep_aspect(int x1, int y1, int x2, int y2, int tex_width, int tex_height, int width, int height)
    {
        int px1 = Math.Max(0, x1);
        int py1 = Math.Max(0, y1);
        int px2 = Math.Min(tex_width, x2);
        int py2 = Math.Min(tex_height, y2);

        float aspect = height / (float)width;

        int ow = px2 - px1;
        int oh = py2 - py1;
        float oaspect = oh / (float)ow;

        if (aspect <= oaspect)
        {
            float w = oh / aspect;
            px1 = (int)(px1 - (w - ow) / 2);
            px2 = (int)(px1 + w);
        }
        else
        {
            float h = ow * aspect;
            py1 = (int)(py1 - (h - oh) / 2);
            py2 = (int)(py1 + h);
        }

        px1 = Math.Max(0, px1);
        py1 = Math.Max(0, py1);
        px2 = Math.Min(tex_width, px2);
        py2 = Math.Min(tex_height, py2);

        return new int[4] {px1, py1, px2, py2};
    }

    private float[] preprocessTexture(int[] box, Color32[] camera, int tex_width, int tex_height)
    {
        int px1 = box[0];
        int py1 = box[1];
        int px2 = box[2];
        int py2 = box[3];
        int w = px2 - px1;
        int h = py2 - py1;

        // slice and resize (convert from bottom-to-top to top-to-bottom sequence)
        Color32[] crop = new Color32[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                crop[y * w + x] = camera[(py1 + h - 1 - (py1 + y)) * tex_width + (px1 + x)];
            }
        }
        crop = ResizeImage(crop, w, h, RESNET_INPUT_WIDTH, RESNET_INPUT_HEIGHT);

        // normalize data and convert array of Color32 to array of float (bbb..ggg..rrr.. format)
        int size = RESNET_INPUT_WIDTH * RESNET_INPUT_HEIGHT;
        float[] texture = new float[3 * size];
        float factor = 1 / 255f;
        float[] mean = {0.406f, 0.456f, 0.485f}, std = {0.225f, 0.224f, 0.229f};
        
        for (int y = 0; y < RESNET_INPUT_HEIGHT; y++)
        {
            for (int x = 0; x < RESNET_INPUT_WIDTH; x++)
            {
                int index = y * RESNET_INPUT_WIDTH + x;
                
                texture[index] = (crop[index].b * factor - mean[0]) / std[0];
                texture[index + size] = (crop[index].g * factor - mean[1]) / std[1];
                texture[index + size * 2] = (crop[index].r * factor - mean[2]) / std[2];
            }
        }        
        return texture;
    }

    private Color32 [] ResizeImage(Color32[] inputImage, int tex_width, int tex_height, int width, int height)
    {
        Color32[] outputImage = new Color32[width * height];
        for(int y = 0; y < height; y++)
        {
            for(int x = 0; x < width; x++)
            {
                int x2 = x * tex_width / width;
                int y2 = y * tex_height / height;
                outputImage[y * width + x]=inputImage[y2 * tex_width + x2];
            }
        }
        return outputImage;
    }

    private AiliaPoseEstimator.AILIAPoseEstimatorObjectPose postProcessOutput(float[] output, int tex_width, int tex_height, int[] p)
    {
        int[,,] coords = new int[1, RESNET_OUTPUT_JOINT_COUNT, 2];
        float[,,] maxVals = new float[1, RESNET_OUTPUT_JOINT_COUNT, 1];
        getMaxPreds(output, coords, maxVals);

        float[,,] preds = new float[1, RESNET_OUTPUT_JOINT_COUNT, 2];
        int size = RESNET_OUTPUT_WIDTH * RESNET_OUTPUT_HEIGHT;
        for (int joint = 0; joint < RESNET_OUTPUT_JOINT_COUNT; joint++)
        {
            int index = joint * size;
            int px = coords[0, joint, 0];
            int py = coords[0, joint, 1];
            if (1 < px && px < RESNET_OUTPUT_WIDTH - 1 && 1 < py && py < RESNET_OUTPUT_HEIGHT - 1)
            {
                float diff_x = output[index + py * RESNET_OUTPUT_WIDTH + px + 1] - output[index + py * RESNET_OUTPUT_WIDTH + px - 1];
                float diff_y = output[index + (py + 1) * RESNET_OUTPUT_WIDTH + px] - output[index + (py - 1) * RESNET_OUTPUT_WIDTH + px];
                preds[0, joint, 0] = px + Math.Sign(diff_x) * 0.25f;
                preds[0, joint, 1] = py + Math.Sign(diff_y) * 0.25f;
            }
            else
            {
                preds[0, joint, 0] = px;
                preds[0, joint, 1] = py;
            }
        }

        Vector2 tex_center = new Vector2(RESNET_INPUT_WIDTH / 2.0f, RESNET_INPUT_HEIGHT / 2.0f);
        Vector2 center = new Vector2(RESNET_OUTPUT_WIDTH / 2.0f, RESNET_OUTPUT_HEIGHT / 2.0f);
        Vector2 scale = new Vector2(1.0f, 1.0f);
        preds = transformPreds(coords, tex_center, scale, center);
        float scale_x = (float)(p[2] - p[0]) / tex_width;
        float scale_y = (float)(p[3] - p[1]) / tex_height;
        float offset_x = (float)p[0] / tex_width;
        float offset_y = (float)p[1] / tex_height;
        return getResult(preds, maxVals, scale_x, scale_y, offset_x, offset_y);
    }

    private void getMaxPreds(float[] batchHeatmaps, int[,,] preds, float[,,] maxVals)
    {
        int size = RESNET_OUTPUT_WIDTH * RESNET_OUTPUT_HEIGHT;
        for (int joint = 0; joint < RESNET_OUTPUT_JOINT_COUNT; joint++)
        {
            int index = joint * size;
            float maxval = batchHeatmaps[index];
            int maxind_x = 0, maxind_y = 0;
            for (int y = 0; y < RESNET_OUTPUT_HEIGHT; y++)
            {
                for (int x = 0; x < RESNET_OUTPUT_WIDTH; x++)
                {
                    if (batchHeatmaps[index + y * RESNET_OUTPUT_WIDTH + x] > maxval)
                    {
                        maxval = batchHeatmaps[index + y * RESNET_OUTPUT_WIDTH + x];
                        maxind_x = x;
                        maxind_y = y;
                    }
                }
            }
            maxVals[0, joint, 0] = maxval;
            if (maxval < 0)
            {
                preds[0, joint, 0] = 0;
                preds[0, joint, 1] = 0;
            }
            else
            {
                preds[0, joint, 0] = maxind_x;
                preds[0, joint, 1] = maxind_y;
            }
        }
    }

    private float[,,] transformPreds(int[,,] coords, Vector2 center, Vector2 scale, Vector2 output_center)
    {
        float[,,] preds = new float[1, RESNET_OUTPUT_JOINT_COUNT, 2];
        Vector2 scale_tmp = 200f * scale;
        Vector2[] src = new Vector2[3], dst = new Vector2[3];
        src[0] = center;
        src[1] = center + new Vector2(0f, -scale_tmp.x / 2f);
        src[2] = get3rdPoint(src[0], src[1]);
        dst[0] = output_center;
        dst[1] = output_center + new Vector2(0f, -output_center.x);
        dst[2] = get3rdPoint(dst[0], dst[1]);
        Matrix4x4 trans = GetAffineTransform(dst, src);
        for (int i = 0; i < 4; i++)
        {
            Vector4 vec = trans.GetRow(i);
        }
        for (int joint = 0; joint < RESNET_OUTPUT_JOINT_COUNT; joint++)
        {
            Vector2 point = transformPoint(trans, new Vector2(coords[0, joint, 0], coords[0, joint, 1]));
            preds[0, joint, 0] = point.x;
            preds[0, joint, 1] = point.y;
        }
        return preds;
    }

    private Vector2 get3rdPoint(Vector2 a, Vector2 b)
    {
        Vector2 direct = a - b;
        return b + new Vector2(-direct.y, direct.x);
    }

    private Matrix4x4 GetAffineTransform(Vector2[] a, Vector2[] b)
    {
        Matrix4x4 matA = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.zero);
        Matrix4x4 matB = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.zero);
        matA.SetRow(0, new Vector4(a[0].x, a[1].x, a[2].x, 0f));
        matA.SetRow(1, new Vector4(a[0].y, a[1].y, a[2].y, 0f));
        matA.SetRow(2, new Vector4(1f, 1f, 1f, 0f));
        matB.SetRow(0, new Vector4(b[0].x, b[1].x, b[2].x, 0f));
        matB.SetRow(1, new Vector4(b[0].y, b[1].y, b[2].y, 0f));
        matB.SetRow(2, new Vector4(1f, 1f, 1f, 0f));

        Matrix4x4 transMat = matB * matA.inverse;

        return transMat;
    }

    private Vector2 transformPoint(Matrix4x4 matrix, Vector2 point)
    {
        Vector4 transformedPoint = matrix * new Vector4(point.x, point.y, 1, 1);

        return new Vector2(transformedPoint.x, transformedPoint.y);
    }

    private AiliaPoseEstimator.AILIAPoseEstimatorObjectPose getResult(float[,,] preds, float[,,] maxVals, float scale_x, float scale_y, float offset_x, float offset_y)
    {
        AiliaPoseEstimator.AILIAPoseEstimatorObjectPose pose = new AiliaPoseEstimator.AILIAPoseEstimatorObjectPose();
        pose.points = new AiliaPoseEstimator.AILIAPoseEstimatorKeypoint[19];
        float total_score = 0f;
        int num_valid_points = 0;
        for (int i = 0; i < AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_CNT; i++)
        {
            AiliaPoseEstimator.AILIAPoseEstimatorKeypoint keypoint = new AiliaPoseEstimator.AILIAPoseEstimatorKeypoint();
            float x, y;
            if (i == AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_BODY_CENTER)
            {
                x = (preds[0, AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_SHOULDER_LEFT, 0] +
                     preds[0, AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_SHOULDER_RIGHT, 0] +
                     preds[0, AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_HIP_LEFT, 0] +
                     preds[0, AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_HIP_RIGHT, 0]) / 4;
                y = (preds[0, AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_SHOULDER_LEFT, 1] +
                     preds[0, AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_SHOULDER_RIGHT, 1] +
                     preds[0, AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_HIP_LEFT, 1] +
                     preds[0, AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_HIP_RIGHT, 1]) / 4;
                keypoint.score = Math.Min(Math.Min(Math.Min(
                    maxVals[0, AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_SHOULDER_LEFT, 0],
                    maxVals[0, AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_SHOULDER_RIGHT, 0]),
                    maxVals[0, AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_HIP_LEFT, 0]),
                    maxVals[0, AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_HIP_RIGHT, 0]);
                keypoint.interpolated = 1;
            }
            else if (i == AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_SHOULDER_CENTER)
            {
                x = (preds[0, AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_SHOULDER_LEFT, 0] +
                     preds[0, AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_SHOULDER_RIGHT, 0]) / 2;
                y = (preds[0, AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_SHOULDER_LEFT, 1] +
                     preds[0, AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_SHOULDER_RIGHT, 1]) / 2;
                keypoint.score = Math.Min(
                    maxVals[0, AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_SHOULDER_LEFT, 0],
                    maxVals[0, AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_SHOULDER_RIGHT, 0]);
                keypoint.interpolated = 1;
            }
            else
            {
                x = preds[0, i, 0];
                y = preds[0, i, 1];
                keypoint.score = maxVals[0, i, 0];
            }

            //keypoint.x = (RESNET_INPUT_WIDTH - 1 - x) / RESNET_INPUT_WIDTH * scale_x + offset_x;  // reverse x-axis
            keypoint.x = x / RESNET_INPUT_WIDTH * scale_x + offset_x;
            keypoint.y = y / RESNET_INPUT_HEIGHT * scale_y + offset_y;
            num_valid_points++;
            total_score += keypoint.score;
            pose.points[i] = keypoint;
        }
        pose.total_score = total_score;
        pose.num_valid_points = num_valid_points;
        return pose;
    }
}