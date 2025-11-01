using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal.PostProcessing
{
	internal sealed class LogHistogram
	{
		class PassData
		{
			public TextureHandle cameraSource;
			public ComputeShader computeShader;
			public int kernel;
			public Vector4 scaleOffsetRes;
			public Vector3Int threads;
			public ComputeBuffer buffer;
		}

		public const int rangeMin = -9; // ev
		public const int rangeMax = 9; // ev

		// Don't forget to update 'ExposureHistogram.hlsl' if you change these values !
		const int k_Bins = 128;

		public readonly ComputeBuffer buffer;

		public LogHistogram()
		{
			buffer = new ComputeBuffer(k_Bins, sizeof(uint));
		}

		public void Generate(RenderGraphModule.RenderGraph renderGraph, UniversalResourceData frameData)
		{
			var scaleOffsetRes = GetHistogramScaleOffsetRes(renderGraph, frameData);
			var compute = PostProcessResources.Instance.computeShaders.exposureHistogram;

			// Clear the buffer on every frame as we use it to accumulate luminance values on each frame
			int kernel = compute.FindKernel("KEyeHistogramClear");
			compute.GetKernelThreadGroupSizes(kernel, out var threadX, out var threadY, out var threadZ);

			using(var builder = renderGraph.AddComputePass<PassData>("Histogram", out var d))
			{
				d.cameraSource = frameData.activeColorTexture;
				d.computeShader = compute;
				d.kernel = kernel;
				d.scaleOffsetRes = scaleOffsetRes;
				d.threads = new Vector3Int((int)threadX, (int)threadY, (int)threadZ);
				d.buffer = buffer;
				builder.UseTexture(frameData.activeColorTexture);
				builder.AllowPassCulling(false);
				builder.SetRenderFunc<PassData>(ExecuteHistogramGen);
			}
			/*
			cmd.SetComputeBufferParam(compute, kernel, "_HistogramBuffer", data);
			compute.GetKernelThreadGroupSizes(kernel, out threadX, out threadY, out threadZ);
			cmd.DispatchCompute(compute, kernel, Mathf.CeilToInt(k_Bins / (float)threadX), 1, 1);

			// Get a log histogram
			kernel = compute.FindKernel("KEyeHistogram");
			cmd.SetComputeBufferParam(compute, kernel, "_HistogramBuffer", data);
			cmd.SetComputeTextureParam(compute, kernel, "_Source", source);
			cmd.SetComputeVectorParam(compute, "_ScaleOffsetRes", scaleOffsetRes);

			compute.GetKernelThreadGroupSizes(kernel, out threadX, out threadY, out threadZ);
			cmd.DispatchCompute(compute, kernel,
				Mathf.CeilToInt(scaleOffsetRes.z / 2f / threadX),
				Mathf.CeilToInt(scaleOffsetRes.w / 2f / threadY),
				1
			);
			*/
		}

		private static void ExecuteHistogramGen(PassData data, ComputeGraphContext ctx)
		{
			ctx.cmd.SetComputeBufferParam(data.computeShader, data.kernel, "_HistogramBuffer", data.buffer);
			ctx.cmd.DispatchCompute(data.computeShader, data.kernel, Mathf.CeilToInt(k_Bins / (float)data.threads.x), 1, 1);

			// Get a log histogram
			data.kernel = data.computeShader.FindKernel("KEyeHistogram");
			ctx.cmd.SetComputeBufferParam(data.computeShader, data.kernel, "_HistogramBuffer", data.buffer);
			ctx.cmd.SetComputeTextureParam(data.computeShader, data.kernel, "_Source", data.cameraSource);
			ctx.cmd.SetComputeVectorParam(data.computeShader, "_ScaleOffsetRes", data.scaleOffsetRes);

			ctx.cmd.DispatchCompute(data.computeShader, data.kernel, Mathf.CeilToInt(data.scaleOffsetRes.z / 2f / data.threads.x), Mathf.CeilToInt(data.scaleOffsetRes.w / 2f / data.threads.y), 1);
		}

		public Vector4 GetHistogramScaleOffsetRes(RenderGraphModule.RenderGraph renderGraph, UniversalResourceData frameData)
		{
			float diff = rangeMax - rangeMin;
			float scale = 1f / diff;
			float offset = -rangeMin * scale;
			var descriptor = frameData.cameraColor.GetDescriptor(renderGraph);
			return new Vector4(scale, offset, descriptor.width, descriptor.height);
		}

		public void Release()
		{
			if(buffer != null) buffer.Release();
		}
	}
}
