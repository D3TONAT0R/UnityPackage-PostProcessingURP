using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEngine.Rendering.Universal.PostProcessing
{
	public enum PostProcessingPassEvent
	{
		BeforeSkybox = RenderPassEvent.BeforeRenderingSkybox,
		BeforeTransparents = RenderPassEvent.BeforeRenderingTransparents,
		BeforePostProcessing = RenderPassEvent.BeforeRenderingPostProcessing,
		AfterPostProcessing = RenderPassEvent.AfterRenderingPostProcessing
	}

	public abstract class CustomPostProcessVolumeComponent : VolumeComponent, IPostProcessComponent
	{
		public ClampedFloatParameter blend = new ClampedFloatParameter(0f, 0, 1, true);

		public abstract string ShaderName { get; }

		public virtual int PassIndex => 0;

		public virtual bool IgnorePostProcessingFlag => false;

		public virtual ScriptableRenderPassInput Requirements => ScriptableRenderPassInput.Color;

		public abstract PostProcessingPassEvent PassEvent { get; }

		private Material material;

		public virtual bool IsActive() => blend.value > 0;

		public virtual bool IsTileCompatible() => true;

		public void Setup(RenderingData renderingData, out Material blitMaterial, out int pass)
		{
			var shader = Shader.Find(ShaderName);
			if(!shader)
			{
				Debug.Log($"Failed to find custom post processing shader '{ShaderName}'");
			}
			if(!material) material = new Material(shader);
			material.SetFloat("_Blend", blend.value);
			ApplyProperties(material, renderingData);
			blitMaterial = material;
			pass = PassIndex;
		}

		public abstract void ApplyProperties(Material material, RenderingData renderingData);
	}
}
