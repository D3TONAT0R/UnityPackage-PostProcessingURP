using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Rendering.Universal.PostProcessing
{
    public class PostProcessResources : ScriptableObject
    {
		[System.Serializable]
		public struct CustomResource
		{
			public string id;
			public Object resource;
		}

		public static PostProcessResources Instance
		{
			get
			{
				if(!instance) instance = Resources.Load<PostProcessResources>("CustomPostProcessResources");
				return instance;
			}
		}
		private static PostProcessResources instance;

		[System.Serializable]
		public class ComputeShaders
		{
			public ComputeShader autoExposure;
			public ComputeShader exposureHistogram;
		}

		public ComputeShaders computeShaders = new ComputeShaders();

		private readonly Dictionary<string, Object> customResourceDictionary = new Dictionary<string, Object>();

		public T GetCustomReource<T>(string id) where T : Object
		{
			return (T)customResourceDictionary[id];
		}

		public bool TryGetCustomResource<T>(string id, out T resource) where T : Object
		{
			if(customResourceDictionary.TryGetValue(id, out var obj))
			{
				resource = obj as T;
				return resource != null;
			}
			else
			{
				resource = null;
				return false;
			}
		}

		public void SetCustomResource(string id, Object resource)
		{
			if(customResourceDictionary.ContainsKey(id))
			{
				customResourceDictionary[id] = resource;
			}
			else
			{
				customResourceDictionary.Add(id, resource);
			}
		}
	}
}
