using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEngine.Rendering.Universal.PostProcessing
{
	[VolumeComponentMenu("Post-processing/Compression")]
	public class Compression : CustomPostProcessVolumeComponent
	{
		public IntParameter frequency = new ClampedIntParameter(8, 2, 16);
		public IntParameter levels = new IntParameter(10);
		public IntParameter blockSize = new ClampedIntParameter(8, 2, 32);

		private RenderTextureDescriptor dctDescriptor;
		private RTHandle dctHandle;
		private RTHandle quantizationHandle;

		public override string ShaderName => "Hidden/PostProcessing/Compression";

		public override PostProcessingPassEvent PassEvent => PostProcessingPassEvent.AfterRendering;

		public override void AddPasses(List<int> passes)
		{
			passes.Add(2);
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			dctDescriptor = new RenderTextureDescriptor(Screen.width, Screen.height, RenderTextureFormat.ARGB32, 0, 0);
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			if(dctHandle != null) dctHandle.Release();
			if(quantizationHandle != null) quantizationHandle.Release();
		}

		public override void Setup(CustomPostProcessPass pass, RenderingData renderingData, List<int> passes)
		{
			base.Setup(pass, renderingData, passes);
			dctDescriptor.width = renderingData.cameraData.cameraTargetDescriptor.width;
			dctDescriptor.height = renderingData.cameraData.cameraTargetDescriptor.height;
			dctDescriptor.colorFormat = renderingData.cameraData.cameraTargetDescriptor.colorFormat;
			RenderingUtils.ReAllocateIfNeeded(ref dctHandle, dctDescriptor, name: "Temp_DCT");
			RenderingUtils.ReAllocateIfNeeded(ref quantizationHandle, dctDescriptor, name: "Temp_Quantization");
		}

		public override void ApplyProperties(Material material, RenderingData renderingData)
		{
			material.SetFloat("_Frequency", frequency.value);
			material.SetFloat("_Levels", levels.value);
			material.SetInt("_BlockSize", blockSize.value);
		}

		public override void Render(CustomPostProcessPass feature, RenderingData renderingData, CommandBuffer cmd, RTHandle from, RTHandle to, int passIndex)
		{
			feature.Blit(cmd, from, dctHandle, blitMaterial, 0);
			cmd.SetGlobalTexture("_DCTTexture", dctHandle);
			feature.Blit(cmd, from, quantizationHandle, blitMaterial, 1);
			cmd.SetGlobalTexture("_QuantizationTexture", quantizationHandle);
			feature.Blit(cmd, from, to, blitMaterial, 2);
		}
	}
}
