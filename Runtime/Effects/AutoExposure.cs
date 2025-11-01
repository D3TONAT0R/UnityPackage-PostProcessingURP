using System;
using System.Collections.Generic;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal.PostProcessing
{
	/// <summary>
	/// Eye adaptation modes.
	/// </summary>
	public enum EyeAdaptation
	{
		/// <summary>
		/// Progressive (smooth) eye adaptation.
		/// </summary>
		Progressive,

		/// <summary>
		/// Fixed (instant) eye adaptation.
		/// </summary>
		Fixed
	}

	/// <summary>
	/// A volume parameter holding a <see cref="EyeAdaptation"/> value.
	/// </summary>
	[Serializable]
	public sealed class EyeAdaptationParameter : VolumeParameter<EyeAdaptation> { }

	/// <summary>
	/// This class holds settings for the Auto Exposure effect.
	/// </summary>
	[Serializable]
	[VolumeComponentMenu("Post-processing/Auto Exposure")]
	public class AutoExposure : CustomPostProcessVolumeComponent
	{
		private class PerCameraData
		{
			const int k_NumEyes = 2;
			const int k_NumAutoExposureTextures = 2;

			public LogHistogram logHistogram = new LogHistogram();

			public readonly RTHandle[][] autoExposurePool = new RTHandle[k_NumEyes][];
			public int[] autoExposurePingPong = new int[k_NumEyes];

			public ulong frameNumber = 0;
			public float lastUseTimestamp;

			public PerCameraData()
			{
				for(int eye = 0; eye < k_NumEyes; eye++)
				{
					autoExposurePool[eye] = new RTHandle[k_NumAutoExposureTextures];
					autoExposurePingPong[eye] = 0;
				}
			}

			public void UpdateCounter()
			{
				frameNumber++;
				lastUseTimestamp = Time.realtimeSinceStartup;
			}

			public void CheckTextures(int eye)
			{
				CheckTexture(eye, 0);
				CheckTexture(eye, 1);
			}

			void CheckTexture(int eye, int id)
			{
				if(autoExposurePool[eye][id] == null)
				{
					var descriptor = new RenderTextureDescriptor(1, 1, RenderTextureFormat.RFloat, 0)
					{
						enableRandomWrite = true,
						useMipMap = false,
						autoGenerateMips = false,
						sRGB = false,
						msaaSamples = 1,
					};
					autoExposurePool[eye][id] = RTHandles.Alloc(descriptor);
				}
			}

			public void Release()
			{
				if(logHistogram != null) logHistogram.Release();
				foreach(var rtEyeSet in autoExposurePool)
				{
					foreach(var rt in rtEyeSet)
					{
#if UNITY_EDITOR
						if(Application.isPlaying)
							Destroy(rt);
						else
							DestroyImmediate(rt);
#else
						Destroy(rt);
#endif
					}
				}
			}
		}

		/// <summary>
		/// These values are the lower and upper percentages of the histogram that will be used to
		/// find a stable average luminance. Values outside of this range will be discarded and wont
		/// contribute to the average luminance.
		/// </summary>
		[Tooltip("Filters the bright and dark parts of the histogram when computing the average luminance. This is to avoid very dark pixels and very bright pixels from contributing to the auto exposure. Unit is in percent.")]
		public FloatRangeParameter filteringPercent = new FloatRangeParameter(new Vector2(50, 95), 0, 99);

		/// <summary>
		/// Minimum average luminance to consider for auto exposure (in EV).
		/// </summary>
		[Tooltip("Minimum average luminance to consider for auto exposure. Unit is EV.")]
		public MinMaxFloatParameter minimumEV = new MinMaxFloatParameter(0, LogHistogram.rangeMin, LogHistogram.rangeMax);

		/// <summary>
		/// Maximum average luminance to consider for auto exposure (in EV).
		/// </summary>
		[Tooltip("Maximum average luminance to consider for auto exposure. Unit is EV.")]
		public MinMaxFloatParameter maximumEV = new MinMaxFloatParameter(0, -9, 9);

		/// <summary>
		/// Middle-grey value. Use this to compensate the global exposure of the scene.
		/// </summary>
		[Min(0f), Tooltip("Use this to scale the global exposure of the scene.")]
		public FloatParameter exposureCompensation = new FloatParameter(1);

		/// <summary>
		/// The type of eye adaptation to use.
		/// </summary>
		[Tooltip("Use \"Progressive\" if you want auto exposure to be animated. Use \"Fixed\" otherwise.")]
		public EyeAdaptationParameter type = new EyeAdaptationParameter { value = EyeAdaptation.Progressive };

		/// <summary>
		/// The adaptation speed from a dark to a light environment.
		/// </summary>
		[Min(0f), Tooltip("Adaptation speed from a dark to a light environment.")]
		public FloatParameter speedUp = new FloatParameter(2);

		/// <summary>
		/// The adaptation speed from a light to a dark environment.
		/// </summary>
		[Min(0f), Tooltip("Adaptation speed from a light to a dark environment.")]
		public FloatParameter speedDown = new FloatParameter(1);

		private readonly Dictionary<UniversalCameraData, PerCameraData> perCameraDatas = new Dictionary<UniversalCameraData, PerCameraData>();

		public override string ShaderName => "Hidden/PostProcessing/AutoExposureBlit";

		public override PostProcessingPassEvent PassEvent => PostProcessingPassEvent.BeforePostProcessing;

		public override bool IsActive()
		{
			return base.IsActive()
				&& SystemInfo.supportsComputeShaders
				&& !(Application.platform == RuntimePlatform.Android && SystemInfo.graphicsDeviceType != GraphicsDeviceType.Vulkan)
				&& SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RFloat);
		}

		public override void AddPasses(List<int> passes)
		{
			if(PostProcessResources.Instance.computeShaders.autoExposure) passes.Add(0);
		}

		public override void Render(RenderGraphModule.RenderGraph renderGraph, UniversalResourceData frameData, ContextContainer context)
		{
			if(blend.value <= 0.0f) return;
			var urpAdditionalData = context.Get<UniversalCameraData>();
			var perCameraData = GetPerCameraData(urpAdditionalData);

			perCameraData.logHistogram.Generate(renderGraph, frameData);

			PerformLookup(renderGraph, frameData, context, perCameraData);

			/*
			blitMaterial.SetTexture("_AutoExposureTex", currentAutoExposure);
			feature.Blit(cmd, source, destination, blitMaterial, 0);
			*/
		}

		class AutoExposurePassData
		{
			public int kernel;
			public bool firstFrame;
			public PerCameraData perCameraData;
			public TextureHandle autoExposureSrc;
			public TextureHandle autoExposureDst;
			public float lowPercent;
			public float highPercent;
			public float minimumEV;
			public float maximumEV;
			public float speedUp;
			public float speedDown;
			public float exposureCompensation;
			public Vector4 scaleOffsetRes;
		}

		private void PerformLookup(RenderGraphModule.RenderGraph renderGraph, UniversalResourceData frameData, ContextContainer context, PerCameraData perCameraData)
		{
			var computeShader = PostProcessResources.Instance.computeShaders.autoExposure;
			int xrActiveEye = 0;
			// Prepare autoExpo texture pool
			perCameraData.CheckTextures(xrActiveEye);

			// Make sure filtering values are correct to avoid apocalyptic consequences
			float lowPercent = filteringPercent.value.x;
			float highPercent = filteringPercent.value.y;
			const float kMinDelta = 1e-2f;
			highPercent = Mathf.Clamp(highPercent, 1f + kMinDelta, 99f);
			lowPercent = Mathf.Clamp(lowPercent, 1f, highPercent - kMinDelta);

			// Clamp min/max adaptation values as well
			float minLum = minimumEV.value;
			float maxLum = maximumEV.value;
			minimumEV.value = Mathf.Min(minLum, maxLum);
			maximumEV.value = Mathf.Max(minLum, maxLum);

			// Compute average luminance & auto exposure
			bool resetHistory = context.Get<UniversalCameraData>().camera.GetUniversalAdditionalCameraData().resetHistory;
			bool firstFrame = resetHistory || perCameraData.frameNumber == 0 || !Application.isPlaying || PostProcessResources.Instance.TryGetCustomResource<Texture2D>("mytexture", out _);

			string adaptation;
			if(firstFrame || type.value == EyeAdaptation.Fixed)
				adaptation = "KAutoExposureAvgLuminance_fixed";
			else
				adaptation = "KAutoExposureAvgLuminance_progressive";

			using(var builder = renderGraph.AddComputePass<AutoExposurePassData>("Auto Exposure Lookup", out var data)) {
				data.perCameraData = perCameraData;
				data.kernel = computeShader.FindKernel(adaptation);
				data.lowPercent = lowPercent;
				data.highPercent = highPercent;
				data.minimumEV = minimumEV.value;
				data.maximumEV = maximumEV.value;
				data.scaleOffsetRes = perCameraData.logHistogram.GetHistogramScaleOffsetRes(renderGraph, frameData);
				builder.AllowGlobalStateModification(true);
				if(firstFrame)
				{
					data.autoExposureDst = renderGraph.ImportTexture(perCameraData.autoExposurePool[xrActiveEye][0]);
					builder.UseTexture(data.autoExposureDst, AccessFlags.ReadWrite);
					data.firstFrame = true;
				}
				else
				{
					int pp = perCameraData.autoExposurePingPong[xrActiveEye];
					var src = perCameraData.autoExposurePool[xrActiveEye][++pp % 2];
					var dst = perCameraData.autoExposurePool[xrActiveEye][++pp % 2];

					data.autoExposureSrc = renderGraph.ImportTexture(src);
					data.autoExposureDst = renderGraph.ImportTexture(dst);
					builder.UseTexture(data.autoExposureSrc, AccessFlags.ReadWrite);
					builder.UseTexture(data.autoExposureDst, AccessFlags.ReadWrite);

					perCameraData.autoExposurePingPong[xrActiveEye] = ++pp % 2;
				}
				builder.SetRenderFunc<AutoExposurePassData>(ExecuteLookup);

				RenderTexture currentAutoExposure;

				
			}

			perCameraData.UpdateCounter();
		}

		private static void ExecuteLookup(AutoExposurePassData data, ComputeGraphContext ctx)
		{
			var cmd = ctx.cmd;
			var computeShader = PostProcessResources.Instance.computeShaders.autoExposure;
			cmd.SetComputeBufferParam(computeShader, data.kernel, "_HistogramBuffer", data.perCameraData.logHistogram.data);
			cmd.SetComputeVectorParam(computeShader, "_Params1", new Vector4(data.lowPercent * 0.01f, data.highPercent * 0.01f, Exp2(data.minimumEV), Exp2(data.maximumEV)));
			cmd.SetComputeVectorParam(computeShader, "_Params2", new Vector4(data.speedDown, data.speedUp, data.exposureCompensation, Time.deltaTime));
			cmd.SetComputeVectorParam(computeShader, "_ScaleOffsetRes", data.scaleOffsetRes);

			if(data.firstFrame)
			{
				// We don't want eye adaptation when not in play mode because the GameView isn't
				// animated, thus making it harder to tweak. Just use the final auto exposure value.
				cmd.SetComputeTextureParam(computeShader, data.kernel, "_Destination", data.autoExposureDst);
				cmd.DispatchCompute(computeShader, data.kernel, 1, 1, 1);

				// Copy current exposure to the other pingpong target to avoid adapting from black
				//cmd.CopyTexture(m_AutoExposurePool[xrActiveEye][0], m_AutoExposurePool[xrActiveEye][1]);
				//cmd.CopyTexture(perCameraData.autoExposurePool[xrActiveEye][0], perCameraData.autoExposurePool[xrActiveEye][1]);
				//m_ResetHistory = false;
			}
			else
			{
				cmd.SetComputeTextureParam(computeShader, data.kernel, "_Source", data.autoExposureSrc);
				cmd.SetComputeTextureParam(computeShader, data.kernel, "_Destination", data.autoExposureDst);
				cmd.DispatchCompute(computeShader, data.kernel, 1, 1, 1);
			}
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			foreach(var data in perCameraDatas.Values)
			{
				data.Release();
			}
			perCameraDatas.Clear();
		}

		private static float Exp2(float x)
		{
			return Mathf.Exp(x * 0.69314718055994530941723212145818f);
		}

		private PerCameraData GetPerCameraData(UniversalCameraData cameraData)
		{
			if(!perCameraDatas.TryGetValue(cameraData, out var data))
			{
				data = new PerCameraData();
				perCameraDatas.Add(cameraData, data);
			}
			return data;
		}
	}
}
