using ailiaSDK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using SA;
using static SA.FullBodyIK;

struct Box
{
    public float xMin;
    public float xMax;
    public float yMin;
    public float yMax;
    public float area;
    public float score;
    public Vector2[] keypoints;

    private int mergedBoxCount;

    public float GetJaccardOverlap(Box other)
    {
        float widthOverlap = Math.Max(0, Math.Min(xMax, other.xMax) - Math.Max(xMin, other.xMin));
        float heightOverlap = Math.Max(0, Math.Min(yMax, other.yMax) - Math.Max(yMin, other.yMin));

        float intersection = widthOverlap * heightOverlap;

        float union = area + other.area - intersection;

        return intersection / union;
    }

    public void Merge(Box other)
    {
        if (mergedBoxCount == 0)
        {
            xMin *= score;
            yMin *= score;
            xMax *= score;
            yMax *= score;

            for (int i = 0; i < AiliaBlazepose.BLAZEPOSE_DETECTOR_KEYPOINT_COUNT; ++i)
            {
                keypoints[i] *= score; 
            }
        }

        xMin += other.score * other.xMin;
        yMin += other.score * other.yMin;
        xMax += other.score * other.xMax;
        yMax += other.score * other.yMax;
        
        for (int i = 0; i < AiliaBlazepose.BLAZEPOSE_DETECTOR_KEYPOINT_COUNT; ++i)
        {
            keypoints[i] += other.score * other.keypoints[i];
        }
        
        score += other.score;
        mergedBoxCount += Math.Max(1, other.mergedBoxCount);
    }

    public void FinalizeMerge()
    {
        if (mergedBoxCount == 0)
        {
            return;
        }

        xMin /= score;
        yMin /= score;
        xMax /= score;
        yMax /= score;

        for (int i = 0; i < AiliaBlazepose.BLAZEPOSE_DETECTOR_KEYPOINT_COUNT; ++i)
        {
            keypoints[i] /= score;
        }

        score /= (1 + mergedBoxCount);

        mergedBoxCount = 0;
    }
}

public struct Landmark
{
    public int index;
    public Vector3 position;
    public float confidence;
}

public struct Transformation
{
    public Vector3 translation;
    public float angle;
    public Vector3 scale;
}

public enum BodyPartIndex
{
    Nose = 0,
    InnerLeftEye,
    LeftEye,
    OuterLeftEye,
    InnerRightEye,
    RightEye,
    OuterRightEye,
    LeftEar,
    RightEar,
    LeftMouth,
    RightMouth,

    LeftShoulder,
    RightShoulder,
    LeftElbow,
    RightElbow,
    LeftWrist,
    RightWrist,
    LeftPinky,
    RightPinky,
    LeftIndex,
    RightIndex,
    LeftThumb,
    RightThumb,
    LeftHip,
    RightHip,
    LeftKnee,
    RightKnee,
    LeftAnkle,
    RightAnkle,
    LeftHeel,
    RightHeel,
    LeftFootIndex,
    RightFootIndex,
}

public class AiliaBlazepose : IDisposable
{
    public ComputeShader computeShader = null;
    internal bool Smooth;
    float prevTime;

    private AiliaModel ailiaPoseDetection = new AiliaModel();
    private AiliaModel ailiaPoseEstimation = new AiliaModel();
    private FullBodyIKBehaviour referenceModel;

    private static readonly int BLAZEPOSE_DETECTOR_INPUT_RESOLUTION = 224;
    private static readonly uint BLAZEPOSE_DETECTOR_INPUT_CHANNEL_COUNT = 3;
    private static readonly uint BLAZEPOSE_DETECTOR_TENSOR_COUNT = 2254;
    private static readonly uint BLAZEPOSE_DETECTOR_TENSOR_SIZE = 12;
    public static readonly uint BLAZEPOSE_DETECTOR_KEYPOINT_COUNT = 4;
    private static readonly float BLAZEPOSE_DETECTOR_RAW_SCORE_THRESHOLD = 100;
    private static readonly float BLAZEPOSE_DETECTOR_MINIMUM_SCORE_THRESHOLD = 0.5f;
    private static readonly float BLAZEPOSE_DETECTOR_MINIMUM_OVERLAP_THRESHOLD = 0.3f;
    private float[,] anchors;

    private static readonly int BLAZEPOSE_ESTIMATOR_INPUT_RESOLUTION = 256;
    private static readonly int BLAZEPOSE_ESTIMATOR_TENSOR_COUNT = 1;
    private static readonly int BLAZEPOSE_ESTIMATOR_TENSOR_SIZE = 195;

    int kernelIndex = -1;
    int ID_InputTexture = Shader.PropertyToID("InputTexture");
    int ID_InputWidth = Shader.PropertyToID("InputWidth");
    int ID_InputHeight = Shader.PropertyToID("InputHeight");
    int ID_OutputTexture = Shader.PropertyToID("OutputTexture");
    int ID_OutputWidth = Shader.PropertyToID("OutputWidth");
    int ID_OutputHeight = Shader.PropertyToID("OutputHeight");
    int ID_Matrix = Shader.PropertyToID("Matrix");
    int ID_BackgroundColor = Shader.PropertyToID("BackgroundColor");

