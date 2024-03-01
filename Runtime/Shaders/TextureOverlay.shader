Shader "Hidden/PostProcessing/TextureOverlay"
{
	Properties
	{
		_VertResolution("Vertical Resolution", Int) = 270
		_Blend("Blend", Range(0,1)) = 1.0
	}
	SubShader
	{
		Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

		Pass
		{

			HLSLPROGRAM
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

			#pragma vertex Vert
			#pragma fragment Frag

			float _Blend;
			uint _BlendMode;
			float4 _Tint;

			TEXTURE2D(_OverlayTexture);
			SAMPLER(sampler_OverlayTexture);

			#define BLEND_NORMAL 0
			#define BLEND_MULTIPLY 1
			#define BLEND_ADDITIVE 2
			#define BLEND_OVERLAY 3

			float3 OverlayBlend(float3 base, float3 overlay)
			{
				float3 result;
				//Apply Overlay blend mode formula
				result.rgb = lerp(2.0 * base * overlay, 1.0 - 2.0 * (1.0 - base) * (1.0 - overlay), step(0.5, overlay));
				//Blend alpha values
				//result.a = srcColor.a + dstColor.a * (1.0 - srcColor.a);
				return result;
			}

			float4 Frag(Varyings i) : SV_Target
			{
				float4 color = SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_LinearClamp, i.texcoord, 0);

				float4 overlay = SAMPLE_TEXTURE2D_LOD(_OverlayTexture, sampler_OverlayTexture, i.texcoord, 0) * _Tint;

				float blend = _Blend * saturate(overlay.a);

				float3 modified = color.rgb;
				if (_BlendMode == BLEND_NORMAL) modified = overlay.rgb;
				else if (_BlendMode == BLEND_MULTIPLY) modified.rgb *= overlay.rgb;
				else if (_BlendMode == BLEND_ADDITIVE) modified.rgb += overlay.rgb;
				else if (_BlendMode == BLEND_OVERLAY) modified.rgb = OverlayBlend(color, overlay);

				color.rgb = lerp(color.rgb, modified, blend);
				return color;
			}

			ENDHLSL
		}
	}
}