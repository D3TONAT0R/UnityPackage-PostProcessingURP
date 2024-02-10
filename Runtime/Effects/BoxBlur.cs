using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEngine.Rendering.Universal.PostProcessing
{
	[VolumeComponentMenu("Post-processing/Blur")]
	public class BoxBlur : CustomPostProcessVolumeComponent
	{
		public PassEventParameter injectionPoint = new PassEventParameter(PostProcessingPassEvent.AfterPostProcessing, false);
		public ClampedFloatParameter horizontalBlur = new ClampedFloatParameter(0f, 0, 0.1f, false);
		public ClampedFloatParameter verticalBlur = new ClampedFloatParameter(0f, 0, 0.1f, false);

		public override string ShaderName => "Hidden/PostProcessing/BoxBlur";

		public override bool IsActive()
		{
			return base.IsActive() && (horizontalBlur.value > 0 || verticalBlur.value > 0);
		}

		public override void AddPasses(List<int> passes)
		{
			if(verticalBlur.value > 0) passes.Add(0);
			if(horizontalBlur.value > 0) passes.Add(1);
		}

		public override PostProcessingPassEvent PassEvent => injectionPoint.value;

		public override void ApplyProperties(Material material, RenderingData renderingData)
		{
			material.SetFloat("_HorizontalBlur", horizontalBlur.value);
			material.SetFloat("_VerticalBlur", verticalBlur.value);
		}
	}
}