    public static readonly (BodyPartIndex, BodyPartIndex)[] BodyPartConnections = new (BodyPartIndex, BodyPartIndex)[]
    {
        (BodyPartIndex.Nose, BodyPartIndex.InnerLeftEye),
        (BodyPartIndex.InnerLeftEye, BodyPartIndex.LeftEye),
        (BodyPartIndex.LeftEye, BodyPartIndex.OuterLeftEye),
        (BodyPartIndex.OuterLeftEye, BodyPartIndex.LeftEar),

        (BodyPartIndex.Nose, BodyPartIndex.InnerRightEye),
        (BodyPartIndex.InnerRightEye, BodyPartIndex.RightEye),
        (BodyPartIndex.RightEye, BodyPartIndex.OuterRightEye),
        (BodyPartIndex.OuterRightEye, BodyPartIndex.RightEar),

        (BodyPartIndex.LeftMouth, BodyPartIndex.RightMouth),

        (BodyPartIndex.LeftShoulder, BodyPartIndex.RightShoulder),
        (BodyPartIndex.LeftShoulder, BodyPartIndex.LeftElbow),
        (BodyPartIndex.LeftElbow, BodyPartIndex.LeftWrist),
        (BodyPartIndex.RightShoulder, BodyPartIndex.RightElbow),
        (BodyPartIndex.RightElbow, BodyPartIndex.RightWrist),

        (BodyPartIndex.LeftWrist, BodyPartIndex.LeftPinky),
        (BodyPartIndex.LeftPinky, BodyPartIndex.LeftIndex),
        (BodyPartIndex.LeftWrist, BodyPartIndex.LeftIndex),
        (BodyPartIndex.LeftWrist, BodyPartIndex.LeftThumb),

        (BodyPartIndex.RightWrist, BodyPartIndex.RightPinky),
        (BodyPartIndex.RightPinky, BodyPartIndex.RightIndex),
        (BodyPartIndex.RightWrist, BodyPartIndex.RightIndex),
        (BodyPartIndex.RightWrist, BodyPartIndex.RightThumb),

        (BodyPartIndex.LeftShoulder, BodyPartIndex.LeftHip),
        (BodyPartIndex.RightShoulder, BodyPartIndex.RightHip),
        (BodyPartIndex.LeftHip, BodyPartIndex.RightHip),

        (BodyPartIndex.LeftHip, BodyPartIndex.LeftKnee),
        (BodyPartIndex.LeftKnee, BodyPartIndex.LeftAnkle),
        (BodyPartIndex.RightHip, BodyPartIndex.RightKnee),
        (BodyPartIndex.RightKnee, BodyPartIndex.RightAnkle),

        (BodyPartIndex.LeftAnkle, BodyPartIndex.LeftHeel),
        (BodyPartIndex.LeftHeel, BodyPartIndex.LeftFootIndex),
        (BodyPartIndex.LeftAnkle, BodyPartIndex.LeftFootIndex),

        (BodyPartIndex.RightAnkle, BodyPartIndex.RightHeel),
        (BodyPartIndex.RightHeel, BodyPartIndex.RightFootIndex),
        (BodyPartIndex.RightAnkle, BodyPartIndex.RightFootIndex),
    };

    public static readonly BodyPartIndex[][] bodyBranches = new BodyPartIndex[][]
    {
        new BodyPartIndex[] { BodyPartIndex.LeftShoulder, BodyPartIndex.LeftElbow, BodyPartIndex.LeftWrist, BodyPartIndex.LeftThumb },
        new BodyPartIndex[] { BodyPartIndex.LeftShoulder, BodyPartIndex.LeftElbow, BodyPartIndex.LeftWrist, BodyPartIndex.LeftIndex },
        new BodyPartIndex[] { BodyPartIndex.LeftShoulder, BodyPartIndex.LeftElbow, BodyPartIndex.LeftWrist, BodyPartIndex.LeftPinky },

        new BodyPartIndex[] { BodyPartIndex.RightShoulder, BodyPartIndex.RightElbow, BodyPartIndex.RightWrist, BodyPartIndex.RightThumb },
        new BodyPartIndex[] { BodyPartIndex.RightShoulder, BodyPartIndex.RightElbow, BodyPartIndex.RightWrist, BodyPartIndex.RightIndex },
        new BodyPartIndex[] { BodyPartIndex.RightShoulder, BodyPartIndex.RightElbow, BodyPartIndex.RightWrist, BodyPartIndex.RightPinky },

        new BodyPartIndex[] { BodyPartIndex.LeftHip, BodyPartIndex.LeftKnee, BodyPartIndex.LeftAnkle, BodyPartIndex.LeftFootIndex },

        new BodyPartIndex[] { BodyPartIndex.RightHip, BodyPartIndex.RightKnee, BodyPartIndex.RightAnkle, BodyPartIndex.RightFootIndex },
    };

