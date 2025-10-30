using System.Collections.Generic;
using UnityEngine.Rendering.RenderGraphModule;

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
		public class PassData
		{
			public TextureHandle source;
			public Material material;
		}

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

		protected bool initialized = false;
		protected Material blitMaterial;

		public virtual bool IsActive() => blend.value > 0;

		public virtual bool IsTileCompatible() => true;

		public virtual void Setup(CustomPostProcessPass pass, RenderingData renderingData, List<int> passes)
		{
			Initialize();
			if(blitMaterial)
			{
				blitMaterial.SetFloat("_Blend", blend.value);
				SetMaterialProperties(blitMaterial);
			}
			AddPasses(passes);
		}

		protected virtual void Initialize()
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
					//pass.renderer.ReferenceShader(shader);
#endif
				}
			}
			initialized = true;
		}

		public virtual void Render(RenderGraphModule.RenderGraph renderGraph, UniversalResourceData frameData, ContextContainer context)
		{
			var data = context.Get<UniversalCameraData>();
			if(data != null && (data.isSceneViewCamera && !VisibleInSceneView) || !data.postProcessEnabled) return;
			if(blend.value <= 0.0f)
				return;
			//Debug.Log("Blend > 0 for "+GetType().Name);
			if(!initialized)
			{
				Initialize();
			}
			if(!blitMaterial)
			{
				Debug.LogWarning("Shader missing");
				return;
			}
			SetMaterialProperties(blitMaterial);
			using(var builder = renderGraph.AddRasterRenderPass<PassData>(GetType().Name, out var passData))
			{
				passData.source = frameData.activeColorTexture;
				passData.material = blitMaterial;

				builder.UseTexture(passData.source);

				TextureDesc desc = frameData.activeColorTexture.GetDescriptor(renderGraph);
				desc.depthBufferBits = 0;
				desc.name = GetType().Name + "_ColorTexture";
				TextureHandle destination = renderGraph.CreateTexture(desc);

				builder.SetRenderAttachment(destination, 0);
				builder.AllowGlobalStateModification(true);

				builder.AllowPassCulling(false);
				builder.SetRenderFunc<PassData>(ExecuteRasterRenderPass);

				frameData.cameraColor = destination;
			}
		}

		protected virtual void ExecuteRasterRenderPass(PassData data, RasterGraphContext context)
		{
			Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), data.material, 0);
		}

		public virtual void SetMaterialProperties(Material mat)
		{
			mat.SetFloat("_Blend", blend.value);
		}

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
