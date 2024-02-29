﻿Shader "Hidden/PostProcessing/Blur"
{
	HLSLINCLUDE

	#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
	// The Blit.hlsl file provides the vertex shader (Vert),
	// the input structure (Attributes), and the output structure (Varyings)
	#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

	float _Blend;
	//TEXTURE2D(_BlitTexture);
	float4 _BlitTexture_TexelSize;
	//float4 _BlitTexture_ST;

	#define _BlitTexture_ST float4(1.0, 1.0, 0.0, 0.0)

	uniform half4 _Parameter;
	//uniform half4 _BlitTexture_TexelSize;

	// Weight Curves..
	static const half curve[7] = { 0.0205, 0.0855, 0.232, 0.324, 0.232, 0.0855, 0.0205 };
	static const half4 curve4[7] = { half4(0.0205,0.0205,0.0205,0), half4(0.0855,0.0855,0.0855,0), half4(0.232,0.232,0.232,0), half4(0.324,0.324,0.324,1), half4(0.232,0.232,0.232,0), half4(0.0855,0.0855,0.0855,0), half4(0.0205,0.0205,0.0205,0) };

	struct VaryingsDownsample
	{
		float4 positionCS : SV_POSITION;
		float2 texcoord   : TEXCOORD0;
		UNITY_VERTEX_OUTPUT_STEREO
		half2 uv20    :TEXCOORD1;
		half2 uv21    :TEXCOORD2;
		half2 uv22    :TEXCOORD3;
		half2 uv23    :TEXCOORD4;
	};

	struct VaryingsBlurCoords8
	{
		float4 positionCS : SV_POSITION;
		float2 texcoord   : TEXCOORD0;
		UNITY_VERTEX_OUTPUT_STEREO
		half2 offs    :TEXCOORD1;
	};

	struct VaryingsBlurCoordsSGX
	{
		float4 positionCS : SV_POSITION;
		float2 texcoord   : TEXCOORD0;
		UNITY_VERTEX_OUTPUT_STEREO
		half4 offs[3] :TEXCOORD1;
	};

	float2 TransformTriangleVertexToUV(float2 vertex)
	{
		float2 uv = (vertex + 1.0) * 0.5;
		return uv;
	}

	inline float2 UnityStereoScreenSpaceUVAdjustInternal(float2 uv, float4 scaleAndOffset)
	{
		return uv.xy * scaleAndOffset.xy + scaleAndOffset.zw;
	}

	inline float4 UnityStereoScreenSpaceUVAdjustInternal(float4 uv, float4 scaleAndOffset)
	{
		return float4(UnityStereoScreenSpaceUVAdjustInternal(uv.xy, scaleAndOffset), UnityStereoScreenSpaceUVAdjustInternal(uv.zw, scaleAndOffset));
	}

	#define UnityStereoScreenSpaceUVAdjust(x, y) UnityStereoScreenSpaceUVAdjustInternal(x, y)

	VaryingsDownsample VertDownsample(Attributes v)
	{
		VaryingsDownsample o;
		#include "CGIncludes/BlurVertBlock.cginc"

		/*
#if UNITY_UV_STARTS_AT_TOP
		o.texcoord = o.texcoord * float2(1.0, -1.0) + float2(0.0, 1.0);
#endif
*/

		o.positionCS = float4(o.positionCS.xy, 0.0, 1.0); //<-----
		//o.uv20 = UnityStereoScreenSpaceUVAdjust(o.texcoord + _BlitTexture_TexelSize.xy * half2(0.5h, 0.5h), _BlitTexture_ST);
		o.uv20 = UnityStereoScreenSpaceUVAdjust(o.texcoord + _BlitTexture_TexelSize.xy, _BlitTexture_ST);
		o.uv21 = UnityStereoScreenSpaceUVAdjust(o.texcoord + _BlitTexture_TexelSize.xy * half2(-0.5h, -0.5h), _BlitTexture_ST);
		o.uv22 = UnityStereoScreenSpaceUVAdjust(o.texcoord + _BlitTexture_TexelSize.xy * half2(0.5h, -0.5h), _BlitTexture_ST);
		o.uv23 = UnityStereoScreenSpaceUVAdjust(o.texcoord + _BlitTexture_TexelSize.xy * half2(-0.5h, 0.5h), _BlitTexture_ST);
		return o;
	}

	float4 FragDownsample(VaryingsDownsample i) :SV_Target
	{
		float4 color = 0.0;
		color += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, i.uv20);
		color += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, i.uv21);
		color += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, i.uv22);
		color += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, i.uv23);

		return color / 4;
	}

	VaryingsBlurCoords8 VertBlurHorizontal(Attributes v)
	{
		VaryingsBlurCoords8 o;
		#include "CGIncludes/BlurVertBlock.cginc"

		#if UNITY_UV_STARTS_AT_TOP
			o.texcoord = o.texcoord * float2(1.0, -1.0) + float2(0.0, 1.0);
		#endif

		o.offs = _BlitTexture_TexelSize.xy * half2(1.0, 0.0) * _Parameter.x;

		return o;
	}

	VaryingsBlurCoords8 VertBlurVertical(Attributes v)
	{
		VaryingsBlurCoords8 o;
		#include "CGIncludes/BlurVertBlock.cginc"

		#if UNITY_UV_STARTS_AT_TOP
		o.texcoord = o.texcoord * float2(1.0, -1.0) + float2(0.0, 1.0);
		#endif

		o.offs = _BlitTexture_TexelSize.xy * half2(0.0, 1.0) * _Parameter.x;

		return o;
	}

	float4 FragBlur8(VaryingsBlurCoords8 i):SV_Target
	{
		half2 uv = i.texcoord.xy;
		half2 netFilterWidth = i.offs;
		half2 coords = uv - netFilterWidth * 3.0;

		float4 color = 0;
		for(int l=0; l<7; l++)
		{
			half4 tap = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, UnityStereoScreenSpaceUVAdjust(coords, _BlitTexture_ST));
			color += tap * curve4[l];
			coords += netFilterWidth;
		}

		return color;
	}

	VaryingsBlurCoordsSGX VertBlurHorizontalSGX(Attributes v)
	{
		VaryingsBlurCoordsSGX o;
		#include "CGIncludes/BlurVertBlock.cginc"

#if UNITY_UV_STARTS_AT_TOP
		o.texcoord = o.texcoord * float2(1.0, -1.0) + float2(0.0, 1.0);
#endif

		half offsetMagnitude = _BlitTexture_TexelSize.x * _Parameter.x;
		o.offs[0] = UnityStereoScreenSpaceUVAdjust(o.texcoord.xyxy + offsetMagnitude * half4(-3.0h, 0.0h, 3.0h, 0.0h), _BlitTexture_ST);
		o.offs[1] = UnityStereoScreenSpaceUVAdjust(o.texcoord.xyxy + offsetMagnitude * half4(-2.0h, 0.0h, 2.0h, 0.0h), _BlitTexture_ST);
		o.offs[2] = UnityStereoScreenSpaceUVAdjust(o.texcoord.xyxy + offsetMagnitude * half4(-1.0h, 0.0h, 1.0h, 0.0h), _BlitTexture_ST);

		return o;
	}

	VaryingsBlurCoordsSGX VertBlurVerticalSGX(Attributes v)
	{
		VaryingsBlurCoordsSGX o;
		#include "CGIncludes/BlurVertBlock.cginc"

		#if UNITY_UV_STARTS_AT_TOP
		o.texcoord = o.texcoord * float2(1.0, -1.0) + float2(0.0, 1.0);
		#endif

		half offsetMagnitude = _BlitTexture_TexelSize.y * _Parameter.x;
		o.offs[0] = UnityStereoScreenSpaceUVAdjust(o.texcoord.xyxy + offsetMagnitude * half4(0.0h, -3.0h, 0.0h, 3.0h), _BlitTexture_ST);
		o.offs[1] = UnityStereoScreenSpaceUVAdjust(o.texcoord.xyxy + offsetMagnitude * half4(0.0h, -2.0h, 0.0h, 2.0h), _BlitTexture_ST);
		o.offs[2] = UnityStereoScreenSpaceUVAdjust(o.texcoord.xyxy + offsetMagnitude * half4(0.0h, -1.0h, 0.0h, 1.0h), _BlitTexture_ST);

		return o;
	}

	float4 FragBlurSGX(VaryingsBlurCoordsSGX i):SV_Target
	{
		half2 uv = i.texcoord.xy;

		half4 unmod = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, i.texcoord);
		half4 color = unmod * curve4[3];

		for(int l=0; l<3; l++)
		{
			half4 tapA = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, i.offs[l].xy);
			half4 tapB = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, i.offs[l].zw);
			color += (tapA + tapB) * curve4[l];
		}

		return lerp(unmod, color, _Blend);
		//return color;
	}

	float4 FragFinal(Varyings i) : SV_Target
	{
		return SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, i.texcoord);
	}

	ENDHLSL

	SubShader
	{
		Tags{ "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
		LOD 100
		ZWrite Off Cull Off

		Pass // 0
		{
			Name "Downsample"
			HLSLPROGRAM

			#pragma vertex VertDownsample
			#pragma fragment FragDownsample

			ENDHLSL
		}
		
		Pass // 1
		{
			Name "BlurVertical"
			HLSLPROGRAM

			#pragma vertex VertBlurVertical
			#pragma fragment FragBlur8

			ENDHLSL
		}

		Pass // 2
		{
			Name "BlurHorizontal"
			HLSLPROGRAM

			#pragma vertex VertBlurHorizontal
			#pragma fragment FragBlur8

			ENDHLSL
		}

		Pass // 3
		{
			Name "BlurVerticalSGX"
			HLSLPROGRAM

			#pragma vertex VertBlurVerticalSGX
			#pragma fragment FragBlurSGX

			ENDHLSL
		}

		Pass // 4
		{
			Name "BlurHorizontalSGX"
			HLSLPROGRAM

			#pragma vertex VertBlurHorizontalSGX
			#pragma fragment FragBlurSGX

			ENDHLSL
		}

		Pass // 4
		{
			Name "FinalBlit"
			HLSLPROGRAM

			#pragma vertex Vert
			#pragma fragment FragFinal

			ENDHLSL
		}
	}
}