    public static readonly (BodyPartIndex, BodyPartIndex)[] connectionToAdjust = new (BodyPartIndex, BodyPartIndex)[]
        {
            (BodyPartIndex.LeftHip, BodyPartIndex.LeftShoulder),
            (BodyPartIndex.RightHip, BodyPartIndex.RightShoulder),
            (BodyPartIndex.LeftShoulder, BodyPartIndex.RightShoulder),

            (BodyPartIndex.LeftShoulder, BodyPartIndex.LeftElbow),
            (BodyPartIndex.LeftElbow, BodyPartIndex.LeftWrist),

            (BodyPartIndex.RightShoulder, BodyPartIndex.RightElbow),
            (BodyPartIndex.RightElbow, BodyPartIndex.RightWrist),

            (BodyPartIndex.LeftHip, BodyPartIndex.LeftKnee),
            (BodyPartIndex.LeftKnee, BodyPartIndex.LeftAnkle),

            (BodyPartIndex.RightHip, BodyPartIndex.RightKnee),
            (BodyPartIndex.RightKnee, BodyPartIndex.RightAnkle),
        };

    public readonly Dictionary<(BodyPartIndex, BodyPartIndex), float> connectionTargetLength = new Dictionary<(BodyPartIndex, BodyPartIndex), float>();

    public readonly Dictionary<BodyPartIndex, Bone> blazePoseToBodyBone;

    struct JsonFloatArray
    {
        public float[] array;
    }

    public AiliaBlazepose(bool gpuMode, FullBodyIKBehaviour referenceModel = null)
    {
        this.referenceModel = referenceModel;

        if (this.referenceModel)
        {
            blazePoseToBodyBone = new Dictionary<BodyPartIndex, Bone>()
            {
                [BodyPartIndex.LeftShoulder] = referenceModel.fullBodyIK.leftArmBones.arm,
                [BodyPartIndex.LeftElbow] = referenceModel.fullBodyIK.leftArmBones.elbow,
                [BodyPartIndex.LeftWrist] = referenceModel.fullBodyIK.leftArmBones.wrist,

                [BodyPartIndex.RightShoulder] = referenceModel.fullBodyIK.rightArmBones.arm,
                [BodyPartIndex.RightElbow] = referenceModel.fullBodyIK.rightArmBones.elbow,
                [BodyPartIndex.RightWrist] = referenceModel.fullBodyIK.rightArmBones.wrist,

                [BodyPartIndex.LeftHip] = referenceModel.fullBodyIK.leftLegBones.leg,
                [BodyPartIndex.LeftKnee] = referenceModel.fullBodyIK.leftLegBones.knee,
                [BodyPartIndex.LeftAnkle] = referenceModel.fullBodyIK.leftLegBones.foot,

                [BodyPartIndex.RightHip] = referenceModel.fullBodyIK.rightLegBones.leg,
                [BodyPartIndex.RightKnee] = referenceModel.fullBodyIK.rightLegBones.knee,
                [BodyPartIndex.RightAnkle] = referenceModel.fullBodyIK.rightLegBones.foot,
            };

            foreach (var connection in connectionToAdjust)
            {
                Vector3 startBonePosition = blazePoseToBodyBone[connection.Item1].worldPosition;
                Vector3 endBonePosition = blazePoseToBodyBone[connection.Item2].worldPosition;

                connectionTargetLength[connection] = Vector3.Distance(startBonePosition, endBonePosition);
            }
        }

        bool status;
        string assetPath = Application.streamingAssetsPath + "/AILIA";

        if (gpuMode)
        {
            ailiaPoseDetection.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
        }
        string modelName = "pose_detection";
        status = ailiaPoseDetection.OpenFile($"{assetPath}/{modelName}.onnx.prototxt", $"{assetPath}/{modelName}.onnx");
        if (status == false)
        {
            string message = $"Could not load model {modelName}";
            Debug.LogError(message);
            throw new Exception(message);
        }
        Debug.Log($"Model loaded {modelName}");

        if (gpuMode)
        {
            ailiaPoseEstimation.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
        }
        modelName = "pose_landmark_heavy";
        status = ailiaPoseEstimation.OpenFile($"{assetPath}/{modelName}.onnx.prototxt", $"{assetPath}/{modelName}.onnx");
        if (status == false)
        {
            string message = $"Could not load model {modelName}";
            Debug.LogError(message);
            throw new Exception(message);
        }

        Debug.Log($"Model loaded {modelName}");

        string anchorsJSON = File.ReadAllText($"{assetPath}/blazepose_anchors.json");
        float[] anchorsFlat = JsonUtility.FromJson<JsonFloatArray>($"{{ \"array\": {anchorsJSON} }}").array;

        anchors = new float[BLAZEPOSE_DETECTOR_TENSOR_COUNT, 4];

        for (int i = 0; i < anchorsFlat.Length; ++i)
        {
            anchors[i / 4, i % 4] = anchorsFlat[i];
        }

        inputArray = new float[BLAZEPOSE_DETECTOR_INPUT_RESOLUTION * BLAZEPOSE_DETECTOR_INPUT_RESOLUTION * BLAZEPOSE_DETECTOR_INPUT_CHANNEL_COUNT];
        rawBoxesOutput = new float[BLAZEPOSE_DETECTOR_TENSOR_COUNT * BLAZEPOSE_DETECTOR_TENSOR_SIZE];
        rawScoresOutput = new float[BLAZEPOSE_DETECTOR_TENSOR_COUNT];
        estimationScoreBuffer = new float[1];
        estimationInputArray = new float[BLAZEPOSE_ESTIMATOR_INPUT_RESOLUTION * BLAZEPOSE_ESTIMATOR_INPUT_RESOLUTION * BLAZEPOSE_DETECTOR_INPUT_CHANNEL_COUNT];
        estimationOutputBuffer = new float[BLAZEPOSE_ESTIMATOR_TENSOR_COUNT * BLAZEPOSE_ESTIMATOR_TENSOR_SIZE];
    }

