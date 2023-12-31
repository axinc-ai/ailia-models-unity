using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Runtime.InteropServices;

using System.Linq;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

using System.Drawing;

namespace ailiaSDK
{
	public class AiliaPaddleOCR
	{

		public const int PADDLEOCR_DETECTOR_INPUT_BATCH_SIZE = 1;
		public const int PADDLEOCR_DETECTOR_INPUT_CHANNEL_COUNT = 3;
		public int PADDLEOCR_DETECTOR_INPUT_HEIGHT_SIZE = 832;
		public int PADDLEOCR_DETECTOR_INPUT_WIDTH_SIZE = 1536;

		public const int PADDLEOCR_DETECTOR_OUTPUT_BATCH_SIZE = 1;
		public const int PADDLEOCR_DETECTOR_OUTPUT_CHANNEL_COUNT = 1;
		public const int PADDLEOCR_DETECTOR_OUTPUT_HEIGHT_SIZE = 832;
		public const int PADDLEOCR_DETECTOR_OUTPUT_WIDTH_SIZE = 1536;

		public const float DETECTION_THRESH = 0.3f;

		public const int LIMITED_MAX_WIDTH = 1280;
    	public const int LIMITED_MIN_WIDTH = 16;

		public const int PADDLEOCR_CLASSIFIER_INPUT_BATCH_SIZE = 1;
		public const int PADDLEOCR_CLASSIFIER_INPUT_CHANNEL_COUNT = 3;
		public const int PADDLEOCR_CLASSIFIER_INPUT_HEIGHT_SIZE = 48;
		public const int PADDLEOCR_CLASSIFIER_INPUT_WIDTH_SIZE = 192;

		public const int CLASSIFICATION_IMAGE_HEIGHT = 48;
		public const int CLASSIFICATION_IMAGE_WIDTH = 192;
		
		public const int RECOGNITION_IMAGE_HEIGHT = 32;
		public const int RECOGNITION_IMAGE_WIDTH = 320;

		public const int CAMERA_HEIGHT = 1080;
		public const int CAMERA_WIDTH = 1920;

		public const int IMAGE_SCALE = 4; //処理を軽くするために画像のサイズを調整


		public struct TextInfo
		{
			public List<Vector2> box;
			public string angle;
			public string text;
			public float score;
		}


