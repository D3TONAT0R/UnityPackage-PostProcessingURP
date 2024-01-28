void ComputeNeighbors_float(in float2 screenPos, out float2 left, out float2 right, out float2 bottom, out float2 top)
{
	left = screenPos - float2(1.0 / _ScreenParams.x, 0);
	right = screenPos + float2(1.0 / _ScreenParams.x, 0);
	bottom = screenPos - float2(0, 1.0 / _ScreenParams.y);
	top = screenPos + float2(0, 1.0 / _ScreenParams.y);
}
