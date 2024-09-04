/* AILIA Unity Plugin Hand Detector Sample */
/* Copyright 2022 AXELL CORPORATION */

using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Runtime.InteropServices;

using UnityEngine;
using UnityEngine.UI;

using ailia;

namespace ailiaSDK {
    public class AiliaHandDetectorsSample : AiliaRenderer {
        // Choose model
        public enum HandDetectorModels
        {
                blazehand
        }

        [SerializeField]
        private HandDetectorModels ailiaModelType = HandDetectorModels.blazehand;
        [SerializeField]
        private GameObject UICanvas = null;

        //Settings
        [SerializeField]
        private bool gpu_mode = false;
        [SerializeField]
        private int camera_id = 0;
        

        //Result
        RawImage raw_image = null;
        Text label_text = null;
        Text mode_text = null;

        // Parameter
        private bool[] presence = new bool[2];

        //Preview
        private Texture2D preview_texture = null;

        //AILIA
        private AiliaModel ailia_palm_detector = new AiliaModel();
        private AiliaModel ailia_hand_detector = new AiliaModel();

        private AiliaBlazehand blaze_hand = new AiliaBlazehand();

        private AiliaCamera ailia_camera = new AiliaCamera();
        private AiliaDownload ailia_download = new AiliaDownload();

        // AILIA open file
        private bool FileOpened = false;

        private void CreateAiliaDetector(HandDetectorModels modelType)
        {
            string asset_path = Application.temporaryCachePath;
            var urlList = new List<ModelDownloadURL>();
            if (gpu_mode)
            {
                ailia_palm_detector.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
                ailia_hand_detector.Environment(Ailia.AILIA_ENVIRONMENT_TYPE_GPU);
            }
            switch (modelType)
            {		
                case HandDetectorModels.blazehand:
                    mode_text.text = "ailia hand Detector";

                    urlList.Add(new ModelDownloadURL() { folder_path = "blazepalm", file_name = "blazepalm.onnx.prototxt" });
                    urlList.Add(new ModelDownloadURL() { folder_path = "blazepalm", file_name = "blazepalm.onnx" });
                    urlList.Add(new ModelDownloadURL() { folder_path = "blazehand", file_name = "blazehand.onnx.prototxt" });
                    urlList.Add(new ModelDownloadURL() { folder_path = "blazehand", file_name = "blazehand.onnx" });

                    StartCoroutine(ailia_download.DownloadWithProgressFromURL(urlList, () =>
                    {
                        FileOpened = ailia_palm_detector.OpenFile(asset_path + "/blazepalm.onnx.prototxt", asset_path + "/blazepalm.onnx");
                        FileOpened = ailia_hand_detector.OpenFile(asset_path + "/blazehand.onnx.prototxt", asset_path + "/blazehand.onnx");
                    }));

                    break;

                default:
                    Debug.Log("Others ailia models are working in progress.");
                    break;
            }
        }


        private void DestroyAiliaDetector()
        {
            ailia_palm_detector.Close();
            ailia_hand_detector.Close();
        }

        // Use this for initialization
        void Start()
        {
			AiliaLicense.CheckAndDownloadLicense();
            SetUIProperties();
            CreateAiliaDetector(ailiaModelType);
            ailia_camera.CreateCamera(camera_id);
        }

        // Update is called once per frame
        void Update()
        {
            if (!ailia_camera.IsEnable())
            {
                return;
            }
            if (!FileOpened)
            {
                return;
            }

            //Clear result
            Clear();

            //Get camera image
            int tex_width = ailia_camera.GetWidth();
            int tex_height = ailia_camera.GetHeight();
            if (preview_texture == null)
            {
                preview_texture = new Texture2D(tex_width, tex_height);
                raw_image.texture = preview_texture;
            }
            Color32[] camera = ailia_camera.GetPixels32();

            //Blazehand
            long detection_start_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
            List<AiliaBlazehand.HandInfo> result_detections = blaze_hand.Main(ailia_palm_detector, ailia_hand_detector, camera, tex_width, tex_height);
            long detection_end_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
            long detection_time = (detection_end_time - detection_start_time);

            // Draw result
            if(ailiaModelType==HandDetectorModels.blazehand){
                // Blazehand
                presence[0] = false;
                presence[1] = false;
                if(AiliaBlazehand.tracking)
                {
                    for(int i = 0; i < result_detections.Count; i++)
                    {
                        AiliaBlazehand.HandInfo hand = result_detections[i];

                        // Draw points
                        for (int k = 0; k < AiliaBlazehand.HAND_NUM_KEYPOINTS; k++)
                        {
                            int x = (int)hand.landmarks[k].x;
                            int y = (int)hand.landmarks[k].y;
                            DrawRect2D(Color.red, x, y, 2, 2, tex_width, tex_height);
                        }

                        // Draw lines
                        for (int k = 0; k < AiliaBlazehand.HAND_NUM_CONNECTIONS; k++)
                        {
                            int x0 = (int)hand.landmarks[AiliaBlazehand.HAND_CONNECTIONS[k,0]].x;
                            int y0 = (int)hand.landmarks[AiliaBlazehand.HAND_CONNECTIONS[k,0]].y;

                            int x1 = (int)hand.landmarks[AiliaBlazehand.HAND_CONNECTIONS[k,1]].x;
                            int y1 = (int)hand.landmarks[AiliaBlazehand.HAND_CONNECTIONS[k,1]].y;

                            DrawLine(Color.green, x0, y0, 0, x1, y1, 0, tex_width, tex_height);
                        }

                            if(hand.handed > 0.5)
                            {
                                presence[0] = true;
                            }
                            else
                            {
                                presence[1] = true;
                            }
                    }
                }

                string text;
                if(presence[0] && presence[1])
                {
                    text = "Left and right";
                }
                else if(presence[0])
                {
                    text = "Right";
                }
                else if(presence[1])
                {
                    text = "Left";
                }
                else
                {
                    text = "No hand";
                }
                
                DrawText(Color.magenta, text, 10, 10, tex_width, tex_height);
            }

            if (label_text != null)
            {
                if(ailiaModelType==HandDetectorModels.blazehand){
                    label_text.text = detection_time + "ms\n" + ailia_hand_detector.EnvironmentName();
                }
            }

            //Apply
            preview_texture.SetPixels32(camera);
            preview_texture.Apply();
        }

        void SetUIProperties()
        {
            if (UICanvas == null) return;
            // Set up UI for AiliaDownloader
            var downloaderProgressPanel = UICanvas.transform.Find("DownloaderProgressPanel");
            ailia_download.DownloaderProgressPanel = downloaderProgressPanel.gameObject;
            // Set up lines
            line_panel = UICanvas.transform.Find("LinePanel").gameObject;
            lines = UICanvas.transform.Find("LinePanel/Lines").gameObject;
            line = UICanvas.transform.Find("LinePanel/Lines/Line").gameObject;
            text_panel = UICanvas.transform.Find("TextPanel").gameObject;
            text_base = UICanvas.transform.Find("TextPanel/TextHolder").gameObject;

            raw_image = UICanvas.transform.Find("RawImage").gameObject.GetComponent<RawImage>();
            label_text = UICanvas.transform.Find("LabelText").gameObject.GetComponent<Text>();
            mode_text = UICanvas.transform.Find("ModeLabel").gameObject.GetComponent<Text>();
        }

        void OnApplicationQuit()
        {
            DestroyAiliaDetector();
            ailia_camera.DestroyCamera();
        }

        void OnDestroy()
        {
            DestroyAiliaDetector();
            ailia_camera.DestroyCamera();
        }
    }
}
