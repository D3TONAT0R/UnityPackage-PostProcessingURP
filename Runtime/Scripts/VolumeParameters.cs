using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Rendering.Universal.PostProcessing
{
    [System.Serializable]
    public class MinMaxFloatParameter : VolumeParameter<float>
	{
        public readonly float limitMin;
        public readonly float limitMax;

        public MinMaxFloatParameter(float value, float min, float max, bool overrideState = false) : base(value, overrideState)
        {
            limitMin = min;
            limitMax = max;
        }

		public override void Interp(float from, float to, float t)
		{
            m_Value = Mathf.Clamp(Mathf.Lerp(from, to, t), limitMin, limitMax);
		}
	}
}
