// E2Pose

using ailiaSDK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

using ailia;

public struct E2PoseLandmark
{
    public int index;
    public Vector3 position;
    public float confidence;
}


public enum E2PoseBodyPartIndex
{
    Nose = 0,
    LeftEye,
    RightEye,
    LeftEar,
    RightEar,
    LeftShoulder,
    RightShoulder,
    LeftElbow,
    RightElbow,
    LeftWrist,
    RightWrist,
    LeftHip,
    RightHip,
    LeftKnee,
    RightKnee,
    LeftAnkle,
    RightAnkle,
}

public class AiliaE2Pose : IDisposable
{

    private AiliaModel ailiaPoseEstimation = new AiliaModel();
    private int modelChannel = 3;
    private int modelWidth = 0;
    private int modelHeight = 0;
    const int E2PoseKeyPointN = 17;

    /*
    private bool EnvironmentWithoutFP16(AiliaModel model, int type)
    {
        int count = model.GetEnvironmentCount();
        if (count == -1)
        {
            return false;
        }

        for (int i = 0; i < count; i++)
        {
            Ailia.AILIAEnvironment env = model.GetEnvironment(i);
            if (env == null)
            {
                return false;
            }

            if (env.type == type)
            {
                if ((env.props & Ailia.AILIA_ENVIRONMENT_PROPERTY_FP16) != 0)
                {
                    continue;
                }
                if (!model.SelectEnvironment(i))
                {
                    return false;
                }
                if (env.backend == Ailia.AILIA_ENVIRONMENT_BACKEND_CUDA || env.backend == Ailia.AILIA_ENVIRONMENT_BACKEND_VULKAN)
                {
                    return true;    //優先
                }
            }
        }
        return true;
    }
    */

    public AiliaE2Pose(bool gpuMode, string protoPath, string weightPath, int poseWidth, int poseHeight)
    {
        bool status;

        if (gpuMode)
        {
            ailiaPoseEstimation.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
        }
        status = ailiaPoseEstimation.OpenFile(protoPath, weightPath);
        if (status == false)
        {
            string message = $"Could not load model {weightPath}";
            Debug.LogError(message);
            throw new Exception(message);
        }

        Debug.Log($"Model loaded {weightPath}");

        modelWidth = poseWidth;
        modelHeight = poseHeight;

        inputArray = new float[modelWidth * modelHeight * modelChannel];
        /*
        rawBoxesOutput = new float[BLAZEPOSE_DETECTOR_TENSOR_COUNT * BLAZEPOSE_DETECTOR_TENSOR_SIZE];
        rawScoresOutput = new float[BLAZEPOSE_DETECTOR_TENSOR_COUNT];
        estimationScoreBuffer = new float[1];
        estimationInputArray = new float[BLAZEPOSE_ESTIMATOR_INPUT_RESOLUTION * BLAZEPOSE_ESTIMATOR_INPUT_RESOLUTION * BLAZEPOSE_DETECTOR_INPUT_CHANNEL_COUNT];
        estimationOutputBuffer = new float[BLAZEPOSE_ESTIMATOR_TENSOR_COUNT * BLAZEPOSE_ESTIMATOR_TENSOR_SIZE];
        */
    }

    private float[] inputArray;
    private float[] rawBoxesOutput;
    private float[] rawScoresOutput;
    /*
    private List<Box> boxes;
    private Box? poseDetectionBox;
    RenderTexture preprocessBuffer;
    */

    private void Preprocess(Color32 [] camera, int tex_width, int tex_height)
    {
        int factor = 1;
        for (int heightIndex = 0; heightIndex < modelHeight; heightIndex++)
        {
            for (int widthIndex = 0; widthIndex < modelWidth; widthIndex++)
            {
                int index = (int) (((heightIndex * modelWidth) + widthIndex) * modelChannel);
				Color32 value = camera[(modelHeight - heightIndex - 1) * modelWidth + widthIndex];

				inputArray[index + 0] = value.r * factor;
				inputArray[index + 1] = value.g * factor;
				inputArray[index + 2] = value.b * factor;
			}
        }
    }

