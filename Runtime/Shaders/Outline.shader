Shader "Hidden/PostProcessing/Outline"
{
	Properties
	{
		_VertResolution("Vertical Resolution", Int) = 270
		_Blend("Blend", Range(0,1)) = 1.0
		_BackgroundColor("Background Color", Color) = (1,1,1,0)
		_LineColor("Line Color", Color) = (0,0,0,1)
		_DepthThreshold("Depth Threshold", Range(0,0.1)) = 0.01
		_NormalThreshold("Normal Threshold", Range(0,1)) = 0.15
		_LineWidth("Line Width", Int) = 1
		_Range("Range", Float) = 10.0
		_RangeFade("Range Fade", Range(0,1)) = 0.5
	}
	SubShader
	{
		Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

		HLSLINCLUDE

		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

		ENDHLSL

		Pass
		{
			Name "EdgeDetection"
			HLSLPROGRAM

			#pragma vertex Vert
			#pragma fragment Frag

			struct Data
			{
				float4 color;
				float depth;
				float3 normal;
			};

			struct Diffs
			{
				float depth;
				float normal;
				float3 color;
			};

			float _DepthThreshold;
			float _NormalThreshold;
			float _ColorThreshold;

			float _Range;
			float _RangeFadeStart;

			Data SampleData(float2 texcoord)
			{
				Data o;
				o.color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, texcoord);
				o.depth = LinearEyeDepth(SampleSceneDepth(texcoord), _ZBufferParams);
				o.normal = SampleSceneNormals(texcoord);
				return o;
			}

			Data SampleData(float2 texcoord, int2 pixelOffset)
			{
				texcoord += (1.0 - _ScreenParams.zw) * pixelOffset;
				return SampleData(texcoord);
			}

			Diffs CalcDifference(Data source, Data other, Diffs diffs)
			{
				diffs.depth = max(diffs.depth, abs(source.depth - other.depth));
				float limit = _ProjectionParams.z * 0.99;
				if(source.depth < limit || other.depth < limit)
				{
					diffs.normal = max(diffs.normal, abs(1.0 - dot(source.normal, other.normal)));
				}
				diffs.color = max(diffs.color, abs(source.color - other.color));
				return diffs;
			}

			float4 Frag(Varyings i) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

				Data source = SampleData(i.texcoord);
				Diffs diffs = (Diffs)0;
				Data dx = SampleData(i.texcoord, int2(1, 0));
				Data dy = SampleData(i.texcoord, int2(0, 1));
				diffs = CalcDifference(source, dx, diffs);
				diffs = CalcDifference(source, dy, diffs);

				float3 viewDir = normalize(mul(UNITY_MATRIX_V, float3(0,0,1)));

				float normalDot = abs(dot(viewDir, source.normal));

				float depthLine = 1.0 - step(diffs.depth, _DepthThreshold * source.depth * (1.0 / normalDot));
				float normalLine = 1.0 - step(diffs.normal, _NormalThreshold);
				float colorLine = 1.0 - step(dot(diffs.color, float3(0.2126, 0.7152, 0.0722)), _ColorThreshold);

				float fadeStart = _Range * _RangeFadeStart;
				float fadeEnd = _Range;
				float minDepth = min(source.depth, min(dx.depth, dy.depth));
				float distanceFade = saturate(((minDepth - fadeEnd) / (fadeStart - fadeEnd)));

				return float4(depthLine, normalLine, colorLine, distanceFade);
			}

			ENDHLSL
		}

		Pass
		{
			Name "OutlineRender"
			HLSLPROGRAM

			#pragma vertex Vert
			#pragma fragment Frag

			float _Blend;
			uint _LineWidth;

			float4 _BackgroundColor;
			float4 _LineColor;

			TEXTURE2D(_EdgeDetectionTexture);

			float4 Frag(Varyings i) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

				float4 maxDetection = 0.0;

				float4 source = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, i.texcoord);

				int i0 = -floor(_LineWidth / 2.0);
				int i1 = ceil(_LineWidth / 2.0);
				[loop]
				for(int x = i0; x < i1; x++)
				{
					[loop]
					for(int y = i0; y < i1; y++)
					{
						if(distance(int2(x,y), int2(0,0)) <= _LineWidth / 2.0)
						{
							float2 uv = i.texcoord + (1.0 - _ScreenParams.zw) * int2(x, y);
							maxDetection = max(maxDetection, SAMPLE_TEXTURE2D(_EdgeDetectionTexture, sampler_LinearClamp, uv));
						}
					}
				}
				float detection = saturate(maxDetection.x + maxDetection.y + maxDetection.z) * pow(maxDetection.a, 2.0);
				float4 effect = lerp(_BackgroundColor, _LineColor, detection);

				float3 final = lerp(source.rgb, effect.rgb, _Blend * effect.a);
				return float4(final, source.a);
			}

			ENDHLSL
		}
	}
}