		public List<TextInfo> Detection(AiliaModel ailia_model, Color32[] camera, int tex_width, int tex_height)
		{
			bool status;

			//リサイズ
			float[] data = new float[PADDLEOCR_DETECTOR_INPUT_BATCH_SIZE * PADDLEOCR_DETECTOR_INPUT_CHANNEL_COUNT * PADDLEOCR_DETECTOR_INPUT_HEIGHT_SIZE * PADDLEOCR_DETECTOR_INPUT_WIDTH_SIZE];
			int w = PADDLEOCR_DETECTOR_INPUT_WIDTH_SIZE;
			int h = PADDLEOCR_DETECTOR_INPUT_HEIGHT_SIZE;

			float scale = 1.0f * tex_width / w;
			for (int y = 0; y < h; y++)
			{
				for (int x = 0; x < w; x++)
				{
					int y2 = (int)(1.0 * y * scale);
					int x2 = (int)(1.0 * x * scale);
					if (x2 < 0 || y2 < 0 || x2 >= tex_width || y2 >= tex_height)
					{
						data[(y * w + x) + 0 * w * h] = 0;
						data[(y * w + x) + 1 * w * h] = 0;
						data[(y * w + x) + 2 * w * h] = 0;
						continue;
					}
					data[(y * w + x) + 0 * w * h] = (float)((camera[(tex_height - 1 - y2) * tex_width + x2].r) / 255.0);
					data[(y * w + x) + 1 * w * h] = (float)((camera[(tex_height - 1 - y2) * tex_width + x2].g) / 255.0);
					data[(y * w + x) + 2 * w * h] = (float)((camera[(tex_height - 1 - y2) * tex_width + x2].b) / 255.0);
				}
			}


			uint[] input_blobs = ailia_model.GetInputBlobList();
			int inputBlobIndex = ailia_model.FindBlobIndexByName("input");

			//SetInputBlobShape
			status = ailia_model.SetInputBlobShape(
				new Ailia.AILIAShape
				{
					x = (uint)PADDLEOCR_DETECTOR_INPUT_WIDTH_SIZE,
					y = (uint)PADDLEOCR_DETECTOR_INPUT_HEIGHT_SIZE,
					z = (uint)PADDLEOCR_DETECTOR_INPUT_CHANNEL_COUNT,
					w = PADDLEOCR_DETECTOR_INPUT_BATCH_SIZE,
					dim = 4
				},
				inputBlobIndex
			);
			if (!status)
			{
				Debug.LogError("Could not set input blob shape");
				Debug.LogError(ailia_model.GetErrorDetail());
			}

			//SetInputBlobData
			status = ailia_model.SetInputBlobData(data, (int)input_blobs[0]);
			if (!status)
			{
				Debug.LogError("Could not set input blob data");
				Debug.LogError(ailia_model.GetErrorDetail());
			}

			//Update
			bool result = ailia_model.Update();
			if (!result)
			{
				Debug.Log(ailia_model.GetErrorDetail());
			}


			//Get(Output)BlobData
			float[] box_data = new float[PADDLEOCR_DETECTOR_OUTPUT_BATCH_SIZE * PADDLEOCR_DETECTOR_OUTPUT_CHANNEL_COUNT * PADDLEOCR_DETECTOR_OUTPUT_HEIGHT_SIZE * PADDLEOCR_DETECTOR_OUTPUT_WIDTH_SIZE];
			int outputBlobIndex = ailia_model.FindBlobIndexByName("output");
			status = ailia_model.GetBlobData(box_data, outputBlobIndex);
			if (status == false)
			{
				Debug.LogError("Could not get output blob data " + outputBlobIndex);
				Debug.LogError(ailia_model.GetErrorDetail());
			}



			uint[] output_blobs = ailia_model.GetOutputBlobList();
			//Debug.Log(string.Join(",", output_blobs)); //152

			Ailia.AILIAShape box_shape = ailia_model.GetBlobShape((int)output_blobs[0]);
			//Debug.Log(box_shape.x); //1536
			//Debug.Log(box_shape.y); //832
			//Debug.Log(box_shape.z); //1
			//Debug.Log(box_shape.w); //1

			List<TextInfo> detections = null;
			float aspect = (float)tex_width / tex_height;
			detections = PostProcess(box_data, box_shape, w, h, aspect);


			if (detections.Count == 0)
			{
				//Debug.Log("null");
				return null;
			}

			return detections;
		}


