namespace UnityEngine.Rendering.Universal.PostProcessing
{
    internal sealed class LogHistogram
    {
        public const int rangeMin = -9; // ev
        public const int rangeMax = 9; // ev

        // Don't forget to update 'ExposureHistogram.hlsl' if you change these values !
        const int k_Bins = 128;

        public readonly ComputeBuffer data;

        public LogHistogram()
		{
            data = new ComputeBuffer(k_Bins, sizeof(uint));
        }

        public void Generate(CommandBuffer cmd, ref RenderingData renderingData, RTHandle source)
        {
            uint threadX, threadY, threadZ;
            var scaleOffsetRes = GetHistogramScaleOffsetRes(renderingData);
            var compute = PostProcessResources.Instance.computeShaders.exposureHistogram;
            cmd.BeginSample("LogHistogram");

            // Clear the buffer on every frame as we use it to accumulate luminance values on each frame
            int kernel = compute.FindKernel("KEyeHistogramClear");
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

            cmd.EndSample("LogHistogram");
        }

        public Vector4 GetHistogramScaleOffsetRes(RenderingData renderingData)
        {
            float diff = rangeMax - rangeMin;
            float scale = 1f / diff;
            float offset = -rangeMin * scale;
            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            return new Vector4(scale, offset, descriptor.width, descriptor.height);
        }

        public void Release()
        {
            if(data != null) data.Release();
        }
    }
}
