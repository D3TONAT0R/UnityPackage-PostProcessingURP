using System;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal.PostProcessing
{
	/// <summary>
	/// Represents a volume parameter that holds a <see cref="PostProcessingPassEvent"/> value.
	/// </summary>
	/// <remarks>This parameter is used to define when a post-processing effect occurs within a rendering pipeline.</remarks>
	[System.Serializable]
	public class PassEventParameter : VolumeParameter<PostProcessingPassEvent>
	{
		public PassEventParameter(PostProcessingPassEvent value, bool overrideState = false) : base(value, overrideState)
		{

		}
	}

	/// <summary>
	/// The base class for custom post-processing volume components.
	/// </summary>
	public abstract class CustomPostProcessVolumeComponent : VolumeComponent, IPostProcessComponent
	{
		/// <summary>
		/// A container for data used during the rendering of a post-processing pass.
		/// </summary>
		public class PassData
		{
			public TextureHandle source;
			public Material material;
			public int passIndex;
		}

		//These Will be drawn manually with a custom editor

		/// <summary>
		/// Whether this post-processing effect is enabled.
		/// </summary>
		[HideInInspector]
		public BoolParameter enabled = new BoolParameter(false);
		//Only visible if SupportsBlending is true
		/// <summary>
		/// The blending factor for this effect, ranging from 0 to 1. Only applicable if <see cref="SupportsBlending"/> is true."/>
		/// </summary>
		[HideInInspector]
		public ClampedFloatParameter blend = new ClampedFloatParameter(0f, 0, 1, true);

		/// <summary>
		/// The name of the primary shader used for this post-processing effect.
		/// </summary>
		public abstract string ShaderName { get; }

		/// <summary>
		/// The injection point for this post-processing effect within the rendering pipeline.
		/// </summary>
		public abstract PostProcessingPassEvent InjectionPoint { get; }

		/// <summary>
		/// If true, the effect will be rendered even if post-processing is disabled for the camera.
		/// </summary>
		public virtual bool IgnorePostProcessingFlag => false;

		/// <summary>
		/// If true, the effect supports blending and will use the blend parameter.
		/// </summary>
		public virtual bool SupportsBlending => false;

		/// <summary>
		/// If true, the effect will be rendered in the scene view. To render all effects in the scene view regardless of this setting,
		/// use the scene view "Post Processing" debug view.
		/// </summary>
		public virtual bool VisibleInSceneView => true;

		//TODO: check requirements and request depth / normals if needed
		/// <summary>
		/// The input requirements for this post-processing effect.
		/// </summary>
		public virtual ScriptableRenderPassInput Requirements => ScriptableRenderPassInput.Color;

		/// <summary>
		/// The render pass event corresponding to the injection point of this effect.
		/// </summary>
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

		/// <inheritdoc/>
		public virtual bool IsActive() {
			if(!enabled.value)
				return false;
			if(SupportsBlending)
				return blend.value > 0;
			return true;
		}

		/// <inheritdoc/>
		public virtual bool IsTileCompatible() => true;

		/// <summary>
		/// Called before the effect is rendered for the first time.
		/// </summary>
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

		/// <summary>
		/// Renders the post-processing effect, if enabled in the given context.
		/// </summary>
		public void Render(CustomPostProcessPass pass, RenderGraphModule.RenderGraph renderGraph,
			UniversalResourceData frameData, ContextContainer context)
		{
			if(!BeginRender(pass, context)) return;
#if UNITY_EDITOR
			if(blitMaterial && blitMaterial.shader != null) pass.RenderFeature.ReferenceShader(blitMaterial.shader);
#endif
			RenderEffect(pass, renderGraph, frameData, context);
		}

		/// <summary>
		/// Performs the actual rendering of the effect. Unless overridden, it performs a blit using the effect's material.
		/// </summary>
		protected virtual void RenderEffect(CustomPostProcessPass pass, RenderGraphModule.RenderGraph renderGraph,
			UniversalResourceData frameData, ContextContainer context)
		{
			Blit(renderGraph, frameData);
		}

		/// <summary>
		/// Initializes the effect for rendering and checks if it should be rendered based on the camera context.
		/// </summary>
		protected bool BeginRender(CustomPostProcessPass pass, ContextContainer context)
		{
			var data = context.Get<UniversalCameraData>();
			if(!CheckEffectVisibilityForCamera(data))
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

		/// <summary>
		/// Sets material properties for the effect's material.
		/// </summary>
		public virtual void SetMaterialProperties(Material mat)
		{

		}

		private bool CheckEffectVisibilityForCamera(UniversalCameraData data)
		{
			if(data == null) return true;
#if UNITY_EDITOR
			if(data.isSceneViewCamera)
			{
				if(UnityEditor.SceneView.currentDrawingSceneView.cameraMode.name == "Post Processing")
				{
					//Always draw effect in scene view when the view mode is set to "Post Processing"
					//TODO: scene cameras in custom draw modes are always with HDR disabled
					return true;
				}
				if(data.isSceneViewCamera && !VisibleInSceneView) return false;
			}
#endif
			if(!IgnorePostProcessingFlag && !data.postProcessEnabled)
			{
				return false;
			}
			return true;
		}

		private void InitializeBlitMaterial()
		{
			float blendValue = SupportsBlending ? blend.value : 1.0f;
			blitMaterial.SetFloat(blendPropertyId, blendValue);
			SetMaterialProperties(blitMaterial);
		}

		/// <summary>
		/// Performs a blit operation using the effect's material.
		/// </summary>
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

		/// <summary>
		/// The execution function for the raster render pass that performs the blit operation.
		/// </summary>
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