    private float[] inputArray;
    private float[] rawBoxesOutput;
    private float[] rawScoresOutput;
    private List<Box> boxes;
    private Box? poseDetectionBox;
    RenderTexture preprocessBuffer;
    private void PreprocessTexture(Texture2D texture)
    {
        if(preprocessBuffer == null)
		{
            preprocessBuffer = new RenderTexture(BLAZEPOSE_DETECTOR_INPUT_RESOLUTION, BLAZEPOSE_DETECTOR_INPUT_RESOLUTION, 0, RenderTextureFormat.ARGB32);
        }

        Texture2D letterboxed = TexturePreprocessor.PreprocessTexture(texture, preprocessBuffer, BLAZEPOSE_DETECTOR_INPUT_RESOLUTION * Vector2.one);
		Color32[] colorData = letterboxed.GetPixels32();
        const float factor = 1 / 255f;

        for (int heightIndex = 0; heightIndex < BLAZEPOSE_DETECTOR_INPUT_RESOLUTION; heightIndex++)
        {
            for (int widthIndex = 0; widthIndex < BLAZEPOSE_DETECTOR_INPUT_RESOLUTION; widthIndex++)
            {
                int index = (int) (((heightIndex * BLAZEPOSE_DETECTOR_INPUT_RESOLUTION) + widthIndex) * BLAZEPOSE_DETECTOR_INPUT_CHANNEL_COUNT);
				Color32 value = colorData[(BLAZEPOSE_DETECTOR_INPUT_RESOLUTION - heightIndex - 1) * BLAZEPOSE_DETECTOR_INPUT_RESOLUTION + widthIndex];

				inputArray[index + 0] = value.r * factor;
				inputArray[index + 1] = value.g * factor;
				inputArray[index + 2] = value.b * factor;
			}
        }
    }

    private uint estimationInputWidth;
    private uint estimationInputHeight;
    private float[] estimationInputArray;
    private float[] estimationOutputBuffer;
    float[] estimationScoreBuffer;
    public List<Landmark> landmarks = new List<Landmark>();
    public List<Landmark> smoothLandmarks = new List<Landmark>();
    public List<Landmark> smoothDiffLandmarks = new List<Landmark>();

    public GameObject renderer;
    public GameObject rendererLandmarks;
    private GameObject rendererConnections;

    public float smoothingCutoff = 5;
    public bool straightenModel = true;


    public Vector3? GetLandmarkPosition(BodyPartIndex bpi)
    {
        if (landmarks.Count == 0)
        {
            return null;
        }

        return rendererLandmarks.transform.GetChild((int) bpi).position;
    }

    private void PreprocessTextureEstimation(Texture2D texture)
    {
        estimationInputWidth = ((uint)texture.width);
        estimationInputHeight = ((uint)texture.height);

        Color32[] colorData = texture.GetPixels32();
        const float factor = 1 / 255f;

        for (int heightIndex = 0; heightIndex < estimationInputHeight; heightIndex++)
        {
            for (int widthIndex = 0; widthIndex < estimationInputWidth; widthIndex++)
            {
                int index = (int)(((heightIndex * estimationInputWidth) + widthIndex) * BLAZEPOSE_DETECTOR_INPUT_CHANNEL_COUNT);
				Color32 value = colorData[heightIndex * estimationInputWidth + widthIndex];

				estimationInputArray[index + 0] = value.r * factor;
                estimationInputArray[index + 1] = value.g * factor;
                estimationInputArray[index + 2] = value.b * factor;
            }
        }
    }

