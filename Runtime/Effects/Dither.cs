namespace UnityEngine.Rendering.Universal.PostProcessing
{
	[VolumeComponentMenu("Post-processing/Dither")]
	public class Dither : CustomPostProcessVolumeComponent
	{
		public TextureParameter ditherTexture = new TextureParameter(null);
		public MinIntParameter ditherScale = new MinIntParameter(1, 1);
		public MinIntParameter colorBitDepth = new MinIntParameter(8, 1);
		public MinIntParameter verticalResolutionLimit = new MinIntParameter(1080, 1);
		public ClampedFloatParameter gammaCorrection = new ClampedFloatParameter(1f, 0, 1);

		public override string ShaderName => "Hidden/PostProcessing/Dither";

		public override PostProcessingPassEvent InjectionPoint => PostProcessingPassEvent.AfterPostProcessing;

		private static Texture2D DefaultDitherTexture
		{
			get
			{
				if(!defaultDitherTexture) defaultDitherTexture = GenerateDefaultDitherTexture();
				return defaultDitherTexture;
			}
		}
		private static Texture2D defaultDitherTexture;

		public override void SetMaterialProperties(Material material)
		{
			base.SetMaterialProperties(material);
			var texture = ditherTexture.value;
			if(!texture) texture = DefaultDitherTexture;
			material.SetTexture("_DitherTex", texture);
			material.SetFloat("_DitherTexSize", texture.width);
			int downScale = ditherScale.value;
			while(Screen.height / downScale > verticalResolutionLimit.value && downScale < 16)
			{
				downScale++;
			}
			material.SetFloat("_DownScale", downScale);
			material.SetInt("_ColorBitDepth", colorBitDepth.value);
			material.SetFloat("_GammaCorrection", gammaCorrection.value);
		}

		private static Texture2D GenerateDefaultDitherTexture()
		{
			var texture = new Texture2D(4, 4, TextureFormat.ARGB32, false, true);
			texture.SetPixels32(GetColors(new byte[] {
				0xB0, 0x70, 0x90, 0x50,
				0x30, 0xF0, 0x10, 0xD0,
				0x80, 0x40, 0xA0, 0x60,
				0x00, 0xC0, 0x20, 0xE0
			}));
			texture.Apply();
			texture.filterMode = FilterMode.Point;
			texture.wrapMode = TextureWrapMode.Repeat;
			return texture;
		}

		private static Color32[] GetColors(byte[] pixels)
		{
			Color32[] colors = new Color32[pixels.Length];
			for(int i = 0; i < pixels.Length; i++)
			{
				byte b = pixels[i];
				colors[i] = new Color32(b, b, b, b);
			}
			return colors;
		}
	}
}
