using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

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

		public override PostProcessingPassEvent PassEvent => PostProcessingPassEvent.AfterRendering;

		public override void AddPasses(List<int> passes)
		{
			passes.Add(1);
		}

		public override void ApplyProperties(Material material, CustomPostProcessRenderContext context)
		{
			material.SetFloat("_Frequency", frequency.value);
			material.SetFloat("_Levels", levels.value);
			material.SetInt("_BlockSize", blockSize.value);
			material.SetFloat("_DCTGamma", compressionGamma.value);
		}

		public override void Render(CustomPostProcessRenderContext context, TextureHandle from, TextureHandle to, int passIndex)
		{
			var dctDescriptor = context.cameraData.cameraTargetDescriptor;
			dctDescriptor.depthBufferBits = 0;
			dctDescriptor.colorFormat = RenderTextureFormat.DefaultHDR;
			var dctHandle = context.renderGraph.CreateTexture(new TextureDesc(dctDescriptor));
			context.renderGraph.AddBlitPass(new RenderGraphUtils.BlitMaterialParameters(from, dctHandle, blitMaterial, 0));
			//Feature.Blit(Cmd, From, dctHandle, blitMaterial, 0);
			//Cmd.SetGlobalTexture("_DCTTexture", dctHandle);
			//Feature.Blit(Cmd, From, To, blitMaterial, 1);
		}
	}
}
