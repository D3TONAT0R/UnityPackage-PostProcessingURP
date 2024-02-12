using System;
using System.Collections.Generic;

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

			public readonly RenderTexture[][] autoExposurePool = new RenderTexture[k_NumEyes][];
			public int[] autoExposurePingPong = new int[k_NumEyes];

			public ulong frameNumber = 0;
			public float lastUseTimestamp;

			public PerCameraData()
			{
				for(int eye = 0; eye < k_NumEyes; eye++)
				{
					autoExposurePool[eye] = new RenderTexture[k_NumAutoExposureTextures];
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
				if(autoExposurePool[eye][id] == null || !autoExposurePool[eye][id].IsCreated())
				{
					autoExposurePool[eye][id] = new RenderTexture(1, 1, 0, RenderTextureFormat.RFloat) { enableRandomWrite = true };
					autoExposurePool[eye][id].Create();
				}
			}

			public void Release()
			{
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

		public BoolParameter enabled = new BoolParameter(false, false);

		/// <summary>
		/// These values are the lower and upper percentages of the histogram that will be used to
		/// find a stable average luminance. Values outside of this range will be discarded and wont
		/// contribute to the average luminance.
		/// </summary>
		//[MinMax(1f, 99f), DisplayName("Filtering (%)"), Tooltip("Filters the bright and dark parts of the histogram when computing the average luminance. This is to avoid very dark pixels and very bright pixels from contributing to the auto exposure. Unit is in percent.")]
		[Tooltip("Filters the bright and dark parts of the histogram when computing the average luminance. This is to avoid very dark pixels and very bright pixels from contributing to the auto exposure. Unit is in percent.")]
		public Vector2Parameter filtering = new Vector2Parameter(new Vector2(50, 95));

		/// <summary>
		/// Minimum average luminance to consider for auto exposure (in EV).
		/// </summary>
		//[Range(LogHistogram.rangeMin, LogHistogram.rangeMax), DisplayName("Minimum (EV)"), Tooltip("Minimum average luminance to consider for auto exposure. Unit is EV.")]
		[Tooltip("Minimum average luminance to consider for auto exposure. Unit is EV.")]
		public ClampedFloatParameter minLuminance = new ClampedFloatParameter(0, -9, 9);

		/// <summary>
		/// Maximum average luminance to consider for auto exposure (in EV).
		/// </summary>
		//[Range(LogHistogram.rangeMin, LogHistogram.rangeMax), DisplayName("Maximum (EV)"), Tooltip("Maximum average luminance to consider for auto exposure. Unit is EV.")]
		[Tooltip("Maximum average luminance to consider for auto exposure. Unit is EV.")]
		public ClampedFloatParameter maxLuminance = new ClampedFloatParameter(0, -9, 9);

		/// <summary>
		/// Middle-grey value. Use this to compensate the global exposure of the scene.
		/// </summary>
		//[Min(0f), DisplayName("Exposure Compensation"), Tooltip("Use this to scale the global exposure of the scene.")]
		[Min(0f), Tooltip("Use this to scale the global exposure of the scene.")]
		public FloatParameter keyValue = new FloatParameter(1);

		/// <summary>
		/// The type of eye adaptation to use.
		/// </summary>
		//[DisplayName("Type"), Tooltip("Use \"Progressive\" if you want auto exposure to be animated. Use \"Fixed\" otherwise.")]
		[Tooltip("Use \"Progressive\" if you want auto exposure to be animated. Use \"Fixed\" otherwise.")]
		public EyeAdaptationParameter eyeAdaptation = new EyeAdaptationParameter { value = EyeAdaptation.Progressive };

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

		private readonly Dictionary<UniversalAdditionalCameraData, PerCameraData> perCameraDatas = new Dictionary<UniversalAdditionalCameraData, PerCameraData>();

		public override string ShaderName => "Hidden/PostProcessing/AutoExposureBlit";

		public override PostProcessingPassEvent PassEvent => PostProcessingPassEvent.BeforePostProcessing;

		public override void ApplyProperties(Material material, RenderingData renderingData) { }

		public override bool IsActive()
		{
			return base.IsActive()
				&& SystemInfo.supportsComputeShaders
				&& !(Application.platform == RuntimePlatform.Android && SystemInfo.graphicsDeviceType != GraphicsDeviceType.Vulkan)
				&& SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RFloat);
		}

		public override void Setup(RenderingData renderingData, List<int> passes)
		{
			base.Setup(renderingData, passes);
		}

		public override void AddPasses(List<int> passes)
		{
			if(PostProcessResources.Instance.computeShaders.autoExposure) passes.Add(0);
		}

		public override void Render(CustomPostProcessPass feature, RenderingData renderingData, CommandBuffer cmd, RTHandle source, RTHandle destination, int passIndex)
		{
			var urpAdditionalData = renderingData.cameraData.camera.GetUniversalAdditionalCameraData();
			var perCameraData = GetPerCameraData(urpAdditionalData);

			perCameraData.logHistogram.Generate(cmd, ref renderingData, source);

			RenderTexture currentAutoExposure = PerformLookup(cmd, ref renderingData, perCameraData, urpAdditionalData.resetHistory);

			blitMaterial.SetTexture("_AutoExposureTex", currentAutoExposure);
			feature.Blit(cmd, source, destination, blitMaterial, 0);
		}

		private RenderTexture PerformLookup(CommandBuffer cmd, ref RenderingData renderingData, PerCameraData perCameraData, bool resetHistory)
		{
			cmd.BeginSample("AutoExposureLookup");

			var computeShader = PostProcessResources.Instance.computeShaders.autoExposure;
			int xrActiveEye = 0;
			// Prepare autoExpo texture pool
			perCameraData.CheckTextures(xrActiveEye);

			// Make sure filtering values are correct to avoid apocalyptic consequences
			float lowPercent = filtering.value.x;
			float highPercent = filtering.value.y;
			const float kMinDelta = 1e-2f;
			highPercent = Mathf.Clamp(highPercent, 1f + kMinDelta, 99f);
			lowPercent = Mathf.Clamp(lowPercent, 1f, highPercent - kMinDelta);

			// Clamp min/max adaptation values as well
			float minLum = minLuminance.value;
			float maxLum = maxLuminance.value;
			minLuminance.value = Mathf.Min(minLum, maxLum);
			maxLuminance.value = Mathf.Max(minLum, maxLum);

			// Compute average luminance & auto exposure
			bool firstFrame = resetHistory || perCameraData.frameNumber == 0 || !Application.isPlaying;

			string adaptation;
			if(firstFrame || eyeAdaptation.value == EyeAdaptation.Fixed)
				adaptation = "KAutoExposureAvgLuminance_fixed";
			else
				adaptation = "KAutoExposureAvgLuminance_progressive";

			int kernel = computeShader.FindKernel(adaptation);
			cmd.SetComputeBufferParam(computeShader, kernel, "_HistogramBuffer", perCameraData.logHistogram.data);
			cmd.SetComputeVectorParam(computeShader, "_Params1", new Vector4(lowPercent * 0.01f, highPercent * 0.01f, Exp2(minLuminance.value), Exp2(maxLuminance.value)));
			cmd.SetComputeVectorParam(computeShader, "_Params2", new Vector4(speedDown.value, speedUp.value, keyValue.value, Time.deltaTime));
			cmd.SetComputeVectorParam(computeShader, "_ScaleOffsetRes", perCameraData.logHistogram.GetHistogramScaleOffsetRes(renderingData));

			RenderTexture currentAutoExposure;

			if(firstFrame)
			{
				// We don't want eye adaptation when not in play mode because the GameView isn't
				// animated, thus making it harder to tweak. Just use the final auto exposure value.
				currentAutoExposure = perCameraData.autoExposurePool[xrActiveEye][0];
				cmd.SetComputeTextureParam(computeShader, kernel, "_Destination", currentAutoExposure);
				cmd.DispatchCompute(computeShader, kernel, 1, 1, 1);

				// Copy current exposure to the other pingpong target to avoid adapting from black
				//cmd.CopyTexture(m_AutoExposurePool[xrActiveEye][0], m_AutoExposurePool[xrActiveEye][1]);
				cmd.CopyTexture(perCameraData.autoExposurePool[xrActiveEye][0], perCameraData.autoExposurePool[xrActiveEye][1]);
				//m_ResetHistory = false;
			}
			else
			{
				int pp = perCameraData.autoExposurePingPong[xrActiveEye];
				var src = perCameraData.autoExposurePool[xrActiveEye][++pp % 2];
				var dst = perCameraData.autoExposurePool[xrActiveEye][++pp % 2];

				cmd.SetComputeTextureParam(computeShader, kernel, "_Source", src);
				cmd.SetComputeTextureParam(computeShader, kernel, "_Destination", dst);
				cmd.DispatchCompute(computeShader, kernel, 1, 1, 1);

				perCameraData.autoExposurePingPong[xrActiveEye] = ++pp % 2;
				currentAutoExposure = dst;
			}

			cmd.EndSample("AutoExposureLookup");

			perCameraData.UpdateCounter();

			return currentAutoExposure;
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

		private float Exp2(float x)
		{
			return Mathf.Exp(x * 0.69314718055994530941723212145818f);
		}

		private PerCameraData GetPerCameraData(UniversalAdditionalCameraData cameraData)
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
