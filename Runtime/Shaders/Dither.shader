Shader "Hidden/PostProcessing/Dither"
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
                HLSLPROGRAM
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

                #pragma vertex vert
                #pragma fragment frag

                half _Blend;

                TEXTURE2D(_MainTex);
                SAMPLER(sampler_MainTex);
                TEXTURE2D(_DitherTex);
                SAMPLER(sampler_DitherTex);

                half _DitherTexSize;
                half _DownScale;
                int _ColorBitDepth;
                half _GammaCorrection;

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

                float2 Pixelate(float2 coord, float2 resolution) {
                    coord *= resolution;
                    coord = floor(coord) + 0.5;
                    return coord / resolution;
                }

                half gray(float3 col) {
                    return 0.21 * col.r + 0.72 * col.g + 0.07 * col.b;
                }

                half dither(half ditherValue, half colorValue) {
                    colorValue = lerp(colorValue, pow(abs(colorValue), 0.454545), _GammaCorrection);
                    half lower = floor(colorValue * _ColorBitDepth) / _ColorBitDepth;
                    half upper = ceil(colorValue * _ColorBitDepth) / _ColorBitDepth;
                    half pos = (colorValue - lower) / (upper - lower);
                    half result;
                    if (pos > ditherValue) result = upper;
                    else result = lower;
                    result = lerp(result, pow(abs(result), 2.2), _GammaCorrection);
                    return result;
                }

                float4 frag(Varyings i) : SV_Target
                {
                    float2 pixelatedTexCoord;
                    if (_DownScale > 1) {
                        pixelatedTexCoord = Pixelate(i.uv, _ScreenParams.xy / _DownScale);
                    }
                    else {
                        pixelatedTexCoord = i.uv;
                    }
                    float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, pixelatedTexCoord);
                    float2 pixelCoord = i.uv.xy;
                    float aspect = _ScreenParams.x / _ScreenParams.y;
                    pixelCoord.x *= aspect;
                    pixelCoord *= _ScreenParams.y;
                    pixelCoord /= _DitherTexSize * _DownScale;
                    float ditherValue = SAMPLE_TEXTURE2D(_DitherTex, sampler_DitherTex, pixelCoord).r * 0.9 + 0.1;
                    float4 dithered = float4(dither(ditherValue, color.r), dither(ditherValue, color.g), dither(ditherValue, color.b), color.a);
                    return lerp(color, dithered, _Blend);
                }

                ENDHLSL
            }
        }
            FallBack "Diffuse"
}