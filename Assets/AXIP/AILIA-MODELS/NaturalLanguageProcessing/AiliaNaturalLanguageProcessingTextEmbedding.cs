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
		private Ailia.AILIAShape SetShape(AiliaModel model,int n_sqeuences)
		{
			Ailia.AILIAShape shape = null;
			shape = new Ailia.AILIAShape();
			shape.x = (uint)n_sqeuences;
			shape.y = 1;
			shape.z = 1;
			shape.w = 1;
			shape.dim = 2;
			model.SetInputShape(shape);
			return model.GetOutputShape();
		}

		private void Norm(float [] data){
			float norm = 0;
			for (int i = 0; i < data.Length; i++){
				norm += data[i] * data[i];
			}
			norm = Mathf.Sqrt(norm);
			for (int i = 0; i < data.Length; i++){
				data[i] = data[i] / norm;
			}
		}

		public float CosSim(float [] vec1, float [] vec2){
			float sum = 0.0f;
			for (int i = 0; i < vec1.Length; i++){
				sum += vec1[i] * vec1[2];
			}
			return sum;
		}
		public float[] Embedding(string text, AiliaModel model, AiliaTokenizerModel tokenizer){
			int[] tokens = tokenizer.Encode(text);
			float[] input = new float[tokens.Length];
			for (int i = 0; i < tokens.Length; i++){
				input[i] = tokens[i];
			}

			Ailia.AILIAShape output_shape = SetShape(model, tokens.Length);
			float[] output = new float [output_shape.x * output_shape.y * output_shape.z * output_shape.w];
			model.Predict(output, input);

			float[] embedding = new float [output_shape.x];
			for (int i = 0; i < output_shape.y; i++){
				for (int j = 0; j < output_shape.x; j++){
					embedding[j] = embedding[j] + output[i * output_shape.x + j];
				}
			}
			for (int j = 0; j < output_shape.x; j++){
				embedding[j] = embedding[j] / output_shape.y;
			}

			return embedding;
		}
	}
}