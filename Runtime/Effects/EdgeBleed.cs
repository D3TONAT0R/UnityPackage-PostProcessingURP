namespace UnityEngine.Rendering.Universal.PostProcessing
{
	[VolumeComponentMenu("Post-processing/Edge Bleed")]
	public class EdgeBleed : CustomPostProcessVolumeComponent
	{
		public ClampedFloatParameter horizontalBleed = new ClampedFloatParameter(0f, 0, 0.03f, false);
		public ClampedFloatParameter verticalBleed = new ClampedFloatParameter(0f, 0, 0.03f, false);

		public override string ShaderName => "Hidden/PostProcessing/BoxBlur";

		public override PostProcessingPassEvent InjectionPoint => PostProcessingPassEvent.AfterPostProcessing;

		public override bool IsActive()
		{
			return base.IsActive() && (horizontalBleed.value > 0 || verticalBleed.value > 0);
		}

		public override void SetMaterialProperties(Material material)
		{
			material.SetFloat("_Blend", -blend.value);
			material.SetFloat("_Intensity", blend.value);
			material.SetFloat("_HorizontalBlur", horizontalBleed.value);
			material.SetFloat("_VerticalBlur", verticalBleed.value);
		}

		protected override void RenderEffect(CustomPostProcessPass pass, RenderGraphModule.RenderGraph renderGraph,
			UniversalResourceData frameData, ContextContainer context)
		{
			Blit(renderGraph, frameData, 0);
			Blit(renderGraph, frameData, 1);
		}
	}
}
