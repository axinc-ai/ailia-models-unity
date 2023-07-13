/* AILIA Unity Plugin SileroVAD Sample */
/* Copyright 2023 AXELL CORPORATION */

using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Runtime.InteropServices;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace ailiaSDK
{
	public class AiliaSileroVad
	{
		public const int NUM_INPUTS = 4;
		public const int NUM_OUTPUTS = 3;

		// Update is called once per frame
		public float[] VAD(AiliaModel ailia_model, float [] pcm)
		{
			/*
			uint[] input_blobs = ailia_model.GetInputBlobList();
			if (input_blobs != null)
			{
				bool success = ailia_model.SetInputBlobData(data, (int)input_blobs[0]);
				if (!success)
				{
					Debug.Log("Can not SetInputBlobData");
				}

				long start_time = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
				ailia_model.Update();

				List<FaceInfo> detections = null;

				uint[] output_blobs = ailia_model.GetOutputBlobList();
				if (output_blobs != null && output_blobs.Length >= 2)
				{
					Ailia.AILIAShape box_shape = ailia_model.GetBlobShape((int)output_blobs[0]);
					Ailia.AILIAShape score_shape = ailia_model.GetBlobShape((int)output_blobs[1]);

					if (box_shape != null && score_shape != null)
					{
						float[] box_data = new float[box_shape.x * box_shape.y * box_shape.z * box_shape.w];
						float[] score_data = new float[score_shape.x * score_shape.y * score_shape.z * score_shape.w];

						if (ailia_model.GetBlobData(box_data, (int)output_blobs[0]) &&
								ailia_model.GetBlobData(score_data, (int)output_blobs[1]))
						{
							float aspect = (float)tex_width / tex_height;
							detections = PostProcess(box_data, box_shape, score_data, score_shape, w, h, aspect);
						}
					}
				}

				if (detections != null)
				{
					detections = WeightedNonMaxSuppression(detections);
					//Debug.Log("Num faces: " + detections.Count);
				}
				return detections;
			}
			*/
			return null;
		}

/*
		int forward(AILIANetwork *ailia, std::vector<float> *inputs[NUM_INPUTS], std::vector<float> *outputs[NUM_OUTPUTS]){
			int status;

			for (int i = 0; i < NUM_INPUTS; i++){
				unsigned int input_blob_idx = 0;
				status = ailiaGetBlobIndexByInputIndex(ailia, &input_blob_idx, i);
				if (status != AILIA_STATUS_SUCCESS) {
					setErrorDetail("ailiaGetBlobIndexByInputIndex", ailiaGetErrorDetail(ailia));
					return status;
				}

				AILIAShape sequence_shape;
				int batch_size = 1;
				if ( i == 0 ){
					sequence_shape.x=inputs[i]->size() / batch_size;
					sequence_shape.y=batch_size;
					sequence_shape.z=1;
					sequence_shape.w=1;
					sequence_shape.dim=2;
				}
				if ( i == 1 ){
					sequence_shape.x=inputs[i]->size();
					sequence_shape.y=1;
					sequence_shape.z=1;
					sequence_shape.w=1;
					sequence_shape.dim=1;
				}
				if ( i == 2 || i == 3){
					sequence_shape.x=inputs[i]->size() / batch_size / 2;
					sequence_shape.y=batch_size;
					sequence_shape.z=2;
					sequence_shape.w=1;
					sequence_shape.dim=3;
				}
				if (debug){
					printf("input blob shape %d %d %d %d dims %d\n",sequence_shape.x,sequence_shape.y,sequence_shape.z,sequence_shape.w,sequence_shape.dim);
				}

				status = ailiaSetInputBlobShape(ailia,&sequence_shape,input_blob_idx,AILIA_SHAPE_VERSION);
				if(status!=AILIA_STATUS_SUCCESS){
					setErrorDetail("ailiaSetInputBlobShape",ailiaGetErrorDetail(ailia));
					return status;
				}

				if (inputs[i]->size() > 0){
					status = ailiaSetInputBlobData(ailia, &(*inputs[i])[0], inputs[i]->size() * sizeof(float), input_blob_idx);
					if (status != AILIA_STATUS_SUCCESS) {
						setErrorDetail("ailiaSetInputBlobData",ailiaGetErrorDetail(ailia));
						return status;
					}
				}
			}

			status = ailiaUpdate(ailia);
			if (status != AILIA_STATUS_SUCCESS) {
				setErrorDetail("ailiaUpdate",ailiaGetErrorDetail(ailia));
				return status;
			}

			for (int i = 0; i < NUM_OUTPUTS; i++){
				unsigned int output_blob_idx = 0;
				status = ailiaGetBlobIndexByOutputIndex(ailia, &output_blob_idx, i);
				if (status != AILIA_STATUS_SUCCESS) {
					setErrorDetail("ailiaGetBlobIndexByInputIndex",ailiaGetErrorDetail(ailia));
					return status;
				}

				AILIAShape output_blob_shape;
				status=ailiaGetBlobShape(ailia,&output_blob_shape,output_blob_idx,AILIA_SHAPE_VERSION);
				if(status!=AILIA_STATUS_SUCCESS){
					setErrorDetail("ailiaGetBlobShape", ailiaGetErrorDetail(ailia));
					return status;
				}

				if (debug){
					printf("output_blob_shape %d %d %d %d dims %d\n",output_blob_shape.x,output_blob_shape.y,output_blob_shape.z,output_blob_shape.w,output_blob_shape.dim);
				}

				(*outputs[i]).resize(output_blob_shape.x*output_blob_shape.y*output_blob_shape.z*output_blob_shape.w);

				status =ailiaGetBlobData(ailia, &(*outputs[i])[0], outputs[i]->size() * sizeof(float), output_blob_idx);
				if (status != AILIA_STATUS_SUCCESS) {
					setErrorDetail("ailiaGetBlobData",ailiaGetErrorDetail(ailia));
					return status;
				}
			}

			return AILIA_STATUS_SUCCESS;
		}

		std::vector<float> calc_vad(AILIANetwork* net, std::vector<float> wave, int sampleRate, int nChannels, int nSamples)
		{
			int batch = 1;
			int sequence = 1536;

			std::vector<float> input(batch * sequence);
			std::vector<float> sr(1);
			std::vector<float> h(2 * batch * 64);
			std::vector<float> c(2 * batch * 64);

			std::vector<float> conf;

			for (int s = 0; s < nSamples; s+=sequence){
				for (int i = 0; i < input.size(); i++){
					if (s + i < nSamples){
						input[i] = wave[s + i];
					}else{
						input[i] = 0;
					}
				}
				sr[0] = sampleRate;
				if (debug){
					PRINT_OUT("\n");
				}

				std::vector<float> *inputs[NUM_INPUTS];
				inputs[0] = &input;
				inputs[1] = &sr;
				inputs[2] = &h;
				inputs[3] = &c;

				std::vector<float> output(batch);
				
				std::vector<float> *outputs[NUM_OUTPUTS];
				outputs[0] = &output;
				outputs[1] = &h;
				outputs[2] = &c;

				forward(net, inputs, outputs);

				conf.push_back(output[0]);
			}

			return conf;
		}
*/

	}
}