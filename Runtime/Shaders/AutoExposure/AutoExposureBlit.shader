Shader "Hidden/PostProcessing/AutoExposureBlit"
{
    Properties
    {
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
            TEXTURE2D(_AutoExposureTex);

            float2 pixelate(float2 coord, float2 resolution) {
                coord *= resolution;
                coord = floor(coord) + 0.5;
                return coord / resolution;
            }

            float4 Frag(Varyings i) : SV_Target
            {
                float4 color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, i.texcoord);
                float4 adjusted = color;
                float autoExposure = SAMPLE_TEXTURE2D(_AutoExposureTex, sampler_LinearClamp, i.texcoord);
                adjusted.rgb *= autoExposure;
                return lerp(color, adjusted, _Blend);
            }

            ENDHLSL
        }
    }
}