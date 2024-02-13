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

		private SerializedProperty shaderRefsProp;

		private string listTitle;
		private SerializedProperty currentList;

		private void OnEnable()
		{
			effectOrderingProp = serializedObject.FindProperty(nameof(CustomPostProcessRenderer.effectOrdering));

			beforeSkyboxOrdering = CreateList(nameof(CustomPostProcessRenderer.EffectOrdering.beforeSkybox));
			beforeTransparentsOrdering = CreateList(nameof(CustomPostProcessRenderer.EffectOrdering.beforeTransparents));
			beforePostProcessOrdering = CreateList(nameof(CustomPostProcessRenderer.EffectOrdering.beforePostProcess));
			afterPostProcessOrdering = CreateList(nameof(CustomPostProcessRenderer.EffectOrdering.afterPostProcess));
			afterRenderingOrdering = CreateList(nameof(CustomPostProcessRenderer.EffectOrdering.afterRendering));

			shaderRefsProp = serializedObject.FindProperty(nameof(CustomPostProcessRenderer.shaderRefs));
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

			GUILayout.Space(20);
			shaderRefsProp.isExpanded = EditorGUILayout.Foldout(shaderRefsProp.isExpanded, "Referenced Shaders");
			if(shaderRefsProp.isExpanded)
			{
				GUI.enabled = false;
				EditorGUI.indentLevel++;
				for(int i = 0; i < shaderRefsProp.arraySize; i++)
				{
					var arrayElement = shaderRefsProp.GetArrayElementAtIndex(i);
					var shader = arrayElement.objectReferenceValue as Shader;
					if(!shader)
					{
						shaderRefsProp.DeleteArrayElementAtIndex(i);
						i--;
						continue;
					}
					EditorGUILayout.PropertyField(arrayElement, GUIContent.none);
				}
				GUI.enabled = true;
				EditorGUILayout.Space(5);
				Rect rect = EditorGUILayout.GetControlRect();
				rect.xMin = rect.xMax - 120;
				if(GUI.Button(rect, "Clear List"))
				{
					if(EditorUtility.DisplayDialog("Clear Referenced Shaders", "Are you sure you want to clear all references shaders? " +
						"The referenced shaders list will need to be regenerated to ensure that the required shaders are included in builds. \n" +
						"Shaders are automatically added to the list when the effect is first rendered in the editor.",
						"Clear List", "Cancel"))
					{
						shaderRefsProp.ClearArray();
					}
				}
				EditorGUI.indentLevel--;
			}
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