		public List<TextInfo> Classification(AiliaModel ailia_model, Color32[] camera, int tex_width, int tex_height, List<AiliaPaddleOCR.TextInfo> result_detections)
		{
			bool status;

			List<TextInfo> classifications = new List<TextInfo>();

			if(result_detections != null){

				int[,] binary_camera = Color32ToArray(camera, tex_width, tex_height);

				//ROIのデータを抽出する
				List<int[,]> ROI_list = new List<int[,]>();
				for(int i = 0; i < result_detections.Count(); i++){
					Vector2 box_point_1 = new Vector2(result_detections[i].box[0].y * IMAGE_SCALE, result_detections[i].box[0].x * IMAGE_SCALE);
					Vector2 box_point_2 = new Vector2(result_detections[i].box[3].y * IMAGE_SCALE, result_detections[i].box[3].x * IMAGE_SCALE);
					Vector2 box_point_3 = new Vector2(result_detections[i].box[2].y * IMAGE_SCALE, result_detections[i].box[2].x * IMAGE_SCALE);
					Vector2 box_point_4 = new Vector2(result_detections[i].box[1].y * IMAGE_SCALE, result_detections[i].box[1].x * IMAGE_SCALE);
					List<Vector2> roi_box = new List<Vector2>() { box_point_1, box_point_2 ,box_point_3, box_point_4 };
					int[,] ROI = ExtractROI(binary_camera, roi_box);
					int[,] resized_ROI = ResizeNormImgForClassification(ROI);
					int[,] flipped_resized_ROI = FlipVertical(resized_ROI);
					ROI_list.Add(flipped_resized_ROI);
				}


				
				for(int r = 0; r < result_detections.Count(); r++){
				
					ailia_model.OpenFile(Application.temporaryCachePath + "/" +  "chi_eng_num_sym_mobile_cls_org.onnx" + ".prototxt", Application.temporaryCachePath + "/" +  "chi_eng_num_sym_mobile_cls_org.onnx");

					float[] data = new float[PADDLEOCR_CLASSIFIER_INPUT_BATCH_SIZE * PADDLEOCR_CLASSIFIER_INPUT_CHANNEL_COUNT * PADDLEOCR_CLASSIFIER_INPUT_HEIGHT_SIZE * PADDLEOCR_CLASSIFIER_INPUT_WIDTH_SIZE];


					int w = PADDLEOCR_CLASSIFIER_INPUT_WIDTH_SIZE;
					int h = PADDLEOCR_CLASSIFIER_INPUT_HEIGHT_SIZE;
					for (int y = 0; y < h; y++)
					{
						for (int x = 0; x < w; x++)
						{
							data[(y * w + x) + 0 * w * h] = (float)(ROI_list[r][x,y]);
							data[(y * w + x) + 1 * w * h] = (float)(ROI_list[r][x,y]);
							data[(y * w + x) + 2 * w * h] = (float)(ROI_list[r][x,y]);	
										
						}
					}
					

					uint[] input_blobs = ailia_model.GetInputBlobList();
					//Debug.Log(string.Join(",", input_blobs)); //0
					int inputBlobIndex = ailia_model.FindBlobIndexByName("input");
					// Debug.Log(inputBlobIndex); //0			
					status = ailia_model.SetInputBlobShape(
						new Ailia.AILIAShape
						{
							x = (uint)PADDLEOCR_CLASSIFIER_INPUT_WIDTH_SIZE,
							y = (uint)PADDLEOCR_CLASSIFIER_INPUT_HEIGHT_SIZE,
							z = (uint)PADDLEOCR_CLASSIFIER_INPUT_CHANNEL_COUNT,
							w = (uint)PADDLEOCR_CLASSIFIER_INPUT_BATCH_SIZE,
							dim = 4
						},
						inputBlobIndex
					);
					if (!status)
					{
						Debug.LogError("Could not set input blob shape");
						Debug.LogError(ailia_model.GetErrorDetail());
					}

					status = ailia_model.SetInputBlobData(data, (int)input_blobs[0]);
					if (!status)
					{
						Debug.LogError("Could not set input blob data");
						Debug.LogError(ailia_model.GetErrorDetail());
					}

					status = ailia_model.Update();
					if (!status)
					{
						Debug.Log(ailia_model.GetErrorDetail());
					}

					uint[] output_blobs = ailia_model.GetOutputBlobList();			
					Ailia.AILIAShape box_shape = ailia_model.GetBlobShape((int)output_blobs[0]);

					int OUTPUT_CHANNEL_COUNT = (int)box_shape.x;
					int OUTPUT_BATCH_SIZE = (int)box_shape.y;
					float[] out_data = new float[OUTPUT_CHANNEL_COUNT * OUTPUT_BATCH_SIZE];
					int outputBlobIndex = ailia_model.FindBlobIndexByName("output");
					status = ailia_model.GetBlobData(out_data, outputBlobIndex);
					if (status == false)
					{
						Debug.LogError("Could not get output blob data " + outputBlobIndex);
						Debug.LogError(ailia_model.GetErrorDetail());
					}

					
					//推論結果から角度リスト（0,180）の番号を抽出する
					int[] maxIndexs = {0, 1};
					float[] maxProbs = {out_data[0], out_data[1]};

					TextInfo classification = new TextInfo();
					classification.box = result_detections[r].box;
					classification.angle = ClsLabelDecode(maxIndexs, maxProbs).Item1;
					classifications.Add(classification);					
				}
			}

			return classifications;
		}



