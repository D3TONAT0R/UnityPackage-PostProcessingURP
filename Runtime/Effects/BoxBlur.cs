namespace UnityEngine.Rendering.Universal.PostProcessing
{
	[VolumeComponentMenu("Post-processing/Box Blur")]
	public class BoxBlur : CustomPostProcessVolumeComponent
	{
		public PassEventParameter injectionPoint = new PassEventParameter(PostProcessingPassEvent.AfterPostProcessing);
		public ClampedFloatParameter horizontalBlur = new ClampedFloatParameter(0f, 0, 0.1f);
		public ClampedFloatParameter verticalBlur = new ClampedFloatParameter(0f, 0, 0.1f);

		public override string ShaderName => "Hidden/PostProcessing/BoxBlur";

		public override bool IsActive()
		{
			return base.IsActive() && (horizontalBlur.value > 0 || verticalBlur.value > 0);
		}

		public override PostProcessingPassEvent InjectionPoint => injectionPoint.value;

		public override void SetMaterialProperties(Material material)
		{
			material.SetFloat("_HorizontalBlur", horizontalBlur.value);
			material.SetFloat("_VerticalBlur", verticalBlur.value);
		}

		protected override void RenderEffect(CustomPostProcessPass pass, RenderGraphModule.RenderGraph renderGraph,
			UniversalResourceData frameData, ContextContainer context)
		{
			Blit(renderGraph, frameData, 0);
			Blit(renderGraph, frameData, 1);
		}
	}
}
