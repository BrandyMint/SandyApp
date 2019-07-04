sampler2D _DepthToColorTex; float4 _DepthToColorTex_ST;

#ifdef CALC_DEPTH
#pragma target 4.0
sampler2D _DepthTex; float4 _DepthTex_ST;
sampler2D _MapToCameraTex; float4 _MapToCameraTex_ST;

float4 calcDepth (float2 uv) {
    float2 p = tex2Dlod(_MapToCameraTex, float4(uv, 0, 0)).rg;
    float d = tex2Dlod(_DepthTex, float4(uv, 0, 0)).r * 65.535;
    /*if (d < 0.1)
        d = 5;*/
    return float4(p.xy * d, d, 0);
}
#endif

v2f vert (appdata v) {
    v2f o;
    o.uv = TRANSFORM_TEX(v.uv, _DepthToColorTex);
#ifdef CALC_DEPTH
    float4 vertex = calcDepth(o.uv);
#else
    float4 vertex = v.vertex;
#endif
    o.clip = UnityObjectToClipPos(vertex);
    float3 pos = UnityObjectToViewPos(vertex);
    o.pos = float3(pos.xy, -pos.z);
    
    return o;
}

