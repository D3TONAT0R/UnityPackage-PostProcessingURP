void ComputeUVs_float(in float3 worldPos, in float2 scale, out float2 uvX, out float2 uvY, out float2 uvZ)
{
	uvX = worldPos.zy / scale;
	uvY = worldPos.xz / scale;
	uvZ = worldPos.xy / scale;
}

void BlendTri_float(in float4 colX, in float4 colY, in float4 colZ, in float3 worldNormal, out float4 color)
{
	float3 nrm = abs(worldNormal);
	nrm.y *= 1.001;
	nrm.x *= 0.999;
	float prio = max(nrm.x, max(nrm.y, nrm.z));

	if (prio == nrm.x) color = colX;
	else if (prio == nrm.y) color = colY;
	else color = colZ;
}
