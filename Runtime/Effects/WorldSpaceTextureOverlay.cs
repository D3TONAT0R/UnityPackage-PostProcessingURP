using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEngine.Rendering.Universal.PostProcessing
{
	[VolumeComponentMenu("Post-processing/World Space Texture Overlay")]
	public class WorldSpaceTextureOverlay : CustomPostProcessVolumeComponent
	{
		public FloatParameter gridScale = new FloatParameter(1f, false);
		public Texture2DParameter gridTexture = new Texture2DParameter(null, false);
		public FloatParameter range = new FloatParameter(10, false);

		public override string ShaderName => "Hidden/PostProcessing/WorldSpaceTextureOverlay";

		public override PostProcessingPassEvent PassEvent => PostProcessingPassEvent.BeforeTransparents;

		public override void ApplyProperties(Material material, RenderingData renderingData)
		{
			material.SetFloat("_GridScale", gridScale.value);
			material.SetTexture("_GridTex", gridTexture.value);
			material.SetFloat("_Range", range.value);
		}
	}
}
