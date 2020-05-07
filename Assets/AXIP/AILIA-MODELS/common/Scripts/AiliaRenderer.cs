using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AiliaRenderer : MonoBehaviour {

	public Material line_renderer_material;	//Sprite/Default,RenderQueue=Transparent+2000
	public GameObject line_panel;	//LinePanel
	public GameObject lines;		//LinePanel/Lines
	public GameObject text_panel;	//TextPanel
	public GameObject text_base;    //TextPanel/Text
	List<GameObject> textObjectBuffer = new List<GameObject>();
	int textObjectBufferIndex = 0;

	public void Clear(){
		if(lines){
			foreach(Transform position in lines.transform){
				if(position.gameObject){
					Destroy(position.gameObject);
				}
			}
		}
		for(int i = textObjectBufferIndex; i < textObjectBuffer.Count; i++)
		{
			textObjectBuffer[i].SetActive(false);
		}
		textObjectBufferIndex = 0;
	}

	public void DrawBone(Color32 color,int tex_width,int tex_height,AiliaPoseEstimator.AILIAPoseEstimatorObjectPose obj,uint from,uint to,int r){
		float th=0.1f;
		if(obj.points[from].score<=th || obj.points[to].score<=th){
			return;
		}

		int from_x=(int)(tex_width*obj.points[from].x);
		int from_y=(int)(tex_height*obj.points[from].y);
		int to_x=(int)(tex_width*obj.points[to].x);
		int to_y=(int)(tex_height*obj.points[to].y);

		int len=(to_x-from_x)*(to_x-from_x)+(to_y-from_y)*(to_y-from_y);
		len=(int)Mathf.Sqrt(len);

		DrawLine(color,from_x,from_y,obj.points[from].z_local,to_x,to_y,obj.points[to].z_local,tex_width,tex_height);
	}

	public void DrawLine(Color32 color,int from_x,int from_y,float from_z,int to_x,int to_y,float to_z,int tex_width,int tex_height){
		RectTransform panelRect = line_panel.GetComponent<RectTransform> ();
		float width = panelRect.rect.width;
		float height = panelRect.rect.height;

		int r=2;

		if(from_x<r) from_x=r;
		if(from_y<r) from_y=r;
		if(from_x>tex_width-r) from_x=tex_width-r;
		if(from_y>tex_height-r) from_y=tex_height-r;

		if(to_x<r) to_x=r;
		if(to_y<r) to_y=r;
		if(to_x>tex_width-r) to_x=tex_width-r;
		if(to_y>tex_height-r) to_y=tex_height-r;

		RectTransform canvasRect = line_panel.transform.parent.GetComponent<RectTransform> ();

		float delta=0.0001f;

		// Bottom Left
		Vector3 pointPos1 = line_panel.transform.position;
		pointPos1.x += width * (-0.5f + 1.0f-1.0f*from_x/tex_width) * canvasRect.localScale.x;
		pointPos1.y += height * (-0.5f + 1.0f*from_y/tex_height) * canvasRect.localScale.y;
		pointPos1.z += delta;

		// Top Right
		Vector3 pointPos2 = line_panel.transform.position;
		pointPos2.x += width * (-0.5f + 1.0f-1.0f*to_x/tex_width) * canvasRect.localScale.x;
		pointPos2.y += height * (-0.5f + 1.0f*to_y/tex_height) * canvasRect.localScale.y;
		pointPos2.z += delta;

		GameObject newLine = new GameObject ("Line");

		Color32 c1=color;
		c1.a=128+32;

		newLine.transform.parent=lines.gameObject.transform;
		newLine.layer=lines.gameObject.layer;

		LineRenderer lRend = newLine.AddComponent<LineRenderer> ();

		lRend.material = line_renderer_material;
		lRend.startColor=c1;
		lRend.endColor=c1;

		float base_width=r/2.0f;

		from_z=0;
		to_z=0;

		lRend.positionCount=2;
		lRend.startWidth = (from_z*100+1)*base_width;
		lRend.endWidth = (to_z*100+1)*base_width;
		Vector3 startVec = pointPos1;
		Vector3 endVec = pointPos2;
		lRend.SetPosition (0, startVec);
		lRend.SetPosition (1, endVec);
	}

	public void DrawEdgeOfRect2D(Color32 color,int from_x,int from_y,int to_x,int to_y,int tex_width,int tex_height){
		RectTransform panelRect = line_panel.GetComponent<RectTransform> ();
		float width = panelRect.rect.width;
		float height = panelRect.rect.height;

		int r=2;
		float lineW = 1.0f;

		if(from_x<r) from_x=r;
		if(from_y<r) from_y=r;
		if(from_x>tex_width-r) from_x=tex_width-r;
		if(from_y>tex_height-r) from_y=tex_height-r;

		if(to_x<r) to_x=r;
		if(to_y<r) to_y=r;
		if(to_x>tex_width-r) to_x=tex_width-r;
		if(to_y>tex_height-r) to_y=tex_height-r;

		float expand_x = from_y == to_y ? lineW / 2 : 0.0f;
		float expand_y = from_x == to_x ? lineW / 2 : 0.0f;

		RectTransform canvasRect = line_panel.transform.parent.GetComponent<RectTransform> ();

		float delta=0.0001f;

		// Bottom Left
		Vector3 pointPos1 = line_panel.transform.position;
		pointPos1.x += width * (-0.5f + 1.0f-1.0f*from_x/tex_width) * canvasRect.localScale.x + expand_x;
		pointPos1.y += height * (-0.5f + 1.0f*from_y/tex_height) * canvasRect.localScale.y + expand_y;
		pointPos1.z += delta;

		// Top Right
		Vector3 pointPos2 = line_panel.transform.position;
		pointPos2.x += width * (-0.5f + 1.0f-1.0f*to_x/tex_width) * canvasRect.localScale.x - expand_x;
		pointPos2.y += height * (-0.5f + 1.0f*to_y/tex_height) * canvasRect.localScale.y - expand_y;
		pointPos2.z += delta;

		GameObject newLine = new GameObject ("Line");

		Color32 c1=color;
		c1.a=160;

		newLine.transform.parent=lines.gameObject.transform;
		newLine.layer=lines.gameObject.layer;

		LineRenderer lRend = newLine.AddComponent<LineRenderer> ();

		lRend.material = line_renderer_material;
		lRend.startColor=c1;
		lRend.endColor=c1;

		lRend.positionCount=2;
		lRend.startWidth = lineW;
		lRend.endWidth = lineW;
		Vector3 startVec = pointPos1;
		Vector3 endVec = pointPos2;
		lRend.SetPosition (0, startVec);
		lRend.SetPosition (1, endVec);
	}

	public void DrawRect2D(Color32 color, int x, int y, int w, int h,int tex_width,int tex_height){
		DrawEdgeOfRect2D(color, x, y, x, y + h, tex_width, tex_height);
		DrawEdgeOfRect2D(color, x + w, y, x + w, y + h, tex_width, tex_height);
		DrawEdgeOfRect2D(color, x, y, x + w, y, tex_width, tex_height);
		DrawEdgeOfRect2D(color, x, y + h, x + w, y + h, tex_width, tex_height);
	}

	public void DrawText(Color color,string text,int x,int y,int tex_width,int tex_height){
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
}
