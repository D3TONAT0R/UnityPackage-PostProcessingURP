Shader "Hidden/PostProcessing/Compression"
{
	Properties
	{
		_Blend("Blend", Range(0,1)) = 1.0
	}

	SubShader
	{
		Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

		HLSLINCLUDE

		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		// The Blit.hlsl file provides the vertex shader (Vert),
		// the input structure (Attributes), and the output structure (Varyings)
		#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

		//https://www.shadertoy.com/view/XtffDj

		#define SQRT2 0.70710678118
		#define BLOCK_SIZE _BlockSize

		float _Frequency;
		float _Levels;
		uint _BlockSize;
		float _DCTGamma;

		float4 sample(Texture2D tex, float2 texcoord)
		{
			return SAMPLE_TEXTURE2D_LOD(tex, sampler_LinearClamp, texcoord, 0);
		}

		float DCTcoeff(float2 k, float2 x)
		{
			return cos(PI * k.x * x.x) * cos(PI * k.y * x.y);
		}

		void getKValues(float2 texcoord, out float2 k, out float2 K)
		{
			float2 pixelCoord = texcoord * _ScreenParams.xy;
			k = (pixelCoord % BLOCK_SIZE) - 0.5;
			K = pixelCoord - 0.5 - k;
		}

		ENDHLSL

		// DCT Pass
		Pass
		{
			Name "Discrete Cosine Transform"

			HLSLPROGRAM

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			// The Blit.hlsl file provides the vertex shader (Vert),
			// the input structure (Attributes), and the output structure (Varyings)
			#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

			#pragma vertex Vert
			#pragma fragment Frag

			/// This is the discrete cosine transform step, where 8x8 blocs are converted into frequency space
			/// Nice ref: https://unix4lyfe.org/dct/

			float4 Frag(Varyings i) : SV_Target
			{
				float2 k, K;
				getKValues(i.texcoord, k, K);

				float3 val = 0.0;

				float gamma = 1.0 / _DCTGamma;
    
				for(int x = 0; x < BLOCK_SIZE; x++)
    			{
					for(int y = 0; y < BLOCK_SIZE; y++)
					{
						float3 tex = sample(_BlitTexture, (K + float2(x, y) + 0.5) / _ScreenParams.xy).rgb;
						//Apply gamma boost
						tex = pow(tex, gamma);
						tex = saturate(tex);
						val += tex * DCTcoeff(k, (float2(x, y) + 0.5) / BLOCK_SIZE) * (k.x < 0.5 ? SQRT2 : 1.0) * (k.y < 0.5 ? SQRT2 : 1.0);
					}
				}
        
				return float4(val/4.0, 0.0);
			}

			ENDHLSL
		}

		//Main Pass
		Pass
		{
			Name "Main"

			HLSLPROGRAM
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			// The Blit.hlsl file provides the vertex shader (Vert),
			// the input structure (Attributes), and the output structure (Varyings)
			#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

			#pragma vertex Vert
			#pragma fragment Frag

			half _Blend;

			TEXTURE2D(_DCTTexture);

			float4 quantify(float2 texcoord)
			{
				float4 fragColor = sample(_DCTTexture, texcoord);
				fragColor = round(fragColor / BLOCK_SIZE * _Levels) / _Levels * 8;

				return fragColor;
			}
			
			float4 reconstruct(float2 texcoord)
			{
				float2 k, K;
				getKValues(texcoord, k, K);
        
				float3 val = 0.0;
				for(int u = 0; u < _Frequency; u++)
				{
    				for(int v = 0; v < _Frequency; v++)
					{
						float3 quantified = quantify((K+float2(u,v)+0.5)/_ScreenParams.xy).rgb;
						val += quantified * DCTcoeff(float2(u, v), (k + 0.5) / BLOCK_SIZE) * (u == 0 ? SQRT2 : 1.0) * (v == 0 ? SQRT2 : 1.0);
					}
				}

				float4 color = float4(val / 4.0, 1.0);
				color.rgb *= (8.0 / BLOCK_SIZE);

				//Undo gamma change that was applied during the DCT pass
				color.rgb = pow(color.rgb, _DCTGamma);

				return color;
			}

			float4 stopNan(float4 v)
			{
				if (isnan(v.x) || isnan(v.y) || isnan(v.z))
					return float4(0, 0, 0, 1);
				return v;
			}

			float4 Frag(Varyings i) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

				float4 color = SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_LinearClamp, i.texcoord, 0);
				float4 jpeg = stopNan(reconstruct(i.texcoord));
				color = lerp(color, jpeg, _Blend);
				return color;
			}

			ENDHLSL
		}
	}
}