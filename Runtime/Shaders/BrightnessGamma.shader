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

            float4 Frag(Varyings i) : SV_Target
            {
                float4 color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, i.texcoord);

                float4 adjusted = color;
		        adjusted.rgb += _Brightness;
                adjusted.rgb = pow(max(adjusted.rgb, 0.001), 1.0 / _Gamma);

                return lerp(color, adjusted, _Blend);
            }

            ENDHLSL
        }
    }
}