using System.Collections.Generic;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal.PostProcessing.RenderGraph
{
	public class RGPostProcessingRenderer : ScriptableRendererFeature
	{
		private RGPostProcessingPass pass;

		public RGInvertColors invertColors;
		public RGVignette vignette;
		public RGColorFilter colorFilter;
		public RGTextureOverlay textureOverlay;

		public bool renderSimpleEffects = true;
		public List<RGPostEffectBase> simpleEffects;
		public List<CustomPostProcessVolumeComponent> volumeEffects;

		public override void Create()
		{
			pass = new RGPostProcessingPass();
			pass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
			if(renderSimpleEffects)
			{
				simpleEffects = new List<RGPostEffectBase> { invertColors, vignette, colorFilter, textureOverlay };
			}
			else
			{
				simpleEffects = null;
			}
			volumeEffects = new List<CustomPostProcessVolumeComponent>();
		}

		public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
		{
			pass.simpleEffects = simpleEffects;
			volumeEffects.Clear();
			var stack = VolumeManager.instance.stack;
			foreach(var customEffect in EnumerateCustomEffects(stack))
			{
				volumeEffects.Add(customEffect);
			}
			pass.volumeEffects = volumeEffects;
			renderer.EnqueuePass(pass);
		}

		//TODO: very inefficient when performed every frame
		private IEnumerable<CustomPostProcessVolumeComponent> EnumerateCustomEffects(VolumeStack stack)
		{
			var components = (Dictionary<System.Type, VolumeComponent>)typeof(VolumeStack)
				.GetField("components", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(stack);
			foreach(var kv in components)
			{
				if(typeof(CustomPostProcessVolumeComponent).IsAssignableFrom(kv.Key))
				{
					var comp = (CustomPostProcessVolumeComponent)kv.Value;
					//if((int)comp.PassEvent == (int)renderPassEvent)
					{
						yield return (CustomPostProcessVolumeComponent)kv.Value;
					}
				}
			}
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

		public List<RGPostEffectBase> simpleEffects;
		public List<CustomPostProcessVolumeComponent> volumeEffects;

		public override void RecordRenderGraph(RenderGraphModule.RenderGraph renderGraph, ContextContainer context)
		{
			UniversalResourceData frameData = context.Get<UniversalResourceData>();
			//renderGraph.BeginProfilingSampler(sampler);
			if(simpleEffects != null)
			{
				for(var i = 0; i < simpleEffects.Count; i++)
				{
					var effect = simpleEffects[i];
					effect.Render(renderGraph, frameData, context);
				}
			}
			if(volumeEffects != null)
			{
				for(var i = 0; i < volumeEffects.Count; i++)
				{
					var effect = volumeEffects[i];
					if(effect != null)
					{
						effect.Render(renderGraph, frameData, context);
					}
				}
			}	
			//renderGraph.EndProfilingSampler(sampler);
		}
	}
}
