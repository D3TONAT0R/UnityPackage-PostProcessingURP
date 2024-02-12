using UnityEngine;
using UnityEngine.Rendering.Universal.PostProcessing;

namespace UnityEditor.Rendering.Universal.PostProcessing
{
	[VolumeParameterDrawer(typeof(MinMaxFloatParameter))]
	public class MinMaxFloatParameterDrawer : VolumeParameterDrawer
	{
		public override bool OnGUI(SerializedDataParameter parameter, GUIContent title)
		{
			EditorGUILayout.PropertyField(parameter.value, title);
			var param = parameter.GetObjectRef<MinMaxFloatParameter>();
			parameter.value.floatValue = Mathf.Clamp(parameter.value.floatValue, param.limitMin, param.limitMax);
			return true;
		}
	}
}
