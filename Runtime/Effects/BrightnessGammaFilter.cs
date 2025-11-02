using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEngine.Rendering.Universal.PostProcessing
{
	[VolumeComponentMenu("Post-processing/Brightness & Gamma Filter")]
	public class BrightnessGammaFilter : CustomPostProcessVolumeComponent
	{
		public static float GlobalBrightnessPreference { get; set; } = 1f;
		public static float GlobalGammaPreference { get; set; } = 1f;

		public BoolParameter useGlobalPreferenceValues = new BoolParameter(true, false);
		public ClampedFloatParameter brightness = new ClampedFloatParameter(1f, -1, 1, false);
		public ClampedFloatParameter gamma = new ClampedFloatParameter(1f, 0.01f, 2f, false);

		public override string ShaderName => "Hidden/PostProcessing/BrightnessGamma";

		public override PostProcessingPassEvent InjectionPoint => PostProcessingPassEvent.AfterPostProcessing;

		public override void SetMaterialProperties(Material material)
		{
			base.SetMaterialProperties(material);
			float brightnessValue;
			float gammaValue;
			if(useGlobalPreferenceValues.value)
			{
				brightnessValue = GlobalBrightnessPreference;
				gammaValue = GlobalGammaPreference;
			}
			else
			{
				brightnessValue = brightness.value;
				gammaValue = gamma.value;
			}
			material.SetFloat("_Brightness", brightnessValue);
			material.SetFloat("_Gamma", gammaValue);
		}
	}
}
