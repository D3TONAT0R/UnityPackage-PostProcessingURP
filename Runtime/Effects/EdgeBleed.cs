using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEngine.Rendering.Universal.PostProcessing
{
	[VolumeComponentMenu("Post-processing/Edge Bleed")]
	public class EdgeBleed : CustomPostProcessVolumeComponent
	{
		public ClampedFloatParameter horizontalBleed = new ClampedFloatParameter(0f, 0, 0.03f, false);
		public ClampedFloatParameter verticalBleed = new ClampedFloatParameter(0f, 0, 0.03f, false);

		public override string ShaderName => "Hidden/PostProcessing/BoxBlur";

		public override PostProcessingPassEvent PassEvent => PostProcessingPassEvent.AfterPostProcessing;

		public override bool IsActive()
		{
			return base.IsActive() && (horizontalBleed.value > 0 || verticalBleed.value > 0);
		}

		public override void AddPasses(List<int> passes)
		{
			if(verticalBleed.value > 0) passes.Add(0);
			if(horizontalBleed.value > 0) passes.Add(1);
		}

		public override void SetMaterialProperties(Material material)
		{
			material.SetFloat("_Blend", -blend.value);
			material.SetFloat("_Intensity", blend.value);
			material.SetFloat("_HorizontalBlur", horizontalBleed.value);
			material.SetFloat("_VerticalBlur", verticalBleed.value);
		}

		public override void Render(RenderGraphModule.RenderGraph renderGraph, UniversalResourceData frameData, ContextContainer context)
		{
			if(!BeginRender(context)) return;
			Blit(renderGraph, frameData, 0);
			Blit(renderGraph, frameData, 1);
		}
	}
}
