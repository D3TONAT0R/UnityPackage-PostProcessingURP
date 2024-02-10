using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Rendering.Universal.PostProcessing
{
	[CreateAssetMenu(menuName = "delete me")]
    public class PostProcessResources : ScriptableObject
    {
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
    }
}
