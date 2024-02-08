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
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
                // The Blit.hlsl file provides the vertex shader (Vert),
                // the input structure (Attributes), and the output structure (Varyings)
                #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

                #pragma vertex Vert
                #pragma fragment Frag

                half _Blend;

                TEXTURE2D(_MainTex);
                SAMPLER(sampler_MainTex);
                TEXTURE2D(_DitherTex);
                SAMPLER(sampler_DitherTex);

                half _DitherTexSize;
                half _DownScale;
                int _ColorBitDepth;
                half _GammaCorrection;

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

                float4 Frag(Varyings i) : SV_Target
                {
                    float2 pixelatedTexCoord;
                    if (_DownScale > 1) {
                        pixelatedTexCoord = Pixelate(i.texcoord, _ScreenParams.xy / _DownScale);
                    }
                    else {
                        pixelatedTexCoord = i.texcoord;
                    }
                    float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, pixelatedTexCoord);
                    float2 pixelCoord = i.texcoord.xy;
                    float aspect = _ScreenParams.x / _ScreenParams.y;
                    pixelCoord.x *= aspect;
                    pixelCoord *= _ScreenParams.y;
                    pixelCoord /= _DitherTexSize * _DownScale;
                    float ditherValue = SAMPLE_TEXTURE2D(_DitherTex, sampler_DitherTex, pixelCoord).r * 0.9 + 0.1;
                    float4 dithered = float4(dither(ditherValue, color.r), dither(ditherValue, color.g), dither(ditherValue, color.b), color.a);
                    float4 originalColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord);
                    return lerp(originalColor, dithered, _Blend);
                }

                ENDHLSL
            }
        }
}