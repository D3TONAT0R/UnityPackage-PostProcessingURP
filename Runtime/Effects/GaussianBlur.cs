using System;
using System.Collections.Generic;

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
			if(blend.value <= 0.0f) return;
			//TODO
			/*
			cmd.BeginSample("BlurPostEffect");

			int ds = downsample.value;

			float widthMod = 1.0f / (1.0f * (1 << ds));

			float _blend = Mathf.Min(blend.value, 1);
			int iterations = blurIterations.value;
			float size = blurSize.value;

			cmd.SetGlobalTexture("_SourceTexture", source);

			feature.Blit(cmd, source, tempRT_A, blitMaterial, (int)Pass.Downsample);

			int horizontalPass = (int)(mode.value == Mode.SgxGaussian ? Pass.BlurHorizontalSGX : Pass.BlurHorizontal);
			int verticalPass = (int)(mode.value == Mode.SgxGaussian ? Pass.BlurVerticalSGX : Pass.BlurVertical);

			for(int i = 0; i < iterations; i++)
			{
				float iterationOffs = i * 1.0f;
				var parameters = new Vector4(size * widthMod + iterationOffs, -size * widthMod - iterationOffs, _blend, 0.0f);
				cmd.SetGlobalVector("_Parameter", parameters);

				// Vertical blur
				feature.Blit(cmd, tempRT_A, tempRT_B, blitMaterial, verticalPass);

				// Horizontal blur
				feature.Blit(cmd, tempRT_B, tempRT_A, blitMaterial, horizontalPass);
			}

			feature.Blit(cmd, tempRT_A, destination, blitMaterial, (int)Pass.FinalBlit);

			cmd.SetGlobalVector("_Parameter", Vector4.zero);

			cmd.EndSample("BlurPostEffect");
			*/
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			if(tempRT_A != null) tempRT_A.Release();
			if(tempRT_B != null) tempRT_B.Release();
		}
	}
}