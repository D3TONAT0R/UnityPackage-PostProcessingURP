using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering.Universal;

namespace UnityEngine.Rendering.Universal.PostProcessing
{
	[System.Serializable]
	public class EffectOrderingList : ISerializationCallbackReceiver
	{
		private List<System.Type> effectTypes = new List<System.Type>();

		[SerializeField]
		private List<string> serializedTypeNames = new List<string>();

		public bool AddIfMissing(System.Type effectType)
		{
			if(!effectTypes.Contains(effectType))
			{
				effectTypes.Add(effectType);
				return true;
			}
			else
			{
				return false;
			}
		}

		public int GetPosition(System.Type effectType)
		{
			int index = effectTypes.IndexOf(effectType);
			if(index >= 0) return index;
			else return effectTypes.Count - 1;
		}

		public void OnAfterDeserialize()
		{
			effectTypes.Clear();
			foreach(var s in serializedTypeNames)
			{
				var t = System.Type.GetType(s);
				if(t != null)
				{
					effectTypes.Add(t);
				}
				else
				{
					Debug.LogError($"Failed to find effect of type '{s}', removing it from the ordering list.");
				}
			}
		}

		public void OnBeforeSerialize()
		{
			serializedTypeNames.Clear();
			foreach(var t in effectTypes)
			{
				serializedTypeNames.Add(t.AssemblyQualifiedName);
			}
		}
	}

	[System.Serializable]
	public class CustomPostProcessRenderer : ScriptableRendererFeature
	{
		[System.Serializable]
		public class EffectOrdering
		{
			public EffectOrderingList beforeSkybox = new EffectOrderingList();
			public EffectOrderingList beforeTransparents = new EffectOrderingList();
			public EffectOrderingList beforePostProcess = new EffectOrderingList();
			public EffectOrderingList afterPostProcess = new EffectOrderingList();
			public EffectOrderingList afterRendering = new EffectOrderingList();
		}

		private CustomPostProcessPass beforeSkyboxPass;
		private CustomPostProcessPass beforeTransparentsPass;
		private CustomPostProcessPass beforePostProcessPass;
		private CustomPostProcessPass afterPostProcessPass;
		private CustomPostProcessPass afterRenderingPass;

		public EffectOrdering effectOrdering = new EffectOrdering();

		public List<Shader> shaderRefs = new List<Shader>();

		public override void Create()
		{
			beforeSkyboxPass = new CustomPostProcessPass(this, RenderPassEvent.BeforeRenderingSkybox, effectOrdering.beforeSkybox);
			beforeTransparentsPass = new CustomPostProcessPass(this, RenderPassEvent.BeforeRenderingTransparents, effectOrdering.beforeTransparents);
			beforePostProcessPass = new CustomPostProcessPass(this, RenderPassEvent.BeforeRenderingPostProcessing, effectOrdering.beforePostProcess);
			afterPostProcessPass = new CustomPostProcessPass(this, RenderPassEvent.AfterRenderingPostProcessing, effectOrdering.afterPostProcess);
			afterRenderingPass = new CustomPostProcessPass(this, RenderPassEvent.AfterRendering, effectOrdering.afterRendering);
		}

		public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
		{
			renderer.EnqueuePass(beforeSkyboxPass);
			renderer.EnqueuePass(beforeTransparentsPass);
			renderer.EnqueuePass(beforePostProcessPass);
			renderer.EnqueuePass(afterPostProcessPass);
			renderer.EnqueuePass(afterRenderingPass);
		}

		public void ReferenceShader(Shader shader)
		{
#if UNITY_EDITOR
			if(shader == null) return;
			if(!shaderRefs.Contains(shader))
			{
				shaderRefs.Add(shader);
			}
			UnityEditor.EditorUtility.SetDirty(this);
#endif
		}
	}
}
