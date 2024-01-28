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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

            #pragma vertex vert
            #pragma fragment frag

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            uint _VertResolution;
            float _Blend;

            struct Attributes
            {
                float4 positionOS       : POSITION;
                float2 uv               : TEXCOORD0;
            };

            struct Varyings
            {
                float2 uv        : TEXCOORD0;
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };


            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.vertex = vertexInput.positionCS;
                output.uv = input.uv;

                return output;
            }

            float2 pixelate(float2 coord, float2 resolution) {
                coord *= resolution;
                coord = floor(coord) + 0.5;
                return coord / resolution;
            }

            float4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float2 aspect = float2(_ScreenParams.x / _ScreenParams.y, 1);
                float2 pixelatedTexCoord = pixelate(i.uv, _VertResolution * aspect);

                float4 color = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, i.uv, 0);
                float4 pixelated = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, pixelatedTexCoord, 0);
                color = lerp(color, pixelated, _Blend);
                return color;
            }

            ENDHLSL
        }
    }
    FallBack "Diffuse"
}