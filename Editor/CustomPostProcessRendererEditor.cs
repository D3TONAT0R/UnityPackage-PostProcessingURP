using System;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering.Universal.PostProcessing;

namespace UnityEditor.Rendering.Universal.PostProcess
{
	[CustomEditor(typeof(CustomPostProcessRenderer))]
	public class CustomPostProcessRendererEditor : Editor
	{
		private SerializedProperty effectOrderingProp;
		private ReorderableList beforeSkyboxOrdering;
		private ReorderableList beforeTransparentsOrdering;
		private ReorderableList beforePostProcessOrdering;
		private ReorderableList afterPostProcessOrdering;
		private ReorderableList afterRenderingOrdering;

		private string listTitle;
		private SerializedProperty currentList;

		private void OnEnable()
		{
			effectOrderingProp = serializedObject.FindProperty(nameof(CustomPostProcessRenderer.effectOrdering));

			beforeSkyboxOrdering = CreateList("beforeSkybox");
			beforeTransparentsOrdering = CreateList("beforeTransparents");
			beforePostProcessOrdering = CreateList("beforePostProcess");
			afterPostProcessOrdering = CreateList("afterPostProcess");
			afterRenderingOrdering = CreateList("afterRendering");
		}

		public override void OnInspectorGUI()
		{
			GUILayout.Space(10);
			GUILayout.Label("Effect Ordering", EditorStyles.boldLabel);
			GUILayout.Space(10);
			listTitle = "Before Skybox";
			currentList = beforeSkyboxOrdering.serializedProperty;
			beforeSkyboxOrdering.DoLayoutList();
			listTitle = "Before Transparents";
			currentList = beforeTransparentsOrdering.serializedProperty;
			beforeTransparentsOrdering.DoLayoutList();
			listTitle = "Before Post Processing";
			currentList = beforePostProcessOrdering.serializedProperty;
			beforePostProcessOrdering.DoLayoutList();
			listTitle = "After Post Processing";
			currentList = afterPostProcessOrdering.serializedProperty;
			afterPostProcessOrdering.DoLayoutList();
			listTitle = "After Rendering";
			currentList = afterRenderingOrdering.serializedProperty;
			afterRenderingOrdering.DoLayoutList();
		}

		private ReorderableList CreateList(string propertyName)
		{
			return new ReorderableList(serializedObject,
				effectOrderingProp.FindPropertyRelative(propertyName).FindPropertyRelative("serializedTypeNames"),
				true, true, false, true)
			{
				drawHeaderCallback = DrawListHeader,
				drawElementCallback = DrawListElement
			};
		}

		private void DrawListHeader(Rect rect)
		{
			int count = currentList.arraySize;
			string countInfo = count == 0 ? "No Effects" : count == 1 ? "1 Effect" : count + " Effects";
			GUI.Label(rect, $"{listTitle} ({countInfo})", EditorStyles.boldLabel);
		}

		private void DrawListElement(Rect rect, int index, bool isActive, bool isFocused)
		{
			Type t = Type.GetType(currentList.GetArrayElementAtIndex(index).stringValue);
			GUI.Label(rect, ObjectNames.NicifyVariableName(t.Name), EditorStyles.boldLabel);
		}
	}
}
