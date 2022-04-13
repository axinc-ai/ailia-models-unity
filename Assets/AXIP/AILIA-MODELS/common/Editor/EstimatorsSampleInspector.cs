using System;
using System.ComponentModel;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace ailiaSDK
{
	[CustomEditor(typeof(AiliaPoseEstimatorsSample))]
	public class EstimatorsSampleInspector : Editor
	{
		SerializedProperty ailiaModelType;
		SerializedProperty uiCanvas;
		SerializedProperty gpuMode;
		SerializedProperty cameraID;
		SerializedProperty optimizedModel;
		SerializedProperty blazeposeFullbodyConmputeShader;
		SerializedProperty ikTarget;

		MonoScript script = null;

		AiliaModelsConst.AiliaModelTypes[] modelArr;
		string[] modelNameArr;
		// Detectors category
		const string category = "Pose Estimation";
		private void OnEnable()
		{
			script = MonoScript.FromMonoBehaviour((AiliaPoseEstimatorsSample)target);

			ailiaModelType = serializedObject.FindProperty("ailiaModelType");
			uiCanvas = serializedObject.FindProperty("UICanvas");
			gpuMode = serializedObject.FindProperty("gpu_mode");
			cameraID = serializedObject.FindProperty("camera_id");
			optimizedModel = serializedObject.FindProperty("optimizedModel");
			blazeposeFullbodyConmputeShader = serializedObject.FindProperty("computeShaderBlazepose");
			ikTarget = serializedObject.FindProperty("ikTarget");

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
			if(ailiaModelType.enumValueIndex == (int)AiliaModelsConst.AiliaModelTypes.lightweight_human_pose_estimation)
			{
				EditorGUI.indentLevel++;
				EditorGUILayout.PropertyField(optimizedModel, new GUIContent("Optimized Model"));
				EditorGUI.indentLevel--;

			}
			if (ailiaModelType.enumValueIndex == (int)AiliaModelsConst.AiliaModelTypes.blazepose_fullbody)
			{
				EditorGUI.indentLevel++;
				EditorGUILayout.PropertyField(blazeposeFullbodyConmputeShader, new GUIContent("Affine Transform Shader"));
				EditorGUILayout.PropertyField(ikTarget);
				EditorGUI.indentLevel--;
			}

			serializedObject.ApplyModifiedProperties();
		}
	}
}
