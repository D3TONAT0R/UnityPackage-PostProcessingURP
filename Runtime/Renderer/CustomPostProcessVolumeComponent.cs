using System.Collections.Generic;

namespace UnityEngine.Rendering.Universal.PostProcessing
{
	public enum PostProcessingPassEvent
	{
		BeforeSkybox = RenderPassEvent.BeforeRenderingSkybox,
		BeforeTransparents = RenderPassEvent.BeforeRenderingTransparents,
		BeforePostProcessing = RenderPassEvent.BeforeRenderingPostProcessing,
		AfterPostProcessing = RenderPassEvent.AfterRenderingPostProcessing,
		AfterRendering = RenderPassEvent.AfterRendering
	}

	[System.Serializable]
	public class PassEventParameter : VolumeParameter<PostProcessingPassEvent>
	{
		public PassEventParameter(PostProcessingPassEvent value, bool overrideState) : base(value, overrideState)
		{

		}
	}

	public abstract class CustomPostProcessVolumeComponent : VolumeComponent, IPostProcessComponent
	{
		[Space(10)]
		public ClampedFloatParameter blend = new ClampedFloatParameter(0f, 0, 1, true);

		public abstract string ShaderName { get; }

		public virtual void AddPasses(List<int> passes)
		{
			passes.Add(0);
		}

		public virtual bool IgnorePostProcessingFlag => false;

		public virtual ScriptableRenderPassInput Requirements => ScriptableRenderPassInput.Color;

		public abstract PostProcessingPassEvent PassEvent { get; }

		private Material material;

		public virtual bool IsActive() => blend.value > 0;

		public virtual bool IsTileCompatible() => true;

		public void Setup(RenderingData renderingData, out Material blitMaterial, List<int> passes)
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
			AddPasses(passes);
		}

		public abstract void ApplyProperties(Material material, RenderingData renderingData);
	}
}
