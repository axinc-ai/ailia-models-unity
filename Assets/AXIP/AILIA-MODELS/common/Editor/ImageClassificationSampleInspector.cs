using System;
using System.ComponentModel;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace ailiaSDK
{
	[CustomEditor(typeof(AiliaImageClassificationSample))]
	public class ImageClassificationSampleInspector : Editor
	{
		SerializedProperty ailiaModelType;
		SerializedProperty resnet50Model;
		SerializedProperty uiCanvas;
		SerializedProperty gpuMode;
		SerializedProperty isEnglish;
		SerializedProperty cameraID;
		MonoScript script = null;

		AiliaModelsConst.AiliaModelTypes[] modelArr;
		string[] modelNameArr;
		// Detectors category
		const string category = "Image Classification";
		private void OnEnable() {
			script = MonoScript.FromMonoBehaviour((AiliaImageClassificationSample)target);

			ailiaModelType = serializedObject.FindProperty("ailiaModelType");
			resnet50Model = serializedObject.FindProperty("resnet50model");
			uiCanvas = serializedObject.FindProperty("UICanvas");
			gpuMode = serializedObject.FindProperty("gpu_mode");
			isEnglish = serializedObject.FindProperty("is_english");
			cameraID = serializedObject.FindProperty("camera_id");
			// Get all model types in the same category
			// var category = ((AiliaModelsConst.AiliaModelTypes)ailiaModelType.enumValueIndex).GetCategory(); //Get category by ailiaModelType default value.
			var allModelsTypes = Enum.GetValues(typeof(AiliaModelsConst.AiliaModelTypes)) as AiliaModelsConst.AiliaModelTypes[];
			modelArr = allModelsTypes.Where(x => x.GetCategory() == category).ToArray();
			modelNameArr = modelArr.Select(x => x.GetDescription()).ToArray();
		}
		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			// base.OnInspectorGUI();
			EditorGUI.BeginDisabledGroup(true);
			EditorGUILayout.ObjectField("Script", script, typeof(MonoScript), false);
			EditorGUI.EndDisabledGroup();

			EditorGUILayout.PropertyField(uiCanvas);
			EditorGUILayout.PropertyField(gpuMode);
			EditorGUILayout.PropertyField(isEnglish);
			EditorGUILayout.PropertyField(cameraID);
			var currentIndex = Array.FindIndex(modelArr, x => x == (AiliaModelsConst.AiliaModelTypes)ailiaModelType.enumValueIndex);
			currentIndex = EditorGUILayout.Popup("AiliaModelType", currentIndex, modelNameArr);
			ailiaModelType.enumValueIndex = (int)modelArr[currentIndex];
			if (ailiaModelType.enumValueIndex == (int)AiliaModelsConst.AiliaModelTypes.resnet50)
			{
				EditorGUI.indentLevel++;
				var index = EditorGUILayout.Popup(
					"Resnet50 Model",
					ArrayUtility.IndexOf(AiliaModelsConst.Resnet50Model, resnet50Model.stringValue),
					AiliaModelsConst.Resnet50Model
				);
				resnet50Model.stringValue = AiliaModelsConst.Resnet50Model[index];
				EditorGUI.indentLevel--;
			}
			serializedObject.ApplyModifiedProperties();
		}
	}
}
