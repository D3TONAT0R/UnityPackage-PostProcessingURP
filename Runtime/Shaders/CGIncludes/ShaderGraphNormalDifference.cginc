void NormalDifference_float(in float3 center, in float3 a, in float3 b, in float3 c, in float3 d, out float3 diff)
{
	float3 acenter = abs(center);
	float3 aa = abs(a);
	float3 ab = abs(b);
	float3 ac = abs(c);
	float3 ad = abs(d);
	float3 diffA = abs(aa - acenter);
	float3 diffB = abs(ab - acenter);
	float3 diffC = abs(ac - acenter);
	float3 diffD = abs(ad - acenter);
	diff = max(max(diffA, diffB), max(diffC, diffD));
}