    private Texture2D ExtractROIFromBox(Texture2D texture, Box box)
    {
        float finalSquareLength = Mathf.Max(texture.width, texture.height);
        int xOffset = ((int) ((finalSquareLength - texture.width) / 2));
        int yOffset = ((int) ((finalSquareLength - texture.height) / 2));

        Box scaledBox = box;
		scaledBox.xMin = finalSquareLength * scaledBox.xMin - xOffset;
		scaledBox.xMax = finalSquareLength * scaledBox.xMax - xOffset;
		scaledBox.yMin = texture.height - 1 - finalSquareLength * scaledBox.yMin + yOffset; 
        scaledBox.yMax = texture.height - 1 - finalSquareLength * scaledBox.yMax + yOffset;
        scaledBox.keypoints = new Vector2[scaledBox.keypoints.Length];

        for (int i = 0; i < box.keypoints.Length; ++i)
        {
            scaledBox.keypoints[i] = new Vector2(
				finalSquareLength * box.keypoints[i].x - xOffset,
				texture.height - 1 - finalSquareLength * box.keypoints[i].y + yOffset
            );
        }

        int kp1 = 0;
        int kp2 = 1;
        float theta0 = Mathf.PI / 2f;
        float dscale = 1.1f;

        float xc = scaledBox.keypoints[kp1].x;
        float yc = scaledBox.keypoints[kp1].y;
        float x1 = scaledBox.keypoints[kp2].x;
        float y1 = scaledBox.keypoints[kp2].y;
        float scale = dscale * Mathf.Sqrt((Mathf.Pow(xc - x1, 2) + Mathf.Pow(yc - y1, 2))) * 2;
        float angle = Mathf.Atan2(yc - y1, xc - x1) - theta0;
        
        Vector2[] points = new Vector2[]
        {
            new Vector2(1, -1),
            new Vector2(1, 1),
            new Vector2(-1, -1)
        };

        for (int i = 0; i < points.Length; ++i) {
            points[i] *= scale / 2;
        }

        float cosAngle = Mathf.Cos(angle);
        float sinAngle = Mathf.Sin(angle);

        for (int i = 0; i < points.Length; ++i)
        {
            Vector2 p = points[i];
            points[i] = new Vector2(
                p.x * cosAngle - p.y * sinAngle + xc,
                p.x * sinAngle + p.y * cosAngle + yc
            );
        }

        int resolution = BLAZEPOSE_ESTIMATOR_INPUT_RESOLUTION - 1;
        Matrix4x4 before_m = new Matrix4x4() {
            m00 = 0, m01 = 0, m02 = resolution, m03 = 0,
            m10 = 0, m11 = resolution, m12 = 0, m13 = 0,
            m20 = 1, m21 = 1, m22 = 1, m23 = 0,
            m30 = 0, m31 = 0, m32 = 0, m33 = 1,
        };

        Matrix4x4 after_m = new Matrix4x4()
        {
            m00 = points[0].x, m01 = points[1].x, m02 = points[2].x, m03 = 0,
            m10 = points[0].y, m11 = points[1].y, m12 = points[2].y, m13 = 0,
            m20 = 1, m21 = 1, m22 = 1, m23 = 0,
            m30 = 0, m31 = 0, m32 = 0, m33 = 1,
        };

        Matrix4x4 transfrom_m = after_m * before_m.inverse;



        if(computeTexture == null)
		{
            computeTexture = new RenderTexture(BLAZEPOSE_ESTIMATOR_INPUT_RESOLUTION, BLAZEPOSE_ESTIMATOR_INPUT_RESOLUTION, 32);
            computeTexture.enableRandomWrite = true;
        }

        if(kernelIndex < 0)
		{
            kernelIndex = computeShader.FindKernel("AffineTransform");
		}

        computeShader.SetTexture(kernelIndex, ID_InputTexture, texture);
        computeShader.SetTexture(kernelIndex, ID_OutputTexture, computeTexture);
        computeShader.SetMatrix(ID_Matrix, transfrom_m);
        computeShader.SetInt(ID_InputWidth, texture.width);
        computeShader.SetInt(ID_InputHeight, texture.height);
        computeShader.SetInt(ID_OutputWidth, BLAZEPOSE_ESTIMATOR_INPUT_RESOLUTION);
        computeShader.SetInt(ID_OutputHeight, BLAZEPOSE_ESTIMATOR_INPUT_RESOLUTION);
        computeShader.SetVector(ID_BackgroundColor, new Vector4(0, 0, 0, 1));

        computeShader.Dispatch(kernelIndex, BLAZEPOSE_ESTIMATOR_INPUT_RESOLUTION / 32 + 1, BLAZEPOSE_ESTIMATOR_INPUT_RESOLUTION / 32 + 1, 1);

        roiTexture = toTexture2D(computeTexture, roiTexture);

        return roiTexture;
    }
    RenderTexture computeTexture;
    Texture2D roiTexture;

    Texture2D toTexture2D(RenderTexture rTex, Texture2D output = null)
    {
        var org = RenderTexture.active;
		output = output ?? new Texture2D(rTex.width, rTex.height, TextureFormat.RGBA32, false);
        RenderTexture.active = rTex;
        output.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
        output.Apply();
        RenderTexture.active = org;
        return output;
    }

    public void RunPoseEstimation(Texture2D texture)
    {
        Texture2D detection = RunDetectionModel(texture);
        if (detection == null)
        {
            Debug.Log("NO POSE");
			return;
        }

		// For testing purposes, also write to a file in the project folder
		//byte[] bytes = detection.EncodeToPNG();
		//Directory.CreateDirectory(Application.dataPath + $"/../BLAZEPOSE");
		//File.WriteAllBytes(Application.dataPath + $"/../BLAZEPOSE/INTER_{detection.width}x{detection.height}_{Time.renderedFrameCount.ToString("00000")}.png", bytes);

		RunEstimationModel(detection);

        UpdateRenderer();
    }