		public List<TextInfo> Recognition(AiliaModel ailia_model, Color32[] camera, int tex_width, int tex_height, List<AiliaPaddleOCR.TextInfo> result_classifications, String[] txt_file, AiliaTextRecognizersSample.Language language, AiliaTextRecognizersSample.ModelSize modelSize)
		{
			bool status;

			List<TextInfo> recognitions = new List<TextInfo>();

			List<(String, float)> result_list = new List<(String, float)>();
			List<List<Vector2>> box_list = new List<List<Vector2>>(); //最後にList<TextInfo> resultにまとめて格納する、ROIの座標情報


			if(result_classifications != null){

				int[,] binary_camera = Color32ToArray(camera, tex_width, tex_height);

				//ROIのデータを抽出する
				List<int[,]> ROI_list = new List<int[,]>();
				for(int i = 0; i < result_classifications.Count(); i++){
					Vector2 box_point_1 = new Vector2(result_classifications[i].box[0].y * IMAGE_SCALE, result_classifications[i].box[0].x * IMAGE_SCALE);
					Vector2 box_point_2 = new Vector2(result_classifications[i].box[3].y * IMAGE_SCALE, result_classifications[i].box[3].x * IMAGE_SCALE);
					Vector2 box_point_3 = new Vector2(result_classifications[i].box[2].y * IMAGE_SCALE, result_classifications[i].box[2].x * IMAGE_SCALE);
					Vector2 box_point_4 = new Vector2(result_classifications[i].box[1].y * IMAGE_SCALE, result_classifications[i].box[1].x * IMAGE_SCALE);
					List<Vector2> roi_box = new List<Vector2>() { box_point_1, box_point_2 ,box_point_3, box_point_4 };
					int[,] ROI = ExtractROI(binary_camera, roi_box);
					int[,] resized_ROI = ResizeNormImgForRecognition(ROI);
					int[,] flipped_resized_ROI = FlipVertical(resized_ROI);
					if(result_classifications[i].angle == "180"){
						flipped_resized_ROI = FlipVertical(flipped_resized_ROI);
					}
					ROI_list.Add(flipped_resized_ROI);
					box_list.Add(roi_box);
				}

				var weight_path_recognition = "";
				switch (language)
				{
					case AiliaTextRecognizersSample.Language.Japanese:
						if (modelSize == AiliaTextRecognizersSample.ModelSize.Server){
							weight_path_recognition = "jpn_eng_num_sym_server_rec_add.onnx";
						}else{
							weight_path_recognition = "jpn_eng_num_sym_mobile_rec_org.onnx";
						}
						break;
					case AiliaTextRecognizersSample.Language.English:
						weight_path_recognition = "eng_num_sym_mobile_rec_org.onnx";
						break;
					case AiliaTextRecognizersSample.Language.Chinese:
						if (modelSize == AiliaTextRecognizersSample.ModelSize.Server){
							weight_path_recognition = "chi_eng_num_sym_server_rec_org.onnx";
						}else{
							weight_path_recognition = "chi_eng_num_sym_mobile_rec_org.onnx";
						}
						break;
					case AiliaTextRecognizersSample.Language.German:
						weight_path_recognition = "ger_eng_num_sym_mobile_rec_org.onnx";
						break;
					case AiliaTextRecognizersSample.Language.French:
						weight_path_recognition = "fre_eng_num_sym_mobile_rec_org.onnx";
						break;
					case AiliaTextRecognizersSample.Language.Korean:
						weight_path_recognition = "kor_eng_num_sym_mobile_rec_org.onnx";
						break;
					default:
						Debug.Log("Others language are working in progress.");
						break;
				}

				for(int r = 0; r < result_classifications.Count(); r++){
				
					ailia_model.OpenFile(Application.temporaryCachePath + "/" +  weight_path_recognition + ".prototxt", Application.temporaryCachePath + "/" +  weight_path_recognition);

					int INPUT_BATCH_SIZE = 1;
					int INPUT_CHANNEL_COUNT = 3;
					int INPUT_HEIGHT_SIZE = 32;
					int INPUT_WIDTH_SIZE = ROI_list[r].GetLength(0);


					float[] data = new float[INPUT_BATCH_SIZE * INPUT_CHANNEL_COUNT * INPUT_HEIGHT_SIZE * INPUT_WIDTH_SIZE];


					int w = INPUT_WIDTH_SIZE;
					int h = INPUT_HEIGHT_SIZE;
					for (int y = 0; y < h; y++)
					{
						for (int x = 0; x < w; x++)
						{
							data[(y * w + x) + 0 * w * h] = (float)(ROI_list[r][x,y]);
							data[(y * w + x) + 1 * w * h] = (float)(ROI_list[r][x,y]);
							data[(y * w + x) + 2 * w * h] = (float)(ROI_list[r][x,y]);	
										
						}
					}
					

					uint[] input_blobs = ailia_model.GetInputBlobList();
					//Debug.Log(string.Join(",", input_blobs)); //0
					int inputBlobIndex = ailia_model.FindBlobIndexByName("input");
					// Debug.Log(inputBlobIndex); //0			
					status = ailia_model.SetInputBlobShape(
						new Ailia.AILIAShape
						{
							x = (uint)INPUT_WIDTH_SIZE,
							y = (uint)INPUT_HEIGHT_SIZE,
							z = (uint)INPUT_CHANNEL_COUNT,
							w = (uint)INPUT_BATCH_SIZE,
							dim = 4
						},
						inputBlobIndex
					);
					if (!status)
					{
						Debug.LogError("Could not set input blob shape");
						Debug.LogError(ailia_model.GetErrorDetail());
					}

					status = ailia_model.SetInputBlobData(data, (int)input_blobs[0]);
					if (!status)
					{
						Debug.LogError("Could not set input blob data");
						Debug.LogError(ailia_model.GetErrorDetail());
					}

					status = ailia_model.Update();
					if (!status)
					{
						Debug.Log(ailia_model.GetErrorDetail());
					}

					uint[] output_blobs = ailia_model.GetOutputBlobList();
					Ailia.AILIAShape box_shape = ailia_model.GetBlobShape((int)output_blobs[0]);



					int OUTPUT_WIDTH_SIZE = (int)box_shape.x;
					int OUTPUT_HEIGHT_SIZE = (int)box_shape.y;
					int OUTPUT_CHANNEL_COUNT = (int)box_shape.z;
					int OUTPUT_BATCH_SIZE = (int)box_shape.w;
					float[] out_data = new float[OUTPUT_BATCH_SIZE * OUTPUT_CHANNEL_COUNT * OUTPUT_HEIGHT_SIZE * OUTPUT_WIDTH_SIZE];
					int outputBlobIndex = ailia_model.FindBlobIndexByName("output");
					status = ailia_model.GetBlobData(out_data, outputBlobIndex);
					if (status == false)
					{
						Debug.LogError("Could not get output blob data " + outputBlobIndex);
						Debug.LogError(ailia_model.GetErrorDetail());
					}


					//推論結果から文字リストの番号を抽出する
					int[] maxIndexs = new int[OUTPUT_HEIGHT_SIZE];
					float[] maxProbs = new float[OUTPUT_HEIGHT_SIZE];
					int removeCount = OUTPUT_WIDTH_SIZE;
					for (int i = 0; i < OUTPUT_HEIGHT_SIZE - 1; i++)
					{
						float[] first4400 = new float[OUTPUT_WIDTH_SIZE];
						Array.Copy(out_data, 0, first4400, 0, first4400.Length);
						float[] newArray = new float[out_data.Length - removeCount];
						Array.Copy(out_data, removeCount, newArray, 0, newArray.Length);
						out_data = newArray;
						int maxIndex = first4400.ToList().IndexOf(first4400.Max());
						float maxProb = first4400.Max();
						maxIndexs[i] = maxIndex;
						maxProbs[i] = maxProb;
					}


					//0の部分を除いて、推論したindex番号をテキストに変換、その確率とタプル構造にしたリストを返す
					result_list.Add(RecLabelDecode(maxIndexs, maxProbs, txt_file));

				}
				
				for(int r = 0; r < result_classifications.Count(); r++){
					TextInfo textinfo = new TextInfo();
					textinfo.box = box_list[r];
					textinfo.text = result_list[r].Item1;
					textinfo.score = result_list[r].Item2;
					recognitions.Add(textinfo);
				}
			}

		
			return recognitions;
		}



