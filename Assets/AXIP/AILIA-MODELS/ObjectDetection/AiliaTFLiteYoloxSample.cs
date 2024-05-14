/* AILIA TFLITE Unity Plugin Yolox Sample */
/* Copyright 2023 AXELL CORPORATION */

using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Runtime.InteropServices;

using UnityEngine;
using UnityEngine.UI;

using ailia;
using ailiaTFLite;

namespace ailiaSDK
{
public class AiliaTFLiteYoloxSample {
    //Settings
    public const Int32 tflite_memory_mode = AiliaTFLite.AILIA_TFLITE_MEMORY_MODE_DEFAULT;
    public const UInt32 tflite_flags = AiliaTFLite.AILIA_TFLITE_FLAG_NONE;

    /**
    * Rectangle
    */
    struct Rect {
        public float start_x;
        public float end_x;
        public float start_y;
        public float end_y;
        public float area;
    };

    //AILIA tflite
    private AiliaTFLiteModel tflite_model = new AiliaTFLiteModel();
    // input tensor info
    private float [] input_data;
    private Int32 [] input_shape;
    // output tensor info
    private float [] output_data;
    private Int32 [] output_shape;

    // Result
    public RawImage raw_image=null;
    public Text label_text=null;
    // Preview
    private Texture2D preview_texture=null;

    private const uint IMAGE_FORMAT_RGBA_B2T  = (0x10);

    private bool ConvertImageData(float[] dst_data, int dst_width, int dst_height,
        Color32 [] src_data, int src_width, int src_height, uint src_format){

        if(src_format != IMAGE_FORMAT_RGBA_B2T) return false;

        // neighest neighbor interpolation

        float ystep = (float)src_height / (float)dst_height;
        float xstep = (float)src_width / (float)dst_width;

        float ysum = 0;
        for(int dy=0; dy<dst_height; dy++){
            int sy = (int)ysum;
            sy = src_height - sy - 1;   // B2T
            float xsum = 0;
            for(int dx=0; dx<dst_width; dx++){
                int sx = (int)xsum;
                Color32 p = src_data[src_width * sy + sx];
                dst_data[dy * dst_width * 3 + dx * 3 + 0] = p.b;
                dst_data[dy * dst_width * 3 + dx * 3 + 1] = p.g;
                dst_data[dy * dst_width * 3 + dx * 3 + 2] = p.r;
                xsum += xstep;
            }
            ysum += ystep;
        }

        return true;
    }

    // Update is called once per frame
    public List<AiliaDetector.AILIADetectorObject> ComputeFromImageB2T (Color32[] camera , int tex_width, int tex_height, float detection_threshold, float iou_threshold) {

        // convert size/format of input image and store into NN input buffer.
        ConvertImageData(input_data, input_shape[2], input_shape[1],
            camera, tex_width, tex_height, IMAGE_FORMAT_RGBA_B2T
        );

        // do inference
        tflite_model.SetInputTensorData(input_data, 0);
        tflite_model.Predict();
        tflite_model.GetOutputTensorData(output_data, 0);

        // post process
        List<AiliaDetector.AILIADetectorObject> objects = new List<AiliaDetector.AILIADetectorObject>();
        yolox_post_process(ref objects, detection_threshold, iou_threshold, output_data);
        return objects;
    }

    public bool CreateAiliaTFLite(string path){
        DestroyAiliaTFLite();

        // open model from file
        int env_id = 0;
        #if UNITY_ANDROID && !UNITY_EDITOR
            env_id = AiliaTFLite.AILIA_TFLITE_ENV_NNAPI;
        #else
            env_id = AiliaTFLite.AILIA_TFLITE_ENV_REFERENCE;
        #endif
        tflite_model.OpenFile(path, env_id, tflite_memory_mode, tflite_flags);
        bool reference_only = false;
        //tflite_model.SelectDevice(ref devices, reference_only);
        tflite_model.AllocateTensors();

        tflite_model.GetInputTensorShape(ref input_shape, 0);
        tflite_model.GetOutputTensorShape(ref output_shape, 0);

        input_data = new float[input_shape[2] * input_shape[1] * 3];
        output_data = new float[output_shape[0] * output_shape[1] * output_shape[2]];
        return true;
    }

    public void DestroyAiliaTFLite(){
        tflite_model.Close();
    }

    public string GetDeviceName(){
        #if UNITY_ANDROID && !UNITY_EDITOR
        return "NNAPI";
        #else
        return "CPU";
        #endif
    }

