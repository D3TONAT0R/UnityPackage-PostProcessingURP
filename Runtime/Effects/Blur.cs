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
		}

		[Serializable]
		public sealed class BlurModeParameter : VolumeParameter<Mode>
		{

		}

		public BlurModeParameter mode = new BlurModeParameter();
		public IntParameter downsample = new IntParameter(1);
		public IntParameter blurIterations = new IntParameter(1);
		public FloatParameter blurSize = new FloatParameter(3f);

		private RenderTextureDescriptor tempDescriptor;
		private RTHandle tempRT;

		public override string ShaderName => "Hidden/PostProcessing/Blur";

		public override PostProcessingPassEvent PassEvent => PostProcessingPassEvent.AfterPostProcessing;

		public override void Setup(CustomPostProcessPass pass, RenderingData renderingData, List<int> passes)
		{
			base.Setup(pass, renderingData, passes);
			tempDescriptor = renderingData.cameraData.cameraTargetDescriptor;
			int ds = downsample.value;
			tempDescriptor.width >>= ds;
			tempDescriptor.height >>= ds;
			RenderingUtils.ReAllocateIfNeeded(ref tempRT, tempDescriptor);
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

			feature.Blit(cmd, source, tempRT, blitMaterial, (int)Pass.Downsample);

			RTHandle rtA = tempRT;
			RTHandle rtB = destination;

			int pass = mode.value == Mode.SgxGaussian ? 2 : 0;

			for(int i = 0; i < iterations; i++)
			{
				float iterationOffs = i * 1.0f;
				cmd.SetGlobalVector("_Parameter", new Vector4(size * widthMod + iterationOffs, -size * widthMod - iterationOffs, _blend, 0.0f));

				// Vertical blur..
				//int rtId2 = Shader.PropertyToID("_BlurPostProcessEffect" + rtIndex++);
				//cmd.GetTemporaryRT(rtId2, rtW, rtH, 0, FilterMode.Bilinear);
				feature.Blit(cmd, rtA, rtB, blitMaterial, (int)Pass.BlurVertical + pass);
				//cmd.ReleaseTemporaryRT(blurId);
				//blurId = rtId2;

				// Horizontal blur..
				//rtId2 = Shader.PropertyToID("_BlurPostProcessEffect" + rtIndex++);
				//cmd.GetTemporaryRT(rtId2, rtW, rtH, 0, FilterMode.Bilinear);
				feature.Blit(cmd, rtB, rtA, blitMaterial, (int)Pass.BlurHorizontal + pass);
				//cmd.BlitFullscreenTriangle(blurId, rtId2, sheet, (int)Pass.BlurHorizontal + pass);
				//cmd.ReleaseTemporaryRT(blurId);
				//blurId = rtId2;
			}

			feature.Blit(cmd, rtA, destination);
			//cmd.Blit(blurId, context.destination);
			//cmd.ReleaseTemporaryRT(blurId);

			cmd.SetGlobalVector("_Parameter", Vector4.zero);

			cmd.EndSample("BlurPostEffect");
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			if(tempRT != null) tempRT.Release();
		}
	}
}