		List<TextInfo> PostProcess(float[] box_data, Ailia.AILIAShape box_shape, int input_w, int input_h, float aspect)
		{
			List<TextInfo> results = new List<TextInfo>();

			float[,] BoxData = new float[PADDLEOCR_DETECTOR_OUTPUT_HEIGHT_SIZE, PADDLEOCR_DETECTOR_OUTPUT_WIDTH_SIZE]; //box_dataを二次元に変換
			int[,] Segmentation = new int[PADDLEOCR_DETECTOR_OUTPUT_HEIGHT_SIZE, PADDLEOCR_DETECTOR_OUTPUT_WIDTH_SIZE]; //BitmapのうちThreshより大きい値の部分だけ1にする

			
			for (int i = 0; i < PADDLEOCR_DETECTOR_OUTPUT_HEIGHT_SIZE; i++)
			{
				for (int j = 0; j < PADDLEOCR_DETECTOR_OUTPUT_WIDTH_SIZE; j++)
				{
					BoxData[i, j] = box_data[i * PADDLEOCR_DETECTOR_OUTPUT_WIDTH_SIZE + j];

					if (BoxData[i, j] > DETECTION_THRESH)
					{
						Segmentation[i, j] = 1;
					}
					else
					{
						Segmentation[i, j] = 0;
					}
				}
			}

			int[,] small_Segmentation = new int[PADDLEOCR_DETECTOR_OUTPUT_HEIGHT_SIZE / IMAGE_SCALE, PADDLEOCR_DETECTOR_OUTPUT_WIDTH_SIZE / IMAGE_SCALE];
			for (int i = 0; i < PADDLEOCR_DETECTOR_OUTPUT_HEIGHT_SIZE / IMAGE_SCALE; i++)
			{
				for (int j = 0; j < PADDLEOCR_DETECTOR_OUTPUT_WIDTH_SIZE / IMAGE_SCALE; j++)
				{
					small_Segmentation[i, j] = Segmentation[i * IMAGE_SCALE, j * IMAGE_SCALE];
				}
			}

			TextInfo result = new TextInfo();
			List<List<Vector2>> boxes = new List<List<Vector2>>();
			boxes = BoxFromitmap(small_Segmentation, 0, 0);
            for (int i = 0; i < boxes.Count; i++)
            {
				result.box = boxes[i];
				results.Add(result);
            }


			return results;
		}


