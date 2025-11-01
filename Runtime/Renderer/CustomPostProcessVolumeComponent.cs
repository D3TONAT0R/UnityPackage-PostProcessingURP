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
		public PassEventParameter(PostProcessingPassEvent value, bool overrideState = false) : base(value, overrideState)
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

		public virtual bool VisibleInSceneView => true;

		protected Material blitMaterial;

		public virtual bool IsActive() => blend.value > 0;

		public virtual bool IsTileCompatible() => true;

		public virtual void Setup(CustomPostProcessPass pass, RenderingData renderingData, List<int> passes)
		{
			SetupMaterial(pass, renderingData);
			if(blitMaterial)
			{
				blitMaterial.SetFloat("_Blend", blend.value);
				ApplyProperties(blitMaterial, renderingData);
			}
			AddPasses(passes);
		}

		protected virtual void SetupMaterial(CustomPostProcessPass pass, RenderingData renderingData)
		{
			if(!blitMaterial && ShaderName != null)
			{
				var shader = Shader.Find(ShaderName);
				if(!shader)
				{
					Debug.LogError($"Failed to find custom post processing shader '{ShaderName}'");
				}
				else
				{
					blitMaterial = new Material(shader);
#if UNITY_EDITOR
					pass.renderer.ReferenceShader(shader);
#endif
				}
			}
		}

		public virtual void Render(CustomPostProcessPass feature, RenderingData renderingData, CommandBuffer cmd, RTHandle from, RTHandle to, int passIndex)
		{
			feature.Blit(cmd, from, to, blitMaterial, passIndex);
		}

		public abstract void ApplyProperties(Material material, RenderingData renderingData);

		protected override void OnDisable()
		{
			base.OnDisable();
			if(blitMaterial)
			{
				if(Application.isPlaying) Destroy(blitMaterial);
				else DestroyImmediate(blitMaterial);
			}
		}
	}
}
