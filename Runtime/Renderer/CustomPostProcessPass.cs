using System.Collections.Generic;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;

namespace UnityEngine.Rendering.Universal.PostProcessing
{
	public class CustomPostProcessPassData
	{
		public CustomPostProcessPass pass;
		public RenderPassEvent passEvent;
		public TextureHandle destinationA;
		public TextureHandle destinationB;
		public List<CustomPostProcessVolumeComponent> activeEffects;
		public List<int> passIndices;
	}

	public struct CustomPostProcessRenderContext
	{
		public readonly RenderGraph renderGraph;
		public readonly ContextContainer frameData;
		public readonly UniversalResourceData urpData;
		public readonly UniversalCameraData cameraData;

		public CustomPostProcessRenderContext(RenderGraph renderGraph, ContextContainer frameData)
		{
			this.renderGraph = renderGraph;
			this.frameData = frameData;
			urpData = frameData.Get<UniversalResourceData>();
			cameraData = frameData.Get<UniversalCameraData>();
		}
	}

	[System.Serializable]
	public class CustomPostProcessPass : ScriptableRenderPass
	{

		// Used to render from camera to post processings
		// back and forth, until we render the final image to
		// the camera
		private static readonly List<CustomPostProcessVolumeComponent> activeEffects = new List<CustomPostProcessVolumeComponent>();
		private static readonly List<int> passIndices = new List<int>();

		private readonly EffectOrderingList orderingListRef;

		public readonly CustomPostProcessRenderer renderer;

		public CustomPostProcessPass(CustomPostProcessRenderer renderer, RenderPassEvent renderPassEvent, EffectOrderingList ordering)
		{
			this.renderer = renderer;
			this.renderPassEvent = renderPassEvent;
			orderingListRef = ordering;
		}

		// This method adds and configures one or more render passes in the render graph.
		// This process includes declaring their inputs and outputs,
		// but does not include adding commands to command buffers.
		public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
		{
			var context = new CustomPostProcessRenderContext(renderGraph, frameData);
			Setup(context.urpData, context.cameraData);


			if(activeEffects.Count == 0) return;

			string passName = "Custom Post Process Pass";

			var cameraColor = context.urpData.activeColorTexture;
			var destinationA = cameraColor;
			var textureDescriptor = renderGraph.GetTextureDesc(destinationA);
			textureDescriptor.name = "DestinationB";
			var destinationB = renderGraph.CreateTexture(textureDescriptor);

			bool sourceB = false;
			int count = activeEffects.Count;
			for(int i = 0; i < count; i++)
			{
				RenderEffect(activeEffects[i], renderGraph, destinationA, destinationB, ref sourceB);
			}

			// Blit from the last temporary render texture back to the camera target
			if(sourceB)
			{
				renderGraph.AddCopyPass(destinationB, cameraColor);
			}
		}

		private void Setup(UniversalResourceData urpData, UniversalCameraData cameraData)
		{
			var stack = VolumeManager.instance.stack;

			var requirements = ScriptableRenderPassInput.Color;

			activeEffects.Clear();
			foreach(var customEffect in EnumerateCustomEffects(stack))
			{
				bool shouldRender = customEffect.IsActive() && (cameraData.postProcessEnabled || customEffect.IgnorePostProcessingFlag);
				if(cameraData.cameraType == CameraType.SceneView && !customEffect.VisibleInSceneView) shouldRender = false;
				// Only process if the effect is active & enabled
				if(shouldRender)
				{
					activeEffects.Add(customEffect);
					requirements |= customEffect.Requirements;
				}
			}
			OrderEffects();

			ConfigureInput(requirements);
		}

		public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
		{
			var stack = VolumeManager.instance.stack;

			var requirements = ScriptableRenderPassInput.Color;

			activeEffects.Clear();
			foreach(var customEffect in EnumerateCustomEffects(stack))
			{
				bool shouldRender = customEffect.IsActive() && (renderingData.postProcessingEnabled || customEffect.IgnorePostProcessingFlag);
				if(renderingData.cameraData.cameraType == CameraType.SceneView && !customEffect.VisibleInSceneView) shouldRender = false;
				// Only process if the effect is active & enabled
				if(shouldRender)
				{
					activeEffects.Add(customEffect);
					requirements |= customEffect.Requirements;
				}
			}
			OrderEffects();

			ConfigureInput(requirements);
		}

		private void OrderEffects()
		{
			for(int i = 0; i < activeEffects.Count; i++)
			{
				var type = activeEffects[i].GetType();
				orderingListRef.AddIfMissing(type);
			}
			activeEffects.Sort((x, y) => orderingListRef.GetPosition(x.GetType()) - orderingListRef.GetPosition(y.GetType()));
		}

		// The actual execution of the pass. This is where custom rendering occurs.
		//public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		private static void Execute(CustomPostProcessPassData data, RasterGraphContext context)
		{
			/*
			if(data.activeEffects.Count == 0) return;

			//Get a CommandBuffer from pool.
			//CommandBuffer cmd = CommandBufferPool.Get("CustomPostProcess " + data.passEvent);
			var cmd = context.cmd;

			var cameraTargetHandle = data.destinationA;
			var lastTarget = cameraTargetHandle;

			int count = activeEffects.Count;
			for(int i = 0; i < count; i++)
			{
				RenderEffect(activeEffects[i], , cmd, ref lastTarget);
			}

			// Blit from the last temporary render texture back to the camera target,
			Blitter.BlitTexture(cmd, lastTarget, cameraTargetHandle, (Material)null, 0);
			Blit(cmd, lastTarget, cameraTargetHandle);
			cmd.

			//Execute the command buffer and release it back to the pool.
			context.cmd.draw
			context.ExecuteCommandBuffer(cmd);
			CommandBufferPool.Release(cmd);
			*/
		}

		private void RenderEffect(CustomPostProcessVolumeComponent effect, CustomPostProcessRenderContext context, TextureHandle destinationA, TextureHandle destinationB, ref bool sourceB)
		{
			passIndices.Clear();
			effect.Setup(this, context, passIndices);
			int passCount = passIndices.Count;
			if(passCount == 0) Debug.LogWarning($"Effect does not have any passes set: " + effect.GetType());
			for(int j = 0; j < passCount; j++)
			{
				int passIndex = passIndices[j];
				var source = sourceB ? destinationB : destinationA;
				var destination = sourceB ? destinationA : destinationB;
				effect.Render(context, source, destination, passIndex);
				sourceB = !sourceB;
			}
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
					if((int)comp.PassEvent == (int)renderPassEvent)
					{
						yield return (CustomPostProcessVolumeComponent)kv.Value;
					}
				}
			}
		}

		public void Dispose()
		{
			//if(destinationAHandle != null) destinationAHandle.Release();
			//if(destinationBHandle != null) destinationBHandle.Release();
		}
	} 
}