    private Texture2D RunDetectionModel(Texture2D inputTexture)
    {
        bool status;

		if (poseDetectionBox == null)
		{
            PreprocessTexture(inputTexture);

            int inputBlobIndex = ailiaPoseDetection.FindBlobIndexByName("input_1");

            status = ailiaPoseDetection.SetInputBlobShape(
                new Ailia.AILIAShape
                {
                    x = (uint)BLAZEPOSE_DETECTOR_INPUT_CHANNEL_COUNT,
                    y = (uint)BLAZEPOSE_DETECTOR_INPUT_RESOLUTION,
                    z = (uint)BLAZEPOSE_DETECTOR_INPUT_RESOLUTION,
                    w = 1,
                    dim = 4
                },
                inputBlobIndex
            );

            if (status == false)
            {
                Debug.LogError("Could not set input blob shape");
                Debug.LogError(ailiaPoseDetection.GetErrorDetail());
            }

            status = ailiaPoseDetection.SetInputBlobData(inputArray, inputBlobIndex);
            if (status == false)
            {
                Debug.LogError("Could not set input blob data");
                Debug.LogError(ailiaPoseDetection.GetErrorDetail());
            }

            bool result = ailiaPoseDetection.Update();
            if (result == false)
            {
                Debug.Log(ailiaPoseDetection.GetErrorDetail());
            }

            int outputBlobIndex = ailiaPoseDetection.FindBlobIndexByName("Identity");
            status = ailiaPoseDetection.GetBlobData(rawBoxesOutput, outputBlobIndex);
            if (status == false)
            {
                Debug.LogError("Could not get output blob data " + outputBlobIndex);
                Debug.LogError(ailiaPoseDetection.GetErrorDetail());
            }

            outputBlobIndex = ailiaPoseDetection.FindBlobIndexByName("Identity_1");
            status = ailiaPoseDetection.GetBlobData(rawScoresOutput, outputBlobIndex);
            if (status == false)
            {
                Debug.LogError("Could not get output blob data " + outputBlobIndex);
                Debug.LogError(ailiaPoseDetection.GetErrorDetail());
            }

            DecodeAndProcessBoxes();

            if (boxes.Count == 0)
            {
				return null;
			}
            else
            {
                poseDetectionBox = boxes[0];
            }
        }

        Texture2D roi = ExtractROIFromBox(inputTexture, poseDetectionBox.Value);
		return roi;
    }

    private void RunEstimationModel(Texture2D inputTexture)
    {
		bool status;
		PreprocessTextureEstimation(inputTexture);

        int inputBlobIndex = ailiaPoseEstimation.FindBlobIndexByName("input_1");

        status = ailiaPoseEstimation.SetInputBlobShape(
            new Ailia.AILIAShape
            {
                x = (uint)BLAZEPOSE_DETECTOR_INPUT_CHANNEL_COUNT,
                y = (uint)BLAZEPOSE_ESTIMATOR_INPUT_RESOLUTION,
                z = (uint)BLAZEPOSE_ESTIMATOR_INPUT_RESOLUTION,
                w = 1,
                dim = 4
            },
            inputBlobIndex
        );

        if (status == false)
        {
            Debug.LogError("Could not set input blob shape");
            Debug.LogError(ailiaPoseEstimation.GetErrorDetail());
        }

        status = ailiaPoseEstimation.SetInputBlobData(estimationInputArray, inputBlobIndex);
        if (status == false)
        {
            Debug.LogError("Could not set input blob data");
            Debug.LogError(ailiaPoseEstimation.GetErrorDetail());
        }

        status = ailiaPoseEstimation.Update();
        if (status == false)
        {
            Debug.Log(ailiaPoseEstimation.GetErrorDetail());
        }

        int outputBlobIndex = ailiaPoseEstimation.FindBlobIndexByName("Identity_1");
        status = ailiaPoseEstimation.GetBlobData(estimationScoreBuffer, outputBlobIndex);
        if (status == false)
        {
            Debug.LogError("Could not get pose score output blob data " + outputBlobIndex);
            Debug.LogError(ailiaPoseEstimation.GetErrorDetail());
        }
        float poseScore = estimationScoreBuffer[0];

        outputBlobIndex = ailiaPoseEstimation.FindBlobIndexByName("Identity");
        status = ailiaPoseEstimation.GetBlobData(estimationOutputBuffer, outputBlobIndex);

        if (status == false)
        {
            Debug.LogError("Could not get output blob data " + outputBlobIndex);
            Debug.LogError(ailiaPoseEstimation.GetErrorDetail());
        }

        DecodeAndProcessLandmarks();

        if (poseScore < 0.3f)
        {
            poseDetectionBox = null;
        }
        else
        {
            Vector2 hips = (landmarks[(int) BodyPartIndex.LeftHip].position + landmarks[(int) BodyPartIndex.RightHip].position) / 2;
            Vector2 nose = landmarks[(int) BodyPartIndex.Nose].position;
            Vector2 aboveHead = hips + 1f * (nose - hips);
            hips.y = 1 - hips.y;
            aboveHead.y = 1 - aboveHead.y;

            float halfBoxSize = (aboveHead - hips).sqrMagnitude;

            Box newBox = new Box
            {
                area = 4 * Mathf.Pow(halfBoxSize, 2),
                keypoints = new Vector2[] { hips, aboveHead },
                xMin = hips.x - halfBoxSize,
                xMax = hips.x + halfBoxSize,
                yMin = hips.y - halfBoxSize,
                yMax = hips.y + halfBoxSize,
            };
        }

        RecenterLandmarksOnHips();

        SmoothLandmarks(Time.time - prevTime);
        prevTime = Time.time;
    }

    private float Sigmoid(float x)
    {
        return 1.0f / (1.0f + Mathf.Exp(-x));
    }

