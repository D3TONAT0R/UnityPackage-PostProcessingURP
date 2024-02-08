Shader "PostProcessing/Pixelate"
{
    Properties
    {
        _MainTex("Main Texture", 2D) = "white" {}
        _VertResolution("Vertical Resolution", Int) = 270
        _Blend("Blend", Range(0,1)) = 1.0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
                            Stencil
        {
            Ref 10
            Comp Equal
        }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            #pragma vertex Vert
            #pragma fragment Frag

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            uint _VertResolution;
            float _Blend;

            float2 pixelate(float2 coord, float2 resolution) {
                coord *= resolution;
                coord = floor(coord) + 0.5;
                return coord / resolution;
            }

            float4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float2 aspect = float2(_ScreenParams.x / _ScreenParams.y, 1);
                float2 pixelatedTexCoord = pixelate(i.texcoord, _VertResolution * aspect);

                float4 color = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, i.texcoord, 0);
                float4 pixelated = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, pixelatedTexCoord, 0);
                color = lerp(color, pixelated, _Blend);
                return color;
            }

            ENDHLSL
        }
    }
}