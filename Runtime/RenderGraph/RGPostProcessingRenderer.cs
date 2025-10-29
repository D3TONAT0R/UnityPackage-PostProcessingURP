using System.Collections.Generic;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal.PostProcessing.RenderGraph
{
	#region Effects
	public abstract class RGPostEffectBase
	{
		public class RGPostProcessingData
		{
			public TextureHandle source;
			public Material material;
		}

		[Range(0.0f, 1.0f)]
		public float blend = 1.0f;
		public bool renderInSceneView = false;

		public Material Material { get; protected set; }

		protected abstract string ShaderName { get; }

		protected bool initialized = false;
		protected bool missingShader = false;

		public virtual void SetMaterialProperties(Material mat)
		{
			mat.SetFloat("_Blend", blend);
		}

		public virtual void Render(RenderGraphModule.RenderGraph renderGraph, UniversalResourceData frameData, ContextContainer context)
		{
			var data = context.Get<UniversalCameraData>();
			if(data != null && (data.isSceneViewCamera && !renderInSceneView) || !data.postProcessEnabled) return;
			if(blend <= 0.0f)
				return;
			if(!initialized)
			{
				var shader = Shader.Find(ShaderName);
				if(shader == null)
				{
					Debug.LogError($"Can't find shader for effect {GetType().Name}: '{ShaderName}'");
					shader = Shader.Find("Hidden/InternalErrorShader");
					missingShader = true;
				}
				Material = new Material(shader);
				initialized = true;
			}
			if(missingShader)
			{
				Debug.LogWarning("Shader missing");
				return;
			}
			SetMaterialProperties(Material);
			using(var builder = renderGraph.AddRasterRenderPass<RGPostProcessingData>(GetType().Name, out var passData))
			{
				passData.source = frameData.activeColorTexture;
				passData.material = Material;

				builder.UseTexture(passData.source);

				TextureDesc desc = frameData.activeColorTexture.GetDescriptor(renderGraph);
				desc.depthBufferBits = 0;
				TextureHandle destination = renderGraph.CreateTexture(desc);

				builder.SetRenderAttachment(destination, 0);
				builder.AllowGlobalStateModification(true);

				builder.SetRenderFunc<RGPostProcessingData>(Execute);

				frameData.cameraColor = destination;
			}
		}

		private static void Execute(RGPostProcessingData data, RasterGraphContext context)
		{
			Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), data.material, 0);
		}
	}

	[System.Serializable]
	public class RGInvertColors : RGPostEffectBase
	{
		protected override string ShaderName => "Hidden/RGInvertColors";
	}

	[System.Serializable]
	public class RGVignette : RGPostEffectBase
	{
		protected override string ShaderName => "Hidden/RGVignette";
	}

	[System.Serializable]
	public class RGColorFilter : RGPostEffectBase
	{
		protected override string ShaderName => "Hidden/RGColorFilter";

		public Color color = Color.white;

		public override void SetMaterialProperties(Material mat)
		{
			base.SetMaterialProperties(mat);
			mat.SetColor("_Color", color);
		}
	}

	[System.Serializable]
	public class RGTextureOverlay : RGPostEffectBase
	{
		public Texture2D overlayTexture;

		protected override string ShaderName => "Hidden/RGTextureOverlay";

		public override void SetMaterialProperties(Material mat)
		{
			base.SetMaterialProperties(mat);
			mat.SetTexture("_OverlayTex", overlayTexture);
		}
	}
	#endregion


	public class RGPostProcessingRenderer : ScriptableRendererFeature
	{
		private RGPostProcessingPass pass;

		public RGInvertColors invertColors;
		public RGVignette vignette;
		public RGColorFilter colorFilter;
		public RGTextureOverlay textureOverlay;

		public List<RGPostEffectBase> effects;

		public override void Create()
		{
			pass = new RGPostProcessingPass();
			pass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
			effects = new List<RGPostEffectBase> { invertColors, vignette, colorFilter, textureOverlay };
		}

		public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
		{
			pass.effects = effects;
			renderer.EnqueuePass(pass);
		}
	}

	public class RGPostProcessingPass : ScriptableRenderPass
	{
		public class RGPostProcessingData
		{
			public TextureHandle source;
			public TextureHandle destination;
			public Material material;
		}

		public List<RGPostEffectBase> effects;

		public override void RecordRenderGraph(RenderGraphModule.RenderGraph renderGraph, ContextContainer context)
		{
			UniversalResourceData frameData = context.Get<UniversalResourceData>();
			if(effects == null || effects.Count == 0)
				return;
			//renderGraph.BeginProfilingSampler(sampler);
			for(var i = 0; i < effects.Count; i++)
			{
				var effect = effects[i];
				effect.Render(renderGraph, frameData, context);
			}
			//renderGraph.EndProfilingSampler(sampler);
		}
	}
}
