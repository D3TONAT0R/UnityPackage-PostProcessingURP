using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEngine.Rendering.Universal.PostProcessing
{
	[VolumeComponentMenu("Post-processing/Invert Colors")]
	public class InvertColors : CustomPostProcessVolumeComponent
	{
		public override string ShaderName => "Hidden/PostProcessing/InvertColors";

		public override PostProcessingPassEvent InjectionPoint => PostProcessingPassEvent.AfterPostProcessing;
	}
}