    private void DecodeAndProcessBoxes()
    {
        List<Box> remainingBoxes = new List<Box>();
        boxes = new List<Box>();

        float xScale = BLAZEPOSE_DETECTOR_INPUT_RESOLUTION;
        float yScale = BLAZEPOSE_DETECTOR_INPUT_RESOLUTION;
        float wScale = BLAZEPOSE_DETECTOR_INPUT_RESOLUTION;
        float hScale = BLAZEPOSE_DETECTOR_INPUT_RESOLUTION;

        Func<int, int, float> getFloat = (tI, cI) => rawBoxesOutput[tI * BLAZEPOSE_DETECTOR_TENSOR_SIZE + cI];

        for (int tI = 0; tI < BLAZEPOSE_DETECTOR_TENSOR_COUNT; ++tI)
        {
            float score = Sigmoid(Mathf.Clamp(rawScoresOutput[tI], -BLAZEPOSE_DETECTOR_RAW_SCORE_THRESHOLD, BLAZEPOSE_DETECTOR_RAW_SCORE_THRESHOLD));

            if (score < BLAZEPOSE_DETECTOR_MINIMUM_SCORE_THRESHOLD)
            {
                continue;
            }

            float xCenter = getFloat(tI, 0) / xScale * anchors[tI, 2] + anchors[tI, 0];
            float yCenter = getFloat(tI, 1) / yScale * anchors[tI, 3] + anchors[tI, 1];
            float width = getFloat(tI, 2) / wScale * anchors[tI, 2];
            float height = getFloat(tI, 3) / hScale * anchors[tI, 3];

            Vector2[] keypoints = new Vector2[BLAZEPOSE_DETECTOR_KEYPOINT_COUNT];

            for (int i = 0; i < BLAZEPOSE_DETECTOR_KEYPOINT_COUNT; ++i)
            {
                int index = 4 + 2 * i;
                keypoints[i] = new Vector2(
                    getFloat(tI, index) / xScale * anchors[tI, 2] + anchors[tI, 0],
                    getFloat(tI, index + 1) / yScale * anchors[tI, 3] + anchors[tI, 1]
                );
            }

            remainingBoxes.Add(new Box
            {
                xMin = xCenter - width / 2,
                yMin = yCenter - height / 2,
                xMax = xCenter + width / 2,
                yMax = yCenter + height / 2,
                keypoints = keypoints,
                area = width * height,
                score = score
            });
        }

        // Merge boxes by weighted non-max suppression
        remainingBoxes.Sort((a, b) => Math.Sign(b.score - a.score));

        while (remainingBoxes.Count > 0)
        {
            Box referenceBox = remainingBoxes[0];
            Box mergedBox = referenceBox;
            remainingBoxes.RemoveAt(0);

            for (int i = 0; i < remainingBoxes.Count; ++i)
            {
                if (referenceBox.GetJaccardOverlap(remainingBoxes[i]) > BLAZEPOSE_DETECTOR_MINIMUM_OVERLAP_THRESHOLD)
                {
                    mergedBox.Merge(remainingBoxes[i]);
                    remainingBoxes.RemoveAt(i);
                    --i;
                }
            }

            mergedBox.FinalizeMerge();
            boxes.Add(mergedBox);
        }
    }

    private void DecodeAndProcessLandmarks()
    {
        landmarks = new List<Landmark>();

        for (int i = 0; i < 33; ++i)
        {
            float x = estimationOutputBuffer[i * 5] / BLAZEPOSE_ESTIMATOR_INPUT_RESOLUTION;
            float y = estimationOutputBuffer[i * 5 + 1] / BLAZEPOSE_ESTIMATOR_INPUT_RESOLUTION;
            float z = estimationOutputBuffer[i * 5 + 2] / BLAZEPOSE_ESTIMATOR_INPUT_RESOLUTION;
            float visibility = estimationOutputBuffer[i * 5 + 3];
            float presence = estimationOutputBuffer[i * 5 + 4];

            landmarks.Add(new Landmark
            {
                position = new Vector3(x, 1 - y, z / 2),
                confidence = Sigmoid(Math.Min(visibility, presence))
            }); ;
        }
    }

    private void RecenterLandmarksOnHips()
    {
        Vector3 poseOrigin = (landmarks[(int)BodyPartIndex.LeftHip].position + landmarks[(int)BodyPartIndex.RightHip].position) / 2;

        for (int i = 0; i < 33; ++i)
        {
            landmarks[i] = new Landmark
            {
                position = landmarks[i].position - poseOrigin,
                confidence = landmarks[i].confidence
            };
        }
    }

    private void AdjustBodyPartsToReferenceModel()
    {
        foreach (var connection in connectionToAdjust)
        {
            if (blazePoseToBodyBone.ContainsKey(connection.Item1) == false || blazePoseToBodyBone.ContainsKey(connection.Item2) == false)
            {
                continue;
            }

            Transform startTransform = rendererLandmarks.transform.GetChild((int)connection.Item1);
            Transform endTransform = rendererLandmarks.transform.GetChild((int)connection.Item2);

            float distance = 0;

            if (connectionTargetLength.ContainsKey(connection))
            {
                distance = connectionTargetLength[connection]; 
            }
            else
            {
                continue;
            }

            Vector3 connectionVector = endTransform.position - startTransform.position;
            Vector3 differenceVector = (distance - connectionVector.magnitude) * connectionVector.normalized;

            var affectedParts = bodyBranches.Select(a =>
            {
                int partOccurenceIndex = Array.IndexOf(a, connection.Item2);

                if (partOccurenceIndex == -1)
                {
                    return Enumerable.Empty<BodyPartIndex>();
                }

                return a.Skip(partOccurenceIndex);
            }).SelectMany(a => a).Distinct().ToArray();

            foreach (var affectedPart in affectedParts)
            {
                rendererLandmarks.transform.GetChild((int)affectedPart).position += differenceVector;
            }
        }
    }

