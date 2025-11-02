using System.Collections.Generic;
using System.Linq;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal.PostProcessing
{
	public class CustomPostProcessPass : ScriptableRenderPass
	{
		public class RGPostProcessingData
		{
			public TextureHandle source;
			public TextureHandle destination;
			public Material material;
		}

		public CustomPostProcessRenderer RenderFeature { get; }

		public EffectOrderingList OrderingList { get; }

		private List<CustomPostProcessVolumeComponent> sortedEffectsList = new List<CustomPostProcessVolumeComponent>();
		private int lastSortingHash;

		public CustomPostProcessPass(CustomPostProcessRenderer renderFeature, RenderPassEvent injectionPoint, EffectOrderingList orderingList)
		{
			RenderFeature = renderFeature;
			OrderingList = orderingList;
			renderPassEvent = injectionPoint;
		}

		private IEnumerable<CustomPostProcessVolumeComponent> GetSortedEffects()
		{
			return RenderFeature.volumeEffects.Where(e => e.RenderPassEvent == renderPassEvent).OrderBy(e => OrderingList.GetPosition(e.GetType()));
		}

		public override void RecordRenderGraph(RenderGraphModule.RenderGraph renderGraph, ContextContainer context)
		{
			UniversalResourceData frameData = context.Get<UniversalResourceData>();
			//renderGraph.BeginProfilingSampler(sampler);
			//Gather effects first
			bool listChanged = OrderingList.Hash != lastSortingHash;
			foreach(var effect in RenderFeature.volumeEffects)
			{
				if(effect.RenderPassEvent != renderPassEvent) continue;
				if(!sortedEffectsList.Contains(effect))
				{
					sortedEffectsList.Add(effect);
					listChanged = true;
				}
			}
			if(listChanged)
			{
				Debug.Log("hoi");
				sortedEffectsList.Sort((a, b) => OrderingList.GetPosition(a.GetType()).CompareTo(OrderingList.GetPosition(b.GetType())));
				lastSortingHash = OrderingList.Hash;
			}

			foreach(var effect in sortedEffectsList)
			{
				if(effect.RenderPassEvent != renderPassEvent) continue;
				try
				{
					effect.Render(this, renderGraph, frameData, context);
				}
				catch(System.Exception e)
				{
					Debug.LogException(e);
				}
			}
			//renderGraph.EndProfilingSampler(sampler);
		}
	}
}