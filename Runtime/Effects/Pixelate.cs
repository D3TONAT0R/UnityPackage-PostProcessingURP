using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEngine.Rendering.Universal.PostProcessing
{
	[VolumeComponentMenu("Post-processing/Pixelate")]
	public class Pixelate : CustomPostProcessVolumeComponent
	{
		public IntParameter verticalResolution = new IntParameter(1080, false);

		public override string ShaderName => "Hidden/PostProcessing/Pixelate";

		public override PostProcessingPassEvent PassEvent => PostProcessingPassEvent.AfterPostProcessing;

		public override void ApplyProperties(Material material, RenderingData renderingData)
		{
			material.SetInt("_VertResolution", verticalResolution.value);
		}
	}
}
