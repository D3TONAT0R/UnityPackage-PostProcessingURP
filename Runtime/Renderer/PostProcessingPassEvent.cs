using System;

namespace UnityEngine.Rendering.Universal.PostProcessing
{
	public enum PostProcessingPassEvent
	{
		BeforeSkybox = RenderPassEvent.BeforeRenderingSkybox,
		BeforeTransparents = RenderPassEvent.BeforeRenderingTransparents,
		BeforePostProcessing = RenderPassEvent.BeforeRenderingPostProcessing,
		AfterPostProcessing = RenderPassEvent.AfterRenderingPostProcessing,
		AfterRendering = RenderPassEvent.AfterRendering
	}

	public static class PostProcessingPassEventExtensions
	{
		public static RenderPassEvent GetRenderPassEvent(this PostProcessingPassEvent passEvent)
		{
			switch(passEvent)
			{
				case PostProcessingPassEvent.BeforeSkybox:
					return RenderPassEvent.BeforeRenderingSkybox;
				case PostProcessingPassEvent.BeforeTransparents:
					return RenderPassEvent.BeforeRenderingTransparents;
				case PostProcessingPassEvent.BeforePostProcessing:
					return RenderPassEvent.BeforeRenderingPostProcessing;
				case PostProcessingPassEvent.AfterPostProcessing:
				case PostProcessingPassEvent.AfterRendering:
					//After rendering must be injected after post-processing otherwise it will not work correctly
					return RenderPassEvent.AfterRenderingPostProcessing;
				default:
					throw new ArgumentOutOfRangeException(nameof(passEvent), passEvent, null);
			}
		}
	}
}