		//カメラからの入力画像を、アスペクト比を変えつつ二値化
		int[,] Color32ToArray(Color32[] camera, int width, int height, float threshold = 200)
		{
			int[,] result = new int[width, height];

			for (int x = 0; x < width; x++)
			{
				for (int y = 0; y < height; y++)
				{
					Color32 pixel = camera[x + y * width];
					// グレースケール値の計算
					float grayscaleValue = (float)(0.299 * pixel.r + 0.587 * pixel.g + 0.114 * pixel.b);
					// 閾値を使用して0または1に変換
					if(grayscaleValue >= threshold){
						result[x, y] = 1;
					}
					else{
						result[x, y] = 0;
					}
				}
			}

			return result;
		}





		//boxのスコアを計算し、小さすぎるboxを除く
		private List<List<Vector2>> BoxFromitmap(int[,] mask, int dest_width, int dest_height)
		{
			List<List<Vector2>> boxes = new List<List<Vector2>>();
			List<List<Vector2>> new_boxes = new List<List<Vector2>>();
			boxes = FindContoursAndGetMinBoxes(mask);

            for (int i = 0; i < boxes.Count; i++)
            {
                if (BoxScoreFast(boxes[i]) > 3)
                {
					new_boxes.Add(boxes[i]);
                }
            }

			return new_boxes;
		}



