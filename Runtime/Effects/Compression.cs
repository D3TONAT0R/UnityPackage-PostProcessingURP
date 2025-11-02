using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal.PostProcessing
{
	[VolumeComponentMenu("Post-processing/Compression")]
	public class Compression : CustomPostProcessVolumeComponent
	{
		public IntParameter frequency = new ClampedIntParameter(8, 2, 16);
		public IntParameter levels = new IntParameter(10);
		public IntParameter blockSize = new ClampedIntParameter(8, 2, 32);
		public FloatParameter compressionGamma = new ClampedFloatParameter(1f, 0.01f, 5f);

		public override string ShaderName => "Hidden/PostProcessing/Compression";

		public override PostProcessingPassEvent InjectionPoint => PostProcessingPassEvent.AfterRendering;

		public override bool VisibleInSceneView => false;

		public override void SetMaterialProperties(Material material)
		{
			material.SetFloat("_Frequency", frequency.value);
			material.SetFloat("_Levels", levels.value);
			material.SetInt("_BlockSize", blockSize.value);
			material.SetFloat("_DCTGamma", compressionGamma.value);
		}

		protected override void RenderEffect(CustomPostProcessPass pass, RenderGraphModule.RenderGraph renderGraph,
			UniversalResourceData frameData, ContextContainer context)
		{
			using(var builder = renderGraph.AddRasterRenderPass<PassData>("DCT Pass", out var passData))
			{
				passData.source = frameData.activeColorTexture;
				passData.material = blitMaterial;
				passData.passIndex = 0;

				builder.UseTexture(passData.source);

				TextureDesc desc = frameData.activeColorTexture.GetDescriptor(renderGraph);
				desc.depthBufferBits = 0;
				desc.name = "DCTTexture";
				TextureHandle dct = renderGraph.CreateTexture(desc);

				builder.SetRenderAttachment(dct, 0);

				builder.AllowPassCulling(false);
				builder.AllowGlobalStateModification(true);
				builder.SetRenderFunc<PassData>(ExecuteRasterRenderPass);
				builder.SetGlobalTextureAfterPass(dct, Shader.PropertyToID("_DCTTexture"));
			}
			Blit(renderGraph, frameData, 1);
		}
	}
}
