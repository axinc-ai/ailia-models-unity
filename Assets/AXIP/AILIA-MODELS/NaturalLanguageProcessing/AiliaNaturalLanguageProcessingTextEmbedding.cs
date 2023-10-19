/* AILIA Unity Plugin Text Embedding Sample */
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
	public class AiliaNaturalLanguageProcessingTextEmbedding
	{

		private void Normalize(float [] data){
			float norm = 0;
			for (int i = 0; i < data.Length; i++){
				norm += data[i] * data[i];
			}
			norm = Mathf.Sqrt(norm);
			for (int i = 0; i < data.Length; i++){
				data[i] = data[i] / norm;
			}
		}

		public float CosSimilarity(float [] vec1, float [] vec2){
			float sum = 0.0f;
			for (int i = 0; i < vec1.Length; i++){
				sum += vec1[i] * vec2[i];
			}
			return sum;
		}

		public float[] Embedding(string text, AiliaModel model, AiliaTokenizerModel tokenizer){
			int[] tokens = tokenizer.Encode(text);

			float[] input = new float[tokens.Length];
			float[] mask = new float[tokens.Length];
			for (int i = 0; i < tokens.Length; i++){
				input[i] = tokens[i];
				mask[i] = 1.0f;
			}

			Ailia.AILIAShape shape = new Ailia.AILIAShape();
			shape.x = (uint)tokens.Length;
			shape.y = 1;
			shape.z = 1;
			shape.w = 1;
			shape.dim = 2;

			model.SetInputBlobShape(shape, (int)model.GetInputBlobList()[0]);
			model.SetInputBlobData(input, (int)model.GetInputBlobList()[0]);
			model.SetInputBlobShape(shape, (int)model.GetInputBlobList()[1]);
			model.SetInputBlobData(mask, (int)model.GetInputBlobList()[1]);
			model.Update();
			Ailia.AILIAShape output_shape = model.GetBlobShape((int)model.GetOutputBlobList()[0]);
			float[] output = new float [output_shape.x * output_shape.y * output_shape.z * output_shape.w];
			model.GetBlobData(output, (int)model.GetOutputBlobList()[0]);

			float[] embedding = new float [output_shape.x];
			for (int i = 0; i < output_shape.y; i++){
				for (int j = 0; j < output_shape.x; j++){
					embedding[j] = embedding[j] + output[i * output_shape.x + j];
				}
			}
			for (int j = 0; j < output_shape.x; j++){
				embedding[j] = embedding[j] / output_shape.y;
			}

			Normalize(embedding);

			return embedding;
		}
	}
}