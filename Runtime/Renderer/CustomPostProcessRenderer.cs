using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace UnityEngine.Rendering.Universal.PostProcessing
{
	[System.Serializable]
	public class EffectOrderingList : ISerializationCallbackReceiver
	{
		private List<Type> effectTypes = new List<System.Type>();

		[SerializeField]
		private List<string> serializedTypeNames = new List<string>();

		public int Hash { get; private set; }

		public bool AddIfMissing(Type effectType)
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

		public int GetPosition(Type effectType)
		{
#if UNITY_EDITOR
			AddIfMissing(effectType);
#endif
			int index = effectTypes.IndexOf(effectType);
			if(index >= 0) return index;
			else return effectTypes.Count - 1;
		}

		public void OnAfterDeserialize()
		{
			effectTypes.Clear();
			foreach(var s in serializedTypeNames)
			{
				var t = Type.GetType(s);
				if(t != null)
				{
					effectTypes.Add(t);
				}
				else
				{
					Debug.LogError($"Failed to find effect of type '{s}', removing it from the ordering list.");
				}
			}
			int h = effectTypes.Count * 123;
			unchecked
			{
				for(int i = 0; i < effectTypes.Count; i++)
				{
					h += effectTypes[i].GetHashCode() * i;
				}
			}
			Hash = h;
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

		public EffectOrdering effectOrdering = new EffectOrdering();
		public List<Shader> shaderRefs = new List<Shader>();

		private CustomPostProcessPass beforeSkyboxPass;
		private CustomPostProcessPass beforeTransparentsPass;
		private CustomPostProcessPass beforePostProcessingPass;
		private CustomPostProcessPass afterPostProcessingPass;
		private CustomPostProcessPass afterRenderingPass;

		[NonSerialized]
		public List<CustomPostProcessVolumeComponent> volumeEffects;

		public override void Create()
		{
			var stack = VolumeManager.instance.stack;
			volumeEffects = EnumerateCustomEffects(stack).ToList();
			beforeSkyboxPass = new CustomPostProcessPass(this, PostProcessingPassEvent.BeforeSkybox, effectOrdering.beforeSkybox);
			beforeTransparentsPass = new CustomPostProcessPass(this, PostProcessingPassEvent.BeforeTransparents, effectOrdering.beforeTransparents);
			beforePostProcessingPass = new CustomPostProcessPass(this, PostProcessingPassEvent.BeforePostProcessing, effectOrdering.beforePostProcess);
			afterPostProcessingPass = new CustomPostProcessPass(this, PostProcessingPassEvent.AfterPostProcessing, effectOrdering.afterPostProcess);
			afterRenderingPass = new CustomPostProcessPass(this, PostProcessingPassEvent.AfterRendering, effectOrdering.afterRendering);
		}

		public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
		{
			renderer.EnqueuePass(beforeSkyboxPass);
			renderer.EnqueuePass(beforeTransparentsPass);
			renderer.EnqueuePass(beforePostProcessingPass);
			renderer.EnqueuePass(afterPostProcessingPass);
			renderer.EnqueuePass(afterRenderingPass);
		}

		[Conditional("UNITY_EDITOR")]
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

		private IEnumerable<CustomPostProcessVolumeComponent> EnumerateCustomEffects(VolumeStack stack)
		{
			var components = (Dictionary<System.Type, VolumeComponent>)typeof(VolumeStack)
				.GetField("components", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
				.GetValue(stack);
			foreach(var kv in components)
			{
				var comp = kv.Value;
				if(comp is CustomPostProcessVolumeComponent customComponent)
				{
					yield return customComponent;
				}
			}
		}
	}
}
