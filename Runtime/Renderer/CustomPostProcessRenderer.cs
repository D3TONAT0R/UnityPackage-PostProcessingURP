using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEngine.Rendering.Universal.PostProcessing
{
    [System.Serializable]
    public class CustomPostProcessRenderer : ScriptableRendererFeature
    {
        private CustomPostProcessPass beforeSkyboxPass;
        private CustomPostProcessPass beforeTransparentsPass;
        private CustomPostProcessPass beforePostProcessPass;
        private CustomPostProcessPass afterPostProcessPass;

        public override void Create()
        {
            beforeSkyboxPass = new CustomPostProcessPass(RenderPassEvent.BeforeRenderingSkybox);
            beforeTransparentsPass = new CustomPostProcessPass(RenderPassEvent.BeforeRenderingTransparents);
            beforePostProcessPass = new CustomPostProcessPass(RenderPassEvent.BeforeRenderingPostProcessing);
            afterPostProcessPass = new CustomPostProcessPass(RenderPassEvent.AfterRenderingPostProcessing);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(beforeSkyboxPass);
            renderer.EnqueuePass(beforeTransparentsPass);
            renderer.EnqueuePass(beforePostProcessPass);
            renderer.EnqueuePass(afterPostProcessPass);
        }
    }
}
