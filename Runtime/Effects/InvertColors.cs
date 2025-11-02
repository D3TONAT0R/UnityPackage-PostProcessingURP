
namespace UnityEngine.Rendering.Universal.PostProcessing
{
	[VolumeComponentMenu("Post-processing/Invert Colors")]
	public class InvertColors : CustomPostProcessVolumeComponent
	{
		public override string ShaderName => "Hidden/PostProcessing/InvertColors";

		public override PostProcessingPassEvent InjectionPoint => PostProcessingPassEvent.AfterPostProcessing;

		protected override void RenderEffect(CustomPostProcessPass pass, RenderGraphModule.RenderGraph renderGraph, UniversalResourceData frameData, ContextContainer context)
		{
			base.RenderEffect(pass, renderGraph, frameData, context);
		}
	}
}
