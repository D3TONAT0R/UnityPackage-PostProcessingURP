using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal.PostProcessing.RenderGraph
{
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

		protected virtual void Execute(RGPostProcessingData data, RasterGraphContext context)
		{
			Blit(context, data.source, data.material);
		}

		protected void Blit(RasterGraphContext context, TextureHandle source, Material material, int passIndex = 0)
		{
			Blitter.BlitTexture(context.cmd, source, new Vector4(1, 1, 0, 0), material, passIndex);
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
}