    public List<List<E2PoseLandmark>> landmarks = new List<List<E2PoseLandmark>>();


    public List<AiliaPoseEstimator.AILIAPoseEstimatorObjectPose> RunPoseEstimation(Color32 [] camera, int tex_width, int tex_height)
    {
		bool status;

        // Preprocess Image
		Preprocess(camera, tex_width, tex_height);

        // Set input blob shape (because model have unsettled shape)
        int inputBlobIndex = ailiaPoseEstimation.FindBlobIndexByName("e2pose/inputimg:0");
        status = ailiaPoseEstimation.SetInputBlobShape(
            new Ailia.AILIAShape
            {
                x = (uint)modelChannel,
                y = (uint)modelWidth,
                z = (uint)modelHeight,
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

        // Set input data
        status = ailiaPoseEstimation.SetInputBlobData(inputArray, inputBlobIndex);
        if (status == false)
        {
            Debug.LogError("Could not set input blob data");
            Debug.LogError(ailiaPoseEstimation.GetErrorDetail());
        }

        // Infer
        status = ailiaPoseEstimation.Update();
        if (status == false)
        {
            Debug.Log(ailiaPoseEstimation.GetErrorDetail());
        }

        // Get output data
        int outputBlobIndex = ailiaPoseEstimation.FindBlobIndexByName("E2Pose_Inference/E2Pose_pose_stage00_reshape_pv/concat:0");

        Ailia.AILIAShape scoreShape = new Ailia.AILIAShape();
        scoreShape = ailiaPoseEstimation.GetBlobShape(outputBlobIndex);

        float[] scoreBuffer = new float[scoreShape.x * scoreShape.y * scoreShape.z * scoreShape.w];
        status = ailiaPoseEstimation.GetBlobData(scoreBuffer, outputBlobIndex);
        if (status == false)
        {
            Debug.LogError("Could not get score blob data " + outputBlobIndex);
            Debug.LogError(ailiaPoseEstimation.GetErrorDetail());
        }

        outputBlobIndex = ailiaPoseEstimation.FindBlobIndexByName("E2Pose_Inference/E2Pose_pose_stage00_reshape_kvxy/concat:0");

        Ailia.AILIAShape posShape = new Ailia.AILIAShape();
        posShape = ailiaPoseEstimation.GetBlobShape(outputBlobIndex);

        float[] posBuffer = new float[posShape.x * posShape.y * posShape.z * posShape.w];
        status = ailiaPoseEstimation.GetBlobData(posBuffer, outputBlobIndex);
        if (status == false)
        {
            Debug.LogError("Could not get pos blob data " + outputBlobIndex);
            Debug.LogError(ailiaPoseEstimation.GetErrorDetail());
        }

        // Poset process
        DecodeAndProcessLandmarks(scoreBuffer, posBuffer);

        return GetResult();
    }

    private void DecodeAndProcessLandmarks(float [] scoreBuffer, float [] posBuffer)
    {
        landmarks = new List<List<E2PoseLandmark>>();
        for (int j = 0; j < posBuffer.Length; j++){
            float score = posBuffer[j];
            float th = 0.5f;
            if (score < th){
                continue;
            }

            List<E2PoseLandmark> landmark = new List<E2PoseLandmark>();

            for (int i = 0; i < E2PoseKeyPointN; ++i)
            {
                float c = scoreBuffer[j * E2PoseKeyPointN * 3 + i * 3 + 0];
                float x = scoreBuffer[j * E2PoseKeyPointN * 3 + i * 3 + 1] * modelWidth;
                float y = scoreBuffer[j * E2PoseKeyPointN * 3 + i * 3 + 2] * modelHeight;
                //float visibility = scoreBuffer[i * 5 + 3];
                //float presence = scoreBuffer[i * 5 + 4];

                landmark.Add(new E2PoseLandmark
                {
                    position = new Vector3(x, y, 0),
                    confidence = c
                });
            }

            landmarks.Add(landmark);
        }
    }

    public List<AiliaPoseEstimator.AILIAPoseEstimatorObjectPose> GetResult(){
        List<AiliaPoseEstimator.AILIAPoseEstimatorObjectPose> result_list=new List<AiliaPoseEstimator.AILIAPoseEstimatorObjectPose>();
        int [] keypoint_list={
            (int)E2PoseBodyPartIndex.Nose,
            (int)E2PoseBodyPartIndex.LeftEye,
            (int)E2PoseBodyPartIndex.RightEye,
            (int)E2PoseBodyPartIndex.LeftEar,
            (int)E2PoseBodyPartIndex.RightEar,
            (int)E2PoseBodyPartIndex.LeftShoulder,
            (int)E2PoseBodyPartIndex.RightShoulder,
            (int)E2PoseBodyPartIndex.LeftElbow,
            (int)E2PoseBodyPartIndex.RightElbow,
            (int)E2PoseBodyPartIndex.LeftWrist,
            (int)E2PoseBodyPartIndex.RightWrist,
            (int)E2PoseBodyPartIndex.LeftHip,
            (int)E2PoseBodyPartIndex.RightHip,
            (int)E2PoseBodyPartIndex.LeftKnee,
            (int)E2PoseBodyPartIndex.RightKnee,
            (int)E2PoseBodyPartIndex.LeftAnkle,
            (int)E2PoseBodyPartIndex.RightAnkle};

        for (int j = 0; j < landmarks.Count; j++){
            List<E2PoseLandmark> landmark = landmarks[j];
            AiliaPoseEstimator.AILIAPoseEstimatorObjectPose one_pose=new AiliaPoseEstimator.AILIAPoseEstimatorObjectPose();
            one_pose.points = new AiliaPoseEstimator.AILIAPoseEstimatorKeypoint[19];
            for(int i=0;i<AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_CNT;i++){
                Vector3 pos = Vector3.zero;
                float conf = 0;
                if(i<=AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_ANKLE_RIGHT){
                    pos = landmark[keypoint_list[i]].position;
                    conf = landmark[keypoint_list[i]].confidence;
                }
                if(i==AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_SHOULDER_CENTER){
                    pos = (landmark[(int)E2PoseBodyPartIndex.LeftShoulder].position + landmark[(int)E2PoseBodyPartIndex.RightShoulder].position)/2;
                    conf = Math.Min(landmark[(int)E2PoseBodyPartIndex.LeftShoulder].confidence,landmark[(int)E2PoseBodyPartIndex.RightShoulder].confidence);
                }
                if(i==AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_BODY_CENTER){
                    pos = (landmark[(int)E2PoseBodyPartIndex.LeftHip].position + landmark[(int)E2PoseBodyPartIndex.RightHip].position + landmark[(int)E2PoseBodyPartIndex.LeftShoulder].position + landmark[(int)E2PoseBodyPartIndex.RightShoulder].position)/4;
                    conf = Math.Min(Math.Min(landmark[(int)E2PoseBodyPartIndex.LeftHip].confidence, landmark[(int)E2PoseBodyPartIndex.RightHip].confidence),Math.Min(landmark[(int)E2PoseBodyPartIndex.LeftShoulder].confidence, landmark[(int)E2PoseBodyPartIndex.RightShoulder].confidence));
                }
                AiliaPoseEstimator.AILIAPoseEstimatorKeypoint keypoint =new AiliaPoseEstimator.AILIAPoseEstimatorKeypoint();
                keypoint.x = pos.x;
                keypoint.y = pos.y;
                keypoint.z_local = pos.z;
                keypoint.score = conf;
                one_pose.points[i] = keypoint;
            }
            result_list.Add(one_pose);
        }

        return result_list;
    }

    #region IDisposable Support
    private bool disposedValue = false; // To detect redundant calls

	protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            ailiaPoseEstimation.Close();
            ailiaPoseEstimation = null;
            disposedValue = true;
        }
    }

    // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
    ~AiliaE2Pose()
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

    public string EnvironmentName(){
        return ailiaPoseEstimation.EnvironmentName();
    }
}