    private void SmoothLandmarks(float deltaTime = 0.016f)
    {
        if (smoothLandmarks.Count == 0)
        {
            for (int i = 0; i < landmarks.Count; ++i)
            {
                smoothLandmarks.Add(landmarks[i]);
                smoothDiffLandmarks.Add(new Landmark
                {
                    index = i,
                    confidence = landmarks[i].confidence,
                    position = Vector2.zero
                });
            }

			return;
		}

        float smoothingFactor = 1 / (2 * Mathf.PI * smoothingCutoff * deltaTime);

        for (int i = 0; i < landmarks.Count; ++i)
        {
            Landmark current = landmarks[i];
            Landmark smooth = new Landmark
            {
                index = i,
                confidence = current.confidence,
                position = smoothingFactor * current.position + (1 - smoothingFactor) * smoothLandmarks[i].position
            };

            smoothLandmarks[i] = smooth;
        }
    }

    private void CreateRenderer()
    {
        if (landmarks.Count > 0)
        {
            renderer = new GameObject("Body renderer");
            rendererLandmarks = new GameObject("Landmarks");
            rendererConnections = new GameObject("Connections");

            var pos = referenceModel.transform.position;
            pos.y += 0.75f;
            renderer.transform.position = pos;

            rendererLandmarks.transform.parent = renderer.transform;
            rendererConnections.transform.parent = renderer.transform;
            rendererLandmarks.transform.localPosition = Vector3.zero;
            rendererConnections.transform.localPosition = Vector3.zero;
            rendererLandmarks.transform.localScale = Vector3.one;
            rendererConnections.transform.localScale = Vector3.one;

            foreach (var bodyPartIndex in Enum.GetValues(typeof(BodyPartIndex)))
            {
                GameObject point = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                point.name = Enum.GetName(typeof(BodyPartIndex), bodyPartIndex);
                point.transform.parent = rendererLandmarks.transform;
                point.transform.localScale = 0.01f * Vector3.one;
                point.transform.localPosition = Vector3.zero;
            }

            foreach (var connection in BodyPartConnections)
            {
                GameObject line = new GameObject($"{Enum.GetName(typeof(BodyPartIndex), connection.Item1)}-{Enum.GetName(typeof(BodyPartIndex), connection.Item2)}");
                line.transform.parent = rendererConnections.transform;
                line.transform.localPosition = Vector3.zero;

                LineRenderer lineRenderer = line.AddComponent<LineRenderer>();
                lineRenderer.useWorldSpace = false;
                lineRenderer.positionCount = 2;
                lineRenderer.widthMultiplier = 0.01f;

                lineRenderer.SetPosition(0, Vector3.zero);
                lineRenderer.SetPosition(1, Vector3.zero);
            }

            renderer.transform.localScale = new Vector3(1.3f, 2, 1.3f);

        }
    }

    private void UpdateRenderer()
    {
        if (renderer == null)
        {
            CreateRenderer();
        }

		List<Landmark> landmarks = (smoothLandmarks.Count > 0 && Smooth ? smoothLandmarks : this.landmarks);

		renderer?.SetActive((landmarks?.Count ?? 0) > 0);
        if ((renderer?.activeSelf ?? false) == false && (landmarks?.Count ?? 0) == 0)
        {
            return;
        }

        foreach (var bodyPartIndex in Enum.GetValues(typeof(BodyPartIndex)))
        {
            Transform point = rendererLandmarks.transform.GetChild((int) bodyPartIndex);
            Vector3 p = landmarks[(int)bodyPartIndex].position;
 
            point.localPosition = p;
        }

        if (referenceModel != null)
        {
            AdjustBodyPartsToReferenceModel();
        }

		for (int i = 0; i < BodyPartConnections.Length; ++i)
		{
			var connection = BodyPartConnections[i];
			LineRenderer lineRenderer = rendererConnections.transform.GetChild(i).GetComponent<LineRenderer>();

			Vector3 p1 = rendererLandmarks.transform.GetChild((int)connection.Item1).localPosition;
			lineRenderer.SetPosition(0, p1);
			Vector3 p2 = rendererLandmarks.transform.GetChild((int)connection.Item2).localPosition;
			lineRenderer.SetPosition(1, p2);
		}
	}

    #region IDisposable Support
    private bool disposedValue = false; // To detect redundant calls

	protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            ailiaPoseDetection.Close();
            ailiaPoseEstimation.Close();
            ailiaPoseDetection = null;
            ailiaPoseEstimation = null;
            computeTexture?.Release();
            preprocessBuffer?.Release();

            disposedValue = true;
        }
    }

    // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
    ~AiliaBlazepose()
    {
      // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
      Dispose(false);
    }

    // This code added to correctly implement the disposable pattern.
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        Dispose(true);
        // TODO: uncomment the following line if the finalizer is overridden above.
        GC.SuppressFinalize(this);
    }
    #endregion

}
