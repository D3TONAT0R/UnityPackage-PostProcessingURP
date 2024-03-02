inline float DecodeFloatRG(float2 enc)
{
	float2 kDecodeDot = float2(1.0, 1 / 255.0);
	return dot(enc, kDecodeDot);
}

inline void DecodeDepthNormal(float4 enc, out float depth, out float3 normal)
{
	depth = DecodeFloatRG(enc.zw);
	depth = sqrt(depth);
	normal = DecodeViewNormalStereo(enc);
}

void SampleDepthNormal(float2 coord, out float depth, out float3 normal)
{
	float4 dn = SAMPLE_TEXTURE2D(_CameraDepthNormalsTexture, sampler_CameraDepthNormalsTexture, coord);
	DecodeDepthNormal(dn, depth, normal);
}

float3 SampleNormal(float2 coord)
{
	float4 dn = SAMPLE_TEXTURE2D(_CameraDepthNormalsTexture, sampler_CameraDepthNormalsTexture, coord);
	float depth;
	float3 normal;
	DecodeDepthNormal(dn, depth, normal);
	return normal;
}

float SampleDepth(float2 texcoord)
{
	return LinearEyeDepth(SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, texcoord).r);
}

float2 offsetCoords(float2 texcoord, int2 pixels)
{
	return texcoord + pixels / _ScreenParams.xy;
}

float highest(float3 f3)
{
	return max(f3.r, max(f3.g, f3.b));
}

void SampleData(float2 texcoord, out float depth, out float3 normal)
{
	depth = SampleDepth(texcoord);
	normal = SampleNormal(texcoord);
}

void Deviation(float2 texcoord, float3 color, int radius, out float sampleDepth, out float depthDev, out float normalDev, out float colorDev)
{
	depthDev = 0;
	normalDev = 0;
	colorDev = 0;

	sampleDepth = SampleDepth(texcoord);
	float3 normal = SampleNormal(texcoord);

	if(radius < 1)
	{
		float3 nrm1, nrm2;
		float d1, d2;
		float3 col1, col2;
		float2 coord1 = offsetCoords(texcoord, int2(1, 0));
		float2 coord2 = offsetCoords(texcoord, int2(0, 1));

		SampleDepthNormal(coord1, d1, nrm1);
		d1 = LinearEyeDepth(SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, coord1).r);
		col1 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, coord1).rgb;
		SampleDepthNormal(coord2, d2, nrm2);
		d2 = LinearEyeDepth(SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, coord2).r);
		col2 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, coord2).rgb;

		//depthDev = max(abs(d1 - sampleDepth), abs(d2 - sampleDepth));
		//depthDev = saturate(-(d1 - sampleDepth));
		normalDev += saturate(1 - dot(normal, nrm1)) + saturate(1 - dot(normal, nrm2));
		colorDev = max(highest(abs(color - col1)), highest(abs(color - col2)));
	}
	else
	{
		[loop]
		for (int x = -radius; x <= radius; x++) {
			[loop]
			for (int y = -radius; y <= radius; y++) {
				float d;
				float3 nrm;
				float2 coord = offsetCoords(texcoord, int2(x, y));
				SampleDepthNormal(coord, d, nrm);
				d = LinearEyeDepth(SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, coord).r);
				float3 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, coord).rgb;

				//depthDev = max(depthDev, abs(d - sampleDepth));
				depthDev = max(depthDev, saturate((d - sampleDepth)));
				normalDev += saturate(1 - dot(normal, nrm));
				colorDev = max(colorDev, highest(abs(color - col)));
			}
		}
		int dim = 2 * _Thickness + 1;
		normalDev /= dim*dim;
	}
}

float SoftThreshold(float value, float threshold, float hardness)
{
	if(hardness > 0.0001) {
		return saturate((value - threshold) * hardness * hardness + 0.5);
	} else {
		if(value > threshold) {
			return 1;
		} else {
			return 0;
		}
	}
}

/*
float DetectEdge(float2 texcoord, float3 color, int radius, half depthTolerance, half normalTolerance, half colorTolerance, half hardness)
{
	float sd, dd, nd, cd = 0;
	Deviation(texcoord, color, radius, sd, dd, nd, cd);
	float e = 0;
	if(dd > sd * depthTolerance) e++;
	//e += SoftThreshold(dd, depthTolerance, hardness);
	e += SoftThreshold(nd, normalTolerance, hardness);
	e += SoftThreshold(cd, colorTolerance * radius, hardness);
	return saturate(e);
}
*/