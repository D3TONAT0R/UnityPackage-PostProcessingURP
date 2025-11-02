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

		public override PostProcessingPassEvent InjectionPoint => PostProcessingPassEvent.AfterPostProcessing;

		public override void SetMaterialProperties(Material material)
		{
			base.SetMaterialProperties(material);
			material.SetInt("_VertResolution", verticalResolution.value);
		}
	}
}
