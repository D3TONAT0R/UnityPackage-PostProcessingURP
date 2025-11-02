using UnityEngine;
using UnityEngine.Rendering.Universal.PostProcessing;

namespace UnityEditor.Rendering.Universal.PostProcessing
{
	[CustomEditor(typeof(CustomPostProcessVolumeComponent), true)]
	public class CustomPostProcessVolumeComponentEditor : VolumeComponentEditor
	{
		SerializedDataParameter enabled;
		SerializedDataParameter blend;

		public override void OnEnable()
		{
			base.OnEnable();
			var o = new PropertyFetcher<CustomPostProcessVolumeComponent>(serializedObject);
			enabled = Unpack(o.Find(x => x.enabled));
			blend = Unpack(o.Find(x => x.blend));
		}

		public override void OnInspectorGUI()
		{
			var comp = volumeComponent as CustomPostProcessVolumeComponent;
			if(!comp.VisibleInSceneView)
			{
				EditorGUILayout.HelpBox("This effect is not visible in the Scene View by default.", MessageType.None);
			}
			PropertyField(enabled);
			if(comp.SupportsBlending)
			{
				PropertyField(blend);
			}
			GUILayout.Space(5);
			base.OnInspectorGUI();
		}
	}
}
