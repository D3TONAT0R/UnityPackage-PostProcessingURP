using System;
using System.Collections.Generic;
using UnityEngine.Rendering.RenderGraphModule;

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

		public override void Setup(CustomPostProcessPass feature, RenderGraph graph, TextureHandle from, TextureHandle to, int passIndex)
		{
			var targetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
			tempDescriptor.colorFormat = targetDescriptor.colorFormat;
			int ds = downsample.value;
			tempDescriptor.width = targetDescriptor.width >> ds;
			tempDescriptor.height = targetDescriptor.height >> ds;
			RenderingUtils.ReAllocateIfNeeded(ref tempRT_A, tempDescriptor, name: "Temp_Downsample_A");
			RenderingUtils.ReAllocateIfNeeded(ref tempRT_B, tempDescriptor, name: "Temp_Downsample_B");
			base.Setup(feature, graph, from, to, passIndex);
		}

		public override void ApplyProperties(Material material, CustomPostProcessRenderContext context)
		{

		}

		public override void Render(CustomPostProcessRenderContext context, TextureHandle from, TextureHandle to, int passIndex)
		{
			inClassName.Cmd.BeginSample("BlurPostEffect");

			int ds = downsample.value;

			float widthMod = 1.0f / (1.0f * (1 << ds));

			float _blend = Mathf.Min(blend.value, 1);
			int iterations = blurIterations.value;
			float size = blurSize.value;

			inClassName.Cmd.SetGlobalTexture("_SourceTexture", inClassName.From);

			inClassName.Feature.Blit(inClassName.Cmd, inClassName.From, tempRT_A, blitMaterial, (int)Pass.Downsample);

			int horizontalPass = (int)(mode.value == Mode.SgxGaussian ? Pass.BlurHorizontalSGX : Pass.BlurHorizontal);
			int verticalPass = (int)(mode.value == Mode.SgxGaussian ? Pass.BlurVerticalSGX : Pass.BlurVertical);

			for(int i = 0; i < iterations; i++)
			{
				float iterationOffs = i * 1.0f;
				var parameters = new Vector4(size * widthMod + iterationOffs, -size * widthMod - iterationOffs, _blend, 0.0f);
				inClassName.Cmd.SetGlobalVector("_Parameter", parameters);

				// Vertical blur
				inClassName.Feature.Blit(inClassName.Cmd, tempRT_A, tempRT_B, blitMaterial, verticalPass);

				// Horizontal blur
				inClassName.Feature.Blit(inClassName.Cmd, tempRT_B, tempRT_A, blitMaterial, horizontalPass);
			}

			inClassName.Feature.Blit(inClassName.Cmd, tempRT_A, inClassName.To, blitMaterial, (int)Pass.FinalBlit);

			inClassName.Cmd.SetGlobalVector("_Parameter", Vector4.zero);

			inClassName.Cmd.EndSample("BlurPostEffect");
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			if(tempRT_A != null) tempRT_A.Release();
			if(tempRT_B != null) tempRT_B.Release();
		}
	}
}