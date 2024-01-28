void compute_float(in float2 screenPos, in float rawDepth, out float3 worldPos) {
	worldPos = ComputeWorldSpacePosition(screenPos, rawDepth, UNITY_MATRIX_I_VP);
}
