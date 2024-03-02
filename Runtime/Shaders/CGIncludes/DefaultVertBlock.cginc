UNITY_SETUP_INSTANCE_ID(v);
UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

#if SHADER_API_GLES
float4 pos = input.positionOS;
float2 uv = input.uv;
#else
float4 pos = GetFullScreenTriangleVertexPosition(v.vertexID);
float2 uv = GetFullScreenTriangleTexCoord(v.vertexID);
#endif

o.positionCS = pos;
o.texcoord = uv * _BlitScaleBias.xy + _BlitScaleBias.zw;
