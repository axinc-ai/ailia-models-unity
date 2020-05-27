using System;
using System.ComponentModel;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace ailiaSDK
{
	[CustomEditor(typeof(AiliaDetectorsSample))]
	public class DetectorsSampleInspector : Editor
	{
		SerializedProperty ailiaModelType;
		SerializedProperty pretrainedModel;
		SerializedProperty uiCanvas;
		SerializedProperty gpuMode;
		SerializedProperty cameraID;
		MonoScript script = null;

		AiliaModelsConst.AiliaModelTypes[] modelArr;
		string[] modelNameArr;
		// Detectors category
		const string category = "Object Detection";
		private void OnEnable() {
			script = MonoScript.FromMonoBehaviour((AiliaDetectorsSample)target);

			ailiaModelType = serializedObject.FindProperty("ailiaModelType");
			pretrainedModel = serializedObject.FindProperty("pretrainedModel");
			uiCanvas = serializedObject.FindProperty("UICanvas");
			gpuMode = serializedObject.FindProperty("gpu_mode");
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
			EditorGUILayout.PropertyField(cameraID);
			var currentIndex = Array.FindIndex(modelArr, x => x == (AiliaModelsConst.AiliaModelTypes)ailiaModelType.enumValueIndex);
			currentIndex = EditorGUILayout.Popup("AiliaModelType", currentIndex, modelNameArr);
			ailiaModelType.enumValueIndex = (int)modelArr[currentIndex];
			if(ailiaModelType.enumValueIndex == (int)AiliaModelsConst.AiliaModelTypes.mobilenet_ssd)
			{
				EditorGUI.indentLevel++;
				var index = EditorGUILayout.Popup(
					"Pretrained model", 
					ArrayUtility.IndexOf(AiliaModelsConst.MobilenetSSDPretrainedModel, pretrainedModel.stringValue),
					AiliaModelsConst.MobilenetSSDPretrainedModel
				);
				pretrainedModel.stringValue = AiliaModelsConst.MobilenetSSDPretrainedModel[index];
				EditorGUI.indentLevel--;
			}

			serializedObject.ApplyModifiedProperties();
		}
	}
}
