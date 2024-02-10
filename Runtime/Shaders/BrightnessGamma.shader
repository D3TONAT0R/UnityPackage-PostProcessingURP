Shader "Hidden/PostProcessing/BrightnessGamma"
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
            float _Brightness;
            float _Gamma;

            float2 pixelate(float2 coord, float2 resolution) {
                coord *= resolution;
                coord = floor(coord) + 0.5;
                return coord / resolution;
            }

            float4 Frag(Varyings i) : SV_Target
            {
                float4 color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, i.texcoord);
                float4 adjusted = color;

                adjusted.rgb = pow(max(adjusted.rgb, 0.001), 1.0 / _Gamma);
		        adjusted.rgb += _Brightness - 1.0;

                return lerp(color, adjusted, _Blend);
            }

            ENDHLSL
        }
    }
}