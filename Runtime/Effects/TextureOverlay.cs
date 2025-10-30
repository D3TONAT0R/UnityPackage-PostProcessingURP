using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEngine.Rendering.Universal.PostProcessing
{
	[VolumeComponentMenu("Post-processing/Texture Overlay")]
	public class TextureOverlay : CustomPostProcessVolumeComponent
	{
		public enum BlendMode
        {
			Normal = 0,
			Multiply = 1,
			Screen = 2,
			Overlay = 3
        }

		[System.Serializable]
		public class BlendModeParameter : VolumeParameter<BlendMode>
        {
			public BlendModeParameter(BlendMode mode, bool overrideState = false) : base(mode, overrideState)
            {

            }
        }

		public PassEventParameter injectionPoint = new PassEventParameter(PostProcessingPassEvent.BeforePostProcessing);
		public BlendModeParameter blendMode = new BlendModeParameter(BlendMode.Normal);
		public TextureParameter texture = new TextureParameter(null);
		public ColorParameter tint = new ColorParameter(Color.white, true, true, true);

		public override string ShaderName => "Hidden/PostProcessing/TextureOverlay";

		public override PostProcessingPassEvent PassEvent => injectionPoint.value;

		public override void SetMaterialProperties(Material material)
		{
			material.SetInt("_BlendMode", (int)blendMode.value);
			material.SetTexture("_OverlayTexture", texture.value ? texture.value : Texture2D.whiteTexture);
			material.SetColor("_Tint", tint.value);
		}
	}
}
