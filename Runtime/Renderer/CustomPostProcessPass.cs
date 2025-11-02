using System;
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

		public PostProcessingPassEvent InjectionPoint { get; }

		private List<CustomPostProcessVolumeComponent> sortedEffectsList = new List<CustomPostProcessVolumeComponent>();
		private int lastSortingHash;

		public CustomPostProcessPass(CustomPostProcessRenderer renderFeature, PostProcessingPassEvent injectionPoint, EffectOrderingList orderingList)
		{
			RenderFeature = renderFeature;
			OrderingList = orderingList;
			InjectionPoint = injectionPoint;
			renderPassEvent = injectionPoint.GetRenderPassEvent();
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
				if(effect.InjectionPoint != InjectionPoint) continue;
				if(!sortedEffectsList.Contains(effect))
				{
					sortedEffectsList.Add(effect);
					listChanged = true;
				}
			}
			if(listChanged)
			{
				sortedEffectsList.Sort((a, b) => OrderingList.GetPosition(a.GetType()).CompareTo(OrderingList.GetPosition(b.GetType())));
				lastSortingHash = OrderingList.Hash;
			}

			foreach(var effect in sortedEffectsList)
			{
				if(effect.InjectionPoint != InjectionPoint) continue;
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