    private int yolox_post_process(ref List<AiliaDetector.AILIADetectorObject> objects,
            float threshold,
            float threshold_iou, float [] buf){
        int iw = input_shape[2];
        int ih = input_shape[1];
        int[] oh = {ih / 8, ih / 16, ih / 32};
        int[] ow = {iw / 8, iw / 16, iw / 32};
        int num_cells = oh[0] * ow[0] + oh[1] * ow[1] + oh[2] * ow[2];
        int category_name_tbl_length = 80;
        int num_elements = 5 + category_name_tbl_length;

        if(num_cells != output_shape[1] || num_elements != output_shape[2]){
            Debug.Log($"error! yolox output_shape[1,2] = ({output_shape[1]}, {output_shape[2]}) != ({num_cells}, {num_elements})\n");
            return -1;
        }

        var boxes = new List<Rect>();
        var probs = new List<float>();
        var categories = new List<uint>();

        int bufp = 0;
        for(int s=0; s<3; s++){
            float stride = Mathf.Pow(2, 3 + s);
            for(int y=0; y<oh[s]; y++){
                for(int x=0; x<ow[s]; x++){
                    // calc max score
                    float max_score = 0;
                    int max_class = 0;
                    for(int cls=0; cls<category_name_tbl_length; cls++){
                        float sc = buf[bufp + 5 + cls];
                        if(sc > max_score){
                            max_score = sc;
                            max_class = cls;
                        }
                    }
                    float score = max_score;
                    float c = buf[bufp + 4];
                    score *= c;
                    if(score >= threshold){
                        float cx = buf[bufp + 0];
                        float cy = buf[bufp + 1];
                        float w = buf[bufp + 2];
                        float h = buf[bufp + 3];
                        float bb_cx = (cx + x) * stride;
                        float bb_cy = (cy + y) * stride;
                        float bb_ww = Mathf.Exp(w) * stride + 1;
                        float bb_hh = Mathf.Exp(h) * stride + 1;
                        //Debug.Log($"s={s}, x={x}, y={y}, class[{max_class}, {category_name_tbl[max_class]}], score={score}, cx={cx}, cy={cy}, w={w}, h={h}, c={c}, bb=[{bb_cx},{bb_cy},{bb_ww},{bb_hh}]\n");
                        
                        // normalize by image width, height
                        bb_cx /= iw;
                        bb_cy /= ih;
                        bb_ww /= iw;
                        bb_hh /= ih;

                        // add this candidate
                        boxes.Add(create_rect_start_end(bb_cx - bb_ww / 2.0f, bb_cy - bb_hh / 2.0f, bb_cx + bb_ww / 2.0f, bb_cy + bb_hh / 2.0f));
                        probs.Add(score);
                        categories.Add((uint)max_class);
                    }
                    bufp += num_elements;
                }
            }
        }
        // NMS and store results
        boxes_to_object_list(ref boxes, ref probs, ref categories, ref objects, threshold, threshold_iou);
        return 0;
    }

    private void boxes_to_object_list(ref List<Rect> boxes,
                                ref List<float> probs,
                                ref List<uint> categories,
                                ref List<AiliaDetector.AILIADetectorObject> objects,
                                float threshold_score,
                                float threshold_iou) {
        uint len = (uint)boxes.Count;
        var indices = pick_indices(ref boxes, ref probs, len, threshold_score, threshold_iou, len);
        foreach (var idx in indices) {
            var box = boxes[idx];
            float w = box.end_x - box.start_x;
            float h = box.end_y - box.start_y;
            if (w < 0 || h < 0)
                continue;
            AiliaDetector.AILIADetectorObject obj = new AiliaDetector.AILIADetectorObject();
            obj.category = categories[idx];
            obj.prob = probs[idx];
            obj.x = box.start_x;
            obj.y = box.start_y;
            obj.w = w;
            obj.h = h;
            objects.Add(obj);
        }
    }

    List<int> pick_indices(ref List<Rect> rect_list,
                           ref List<float> scores,
                           uint spatial_dimension,
                           float score_threshold,
                           float iou_threashold,
                           uint max_output_boxes_per_class){
        // prepare score array
        var score_list = new List<KeyValuePair<int, float>>();
        for (int i = 0; i < spatial_dimension; ++i) {
            var wd = new KeyValuePair<int, float>(i, scores[i]);
            if (wd.Value <= score_threshold) {
                continue;
            }
            score_list.Add(wd);
        }

        // sort scores by ascending order
        score_list.Sort((lhs, rhs) => (int)(rhs.Value - lhs.Value));

        // remove overlapped rects
        List<bool> is_exist = new List<bool>(new bool[score_list.Count]);
        for(int i=0; i<score_list.Count; i++) is_exist[i] = true;
        for(int i=0; i<(score_list.Count-1); i++){
            if(! is_exist[i]) continue;
            for(int j=(i+1); j<score_list.Count; j++){
                var iou = box_iou(rect_list[score_list[i].Key], rect_list[score_list[j].Key]);
                if (iou > iou_threashold) {
                    is_exist[j] = false;
                }
            }
        }

        // create result index array
        var r = new List<int>();
        for(int i=0; i<score_list.Count; i++){
            if(is_exist[i]){
                r.Add(score_list[i].Key);
                if(r.Count >= max_output_boxes_per_class){
                    break;
                }
            }
        }
        return r;
    }

    private float overlap(float x10, float x11, float x20, float x21) {
        float s = x10 > x20 ? x10 : x20;
        float e = x11 < x21 ? x11 : x21;
        return e - s;
    }

    private float box_iou(Rect r1, Rect r2) {
        float ow = overlap(r1.start_x, r1.end_x, r2.start_x, r2.end_x);
        float oh = overlap(r1.start_y, r1.end_y, r2.start_y, r2.end_y);
        if ((ow <= 0) || (oh <= 0)) {
            return 0;
        }
        float i = ow * oh;
        float u = r1.area + r2.area - i;
        if ((r1.area <= 0) || (r2.area <= 0) || (u <= 0)) {
            return 0;
        }
        return i / u;
    }

    private Rect create_rect_start_end(float start_x, float stary_y, float end_x, float end_y){
        Rect r;
        r.start_x = start_x;
        r.start_y = stary_y;
        r.end_x = end_x;
        r.end_y = end_y;
        check_and_flip(ref r.start_x, ref r.end_x);
        check_and_flip(ref r.start_y, ref r.end_y);
        r.area = start_end_to_size(r.start_x, r.end_x) * start_end_to_size(r.start_y, r.end_y);
        return r;
    }

    private void check_and_flip(ref float a, ref float b){
        if (a < b) {
            return;
        }
        float tmp = a;
        a = b;
        b = tmp;
    }

    private float start_end_to_size(float start, float end) {
        return Mathf.Abs(end - start);
    }
}
}
