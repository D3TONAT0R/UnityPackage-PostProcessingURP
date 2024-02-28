using D3T.Utility;
using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;

namespace UnityEngine.Rendering.Universal.PostProcessing
{

    /// <summary>
    /// Blurs the image. 
    /// </summary>
    [VolumeComponentMenu("Post-process/Blur")]
    public class Blur : CustomPostProcessVolumeComponent
    {
        public enum Mode
        {
            StandardGaussian,
            SgxGaussian
        }

        public enum Pass
        {
            Downsample = 0,
            BlurVertical = 1,
            BlurHorizontal = 2,
        }

        [Serializable]
        public sealed class BlurModeParameter : VolumeParameter<BlurEffect.Mode>
        {

        }

        public BlurModeParameter mode = new BlurModeParameter();
        public IntParameter downsample = new IntParameter(1);
        public IntParameter blurIterations = new IntParameter(1);
        public FloatParameter blurSize = new FloatParameter(3f);

        public override string ShaderName => "Hidden/PostProcessing/Blur";

        public override PostProcessingPassEvent PassEvent => PostProcessingPassEvent.AfterPostProcessing;

        public override void Render(CustomPostProcessPass feature, RenderingData renderingData, CommandBuffer cmd, RTHandle from, RTHandle to, int passIndex)
        {
            cmd.BeginSample("BlurPostEffect");

            int ds = downsample.value;

            float widthMod = 1.0f / (1.0f * (1 << ds));

            int rtW = renderingData.cameraData.cameraTargetDescriptor.width >> ds;
            int rtH = renderingData.cameraData.cameraTargetDescriptor.height >> ds;

            float _blend = Mathf.Min(blend.value, 1);
            int iterations = blurIterations.value;
            float size = blurSize.value;

            blitMaterial.SetVector("_Parameter", new Vector4(size * widthMod, -size * widthMod, _blend, 0.0f));

            int blurId = Shader.PropertyToID("_BlurPostProcessEffect");
            cmd.GetTemporaryRT(blurId, rtW, rtH, 0, FilterMode.Bilinear);
            feature.Blit(cmd, from, tempRT, blitMaterial, (int)Pass.Downsample);

            int pass = mode.value == Mode.SgxGaussian ? 2 : 0;

            int rtIndex = 0;
            for(int i = 0; i < iterations; i++)
            {
                float iterationOffs = i * 1.0f;
                sheet.properties.SetVector("_Parameter", new Vector4(size * widthMod + iterationOffs, -size * widthMod - iterationOffs, _blend, 0.0f));

                // Vertical blur..
                int rtId2 = Shader.PropertyToID("_BlurPostProcessEffect" + rtIndex++);
                cmd.GetTemporaryRT(rtId2, rtW, rtH, 0, FilterMode.Bilinear);
                cmd.BlitFullscreenTriangle(blurId, rtId2, sheet, (int)Pass.BlurVertical + pass);
                cmd.ReleaseTemporaryRT(blurId);
                blurId = rtId2;

                // Horizontal blur..
                rtId2 = Shader.PropertyToID("_BlurPostProcessEffect" + rtIndex++);
                cmd.GetTemporaryRT(rtId2, rtW, rtH, 0, FilterMode.Bilinear);
                cmd.BlitFullscreenTriangle(blurId, rtId2, sheet, (int)Pass.BlurHorizontal + pass);
                cmd.ReleaseTemporaryRT(blurId);
                blurId = rtId2;
            }

            cmd.Blit(blurId, context.destination);
            cmd.ReleaseTemporaryRT(blurId);

            cmd.EndSample("BlurPostEffect");
        }

        public override void ApplyProperties(Material material, RenderingData renderingData)
        {
            throw new NotImplementedException();
        }
    }
}