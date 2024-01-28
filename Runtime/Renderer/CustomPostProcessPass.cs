using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEngine.Rendering.Universal.PostProcessing
{
	[System.Serializable]
	public class CustomPostProcessPass : ScriptableRenderPass
	{
		// Used to render from camera to post processings
		// back and forth, until we render the final image to
		// the camera
		RenderTargetIdentifier source;
		RenderTargetIdentifier destinationA;
		RenderTargetIdentifier destinationB;
		RenderTargetIdentifier latestDest;

		readonly int temporaryRTIdA = Shader.PropertyToID("_TempRT_A");
		readonly int temporaryRTIdB = Shader.PropertyToID("_TempRT_B");

		private readonly List<CustomPostProcessVolumeComponent> activeEffects = new List<CustomPostProcessVolumeComponent>();

		public CustomPostProcessPass(RenderPassEvent renderPassEvent)
		{
			this.renderPassEvent = renderPassEvent;
		}

		public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
		{
			// Grab the camera target descriptor. We will use this when creating a temporary render texture.
			RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
			descriptor.depthBufferBits = 0;

			var renderer = renderingData.cameraData.renderer;
			source = renderer.cameraColorTargetHandle;

			// Create a temporary render texture using the descriptor from above.
			cmd.GetTemporaryRT(temporaryRTIdA, descriptor, FilterMode.Bilinear);
			destinationA = new RenderTargetIdentifier(temporaryRTIdA);
			cmd.GetTemporaryRT(temporaryRTIdB, descriptor, FilterMode.Bilinear);
			destinationB = new RenderTargetIdentifier(temporaryRTIdB);

			var stack = VolumeManager.instance.stack;

			var requirements = ScriptableRenderPassInput.Color;

			activeEffects.Clear();
			foreach(var customEffect in EnumerateCustomEffects(stack))
			{
				bool shouldRender = customEffect.IsActive() && (renderingData.postProcessingEnabled || customEffect.IgnorePostProcessingFlag);
				// Only process if the effect is active & enabled
				if(shouldRender)
				{
					activeEffects.Add(customEffect);
					requirements |= customEffect.Requirements;
				}
			}

			ConfigureInput(requirements);
		}

		// The actual execution of the pass. This is where custom rendering occurs.
		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			if(activeEffects.Count == 0) return;

			CommandBuffer cmd = CommandBufferPool.Get("Custom Post Processing");
			cmd.Clear();

			latestDest = source;

			int count = activeEffects.Count;
			for(int i = 0; i < count; i++)
			{
				activeEffects[i].Setup(renderingData, out var material, out int pass);
				BlitTo(cmd, material, pass);
			}

			Blit(cmd, latestDest, source);

			context.ExecuteCommandBuffer(cmd);
			CommandBufferPool.Release(cmd);
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

		void BlitTo(CommandBuffer cmd, Material mat, int pass)
		{
			var first = latestDest;
			var last = first == destinationA ? destinationB : destinationA;
			Blit(cmd, first, last, mat, pass);

			latestDest = last;
		}

		//Cleans the temporary RTs when we don't need them anymore
		public override void OnCameraCleanup(CommandBuffer cmd)
		{
			cmd.ReleaseTemporaryRT(temporaryRTIdA);
			cmd.ReleaseTemporaryRT(temporaryRTIdB);
		}
	} 
}