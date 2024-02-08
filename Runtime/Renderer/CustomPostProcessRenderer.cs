using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering.Universal;

namespace UnityEngine.Rendering.Universal.PostProcessing
{
	[System.Serializable]
	public class CustomPostProcessRenderer : ScriptableRendererFeature
	{
		[System.Serializable]
		public class CustomPostProcessRef : ISerializationCallbackReceiver
		{
			public string assemblyQualifiedName;

			public System.Type Type { get; private set; }

			public CustomPostProcessRef(System.Type type)
			{
				Assert.IsNotNull(type);
				Type = type;
			}

			public void OnAfterDeserialize()
			{
				Type = System.Type.GetType(assemblyQualifiedName);
			}

			public void OnBeforeSerialize()
			{
				assemblyQualifiedName = Type?.AssemblyQualifiedName ?? "";
			}
		}

		[System.Serializable]
		public class EffectOrdering
		{
			public List<CustomPostProcessRef> beforeSkybox = new List<CustomPostProcessRef>();
			public List<CustomPostProcessRef> beforeTransparents = new List<CustomPostProcessRef>();
			public List<CustomPostProcessRef> beforePostProcess = new List<CustomPostProcessRef>();
			public List<CustomPostProcessRef> afterPostProcess = new List<CustomPostProcessRef>();
		}

		private CustomPostProcessPass beforeSkyboxPass;
		private CustomPostProcessPass beforeTransparentsPass;
		private CustomPostProcessPass beforePostProcessPass;
		private CustomPostProcessPass afterPostProcessPass;

		public EffectOrdering effectOrdering = new EffectOrdering();

		public override void Create()
		{
			beforeSkyboxPass = new CustomPostProcessPass(RenderPassEvent.BeforeRenderingSkybox);
			beforeTransparentsPass = new CustomPostProcessPass(RenderPassEvent.BeforeRenderingTransparents);
			beforePostProcessPass = new CustomPostProcessPass(RenderPassEvent.BeforeRenderingPostProcessing);
			afterPostProcessPass = new CustomPostProcessPass(RenderPassEvent.AfterRenderingPostProcessing);
		}

		public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
		{
			renderer.EnqueuePass(beforeSkyboxPass);
			renderer.EnqueuePass(beforeTransparentsPass);
			renderer.EnqueuePass(beforePostProcessPass);
			renderer.EnqueuePass(afterPostProcessPass);
		}
	}
}
