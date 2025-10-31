using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
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

		private RenderTextureDescriptor dctDescriptor;
		private RTHandle dctHandle;
		private RTHandle quantizationHandle;

		public override string ShaderName => "Hidden/PostProcessing/Compression";

		public override PostProcessingPassEvent PassEvent => PostProcessingPassEvent.AfterPostProcessing;

		public override bool VisibleInSceneView => false;

		public override void AddPasses(List<int> passes)
		{
			passes.Add(1);
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			dctDescriptor = new RenderTextureDescriptor(Screen.width, Screen.height, RenderTextureFormat.ARGB32, 0, 0);
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			if(dctHandle != null) dctHandle.Release();
			if(quantizationHandle != null) quantizationHandle.Release();
		}

		public override void Setup(CustomPostProcessPass pass, RenderingData renderingData, List<int> passes)
		{
			base.Setup(pass, renderingData, passes);
			dctDescriptor.width = renderingData.cameraData.cameraTargetDescriptor.width;
			dctDescriptor.height = renderingData.cameraData.cameraTargetDescriptor.height;
			dctDescriptor.colorFormat = RenderTextureFormat.DefaultHDR;
			RenderingUtils.ReAllocateIfNeeded(ref dctHandle, dctDescriptor, name: "Temp_DCT");
			RenderingUtils.ReAllocateIfNeeded(ref quantizationHandle, dctDescriptor, name: "Temp_Quantization");
		}

		public override void SetMaterialProperties(Material material)
		{
			base.SetMaterialProperties(material);
			material.SetFloat("_Frequency", frequency.value);
			material.SetFloat("_Levels", levels.value);
			material.SetInt("_BlockSize", blockSize.value);
			material.SetFloat("_DCTGamma", compressionGamma.value);
		}

		class DCTData : ContextItem
		{
			public TextureHandle dctTexture;

			public override void Reset()
			{
				dctTexture = TextureHandle.nullHandle;
			}
		}

		class CompressionPassData : PassData
		{
			public TextureHandle dctTexture;
		}

		public override void Render(RenderGraphModule.RenderGraph renderGraph, UniversalResourceData frameData, ContextContainer context)
		{
			if(!BeginRender(context)) return;
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
				var dctData = context.Create<DCTData>();
				dctData.dctTexture = dct;

				builder.AllowPassCulling(false);
				builder.AllowGlobalStateModification(true);
				builder.SetRenderFunc<PassData>(ExecuteRasterRenderPass);
				builder.SetGlobalTextureAfterPass(dct, Shader.PropertyToID("_DCTTexture"));
			}
			Blit(renderGraph, frameData, 1);
			/*
			using(var builder = renderGraph.AddRasterRenderPass<CompressionPassData>("Compression", out var passData))
			{
				passData.source = frameData.activeColorTexture;
				passData.material = blitMaterial;
				passData.passIndex = 1;

				builder.UseTexture(passData.source);

				TextureDesc desc = frameData.activeColorTexture.GetDescriptor(renderGraph);
				desc.depthBufferBits = 0;
				desc.name = "Output";
				TextureHandle destination = renderGraph.CreateTexture(desc);

				builder.SetRenderAttachment(destination, 0);

				builder.AllowPassCulling(false);
				builder.SetRenderFunc<CompressionPassData>((data, ctx) =>
				{
					//blitMaterial.SetTexture("_DCTTexture", renderGraph.ImportTexture(context.Get<DCTData>().dctTexture));
					//blitMaterial.SetTexture("_DCTTexture", renderGraph.ImportTexture(data.dctTexture));
					blitMaterial.SetTexture("_DCTTexture", data.dctTexture);
					Blitter.BlitTexture(ctx.cmd, data.source, new Vector4(1, 1, 0, 0), data.material, data.passIndex);
				});

				frameData.cameraColor = destination;
			}
			//TODO
			/*
			feature.Blit(cmd, from, dctHandle, blitMaterial, 0);
			cmd.SetGlobalTexture("_DCTTexture", dctHandle);
			feature.Blit(cmd, from, to, blitMaterial, 1);
			*/
		}
	}
}
