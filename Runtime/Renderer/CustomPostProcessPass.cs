using System.Collections.Generic;

namespace UnityEngine.Rendering.Universal.PostProcessing
{
	[System.Serializable]
	public class CustomPostProcessPass : ScriptableRenderPass
	{
		// Used to render from camera to post processings
		// back and forth, until we render the final image to
		// the camera
		private static RenderTextureDescriptor destinationDescriptor;
		private static RTHandle destinationAHandle;
		private static RTHandle destinationBHandle;

		private readonly List<CustomPostProcessVolumeComponent> activeEffects = new List<CustomPostProcessVolumeComponent>();
		private readonly List<int> passIndices = new List<int>();

		private readonly EffectOrderingList orderingListRef;

		public readonly CustomPostProcessRenderer renderer;

		public CustomPostProcessPass(CustomPostProcessRenderer renderer, RenderPassEvent renderPassEvent, EffectOrderingList ordering)
		{
			this.renderer = renderer;
			this.renderPassEvent = renderPassEvent;
			orderingListRef = ordering;
			destinationDescriptor = new RenderTextureDescriptor(Screen.width, Screen.height, RenderTextureFormat.Default, 0);
		}

		public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
		{
			// Set the texture size to be the same as the camera target size.
			destinationDescriptor.width = cameraTextureDescriptor.width;
			destinationDescriptor.height = cameraTextureDescriptor.height;
			destinationDescriptor.colorFormat = cameraTextureDescriptor.colorFormat;

			// Check if the descriptor has changed, and reallocate the RTHandle if necessary
			RenderingUtils.ReAllocateIfNeeded(ref destinationAHandle, destinationDescriptor, name: "Temp_A");
			RenderingUtils.ReAllocateIfNeeded(ref destinationBHandle, destinationDescriptor, name: "Temp_B");
		}

		public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
		{
			var stack = VolumeManager.instance.stack;

			var requirements = ScriptableRenderPassInput.Color;

			activeEffects.Clear();
			foreach(var customEffect in EnumerateCustomEffects(stack))
			{
				bool shouldRender = customEffect.IsActive() && (renderingData.postProcessingEnabled || customEffect.IgnorePostProcessingFlag);
				if(renderingData.cameraData.cameraType == CameraType.SceneView && !customEffect.EnabledInSceneView) shouldRender = false;
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
		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			if(activeEffects.Count == 0) return;

			//Get a CommandBuffer from pool.
			CommandBuffer cmd = CommandBufferPool.Get("CustomPostProcess "+renderPassEvent);

			RTHandle cameraTargetHandle = renderingData.cameraData.renderer.cameraColorTargetHandle;
			RTHandle lastTarget = cameraTargetHandle;

			int count = activeEffects.Count;
			for(int i = 0; i < count; i++)
			{
				RenderEffect(activeEffects[i], renderingData, cmd, ref lastTarget);
			}

			// Blit from the last temporary render texture back to the camera target,
			Blit(cmd, lastTarget, cameraTargetHandle);

			//Execute the command buffer and release it back to the pool.
			context.ExecuteCommandBuffer(cmd);
			CommandBufferPool.Release(cmd);
		}

		private void RenderEffect(CustomPostProcessVolumeComponent effect, RenderingData renderingData, CommandBuffer cmd, ref RTHandle lastTarget)
		{
			passIndices.Clear();
			effect.Setup(this, renderingData, passIndices);
			int passCount = passIndices.Count;
			if(passCount == 0) Debug.LogWarning($"Effect does not have any passes set: " + effect.GetType());
			for(int j = 0; j < passCount; j++)
			{
				int passIndex = passIndices[j];
				var from = lastTarget;
				var to = lastTarget == destinationAHandle ? destinationBHandle : destinationAHandle;
				effect.Render(this, renderingData, cmd, from, to, passIndex);
				lastTarget = to;
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
			if(destinationAHandle != null) destinationAHandle.Release();
			if(destinationBHandle != null) destinationBHandle.Release();
		}
	} 
}