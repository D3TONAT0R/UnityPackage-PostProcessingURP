using System.Collections.Generic;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal.PostProcessing
{
	[System.Serializable]
	public class CustomPostProcessPass : ScriptableRenderPass
	{
		class PassData
		{
			public CustomPostProcessPass pass;
			public RenderPassEvent passEvent;
			public TextureHandle destinationA;
			public TextureHandle destinationB;
			public List<CustomPostProcessVolumeComponent> activeEffects;
			public List<int> passIndices;
		}

		// Used to render from camera to post processings
		// back and forth, until we render the final image to
		// the camera
		private static RenderTextureDescriptor destinationDescriptor;
		private static RTHandle destinationAHandle;
		private static RTHandle destinationBHandle;

		private static readonly List<CustomPostProcessVolumeComponent> activeEffects = new List<CustomPostProcessVolumeComponent>();
		private static readonly List<int> passIndices = new List<int>();

		private readonly EffectOrderingList orderingListRef;

		public readonly CustomPostProcessRenderer renderer;

		public CustomPostProcessPass(CustomPostProcessRenderer renderer, RenderPassEvent renderPassEvent, EffectOrderingList ordering)
		{
			this.renderer = renderer;
			this.renderPassEvent = renderPassEvent;
			orderingListRef = ordering;
			destinationDescriptor = new RenderTextureDescriptor(Screen.width, Screen.height, RenderTextureFormat.Default, 0);
		}

		// This method adds and configures one or more render passes in the render graph.
		// This process includes declaring their inputs and outputs,
		// but does not include adding commands to command buffers.
		public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
		{
			string passName = "Custom Post Process Pass";

			// Add a raster render pass to the render graph. The PassData type parameter determines
			// the type of the passData output variable.
			using(var builder = renderGraph.AddRasterRenderPass<PassData>(passName,
				out var passData))
			{
				// UniversalResourceData contains all the texture references used by URP,
				// including the active color and depth textures of the camera.
				UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

				// Create a destination texture for the copy operation based on the settings,
				// such as dimensions, of the textures that the camera uses.
				// Set msaaSamples to 1 to get a non-multisampled destination texture.
				// Set depthBufferBits to 0 to ensure that the CreateRenderGraphTexture method
				// creates a color texture and not a depth texture.
				UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
				RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
				desc.msaaSamples = 1;
				desc.depthBufferBits = 0;

				// Populate passData with the data needed by the rendering function
				// of the render pass.
				// Use the camera's active color texture
				// as the source texture for the copy operation.
				passData.destinationA = resourceData.activeColorTexture;
				passData.destinationB = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "DestinationB", false);

				builder.SetRenderAttachment(passData.destinationA, 0, AccessFlags.ReadWrite);
				builder.SetRenderAttachment(passData.destinationB, 0, AccessFlags.ReadWrite);
				/*
				// For demonstrative purposes, this sample creates a temporary destination texture.
				// UniversalRenderer.CreateRenderGraphTexture is a helper method
				// that calls the RenderGraph.CreateTexture method.
				// Using a RenderTextureDescriptor instance instead of a TextureDesc instance
				// simplifies your code.
				TextureHandle destination =
					UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc,
						"CopyTexture", false);

				// Declare that this render pass uses the source texture as a read-only input.
				builder.UseTexture(passData.copySourceTexture);

				// Declare that this render pass uses the temporary destination texture
				// as its color render target.
				// This is similar to cmd.SetRenderTarget prior to the RenderGraph API.
				builder.SetRenderAttachment(destination, 0);

				// RenderGraph automatically determines that it can remove this render pass
				// because its results, which are stored in the temporary destination texture,
				// are not used by other passes.
				// For demonstrative purposes, this sample turns off this behavior to make sure
				// that render graph executes the render pass. 
				builder.AllowPassCulling(false);
				*/

				// Set the ExecutePass method as the rendering function that render graph calls
				// for the render pass. 
				// This sample uses a lambda expression to avoid memory allocations.
				builder.SetRenderFunc((PassData data, RasterGraphContext context)
					=> Execute(data, context));
			}
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
		private static void Execute(PassData data, RasterGraphContext context)
		{
			if(data.activeEffects.Count == 0) return;

			//Get a CommandBuffer from pool.
			CommandBuffer cmd = CommandBufferPool.Get("CustomPostProcess " + renderPassEvent);

			RTHandle cameraTargetHandle = data.destinationA;
			RTHandle lastTarget = cameraTargetHandle;

			int count = activeEffects.Count;
			for(int i = 0; i < count; i++)
			{
				RenderEffect(activeEffects[i], data, cmd, ref lastTarget);
			}

			// Blit from the last temporary render texture back to the camera target,
			Blit(cmd, lastTarget, cameraTargetHandle);

			//Execute the command buffer and release it back to the pool.
			context.ExecuteCommandBuffer(cmd);
			CommandBufferPool.Release(cmd);
		}

		private static void RenderEffect(CustomPostProcessVolumeComponent effect, PassData data, CommandBuffer cmd, ref RTHandle lastTarget)
		{
			passIndices.Clear();
			effect.Setup(data.pass, data, passIndices);
			int passCount = passIndices.Count;
			if(passCount == 0) Debug.LogWarning($"Effect does not have any passes set: " + effect.GetType());
			for(int j = 0; j < passCount; j++)
			{
				int passIndex = passIndices[j];
				var from = lastTarget;
				var to = lastTarget == destinationAHandle ? destinationBHandle : destinationAHandle;
				effect.Render(data.pass, data, cmd, from, to, passIndex);
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