		private List<List<Vector2>> FindContoursAndGetMinBoxes(int[,] mask)
        {
			List<Vector2> contour = new List<Vector2>();
			List<List<Vector2>> contours = new List<List<Vector2>>();
			List<Vector2> box = new List<Vector2>();
			List<List<Vector2>> boxes = new List<List<Vector2>>();
			int x_start, y_start; //輪郭探索の最初の画素
			int xs, ys; //現在注目している画素
			int xp = -1, yp = -1; //注目画素の周りの8近傍のうち、注目している画素

			List<Vector2> c_code = new List<Vector2> { new Vector2(1, 0), new Vector2(1,-1), new Vector2(0,-1), new Vector2(-1,-1), new Vector2(-1,0), new Vector2(-1,1), new Vector2(0,1), new Vector2(1,1)};
			int[] next_code = { 7, 7, 1, 1, 3, 3, 5, 5 };

			int c, cs;
			bool is_found = false;

			for (int i = 0; i < mask.GetLength(0); i++)
            {
                for (int j = 0; j < mask.GetLength(1); j++)
                {
                    if (mask[i,j] == 1)
                    {
						contour = new List<Vector2>(); //初期化
						contour.Add(new Vector2(i, j));

						x_start = i; y_start = j;
						xs = i; ys = j;
						c = 5; // 初期探索方向は左下

						
						while (true)
						{
							is_found = false;
							cs = c;
							
							while (!is_found)
							{
								xp = xs + (int)c_code[c].x;
								yp = ys + (int)c_code[c].y;

								if (IsInside(mask, xp, yp) && mask[xp, yp] == 1)
								{
									contour.Add(new Vector2(xp, yp));
									is_found = true;
									xs = xp;
									ys = yp;
									c = next_code[c];
									break;
								}
								else
								{
									c++;
									if (c > 7) { c = 0; }
                                    if (c == cs) { break; }
								}
							}

							if ((x_start == xp && y_start == yp ) || (!is_found && c == cs)) { break; }
						}


						//取得した輪郭領域の内側の画素を全部0にして調べる必要をなくすとともに、輪郭をギリギリ囲う最小の矩形の座標を取得
						int x_min = mask.GetLength(0), y_min = mask.GetLength(1);
						int x_max = 0, y_max = 0;
						for (int k = 0; k < contour.Count; k++)
						{
							if (x_min > contour[k].x) { x_min = (int)contour[k].x; }
							if (y_min > contour[k].y) { y_min = (int)contour[k].y; }
							if (x_max < contour[k].x) { x_max = (int)contour[k].x; }
							if (y_max < contour[k].y) { y_max = (int)contour[k].y; }
						}

						box = new List<Vector2>(); //初期化

						int buffer = 20 / IMAGE_SCALE;
						box.Add(new Vector2(x_min - buffer, y_min - buffer));
						box.Add(new Vector2(x_min - buffer, y_max + buffer));
						box.Add(new Vector2(x_max + buffer, y_max + buffer));
						box.Add(new Vector2(x_max + buffer, y_min - buffer));



						boxes.Add(box);

						for (int x_in = x_min; x_in < x_max + 1; x_in++)
						{
							for (int y_in = y_min; y_in < y_max + 1; y_in++)
							{
								mask[x_in, y_in] = 0;
							}
						}

						contours.Add(contour);
					}

                }

            }

			return boxes;
        }



		private bool IsInside(int[,] mask, int x, int y)
        {
            if (x > mask.GetLength(0) || y > mask.GetLength(1) || x < 0 || y < 0)
            {
				return false;
            }
            else
            {
				return true;
            }
        }


		//矩形の大きさからスコアを計算する
		private float BoxScoreFast(List<Vector2> box)
        {
			float box_width = box[1].y - box[0].y;
			float box_height = box[3].x - box[0].x;

			float score = box_width * box_height;

			return score;
        }


		//指定した4隅の座標の範囲のデータを抽出する
		private int[,] ExtractROI(int[,] binary_camera, List<Vector2> cornerCoordinates)
		{
			if (binary_camera == null || cornerCoordinates == null || cornerCoordinates.Count != 4)
			{
				Debug.LogError("Invalid input parameters");
			}

			Vector2 bottomLeft = cornerCoordinates[0];
			Vector2 topLeft = cornerCoordinates[1];
			Vector2 topRight = cornerCoordinates[2];
			Vector2 bottomRight = cornerCoordinates[3];


			// 部分行列のサイズを計算
			int width = (int)(topRight.x - topLeft.x) ;
			int height = (int)(topLeft.y - bottomLeft.y) ;

			// 部分行列を初期化
			int[,] ROI = new int[width, height];

			// 部分行列をコピー
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					int originalX = (int)(bottomLeft.x) + x;
					int originalY = 832 - ((int)(bottomLeft.y) + y);

					ROI[x, height - y - 1] = binary_camera[originalX, originalY];
				}

			}

