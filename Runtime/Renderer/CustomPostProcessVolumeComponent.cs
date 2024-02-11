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

		protected Material blitMaterial;

		public virtual bool IsActive() => blend.value > 0;

		public virtual bool IsTileCompatible() => true;

		public virtual void Setup(RenderingData renderingData, List<int> passes)
		{
			SetupMaterial(renderingData);
			if(blitMaterial)
			{
				blitMaterial.SetFloat("_Blend", blend.value);
				ApplyProperties(blitMaterial, renderingData);
			}
			AddPasses(passes);
		}

		protected virtual void SetupMaterial(RenderingData renderingData)
		{
			if(!blitMaterial && ShaderName != null)
			{
				var shader = Shader.Find(ShaderName);
				if(!shader)
				{
					Debug.Log($"Failed to find custom post processing shader '{ShaderName}'");
				}
				if(!blitMaterial) blitMaterial = new Material(shader);
			}
		}

		public virtual void Render(CustomPostProcessPass feature, RenderingData renderingData, CommandBuffer cmd, RTHandle from, RTHandle to, int passIndex)
		{
			feature.Blit(cmd, from, to, blitMaterial, passIndex);
		}

		public abstract void ApplyProperties(Material material, RenderingData renderingData);
	}
}
