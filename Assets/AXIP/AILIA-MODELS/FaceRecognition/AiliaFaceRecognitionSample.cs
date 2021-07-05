using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ailiaSDK
{

    public class AiliaFaceRecognitionSample : AiliaRenderer
    {
        public int camera_id = 0;

        //Result
        public Text label_text = null;
        public Text mode_text = null;
        public RawImage raw_image = null;

        public GameObject UICanvas = null;


        public bool oneshot;


        private AiliaCamera ailia_camera = new AiliaCamera();
        //        private AiliaDownload ailia_download = new AiliaDownload();

        private AiliaClassifierModel ailia_detector = new AiliaClassifierModel();
        private AiliaModel ailia_estimator = new AiliaPoseEstimatorModel();


        private AiliaImageSource ailia_imagesource;


        float[] anchors;

        const float x_scale = 128.0f;
        const float y_scale = 128.0f;


        private Texture2D preview_texture;
        void InitializeModels()
        {
            var asset_path = Application.temporaryCachePath;

            var detector = "blazeface";
            var landmark = "facemesh";
#if true
            var opt = ".opt";
#else //
            var opt = "";
#endif

            AiliaDownload ailia_download = new AiliaDownload();
            ailia_download.DownloaderProgressPanel = UICanvas.transform.Find("DownloaderProgressPanel").gameObject;

            // TODO:後でailia_download.DownloadWithProgressFromURL()に変更する

            ailia_download.DownloadModelFromUrl($"{detector}", $"{detector}{opt}.onnx");
            ailia_download.DownloadModelFromUrl($"{detector}", $"{detector}{opt}.onnx.prototxt");
            ailia_download.DownloadModelFromUrl($"{landmark}", $"{landmark}{opt}.onnx");
            ailia_download.DownloadModelFromUrl($"{landmark}", $"{landmark}{opt}.onnx.prototxt");


            var detector_path = $"{asset_path}/{detector}{opt}.onnx.prototxt";
            Debug.Log($"{detector_path} : {System.IO.File.Exists(detector_path)}");

            ailia_detector.OpenFile($"{asset_path}/{detector}{opt}.onnx.prototxt", $"{asset_path}/{detector}{opt}.onnx");
            ailia_estimator.OpenFile($"{asset_path}/{landmark}{opt}.onnx.prototxt", $"{asset_path}/{landmark}{opt}.onnx");

            var o_shape = ailia_detector.GetOutputShape();
            Debug.Log($"output shape : {o_shape.dim},{o_shape.x},{o_shape.y},{o_shape.z},{o_shape.w}");

            var inputshape = ailia_detector.GetInputShape();
            Debug.Log($"input shape : {inputshape.dim},{inputshape.x},{inputshape.y},{inputshape.z},{inputshape.w}");
        }

        void Start()
        {
            ailia_camera.CreateCamera(camera_id);
            InitializeModels();

            var ailia_imagesource = gameObject.GetComponent<AiliaImageSource>();

            ailia_imagesource.CreateSource($"file://{Application.dataPath}/AXIP/AILIA-MODELS/FaceRecognition/SampleImage/man.png");
            this.ailia_imagesource = ailia_imagesource;

            anchors = FaceRecognitionUtil.LoadNpy($"{Application.dataPath}/AXIP/AILIA-MODELS/FaceRecognition/anchors.npy.bytes");
        }



        void OnDestroy()
        {
            ailia_camera.DestroyCamera();
        }

        void OnApplicationQuit()
        {
            ailia_camera.DestroyCamera();
        }


        void Update()
        {
            if (oneshot)
            {
                OneshotAppImage();
            }
            else
            {
                if (ailia_camera.IsEnable())
                {
                    if (preview_texture == null)
                    {
                        preview_texture = new Texture2D(ailia_camera.GetWidth(), ailia_camera.GetHeight());
                        raw_image.texture = preview_texture;
                    }
                    var camera = ailia_camera.GetPixels32();
                    preview_texture.SetPixels32(camera);
                    preview_texture.Apply();
                }
            }
        }

        static float Sigmoid(float value)
        {
            return (float)(1f / (1f + Mathf.Pow(Mathf.Epsilon, -value)));
        }

        void ResizeTexture(AiliaImageSource imgsrc, out Texture2D tex256, out Texture2D tex128)
        {
            tex256 = null;
            tex128 = null;

            int size0 = imgsrc.Height;
            int size1 = imgsrc.Width;

            var tempTex = imgsrc.GetTexture(new Rect(0, 0, size1, size0));

            int h1, w1, padh1, padw1;

            if (size1 <= size0)
            {
                h1 = 256;
                w1 = 256 * size1 / size0;
                padw1 = (256 - w1) / 2;
                padh1 = 0;
            }
            else
            {
                h1 = 256 * size0 / size1;
                w1 = 256;
                padw1 = 0;
                padh1 = (256 - h1) / 2;
            }
            Debug.Log($"scaleSize : {w1},{h1}");



            var img1_base = AiliaImageUtil.ResizeTexture(tempTex, w1, h1);

            Color32[] img1_256 = new Color32[256 * 256];
            var img1_pix32 = img1_base.GetPixels32();

            for (int py = 0; py < h1; py++)
            {
                for (int px = 0; px < w1; px++)
                {
                    var idx = padw1 + px + ((py + padh1) * 256);
                    var didx = px + (py * w1);
                    img1_256[idx].r = img1_pix32[didx].b;
                    img1_256[idx].g = img1_pix32[didx].g;
                    img1_256[idx].b = img1_pix32[didx].r;
                    img1_256[idx].a = 255;
                }
            }

            var img1 = new Texture2D(256, 256);
            img1.SetPixels32(img1_256);
            img1.Apply();

            tex256 = img1;
            tex128 = AiliaImageUtil.ResizeTexture(img1, 128, 128);
        }

        void OneshotAppImage()
        {
            // aspect比を維持したまま
            Texture2D img1, img2;

            ResizeTexture(ailia_imagesource, out img1, out img2);

            raw_image.texture = img2; // 表示

            var scaledTexRGB32 = img2.GetPixels32();
            Color[] scaledTexRGB = new Color[scaledTexRGB32.Length];

            float[] inputFloat = new float[128 * 128 * 3];
            float[] outputFloat = new float[896 * 16 + 896];

            // (rgb / 127.5) - 1f
            for (int i = 0, max = scaledTexRGB32.Length; i < max; i++)
            {
                Color32 c = scaledTexRGB32[i];

                float r = (float)c.r / 127.5f - 1f;
                float g = (float)c.g / 127.5f - 1f;
                float b = (float)c.b / 127.5f - 1f;

                inputFloat[i + 0 * scaledTexRGB32.Length] = r;
                inputFloat[i + 1 * scaledTexRGB32.Length] = g;
                inputFloat[i + 2 * scaledTexRGB32.Length] = b;
            }


            // ailia_detector.predict
            ailia_detector.Predict(outputFloat, inputFloat);

            // detectorPostProcess
            DetectorPostProcess(outputFloat);

        }

        void DetectorPostProcess(float[] pred_ailia)
        {
            float[] raw_box = new float[896 * 16];
            float[] raw_score = new float[896 * 1];

            // MEMO: raw_boxとraw_scoreの切り分けはおかしい気がする(predictの結果はraw_boxしか返ってきていないのではなかろうか？)

            for (int i = 0; i < raw_box.Length; ++i)
            {
                raw_box[i] = pred_ailia[i];
            }

            for (int i = 0; i < raw_score.Length; ++i)
            {
                raw_score[i] = pred_ailia[i + (896 * 16)];
            }
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.AppendLine("raw_box:");
                for (int i = 0; i < 16; ++i)
                {
                    sb.AppendLine($"{i}: {raw_box[i]}");
                }
                if (0 < sb.Length)
                {
                    Debug.Log(sb.ToString());
                }
            }


            // raw_score => 896x1 ? 
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.AppendLine("raw_score:");
                for (int i = 0; i < 896; ++i)
                {
                    sb.AppendLine($"{i}: {raw_score[i]}");
                }
                if (0 < sb.Length)
                {
                    Debug.Log(sb.ToString());
                }
            }


            var detections = RawOutputToDetections<float>(raw_box, raw_score, anchors);

        }

        T[] RawOutputToDetections<T>(float[] raw_box, float[] raw_score, float[] anchors)
        {
            T[] r0 = new T[0];

            var detection_boxes = DecodeBoxes<float>(raw_box, anchors);
            float threshold = 100f;
            float[] raw_score_tmp = new float[raw_score.Length];

            for (int i = 0; i < raw_score.Length; ++i)
            {
                raw_score_tmp[i] = Mathf.Clamp(raw_score[i], -threshold, threshold);
            }

            float[] detection_scores = new float[raw_score.Length];

            // calc expit == sigmoid
            // detection_scores = expit(raw_score).squeeze(axis=-1)
            for (var i = 0; i < detection_scores.Length; ++i)
            {
                detection_scores[i] = Sigmoid(raw_score_tmp[i]);
            }

            // # Note: we stripped off the last dimension from the scores tensor
            // # because there is only has one class. Now we can simply use a mask
            // # to filter out the boxes with too low confidence.
            // mask = detection_scores >= min_score_thresh

            var min_score_thresh = 0.75f;

            var mask = detection_scores.Select(x => min_score_thresh <= x).ToArray();
            List<T[]> output_detections = new List<T[]>();

            var boxes = detection_boxes.Where((value, idx) => mask[idx]).ToArray();




            return r0;
        }

        T[] DecodeBoxes<T>(float[] raw_boxes, float[] anchors)
        {
            T[] r0 = new T[0];

            // """Converts the predictions into actual coordinates using
            // the anchor boxes. Processes the entire batch at once.
            // """
            // boxes = np.zeros_like(raw_boxes)
            var boxes = new List<float>(raw_boxes.Length); // raw_boxesと同じサイズで0埋め

            // x_center = raw_boxes[..., 0] / x_scale * anchors[:, 2] + anchors[:, 0]
            // y_center = raw_boxes[..., 1] / y_scale * anchors[:, 3] + anchors[:, 1]

            // w = raw_boxes[..., 2] / w_scale * anchors[:, 2]
            // h = raw_boxes[..., 3] / h_scale * anchors[:, 3]

            // boxes[..., 0] = y_center - h / 2.  # ymin
            // boxes[..., 1] = x_center - w / 2.  # xmin
            // boxes[..., 2] = y_center + h / 2.  # ymax
            // boxes[..., 3] = x_center + w / 2.  # xmax

            // for k in range(num_keypoints):
            // offset = 4 + k*2
            // keypoint_x = raw_boxes[..., offset] / \
            // x_scale * anchors[:, 2] + anchors[:, 0]
            // keypoint_y = raw_boxes[..., offset + 1] / \
            //y_scale * anchors[:, 3] + anchors[:, 1]
            // boxes[..., offset] = keypoint_x
            // boxes[..., offset + 1] = keypoint_y

            // return boxes




            return r0;
        }

    }
}