using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace UnityEngine.Rendering.Universal.PostProcessing
{
	[VolumeComponentMenu("Post-processing/Outline")]
	public class Outline : CustomPostProcessVolumeComponent
	{
		public IntParameter lineWidth = new IntParameter(1, false);
		public FloatParameter range = new FloatParameter(25, false);
		public ClampedFloatParameter rangeFadeStart = new ClampedFloatParameter(0.5f, 0, 0.999f, false);
		public FloatParameter depthThreshold = new ClampedFloatParameter(0.01f, 0, 0.1f, false);
		public FloatParameter normalThreshold = new ClampedFloatParameter(0.2f, 0, 1f, false);
		public FloatParameter colorThreshold = new ClampedFloatParameter(0.2f, 0, 1f, false);
		public ColorParameter backgroundColor = new ColorParameter(Color.clear, false);
		public ColorParameter lineColor = new ColorParameter(Color.black, false);

		public ClampedFloatParameter distortion = new ClampedFloatParameter(0.005f, 0, 0.02f, false);

		private RenderTextureDescriptor edgeDetectionDescriptor;
		private RTHandle edgeDetectionTarget;

		public override bool IgnorePostProcessingFlag => false;

		public override string ShaderName => "Hidden/PostProcessing/Outline";

		public override PostProcessingPassEvent PassEvent => PostProcessingPassEvent.AfterPostProcessing;

		public override ScriptableRenderPassInput Requirements => ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal;

		protected override void OnEnable()
		{
			base.OnEnable();
			edgeDetectionDescriptor = new RenderTextureDescriptor(Screen.width, Screen.height, RenderTextureFormat.ARGB32, 0, 0);
		}

		public override void Setup(CustomPostProcessRenderContext context, List<int> passes)
		{
			base.Setup(pass, renderingData, passes);
			var targetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
			edgeDetectionDescriptor.width = targetDescriptor.width;
			edgeDetectionDescriptor.height = targetDescriptor.height;
			RenderingUtils.ReAllocateIfNeeded(ref edgeDetectionTarget, edgeDetectionDescriptor, name: "Temp_EdgeDetection");
		}

		public override void ApplyProperties(Material material, CustomPostProcessRenderContext context)
		{
			material.SetFloat("_Range", range.value);
			material.SetFloat("_RangeFadeStart", rangeFadeStart.value);
			material.SetFloat("_DepthThreshold", depthThreshold.value);
			material.SetFloat("_NormalThreshold", normalThreshold.value);
			material.SetFloat("_ColorThreshold", colorThreshold.value);
			material.SetColor("_BackgroundColor", backgroundColor.value);
			material.SetColor("_LineColor", lineColor.value);
			material.SetFloat("_Distortion", distortion.value);
			material.SetInt("_LineWidth", Mathf.Clamp(lineWidth.value, 1, 32));
		}

		public override void Render(CustomPostProcessRenderContext context, TextureHandle from, TextureHandle to, int passIndex)
		{
			inClassName.Feature.Blit(inClassName.Cmd, inClassName.From, edgeDetectionTarget, blitMaterial, 0);
			blitMaterial.SetTexture("_EdgeDetectionTexture", edgeDetectionTarget);
			base.Render(new InClassName(inClassName.Feature, inClassName.RenderingData, inClassName.Cmd, inClassName.From, inClassName.To, 1));
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			edgeDetectionTarget?.Release();
		}
	}
}
