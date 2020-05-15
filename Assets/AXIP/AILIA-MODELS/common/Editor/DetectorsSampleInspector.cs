using System;
using System.ComponentModel;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AiliaDetectorsSample))]
public class DetectorsSampleInspector : Editor
{
	AiliaDetectorsSample ailiaDetectorsSample;
	SerializedProperty ailiaModelType;

	AiliaModelsConst.AiliaModelTypes[] modelArr;
	string[] modelNameArr;
	private void OnEnable()
	{
		ailiaDetectorsSample = target as AiliaDetectorsSample;

		ailiaModelType = serializedObject.FindProperty("ailiaModelType");
		// Get all model types in the same category
		var category = ((AiliaModelsConst.AiliaModelTypes)ailiaModelType.enumValueIndex).GetCategory();
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

		serializedObject.ApplyModifiedProperties();
	}
}
