using System;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal.PostProcessing
{
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
			public int passIndex;
		}

		//These Will be drawn manually with a custom editor

		[HideInInspector]
		public BoolParameter enabled = new BoolParameter(false);
		//Only visible if SupportsBlending is true
		[HideInInspector]
		public ClampedFloatParameter blend = new ClampedFloatParameter(0f, 0, 1, true);

		public abstract string ShaderName { get; }

		public abstract PostProcessingPassEvent InjectionPoint { get; }

		public virtual bool IgnorePostProcessingFlag => false;

		public virtual bool SupportsBlending => false;

		public virtual bool VisibleInSceneView => true;

		public virtual ScriptableRenderPassInput Requirements => ScriptableRenderPassInput.Color;

		public RenderPassEvent RenderPassEvent
		{
			get
			{
				return InjectionPoint switch
				{
					PostProcessingPassEvent.BeforeSkybox => RenderPassEvent.BeforeRenderingSkybox,
					PostProcessingPassEvent.BeforeTransparents => RenderPassEvent.BeforeRenderingTransparents,
					PostProcessingPassEvent.BeforePostProcessing => RenderPassEvent.BeforeRenderingPostProcessing,
					PostProcessingPassEvent.AfterPostProcessing => RenderPassEvent.AfterRenderingPostProcessing,
					PostProcessingPassEvent.AfterRendering => RenderPassEvent.AfterRendering,
					_ => throw new ArgumentOutOfRangeException()
				};
			}
		}

		protected bool initialized = false;
		protected Material blitMaterial;

		protected string typeName;
		protected string colorTextureName;

		protected static readonly int blendPropertyId = Shader.PropertyToID("_Blend");

		public virtual bool IsActive() {
			if(!enabled.value)
				return false;
			if(SupportsBlending)
				return blend.value > 0;
			return true;
		}

		public virtual bool IsTileCompatible() => true;

		protected virtual void Initialize(CustomPostProcessPass pass)
		{
			typeName = GetType().Name;
			colorTextureName = typeName + "_ColorTexture";
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
				}
			}
			initialized = true;
		}

		public void Render(CustomPostProcessPass pass, RenderGraphModule.RenderGraph renderGraph,
			UniversalResourceData frameData, ContextContainer context)
		{
			if(!BeginRender(pass, context)) return;
#if UNITY_EDITOR
			if(blitMaterial && blitMaterial.shader != null) pass.RenderFeature.ReferenceShader(blitMaterial.shader);
#endif
			RenderEffect(pass, renderGraph, frameData, context);
		}

		protected virtual void RenderEffect(CustomPostProcessPass pass, RenderGraphModule.RenderGraph renderGraph,
			UniversalResourceData frameData, ContextContainer context)
		{
			Blit(renderGraph, frameData);
		}

		protected bool BeginRender(CustomPostProcessPass pass, ContextContainer context)
		{
			var data = context.Get<UniversalCameraData>();
			if(data != null && (data.isSceneViewCamera && !VisibleInSceneView) || (!IgnorePostProcessingFlag && !data.postProcessEnabled))
				return false;
			if(!initialized)
			{
				Initialize(pass);
			}
			if(!blitMaterial)
			{
				Debug.LogWarning("Shader missing");
				return false;
			}
			InitializeBlitMaterial();
			return true;
		}

		private void InitializeBlitMaterial()
		{
			float blendValue = SupportsBlending ? blend.value : 1.0f;
			blitMaterial.SetFloat(blendPropertyId, blendValue);
			SetMaterialProperties(blitMaterial);
		}

		public virtual void SetMaterialProperties(Material mat)
		{

		}

		protected void Blit(RenderGraphModule.RenderGraph renderGraph, UniversalResourceData frameData, int passIndex = 0)
		{
			using(var builder = renderGraph.AddRasterRenderPass<PassData>(typeName, out var passData))
			{
				passData.source = frameData.activeColorTexture;
				passData.material = blitMaterial;
				passData.passIndex = passIndex;

				builder.UseTexture(passData.source);

				TextureDesc desc = passData.source.GetDescriptor(renderGraph);
				desc.depthBufferBits = 0;
				desc.name = colorTextureName;
				TextureHandle destination = renderGraph.CreateTexture(desc);

				builder.SetRenderAttachment(destination, 0);
				//builder.AllowGlobalStateModification(true);

				builder.AllowPassCulling(false);
				builder.SetRenderFunc<PassData>(ExecuteRasterRenderPass);

				frameData.cameraColor = destination;
			}
		}

		protected virtual void ExecuteRasterRenderPass(PassData data, RasterGraphContext context)
		{
			Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), data.material, data.passIndex);
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
