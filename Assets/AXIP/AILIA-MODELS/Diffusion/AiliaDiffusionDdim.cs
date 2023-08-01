/* AILIA Unity Plugin Diffusion Sample */
/* Copyright 2023 AXELL CORPORATION */

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ailiaSDK
{
	public class AiliaDiffusionDdim
	{
		System.Random random = new System.Random(); 

		public float randn(){
			float X = (float)random.NextDouble(); 
			float Y = (float)random.NextDouble(); 
			float Z1 =(float)(Math.Sqrt(-2.0 * Math.Log(X)) * Math.Cos(2.0 * Math.PI * Y)); 
			float Z2 =(float)(Math.Sqrt(-2.0 * Math.Log(X)) * Math.Sin(2.0 * Math.PI * Y));
			return Z1;
		}

		public class DdimParameters{
			public List<int> ddim_timesteps;
			public List<float> alphas;
			public List<float> alphas_prev;
			public List<float> sigmas;
			public List<float> ddim_sqrt_one_minus_alphas;
		}

		// ddim_steps = 50, ddim_eta = 0.0
		public DdimParameters MakeDdimParameters(int ddim_num_steps, float ddim_eta, double [] alphas_cumprod){
			DdimParameters parameters = new DdimParameters();
			int ddpm_num_timesteps = 1000;
			int c = ddpm_num_timesteps / ddim_num_steps;
			parameters.ddim_timesteps = new List<int>();
			parameters.alphas = new List<float>();
			parameters.alphas_prev = new List<float>();
			parameters.sigmas = new List<float>();
			parameters.ddim_sqrt_one_minus_alphas = new List<float>();
			float alpha_prev = (float)alphas_cumprod[0];
			for (int i = 0; i < ddpm_num_timesteps; i+=c){
				parameters.ddim_timesteps.Add(i + 1);
				float alpha = (float)alphas_cumprod[i + 1];
				parameters.alphas.Add(alpha);
				parameters.alphas_prev.Add(alpha_prev);
				float sigma = ddim_eta * (float)Math.Sqrt((1 - alpha_prev) / (1 - alpha) * (1 - alpha / alpha_prev));
				parameters.sigmas.Add(sigma);
				alpha_prev = alpha;
				parameters.ddim_sqrt_one_minus_alphas.Add((float)Math.Sqrt(1.0f - alpha));
			}
			return parameters;
		}

		public void DdimSampling(float [] diffusion_img, float [] diffusion_output, DdimParameters parameters, int index){
			if (diffusion_img.Length != diffusion_output.Length){
				Debug.Log("Image size mismatch on DdimSampling");
				return;
			}

			float a_t = parameters.alphas[index];
			float a_prev = parameters.alphas_prev[index];
			float sigma_t = parameters.sigmas[index];
			float sqrt_one_minus_at = parameters.ddim_sqrt_one_minus_alphas[index];
			float temperature = 1.0f;

			Debug.Log("DdimSampling index "+index+" a_t "+a_t+" a_prev "+a_prev+" sigma_t "+sigma_t+" sqrt_one_minus_at "+sqrt_one_minus_at+" temperature "+temperature);

			for (int i = 0; i < diffusion_img.Length; i++){
				float x = diffusion_img[i];
				float e_t = diffusion_output[i];
				float pred_x0 = (x - sqrt_one_minus_at * e_t) / (float)Math.Sqrt(a_t);
				float dir_xt = (float)Math.Sqrt(1.0f - a_prev - (float)Math.Pow(sigma_t,2)) * e_t;
				float noise = sigma_t * randn() * temperature;
				float x_prev = (float)Math.Sqrt(a_prev) * pred_x0 + dir_xt + noise;
				diffusion_img[i] = x_prev;
			}
		}
	}
}