			return ROI;
		}


		//Classification用のリサイズ
		private int[,] ResizeNormImgForClassification(int[,] img)
		{
			int imgH = CLASSIFICATION_IMAGE_HEIGHT;
			int imgW = CLASSIFICATION_IMAGE_WIDTH;
			
			int w = img.GetLength(0);
			int h = img.GetLength(1);

			double widthRatio = (double)w / imgW;
			double heightRatio = (double)h / imgH;

			int[,] resizedImg = new int[imgW, imgH];
			for (int y = 0; y < imgH; y++)
			{
				for (int x = 0; x < imgW; x++)
				{
					int sourceX = (int)(x * widthRatio);
					int sourceY = (int)(y * heightRatio);

					if (sourceX >= w)
					{
						sourceX = w - 1;
					}

					if (sourceY >= h)
					{
						sourceY = h - 1;
					}

					resizedImg[x, y] = img[sourceX, sourceY];
				}
			}

			return resizedImg;
		}


		//Recognition用のリサイズ
		private int[,] ResizeNormImgForRecognition(int[,] img)
		{
			int imgH = RECOGNITION_IMAGE_HEIGHT;
			int imgW = RECOGNITION_IMAGE_WIDTH;
			imgW = Math.Max(Math.Min(imgW, LIMITED_MAX_WIDTH), LIMITED_MIN_WIDTH);

			int w = img.GetLength(0);
			int h = img.GetLength(1);
			float ratio = w / (float)h;
			int ratio_imgH = (int)Math.Ceiling(imgH * ratio);
			ratio_imgH = Math.Max(ratio_imgH, LIMITED_MIN_WIDTH);
			int resized_w = 0;
			if(ratio_imgH > imgW){
				resized_w = imgW;
			}
        	else{
				resized_w = (int)Math.Ceiling(imgH * ratio);
			}


			int[,] resizedImg = new int[resized_w, imgH];
			double widthRatio = (double)w / resized_w;
			double heightRatio = (double)h / imgH;
			for (int y = 0; y < imgH; y++)
			{
				for (int x = 0; x < resized_w; x++)
				{
					int sourceX = (int)(x * widthRatio);
					int sourceY = (int)(y * heightRatio);

					if (sourceX >= w)
					{
						sourceX = w - 1;
					}

					if (sourceY >= h)
					{
						sourceY = h - 1;
					}

					resizedImg[x, y] = img[sourceX, sourceY];
				}
			}

			return resizedImg;
		}



		private int[,] PaddingImg(int[,] img, int max_width){
			int w = img.GetLength(0);
			int h = img.GetLength(1);

			int[,] PaddingImg = new int[max_width, h];
			for (int y = 0; y < h; y++)
			{
				for (int x = 0; x < max_width; x++)
				{
					if (x < w)
					{
						PaddingImg[x, y] = img[x, y];
					}
					else
					{
						PaddingImg[x, y] = 1;
					}
				}
			}

			return PaddingImg;
		}


		//上下反転
		private int[,] FlipVertical(int[,] img){

			int rows = img.GetLength(0);
			int columns = img.GetLength(1);
			int[,] flippedImg = new int[rows, columns];
			for (int i = 0; i < rows; i++)
			{
				for (int j = 0; j < columns; j++)
				{
					flippedImg[i, j] = img[i, columns - 1 - j];
				}
			}

			return flippedImg;
		}


		//左右反転
		private int[,] FlipHorizontal(int[,] img){

			int rows = img.GetLength(0);
			int columns = img.GetLength(1);
			int[,] flippedImg = new int[rows, columns];
			for (int i = 0; i < rows; i++)
			{
				for (int j = 0; j < columns; j++)
				{
					flippedImg[i, j] = img[rows - 1 - i, j];
				}
			}

			return flippedImg;
		}


		
		private (String, float) ClsLabelDecode(int[] angle_index, float[] angle_prob){

			String result_angle = "";
			float result_prob = 0.0f;
			String[] angle_list = {"0", "180"};

			if(angle_prob[0] < angle_prob[1]){
				result_angle = angle_list[angle_index[1]];
				result_prob = angle_prob[1];
			}
			else{
				result_angle = angle_list[angle_index[0]];
				result_prob = angle_prob[0];
			}
			
			return (result_angle, result_prob);
		}
		

		private (String, float) RecLabelDecode(int[] text_index, float[] text_prob, String[] txt_file){

			if(text_index.Length == 0)
			{
				Debug.LogError("No recognized data");
			}
			String result_text = "";
			float result_prob = 0.0f;
			int result_num = 0;
			int ignored_tokens = 0;
			for(int i = 0; i < text_index.Length; i++){
				if(text_index[i] != 0 && text_index[i] < txt_file.Length){
					result_text += txt_file[text_index[i]];
					result_prob += text_prob[i];
					result_num += 1;
				}
			}

			return (result_text, result_prob/result_num);
		}
	}
}
