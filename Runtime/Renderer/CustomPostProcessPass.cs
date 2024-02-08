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
		private RenderTextureDescriptor destinationDescriptor;
		private RTHandle destinationAHandle;
		private RTHandle destinationBHandle;

		private readonly List<CustomPostProcessVolumeComponent> activeEffects = new List<CustomPostProcessVolumeComponent>();

		public CustomPostProcessPass(RenderPassEvent renderPassEvent)
		{
			this.renderPassEvent = renderPassEvent;
			destinationDescriptor = new RenderTextureDescriptor(Screen.width, Screen.height, RenderTextureFormat.Default, 0);
		}

		public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
		{
			// Set the texture size to be the same as the camera target size.
			destinationDescriptor.width = cameraTextureDescriptor.width;
			destinationDescriptor.height = cameraTextureDescriptor.height;

			// Check if the descriptor has changed, and reallocate the RTHandle if necessary
			RenderingUtils.ReAllocateIfNeeded(ref destinationAHandle, destinationDescriptor);
			RenderingUtils.ReAllocateIfNeeded(ref destinationBHandle, destinationDescriptor);
		}

		public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
		{
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

			//Get a CommandBuffer from pool.
			CommandBuffer cmd = CommandBufferPool.Get("Custom Post Processing");

			RTHandle cameraTargetHandle = renderingData.cameraData.renderer.cameraColorTargetHandle;
			RTHandle lastTarget = cameraTargetHandle;

			int count = activeEffects.Count;
			for(int i = 0; i < count; i++)
			{
				activeEffects[i].Setup(renderingData, out var material, out int pass);
				var from = lastTarget;
				var to = lastTarget == destinationAHandle ? destinationBHandle : destinationAHandle;
				lastTarget = to;
				material.SetTexture("_MainTex", from);
				Blit(cmd, from, to, material, 0);
			}

			// Blit from the last temporary render texture back to the camera target,
			Blit(cmd, lastTarget, cameraTargetHandle);

			//Execute the command buffer and release it back to the pool.
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

		public void Dispose()
		{
			if(destinationAHandle != null) destinationAHandle.Release();
			if(destinationBHandle != null) destinationBHandle.Release();
		}
	} 
}