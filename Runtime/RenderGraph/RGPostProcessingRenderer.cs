using System.Collections.Generic;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal.PostProcessing.RenderGraph
{
	public class RGPostProcessingRenderer : ScriptableRendererFeature
	{
		private RGPostProcessingPass beforeSkyboxPass;
		private RGPostProcessingPass beforeTransparentsPass;
		private RGPostProcessingPass beforePostPass;
		private RGPostProcessingPass afterPostPass;
		private RGPostProcessingPass afterRenderingPass;

		public bool renderVolumeEffects = true;
		public List<CustomPostProcessVolumeComponent> volumeEffects;

		public override void Create()
		{
			beforeSkyboxPass = new RGPostProcessingPass(this, RenderPassEvent.BeforeRenderingSkybox);
			beforeTransparentsPass = new RGPostProcessingPass(this, RenderPassEvent.BeforeRenderingTransparents);
			beforePostPass = new RGPostProcessingPass(this, RenderPassEvent.BeforeRenderingPostProcessing);
			afterPostPass = new RGPostProcessingPass(this, RenderPassEvent.AfterRenderingPostProcessing);
			afterRenderingPass = new RGPostProcessingPass(this, RenderPassEvent.AfterRendering);
			volumeEffects = new List<CustomPostProcessVolumeComponent>();
		}

		public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
		{
			volumeEffects.Clear();
			if(renderVolumeEffects)
			{
				var stack = VolumeManager.instance.stack;
				foreach(var customEffect in EnumerateCustomEffects(stack))
				{
					volumeEffects.Add(customEffect);
				}
			}
			renderer.EnqueuePass(beforePostPass);
			renderer.EnqueuePass(afterPostPass);
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

		private readonly RGPostProcessingRenderer renderer;

		public RGPostProcessingPass(RGPostProcessingRenderer renderer, RenderPassEvent injectionPoint)
		{
			this.renderer = renderer;
			renderPassEvent = injectionPoint;
		}

		public override void RecordRenderGraph(RenderGraphModule.RenderGraph renderGraph, ContextContainer context)
		{
			UniversalResourceData frameData = context.Get<UniversalResourceData>();
			//renderGraph.BeginProfilingSampler(sampler);
			if(renderer.volumeEffects != null)
			{
				for(var i = 0; i < renderer.volumeEffects.Count; i++)
				{
					var effect = renderer.volumeEffects[i];
					if(effect != null && effect.RenderPassEvent == renderPassEvent)
					{
						effect.Render(renderGraph, frameData, context);
					}
				}
			}	
			//renderGraph.EndProfilingSampler(sampler);
		}
	}
}
