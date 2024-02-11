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
	public sealed class AutoExposure : CustomPostProcessVolumeComponent
	{
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

		const int k_NumEyes = 2;
		const int k_NumAutoExposureTextures = 2;

		private static readonly RenderTexture[][] m_AutoExposurePool = new RenderTexture[k_NumEyes][];
		private static int[] m_AutoExposurePingPong = new int[k_NumEyes];
		private RenderTexture m_CurrentAutoExposure;

		private ulong frameNumber = 0;

		//private RenderTextureDescriptor intermediateDescriptor;
		//private RTHandle intermediate;

		private Dictionary<UniversalAdditionalCameraData, LogHistogram> logHistograms = new Dictionary<UniversalAdditionalCameraData, LogHistogram>();

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

		protected override void OnEnable()
		{
			base.OnEnable();
			for(int eye = 0; eye < k_NumEyes; eye++)
			{
				m_AutoExposurePool[eye] = new RenderTexture[k_NumAutoExposureTextures];
				m_AutoExposurePingPong[eye] = 0;
			}
			/*
			intermediateDescriptor = new RenderTextureDescriptor(Screen.width, Screen.height, RenderTextureFormat.Default, 0)
			{
				enableRandomWrite = true
			};
			*/
		}

		void CheckTexture(int eye, int id)
		{
			if(m_AutoExposurePool[eye][id] == null || !m_AutoExposurePool[eye][id].IsCreated())
			{
				m_AutoExposurePool[eye][id] = new RenderTexture(1, 1, 0, RenderTextureFormat.RFloat) { enableRandomWrite = true };
				m_AutoExposurePool[eye][id].Create();
			}
		}

		public override void Setup(RenderingData renderingData, List<int> passes)
		{
			base.Setup(renderingData, passes);

			//RenderingUtils.ReAllocateIfNeeded(ref intermediate, intermediateDescriptor, name: "AutoExpo_Intermediate");
		}

		public override void AddPasses(List<int> passes)
		{
			if(PostProcessResources.Instance.computeShaders.autoExposure) passes.Add(0);
		}

		public override void Render(CustomPostProcessPass feature, RenderingData renderingData, CommandBuffer cmd, RTHandle source, RTHandle destination, int passIndex)
		{
			var urpCameraData = renderingData.cameraData.camera.GetUniversalAdditionalCameraData();
			var logHistogram = GetLogHistogram(urpCameraData);

			logHistogram.Generate(renderingData, cmd, source);

			//return;
			cmd.BeginSample("AutoExposureLookup");

			var computeShader = PostProcessResources.Instance.computeShaders.autoExposure;
			int xrActiveEye = 0;
			// Prepare autoExpo texture pool
			CheckTexture(xrActiveEye, 0);
			CheckTexture(xrActiveEye, 1);

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
			bool resetHistory = urpCameraData.resetHistory;
			bool firstFrame = resetHistory || frameNumber == 0 || !Application.isPlaying;

			string adaptation;
			if(firstFrame || eyeAdaptation.value == EyeAdaptation.Fixed)
				adaptation = "KAutoExposureAvgLuminance_fixed";
			else
				adaptation = "KAutoExposureAvgLuminance_progressive";

			int kernel = computeShader.FindKernel(adaptation);
			cmd.SetComputeBufferParam(computeShader, kernel, "_HistogramBuffer", logHistogram.data);
			cmd.SetComputeVectorParam(computeShader, "_Params1", new Vector4(lowPercent * 0.01f, highPercent * 0.01f, Exp2(minLuminance.value), Exp2(maxLuminance.value)));
			cmd.SetComputeVectorParam(computeShader, "_Params2", new Vector4(speedDown.value, speedUp.value, keyValue.value, Time.deltaTime));
			cmd.SetComputeVectorParam(computeShader, "_ScaleOffsetRes", logHistogram.GetHistogramScaleOffsetRes(renderingData));

			if(firstFrame)
			{
				// We don't want eye adaptation when not in play mode because the GameView isn't
				// animated, thus making it harder to tweak. Just use the final auto exposure value.
				m_CurrentAutoExposure = m_AutoExposurePool[xrActiveEye][0];
				cmd.SetComputeTextureParam(computeShader, kernel, "_Destination", m_CurrentAutoExposure);
				cmd.DispatchCompute(computeShader, kernel, 1, 1, 1);

				// Copy current exposure to the other pingpong target to avoid adapting from black
				//cmd.CopyTexture(m_AutoExposurePool[xrActiveEye][0], m_AutoExposurePool[xrActiveEye][1]);
				cmd.CopyTexture(m_AutoExposurePool[xrActiveEye][0], m_AutoExposurePool[xrActiveEye][1]);
				//m_ResetHistory = false;
			}
			else
			{
				int pp = m_AutoExposurePingPong[xrActiveEye];
				var src = m_AutoExposurePool[xrActiveEye][++pp % 2];
				var dst = m_AutoExposurePool[xrActiveEye][++pp % 2];

				cmd.SetComputeTextureParam(computeShader, kernel, "_Source", src);
				cmd.SetComputeTextureParam(computeShader, kernel, "_Destination", dst);
				cmd.DispatchCompute(computeShader, kernel, 1, 1, 1);

				m_AutoExposurePingPong[xrActiveEye] = ++pp % 2;
				m_CurrentAutoExposure = dst;
			}

			//feature.Blit(cmd, intermediate, to);
			cmd.EndSample("AutoExposureLookup");

			blitMaterial.SetTexture("_AutoExposureTex", m_CurrentAutoExposure);
			feature.Blit(cmd, source, destination, blitMaterial, 0);

			frameNumber++;
			/*
			context.autoExposureTexture = m_CurrentAutoExposure;
			context.autoExposure = settings;
			*/
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			foreach(var rtEyeSet in m_AutoExposurePool)
			{
				foreach(var rt in rtEyeSet)
				{
#if UNITY_EDITOR
					if(Application.isPlaying)
						Object.Destroy(rt);
					else
						Object.DestroyImmediate(rt);
#else
					Object.Destroy(rt);
#endif
				}
			}
		}

		private float Exp2(float x)
		{
			return Mathf.Exp(x * 0.69314718055994530941723212145818f);
		}

		private LogHistogram GetLogHistogram(UniversalAdditionalCameraData cameraData)
		{
			if(!logHistograms.TryGetValue(cameraData, out var histogram))
			{
				histogram = new LogHistogram();
				logHistograms.Add(cameraData, histogram);
			}
			return histogram;
		}
	}
}
