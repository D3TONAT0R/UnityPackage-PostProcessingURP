using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering.Universal.PostProcessing
{

	/// <summary>
	/// Blurs the image. 
	/// </summary>
	[VolumeComponentMenu("Post-processing/Blur")]
	public class Blur : CustomPostProcessVolumeComponent
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

		public override string ShaderName => "Hidden/PostProcessing/Blur";

		public override PostProcessingPassEvent PassEvent => PostProcessingPassEvent.AfterPostProcessing;

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

		public override void ApplyProperties(Material material, RenderingData renderingData)
		{

		}

		public override void Render(CustomPostProcessPass feature, RenderingData renderingData, CommandBuffer cmd, RTHandle source, RTHandle destination, int passIndex)
		{
			cmd.BeginSample("BlurPostEffect");

			int ds = downsample.value;

			float widthMod = 1.0f / (1.0f * (1 << ds));

			float _blend = Mathf.Min(blend.value, 1);
			int iterations = blurIterations.value;
			float size = blurSize.value;

			blitMaterial.SetVector("_Parameter", new Vector4(size * widthMod, -size * widthMod, _blend, 0.0f));

			//int blurId = Shader.PropertyToID("_BlurPostProcessEffect");
			//cmd.GetTemporaryRT(blurId, rtW, rtH, 0, FilterMode.Bilinear);

			cmd.SetGlobalTexture("_SourceTexture", source);

			feature.Blit(cmd, source, tempRT_A, blitMaterial, (int)Pass.Downsample);

			int horizontalPass = (int)(mode.value == Mode.SgxGaussian ? Pass.BlurHorizontalSGX : Pass.BlurHorizontal);
			int verticalPass = (int)(mode.value == Mode.SgxGaussian ? Pass.BlurVerticalSGX : Pass.BlurVertical);

			for(int i = 0; i < iterations; i++)
			{
				float iterationOffs = i * 1.0f;
				cmd.SetGlobalVector("_Parameter", new Vector4(size * widthMod + iterationOffs, -size * widthMod - iterationOffs, _blend, 0.0f));

				// Vertical blur..
				//int rtId2 = Shader.PropertyToID("_BlurPostProcessEffect" + rtIndex++);
				//cmd.GetTemporaryRT(rtId2, rtW, rtH, 0, FilterMode.Bilinear);
				feature.Blit(cmd, tempRT_A, tempRT_B, blitMaterial, horizontalPass);
				//cmd.ReleaseTemporaryRT(blurId);
				//blurId = rtId2;

				// Horizontal blur..
				//rtId2 = Shader.PropertyToID("_BlurPostProcessEffect" + rtIndex++);
				//cmd.GetTemporaryRT(rtId2, rtW, rtH, 0, FilterMode.Bilinear);
				feature.Blit(cmd, tempRT_B, tempRT_A, blitMaterial, verticalPass);
				//cmd.BlitFullscreenTriangle(blurId, rtId2, sheet, (int)Pass.BlurHorizontal + pass);
				//cmd.ReleaseTemporaryRT(blurId);
				//blurId = rtId2;
			}

			feature.Blit(cmd, tempRT_A, destination, blitMaterial, (int)Pass.FinalBlit);
			//cmd.Blit(blurId, context.destination);
			//cmd.ReleaseTemporaryRT(blurId);

			cmd.SetGlobalVector("_Parameter", Vector4.zero);

			cmd.EndSample("BlurPostEffect");
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			if(tempRT_A != null) tempRT_A.Release();
			if(tempRT_B != null) tempRT_B.Release();
		}
	}
}