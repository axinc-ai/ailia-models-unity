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
		AiliaDetectorsSample ailiaDetectorsSample;
		SerializedProperty ailiaModelType;
		SerializedProperty uiCanvas;

		AiliaModelsConst.AiliaModelTypes[] modelArr;
		string[] modelNameArr;
		// Detectors category
		const string category = "Pose Estimation";
		private void OnEnable() {

			ailiaModelType = serializedObject.FindProperty("ailiaModelType");
			uiCanvas = serializedObject.FindProperty("UICanvas");
			// Get all model types in the same category
			// var category = ((AiliaModelsConst.AiliaModelTypes)ailiaModelType.enumValueIndex).GetCategory(); //Get category by ailiaModelType default value.
			var allModelsTypes = Enum.GetValues(typeof(AiliaModelsConst.AiliaModelTypes)) as AiliaModelsConst.AiliaModelTypes[];
			modelArr = allModelsTypes.Where(x => x.GetCategory() == category).ToArray();
			modelNameArr = modelArr.Select(x => x.GetDescription()).ToArray();
		}
		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			base.OnInspectorGUI();

			var currentIndex = Array.FindIndex(modelArr, x => x == (AiliaModelsConst.AiliaModelTypes)ailiaModelType.enumValueIndex);
			currentIndex = EditorGUILayout.Popup("AiliaModelType", currentIndex, modelNameArr);
			ailiaModelType.enumValueIndex = (int)modelArr[currentIndex];
			EditorGUILayout.PropertyField(uiCanvas);

			serializedObject.ApplyModifiedProperties();
		}
	}
}
