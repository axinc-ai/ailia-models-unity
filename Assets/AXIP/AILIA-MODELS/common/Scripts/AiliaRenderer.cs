using ailiaSDK;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace ailiaSDK
{
	public class AiliaRenderer : MonoBehaviour
	{
		public GameObject line_panel;   //LinePanel
		public GameObject lines;        //LinePanel/Lines
		public GameObject line;         //Line to instiate
		public GameObject text_panel;   //TextPanel
		public GameObject text_base;    //TextPanel/Text
		List<GameObject> textObjectBuffer = new List<GameObject>();
		int textObjectBufferIndex = 0;
		List<GameObject> lineObjectBuffer = new List<GameObject>();
		int lineObjectBufferIndex = 0;

		//追加
		public GameObject spheres; //Spheres
		public GameObject sphere; //Sphere to instiate
		public List<GameObject> sphereObjectBuffer = new List<GameObject>();
		public int sphereObjectBufferIndex = 0;

		//追加
		public GameObject lines3D;
		public GameObject line3D;
		List<GameObject> lineObjectBuffer3D = new List<GameObject>();
		int lineObjectBufferIndex3D = 0;

		//追加 座標系の描画用
		public List<GameObject> axisSphereObjectBuffer = new List<GameObject>();
		public int axisSphereObjectBufferIndex = 0;
		List<GameObject> axisLineObjectBuffer3D = new List<GameObject>();
		int axisLineObjectBufferIndex3D = 0;

		public Vector3 drawPos = new Vector3(0.0f, 0.0f, 120.0f); //3次元landmarkの原点
		public float scale = 20; //3次元landmarkの原点の倍率
		public float time = 0; //3次元landmarkを回転させるために実行してからの経過時間を保持する

		private List<uint> LANDMARK_LEFT = new List<uint>() { 1, 3, 5, 7, 9, 11, 13, 15 };
		private List<uint> LANDMARK_RIGHT = new List<uint>() { 2, 4, 6, 8, 10, 12, 14, 16 };

		public void Clear()
		{
			for (int i = lineObjectBufferIndex; i < lineObjectBuffer.Count; i++)
			{
				lineObjectBuffer[i].SetActive(false);
			}
			lineObjectBufferIndex = 0;

			for (int i = textObjectBufferIndex; i < textObjectBuffer.Count; i++)
			{
				textObjectBuffer[i].SetActive(false);
			}
			textObjectBufferIndex = 0;

			//追加
			for (int i = sphereObjectBufferIndex; i < sphereObjectBuffer.Count; i++)
			{
				sphereObjectBuffer[i].SetActive(false);
			}
			sphereObjectBufferIndex = 0;

			//追加
			for (int i = lineObjectBufferIndex3D; i < lineObjectBuffer3D.Count; i++)
			{
				lineObjectBuffer3D[i].SetActive(false);
			}
			lineObjectBufferIndex3D = 0;

			//追加
			for (int i = axisSphereObjectBufferIndex; i < axisSphereObjectBuffer.Count; i++)
			{
				axisSphereObjectBuffer[i].SetActive(false);
			}
			axisSphereObjectBufferIndex = 0;

			//追加
			for (int i = axisLineObjectBufferIndex3D; i < axisLineObjectBuffer3D.Count; i++)
			{
				axisLineObjectBuffer3D[i].SetActive(false);
			}
			axisLineObjectBufferIndex3D = 0;
		}

		public void DrawBone(Color32 color, int tex_width, int tex_height, AiliaPoseEstimator.AILIAPoseEstimatorObjectPose obj, uint from, uint to, int r, bool is_z_local = true)
		{
			float th = 0.1f;
			if (obj.points[from].score <= th || obj.points[to].score <= th)
			{
				return;
			}

			int from_x = (int)(tex_width * obj.points[from].x);
			int from_y = (int)(tex_height * obj.points[from].y);
			int to_x = (int)(tex_width * obj.points[to].x);
			int to_y = (int)(tex_height * obj.points[to].y);

			int len = (to_x - from_x) * (to_x - from_x) + (to_y - from_y) * (to_y - from_y);
			len = (int)Mathf.Sqrt(len);

			if (is_z_local) {
				DrawLine(color, from_x, from_y, obj.points[from].z_local, to_x, to_y, obj.points[to].z_local, tex_width, tex_height);
			}
			else
            {
				DrawLine(color, from_x, from_y, 0, to_x, to_y, 0, tex_width, tex_height);
			}
		}

		public void DrawLine(Color32 color, int from_x, int from_y, float from_z, int to_x, int to_y, float to_z, int tex_width, int tex_height)
		{
			RectTransform panelRect = line_panel.GetComponent<RectTransform>();
			float width = panelRect.rect.width;
			float height = panelRect.rect.height;

			int r = 2;

			if (from_x < r) from_x = r;
			if (from_y < r) from_y = r;
			if (from_x > tex_width - r) from_x = tex_width - r;
			if (from_y > tex_height - r) from_y = tex_height - r;

			if (to_x < r) to_x = r;
			if (to_y < r) to_y = r;
			if (to_x > tex_width - r) to_x = tex_width - r;
			if (to_y > tex_height - r) to_y = tex_height - r;

			RectTransform canvasRect = line_panel.transform.parent.GetComponent<RectTransform>();

			float delta = 0.0001f;

			// Bottom Left
			Vector3 pointPos1 = line_panel.transform.position;
			pointPos1.x += width * (-0.5f + 1.0f - 1.0f * from_x / tex_width) * canvasRect.localScale.x;
			pointPos1.y += height * (-0.5f + 1.0f * from_y / tex_height) * canvasRect.localScale.y;
			pointPos1.z += delta;

			// Top Right
			Vector3 pointPos2 = line_panel.transform.position;
			pointPos2.x += width * (-0.5f + 1.0f - 1.0f * to_x / tex_width) * canvasRect.localScale.x;
			pointPos2.y += height * (-0.5f + 1.0f * to_y / tex_height) * canvasRect.localScale.y;
			pointPos2.z += delta;

			GameObject newLine;
			LineRenderer lRend;
			if (lineObjectBufferIndex < lineObjectBuffer.Count)
			{
				newLine = lineObjectBuffer[lineObjectBufferIndex];
				lRend = newLine.GetComponent<LineRenderer>();
			}
			else
			{
				newLine = Instantiate(line, lines.gameObject.transform);
				newLine.layer = lines.gameObject.layer;
				lRend = newLine.GetComponent<LineRenderer>();
				lineObjectBuffer.Add(newLine);
			}
			lineObjectBufferIndex++;
			newLine.SetActive(true);

			Color32 c1 = color;
			c1.a = 128 + 32;

			lRend.startColor = c1;
			lRend.endColor = c1;

			float base_width = r / 2.0f;

			//from_z = 0;
			//to_z = 0;

			lRend.positionCount = 2;
			lRend.startWidth = System.Math.Max((-from_z * 10 + 1), 1) * base_width;
			lRend.endWidth = System.Math.Max((-to_z * 10 + 1), 1) * base_width;

			Vector3 startVec = pointPos1;
			Vector3 endVec = pointPos2;
			lRend.SetPosition(0, startVec);
			lRend.SetPosition(1, endVec);
		}

		public void DrawEdgeOfRect2D(Color32 color, int from_x, int from_y, int to_x, int to_y, int tex_width, int tex_height)
		{
			RectTransform panelRect = line_panel.GetComponent<RectTransform>();
			float width = panelRect.rect.width;
			float height = panelRect.rect.height;

			int r = 2;
			float lineW = 1.0f;

			if (from_x < r) from_x = r;
			if (from_y < r) from_y = r;
			if (from_x > tex_width - r) from_x = tex_width - r;
			if (from_y > tex_height - r) from_y = tex_height - r;

			if (to_x < r) to_x = r;
			if (to_y < r) to_y = r;
			if (to_x > tex_width - r) to_x = tex_width - r;
			if (to_y > tex_height - r) to_y = tex_height - r;

			float expand_x = from_y == to_y ? lineW / 2 : 0.0f;
			float expand_y = from_x == to_x ? lineW / 2 : 0.0f;

			RectTransform canvasRect = line_panel.transform.parent.GetComponent<RectTransform>();

			float delta = 0.0001f;

			// Bottom Left
			Vector3 pointPos1 = line_panel.transform.position;
			pointPos1.x += width * (-0.5f + 1.0f - 1.0f * from_x / tex_width) * canvasRect.localScale.x + expand_x;
			pointPos1.y += height * (-0.5f + 1.0f * from_y / tex_height) * canvasRect.localScale.y + expand_y;
			pointPos1.z += delta;

			// Top Right
			Vector3 pointPos2 = line_panel.transform.position;
			pointPos2.x += width * (-0.5f + 1.0f - 1.0f * to_x / tex_width) * canvasRect.localScale.x - expand_x;
			pointPos2.y += height * (-0.5f + 1.0f * to_y / tex_height) * canvasRect.localScale.y - expand_y;
			pointPos2.z += delta;

			GameObject newLine;
			LineRenderer lRend;
			if (lineObjectBufferIndex < lineObjectBuffer.Count)
			{
				newLine = lineObjectBuffer[lineObjectBufferIndex];
				lRend = newLine.GetComponent<LineRenderer>();
			}
			else
			{
				newLine = Instantiate(line, lines.gameObject.transform);
				newLine.layer = lines.gameObject.layer;
				lRend = newLine.GetComponent<LineRenderer>();
				lineObjectBuffer.Add(newLine);
			}
			lineObjectBufferIndex++;
			newLine.SetActive(true);

			Color32 c1 = color;
			c1.a = 160;

			lRend.startColor = c1;
			lRend.endColor = c1;

			lRend.positionCount = 2;
			lRend.startWidth = lineW;
			lRend.endWidth = lineW;
			Vector3 startVec = pointPos1;
			Vector3 endVec = pointPos2;
			lRend.SetPosition(0, startVec);
			lRend.SetPosition(1, endVec);
		}

		public void AppendEdgeOfRect2D(Color32 color, int from_x, int from_y, int tex_width, int tex_height, LineRenderer lRend)
		{
			RectTransform panelRect = line_panel.GetComponent<RectTransform>();
			float width = panelRect.rect.width;
			float height = panelRect.rect.height;

			int r = 2;

			if (from_x < r) from_x = r;
			if (from_y < r) from_y = r;
			if (from_x > tex_width - r) from_x = tex_width - r;
			if (from_y > tex_height - r) from_y = tex_height - r;

			RectTransform canvasRect = line_panel.transform.parent.GetComponent<RectTransform>();

			float delta = 0.0001f;

			// Bottom Left
			Vector3 pointPos1 = line_panel.transform.position;
			pointPos1.x += width * (-0.5f + 1.0f - 1.0f * from_x / tex_width) * canvasRect.localScale.x;
			pointPos1.y += height * (-0.5f + 1.0f * from_y / tex_height) * canvasRect.localScale.y;
			pointPos1.z += delta;

			int jointPositionCount = lRend.positionCount;
			lRend.positionCount = lRend.positionCount + 1;

			Vector3 startVec = pointPos1;
			lRend.SetPosition(jointPositionCount, startVec);
		}

		public void DrawRect2D(Color32 color, int x, int y, int w, int h, int tex_width, int tex_height)
		{
			GameObject newLine;
			LineRenderer lRend;
			if (lineObjectBufferIndex < lineObjectBuffer.Count)
			{
				newLine = lineObjectBuffer[lineObjectBufferIndex];
				lRend = newLine.GetComponent<LineRenderer>();
			}
			else
			{
				newLine = Instantiate(line, lines.gameObject.transform);
				newLine.layer = lines.gameObject.layer;
				lRend = newLine.GetComponent<LineRenderer>();
				lineObjectBuffer.Add(newLine);
			}
			lineObjectBufferIndex++;

			lRend.positionCount = 0;
			lRend.loop = true;

			Color32 c1 = color;
			c1.a = 160;

			lRend.startColor = c1;
			lRend.endColor = c1;

			float lineW = 1.0f;
			lRend.startWidth = lineW;
			lRend.endWidth = lineW;

			AppendEdgeOfRect2D(color, x, y, tex_width, tex_height, lRend);
			AppendEdgeOfRect2D(color, x, y + h, tex_width, tex_height, lRend);
			AppendEdgeOfRect2D(color, x + w, y + h, tex_width, tex_height, lRend);
			AppendEdgeOfRect2D(color, x + w, y, tex_width, tex_height, lRend);

			newLine.SetActive(true);
		}

		public void DrawAffine2D(Color32 color, int x, int y, int w, int h, int tex_width, int tex_height, float theta)
		{
			GameObject newLine;
			LineRenderer lRend;
			if (lineObjectBufferIndex < lineObjectBuffer.Count)
			{
				newLine = lineObjectBuffer[lineObjectBufferIndex];
				lRend = newLine.GetComponent<LineRenderer>();
			}
			else
			{
				newLine = Instantiate(line, lines.gameObject.transform);
				newLine.layer = lines.gameObject.layer;
				lRend = newLine.GetComponent<LineRenderer>();
				lineObjectBuffer.Add(newLine);
			}
			lineObjectBufferIndex++;

			lRend.positionCount = 0;
			lRend.loop = true;

			Color32 c1 = color;
			c1.a = 160;

			lRend.startColor = c1;
			lRend.endColor = c1;

			float lineW = 1.0f;
			lRend.startWidth = lineW;
			lRend.endWidth = lineW;

			float cs = (float)System.Math.Cos(-theta);
			float ss = (float)System.Math.Sin(-theta);

			AppendEdgeOfRect2D(color, (int)(x + w / 2 + w / 2 * cs + h / 2 * ss), (int)(y + h / 2 + w / 2 * -ss + h / 2 * cs), tex_width, tex_height, lRend);
			AppendEdgeOfRect2D(color, (int)(x + w / 2 - w / 2 * cs + h / 2 * ss), (int)(y + h / 2 - w / 2 * -ss + h / 2 * cs), tex_width, tex_height, lRend);
			AppendEdgeOfRect2D(color, (int)(x + w / 2 - w / 2 * cs - h / 2 * ss), (int)(y + h / 2 - w / 2 * -ss - h / 2 * cs), tex_width, tex_height, lRend);
			AppendEdgeOfRect2D(color, (int)(x + w / 2 + w / 2 * cs - h / 2 * ss), (int)(y + h / 2 + w / 2 * -ss - h / 2 * cs), tex_width, tex_height, lRend);

			newLine.SetActive(true);
		}

		public void DrawText(Color color, string text, int x, int y, int tex_width, int tex_height)
		{
			RectTransform panelRect = line_panel.GetComponent<RectTransform>();
			float width = panelRect.rect.width;
			float height = panelRect.rect.height;

			int r = 2;
			if (x < r) x = r;
			if (y < r) y = r;

			GameObject text_object = null;
			if (textObjectBufferIndex < textObjectBuffer.Count)
			{
				text_object = textObjectBuffer[textObjectBufferIndex];
			}
			else
			{
				text_object = GameObject.Instantiate(text_base, text_panel.gameObject.transform);
				textObjectBuffer.Add(text_object);
			}
			textObjectBufferIndex++;

			text_object.SetActive(true);
			text_object.transform.GetChild(0).GetComponent<Text>().text = text;
			color.a = 160 / 255.0f;
			text_object.GetComponent<Image>().color = color;
			text_object.GetComponent<RectTransform>().anchoredPosition = new Vector2(x * width / tex_width, -y * height / tex_height);
		}


		public void DrawBone3D(Color32 color, AiliaPoseEstimator.AILIAPoseEstimatorObjectPose obj, uint from, uint to)
		{
			float th = 0.1f;
			if (obj.points[from].score <= th || obj.points[to].score <= th)
			{
				return;
			}

			//追加
			float from_x = obj.points[from].x_local;
			float from_y = obj.points[from].y_local;
			float from_z = obj.points[from].z_local;
			float to_x = obj.points[to].x_local;
			float to_y = obj.points[to].y_local;
			float to_z = obj.points[to].z_local;
			float origin_x = (obj.points[AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_HIP_LEFT].x_local + obj.points[AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_HIP_RIGHT].x_local) / 2.0f;
			float origin_y = (obj.points[AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_HIP_LEFT].y_local + obj.points[AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_HIP_RIGHT].y_local) / 2.0f;
			float origin_z = (obj.points[AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_HIP_LEFT].z_local + obj.points[AiliaPoseEstimator.AILIA_POSE_ESTIMATOR_POSE_KEYPOINT_HIP_RIGHT].z_local) / 2.0f;

			//腰の中点を原点に変える
			from_x -= origin_x;
			from_y -= origin_y;
			from_z -= origin_z;
			to_x -= origin_x;
			to_y -= origin_y;
			to_z -= origin_z;

			//3次元landmarkを回転させる
			double speed = Math.PI / 100.0f; //1フレームあたりの回転角
			time += Time.deltaTime;
			float tmp_from_x = from_x; //更新前の値
			float tmp_from_y = from_y;
			float tmp_from_z = from_z;
			float tmp_to_x = to_x;
			float tmp_to_y = to_y;
			float tmp_to_z = to_z;
			from_x = (float)(tmp_from_x * Math.Cos(speed / Math.PI * time) + tmp_from_z * Math.Sin(speed / Math.PI * time));
			from_y = tmp_from_y;
			from_z = (float)(-tmp_from_x * Math.Sin(speed / Math.PI * time) + tmp_from_z * Math.Cos(speed / Math.PI * time));
			to_x = (float)(tmp_to_x * Math.Cos(speed / Math.PI * time) + tmp_to_z * Math.Sin(speed / Math.PI * time));
			to_y = tmp_to_y;
			to_z = (float)(-tmp_to_x * Math.Sin(speed / Math.PI * time) + tmp_to_z * Math.Cos(speed / Math.PI * time));

			Color sphereColor = Color.white; //球の色だけここで指定する 右(231, 217, 0)
            if (LANDMARK_LEFT.Contains(from))
            {
				sphereColor = new Color(0.0f, 179.0f / 255, 255.0f / 255, 1.0f); //左
			}
			else if (LANDMARK_RIGHT.Contains(from))
            {
				sphereColor = new Color(248.0f / 255, 123.0f / 255, 0.0f, 1.0f); //右
			}

			DrawSphere3D(sphereColor, from_x, from_y, from_z);
			DrawLine3D(color, from_x, from_y, from_z, to_x, to_y, to_z);
		}




		//追加
		public void DrawSphere3D(Color color, float pos_x, float pos_y, float pos_z)
		{
			GameObject newSphere;
			if (sphereObjectBufferIndex < sphereObjectBuffer.Count)
			{
				newSphere = sphereObjectBuffer[sphereObjectBufferIndex];
			}
			else
			{
				newSphere = Instantiate(sphere, spheres.gameObject.transform);
				sphereObjectBuffer.Add(newSphere);
			}
			sphereObjectBufferIndex++;
			newSphere.SetActive(true);

			MeshRenderer mesh = newSphere.GetComponent<MeshRenderer>(); //球の色を変更
			mesh.material.color = color;

			Vector3 pointPos = drawPos; //原点の座標 見えやすいところにずらす
			pointPos.x += pos_x * scale;
			pointPos.y += pos_y * scale;
			pointPos.z += pos_z * scale;

			newSphere.transform.position = pointPos; //大きさと位置を調整
		}


		//追加
		public void DrawLine3D(Color32 color, float from_x, float from_y, float from_z, float to_x, float to_y, float to_z)
		{
			Vector3 pointPos1 = drawPos; //原点の座標
			pointPos1.x += from_x * scale;
			pointPos1.y += from_y * scale;
			pointPos1.z += from_z * scale;

			Vector3 pointPos2 = drawPos; //原点の座標
			pointPos2.x += to_x * scale;
			pointPos2.y += to_y * scale;
			pointPos2.z += to_z * scale;

			GameObject newLine;
			LineRenderer lRend;
			if (lineObjectBufferIndex3D < lineObjectBuffer3D.Count)
			{
				newLine = lineObjectBuffer3D[lineObjectBufferIndex3D];
				lRend = newLine.GetComponent<LineRenderer>();
			}
			else
			{
				newLine = Instantiate(line3D, lines3D.gameObject.transform);
				newLine.layer = line3D.gameObject.layer;
				lRend = newLine.GetComponent<LineRenderer>();
				lineObjectBuffer3D.Add(newLine);
			}
			lineObjectBufferIndex3D++;
			newLine.SetActive(true);

			Color32 c1 = color;
			c1.a = 128 + 32;

			lRend.startColor = c1;
			lRend.endColor = c1;

			lRend.positionCount = 2;
			lRend.startWidth = 1; //仮
			lRend.endWidth = 1; //仮

			Vector3 startVec = pointPos1;
			Vector3 endVec = pointPos2;
			lRend.SetPosition(0, startVec);
			lRend.SetPosition(1, endVec);
		}


		//追加
		public void DrawAxis3D(AiliaPoseEstimator.AILIAPoseEstimatorObjectPose obj)
		{
			float scale = 1.0f;

			//全てのlandmarkのx,y,z座標のうち，絶対値が最大のものを取得する
			//また，y座標が最小のものを取得する
			float abs_max = 0.0f;
			float y_min = 1.0f;
			for (int i = 0; i < obj.points.Length; i++)
			{
				abs_max = Mathf.Max(abs_max, Math.Abs(obj.points[i].x_local), Math.Abs(obj.points[i].y_local), Math.Abs(obj.points[i].z_local));
				y_min = Mathf.Min(y_min, obj.points[i].y_local);
			}
			scale = abs_max + 0.1f;

			//外側の軸
			DrawAxisSphere3D(new Color(0.0f, 92.0f / 255, 0.0f, 1.0f), -scale, y_min, -scale);
			DrawAxisSphere3D(new Color(0.0f, 92.0f / 255, 0.0f, 1.0f), -scale, y_min, scale);
			DrawAxisSphere3D(new Color(0.0f, 92.0f / 255, 0.0f, 1.0f), -scale, y_min - scale * 2, -scale);
			DrawAxisSphere3D(new Color(0.0f, 92.0f / 255, 0.0f, 1.0f), -scale, y_min - scale * 2, scale);
			DrawAxisSphere3D(new Color(0.0f, 92.0f / 255, 0.0f, 1.0f), scale, y_min, -scale);
			DrawAxisSphere3D(new Color(0.0f, 92.0f / 255, 0.0f, 1.0f), scale, y_min, scale);
			DrawAxisSphere3D(new Color(0.0f, 92.0f / 255, 0.0f, 1.0f), scale, y_min - scale * 2, -scale);
			DrawAxisSphere3D(new Color(0.0f, 92.0f / 255, 0.0f, 1.0f), scale, y_min - scale * 2, scale);
			DrawAxisLine3D(Color.white, -scale, y_min, -scale, scale, y_min, -scale);
			DrawAxisLine3D(Color.white, -scale, y_min - scale * 2, -scale, scale, y_min - scale * 2, -scale);
			DrawAxisLine3D(Color.white, -scale, y_min - scale * 2, scale, scale, y_min - scale * 2, scale);
			DrawAxisLine3D(Color.white, -scale, y_min, scale, scale, y_min, scale);
			DrawAxisLine3D(Color.white, -scale, y_min, -scale, -scale, y_min - scale * 2, -scale);
			DrawAxisLine3D(Color.white, scale, y_min, -scale, scale, y_min - scale * 2, -scale);
			DrawAxisLine3D(Color.white, scale, y_min, scale, scale, y_min - scale * 2, scale);
			DrawAxisLine3D(Color.white, -scale, y_min, scale, -scale, y_min - scale * 2, scale);
			DrawAxisLine3D(Color.white, -scale, y_min, -scale, -scale, y_min, scale);
			DrawAxisLine3D(Color.white, scale, y_min, -scale, scale, y_min, scale);
			DrawAxisLine3D(Color.white, scale, y_min - scale * 2, -scale, scale, y_min - scale * 2, scale);
			DrawAxisLine3D(Color.white, -scale, y_min - scale * 2, -scale, -scale, y_min - scale * 2, scale);

			//内側のグリッド線
			DrawAxisLine3D(new Color(1.0f, 1.0f, 1.0f, 0.1f), -scale, y_min, -scale * 0.75f, scale, y_min, -scale * 0.75f, 0.5f);
			DrawAxisLine3D(new Color(1.0f, 1.0f, 1.0f, 0.1f), -scale, y_min, -scale * 0.5f, scale, y_min, -scale * 0.5f, 0.5f);
			DrawAxisLine3D(new Color(1.0f, 1.0f, 1.0f, 0.1f), -scale, y_min, -scale * 0.25f, scale, y_min, -scale * 0.25f, 0.5f);
			DrawAxisLine3D(new Color(1.0f, 1.0f, 1.0f, 0.1f), -scale, y_min, 0.0f, scale, y_min, 0.0f, 0.5f);
			DrawAxisLine3D(new Color(1.0f, 1.0f, 1.0f, 0.1f), -scale, y_min, scale * 0.25f, scale, y_min, scale * 0.25f, 0.5f);
			DrawAxisLine3D(new Color(1.0f, 1.0f, 1.0f, 0.1f), -scale, y_min, scale * 0.5f, scale, y_min, scale * 0.5f, 0.5f);
			DrawAxisLine3D(new Color(1.0f, 1.0f, 1.0f, 0.1f), -scale, y_min, scale * 0.75f, scale, y_min, scale * 0.75f, 0.5f);

			DrawAxisLine3D(new Color(1.0f, 1.0f, 1.0f, 0.1f), -scale * 0.75f, y_min, -scale, -scale * 0.75f, y_min, scale, 0.5f);
			DrawAxisLine3D(new Color(1.0f, 1.0f, 1.0f, 0.1f), -scale * 0.5f, y_min, -scale, -scale * 0.5f, y_min, scale, 0.5f);
			DrawAxisLine3D(new Color(1.0f, 1.0f, 1.0f, 0.1f), -scale * 0.25f, y_min, -scale, -scale * 0.25f, y_min, scale, 0.5f);
			DrawAxisLine3D(new Color(1.0f, 1.0f, 1.0f, 0.1f), 0.0f, y_min, -scale, 0.0f, y_min, scale, 0.5f);
			DrawAxisLine3D(new Color(1.0f, 1.0f, 1.0f, 0.1f), scale * 0.25f, y_min, -scale, scale * 0.25f, y_min, scale, 0.5f);
			DrawAxisLine3D(new Color(1.0f, 1.0f, 1.0f, 0.1f), scale * 0.5f, y_min, -scale, scale * 0.5f, y_min, scale, 0.5f);
			DrawAxisLine3D(new Color(1.0f, 1.0f, 1.0f, 0.1f), scale * 0.75f, y_min, -scale, scale * 0.75f, y_min, scale, 0.5f);

		}


		//追加
		public void DrawAxisSphere3D(Color color, float pos_x, float pos_y, float pos_z)
		{
			GameObject newSphere;
			if (axisSphereObjectBufferIndex < axisSphereObjectBuffer.Count)
			{
				newSphere = axisSphereObjectBuffer[axisSphereObjectBufferIndex];
			}
			else
			{
				newSphere = Instantiate(sphere, spheres.gameObject.transform);
				axisSphereObjectBuffer.Add(newSphere);
			}
			axisSphereObjectBufferIndex++;
			newSphere.SetActive(true);

			MeshRenderer mesh = newSphere.GetComponent<MeshRenderer>();
			mesh.material.color = color;

			//回転させる
			double speed = Math.PI / 100.0f; //1フレームあたりの回転角
			time += Time.deltaTime;
			float tmp_x = pos_x;
			float tmp_y = pos_y;
			float tmp_z = pos_z;
			pos_x = (float)(tmp_x * Math.Cos(speed / Math.PI * time) + tmp_z * Math.Sin(speed / Math.PI * time));
			pos_y = tmp_y;
			pos_z = (float)(-tmp_x * Math.Sin(speed / Math.PI * time) + tmp_z * Math.Cos(speed / Math.PI * time));

			Vector3 pointPos = drawPos; //大きさと位置を調整
			pointPos.x += pos_x * scale;
			pointPos.y += pos_y * scale;
			pointPos.z += pos_z * scale;

			newSphere.transform.position = pointPos;
		}


		//追加
		public void DrawAxisLine3D(Color32 color, float from_x, float from_y, float from_z, float to_x, float to_y, float to_z, float r = 2)
		{
			GameObject newLine;
			LineRenderer lRend;
			if (axisLineObjectBufferIndex3D < axisLineObjectBuffer3D.Count)
			{
				newLine = axisLineObjectBuffer3D[axisLineObjectBufferIndex3D];
				lRend = newLine.GetComponent<LineRenderer>();
			}
			else
			{
				newLine = Instantiate(line3D, lines3D.gameObject.transform);
				newLine.layer = line3D.gameObject.layer;
				lRend = newLine.GetComponent<LineRenderer>();
				axisLineObjectBuffer3D.Add(newLine);
			}
			axisLineObjectBufferIndex3D++;
			newLine.SetActive(true);

			Color32 c1 = color;
			c1.a = 128 + 32;

			lRend.startColor = c1;
			lRend.endColor = c1;

			float base_width = r / 2.0f;

			lRend.positionCount = 2;
			lRend.startWidth = base_width;
			lRend.endWidth = base_width;

			//回転させる
			double speed = Math.PI / 100.0f; //1フレームあたりの回転角
			time += Time.deltaTime;
			float tmp_from_x = from_x;
			float tmp_from_y = from_y;
			float tmp_from_z = from_z;
			from_x = (float)(tmp_from_x * Math.Cos(speed / Math.PI * time) + tmp_from_z * Math.Sin(speed / Math.PI * time));
			from_y = tmp_from_y;
			from_z = (float)(-tmp_from_x * Math.Sin(speed / Math.PI * time) + tmp_from_z * Math.Cos(speed / Math.PI * time));
			float tmp_to_x = to_x;
			float tmp_to_y = to_y;
			float tmp_to_z = to_z;
			to_x = (float)(tmp_to_x * Math.Cos(speed / Math.PI * time) + tmp_to_z * Math.Sin(speed / Math.PI * time));
			to_y = tmp_to_y;
			to_z = (float)(-tmp_to_x * Math.Sin(speed / Math.PI * time) + tmp_to_z * Math.Cos(speed / Math.PI * time));

			Vector3 pointPos1 = drawPos; //大きさと位置を調整
			pointPos1.x += from_x * scale;
			pointPos1.y += from_y * scale;
			pointPos1.z += from_z * scale;

			Vector3 pointPos2 = drawPos;
			pointPos2.x += to_x * scale;
			pointPos2.y += to_y * scale;
			pointPos2.z += to_z * scale;

			Vector3 startVec = pointPos1;
			Vector3 endVec = pointPos2;
			lRend.SetPosition(0, startVec);
			lRend.SetPosition(1, endVec);
		}

	}
} 