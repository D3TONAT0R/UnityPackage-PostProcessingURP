using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEngine.Rendering.Universal.PostProcessing
{
	[VolumeComponentMenu("Post-processing/Outline")]
	public class Outline : CustomPostProcessVolumeComponent
	{
		public FloatParameter range = new FloatParameter(25, false);
		public FloatParameter depthThreshold = new ClampedFloatParameter(0.01f, 0, 0.1f, false);
		public FloatParameter normalThreshold = new ClampedFloatParameter(0.2f, 0, 1f, false);
		public ColorParameter lineColor = new ColorParameter(Color.black, false);
		public ClampedFloatParameter distortion = new ClampedFloatParameter(0.005f, 0, 0.02f, false);

		public override bool IgnorePostProcessingFlag => false;

		public override string ShaderName => "Hidden/PostProcessing/Outline";

		public override PostProcessingPassEvent PassEvent => PostProcessingPassEvent.AfterPostProcessing;

		public override void ApplyProperties(Material material, RenderingData renderingData)
		{
			material.SetFloat("_Range", range.value);
			material.SetFloat("_DepthThreshold", depthThreshold.value);
			material.SetFloat("_NormalThreshold", normalThreshold.value);
			material.SetColor("_LineColor", lineColor.value);
			material.SetFloat("_Distortion", distortion.value);
		}
	}
}
