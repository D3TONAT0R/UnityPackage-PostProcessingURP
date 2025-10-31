using System;
using System.Collections.Generic;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;

namespace UnityEngine.Rendering.Universal.PostProcessing
{
	/// <summary>
	/// Blurs the image. 
	/// </summary>
	[VolumeComponentMenu("Post-processing/Gaussian Blur")]
	public class GaussianBlur : CustomPostProcessVolumeComponent
	{
		public enum Mode
		{
			StandardGaussian,
			SgxGaussian
		}

		public enum Pass
		{
			Downsample = 0,
			BlurVertical = 1,
			BlurHorizontal = 2,
			BlurVerticalSGX = 3,
			BlurHorizontalSGX = 4,
			FinalBlit = 5
		}

		[Serializable]
		public sealed class BlurModeParameter : VolumeParameter<Mode>
		{

		}

		public BlurModeParameter mode = new BlurModeParameter();
		public IntParameter downsample = new ClampedIntParameter(1, 0, 8);
		public IntParameter blurIterations = new ClampedIntParameter(1, 1, 16);
		public FloatParameter blurSize = new FloatParameter(3f);

		private RenderTextureDescriptor tempDescriptor;
		private RTHandle tempRT_A;
		private RTHandle tempRT_B;

		public override string ShaderName => "Hidden/PostProcessing/GaussianBlur";

		public override PostProcessingPassEvent PassEvent => PostProcessingPassEvent.AfterPostProcessing;

		public override bool VisibleInSceneView => false;

		protected override void OnEnable()
		{
			base.OnEnable();
			tempDescriptor = new RenderTextureDescriptor(Screen.width, Screen.height, RenderTextureFormat.ARGB32, 0, 0);
		}

		public override void Setup(CustomPostProcessPass pass, RenderingData renderingData, List<int> passes)
		{
			var targetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
			tempDescriptor.colorFormat = targetDescriptor.colorFormat;
			int ds = downsample.value;
			tempDescriptor.width = targetDescriptor.width >> ds;
			tempDescriptor.height = targetDescriptor.height >> ds;
			RenderingUtils.ReAllocateIfNeeded(ref tempRT_A, tempDescriptor, name: "Temp_Downsample_A");
			RenderingUtils.ReAllocateIfNeeded(ref tempRT_B, tempDescriptor, name: "Temp_Downsample_B");
			base.Setup(pass, renderingData, passes);
		}

		public override void Render(RenderGraphModule.RenderGraph renderGraph, UniversalResourceData frameData, ContextContainer context)
		{
			if(!BeginRender(context)) return;

			int iterations = blurIterations.value;
			float size = blurSize.value;

			var cameraDescriptor = frameData.activeColorTexture.GetDescriptor(renderGraph);

			int down = downsample.value;
			var downsampledDesc = cameraDescriptor;
			downsampledDesc.width >>= down;
			downsampledDesc.height >>= down;

			var downsampled = renderGraph.CreateTexture(downsampledDesc);
			float widthMod = 1.0f / (1.0f * (1 << down));

			//Downsample
			using(var builder = renderGraph.AddRasterRenderPass<PassData>("Gaussian Blur", out var data))
			{
				data.source = frameData.activeColorTexture;
				data.material = blitMaterial;
				data.passIndex = (int)Pass.Downsample;
				builder.SetRenderAttachment(downsampled, 0);
				builder.AllowGlobalStateModification(true);
				builder.UseTexture(data.source);
				builder.SetRenderFunc<PassData>((d, ctx) =>
				{
					ctx.cmd.SetGlobalTexture("_SourceTexture", d.source);
					Blitter.BlitTexture(ctx.cmd, d.source, new Vector4(1, 1, 0, 0), d.material, d.passIndex);
				});
				frameData.cameraColor = downsampled;
			}

			int horizontalPass = (int)(mode.value == Mode.SgxGaussian ? Pass.BlurHorizontalSGX : Pass.BlurHorizontal);
			int verticalPass = (int)(mode.value == Mode.SgxGaussian ? Pass.BlurVerticalSGX : Pass.BlurVertical);

			for(int i = 0; i < iterations; i++)
			{
				float iterationOffs = i * 1.0f;
				using(var builder = renderGraph.AddUnsafePass<PassData>("Set Parameters", out var d))
				{
					builder.AllowGlobalStateModification(true);
					builder.SetRenderFunc<PassData>((_, ctx) =>
					{
						var parameters = new Vector4(size * widthMod + iterationOffs, -size * widthMod - iterationOffs, blend.value, 0.0f);
						var cmd = ctx.cmd;
						cmd.SetGlobalVector("_Parameter", parameters);
					});
				}

				// Vertical blur
				Blit(renderGraph, frameData, verticalPass);

				// Horizontal blur
				Blit(renderGraph, frameData, horizontalPass);
			}

			//TODO: blit resolution does not match screen when downsampled
			Blit(renderGraph, frameData, (int)Pass.FinalBlit);
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			if(tempRT_A != null) tempRT_A.Release();
			if(tempRT_B != null) tempRT_B.Release();
		}
	}
}