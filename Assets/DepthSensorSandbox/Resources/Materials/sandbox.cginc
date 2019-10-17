#ifndef EXTENSION_V2F
#   define EXTENSION_V2F
#endif

float _DepthZero;
float _DepthMaxOffset;
float _DepthMinOffset;

struct appdata {
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
};

struct v2f {
    float4 clip : SV_POSITION;
    float2 uv : TEXCOORD0;
    float3 pos : TEXCOORD1;
    float4 screenPos: TEXCOORD2;
    EXTENSION_V2F
};

#ifdef CALC_DEPTH
    #pragma target 4.0
    sampler2D _DepthTex; float4 _DepthTex_ST;
    sampler2D _MapToCameraTex; float4 _MapToCameraTex_ST;
    
    float4 calcDepth (float2 uv) {
        float2 p = tex2Dlod(_MapToCameraTex, float4(uv, 0, 0)).rg;
        float d = tex2Dlod(_DepthTex, float4(uv, 0, 0)).r * 65.535;
        if (d < 0.1)
            d = 5;
        return float4(p.xy * d, d, 0);
    }
#endif

v2f vert (appdata v) {
    v2f o;
    o.uv = v.uv;
#ifdef CALC_DEPTH
    float4 vertex = calcDepth(o.uv);
#else
    float4 vertex = v.vertex;
#endif
    float3 pos = UnityObjectToViewPos(vertex);
    
    //fix over near clip
    if (pos.z > -_ProjectionParams.y) pos.z = -_ProjectionParams.y - 0.01;
    
    o.clip = mul(UNITY_MATRIX_P, float4(pos, 1.0));
    o.screenPos = ComputeScreenPos(o.clip);
    
    //
    //if (o.uv.x < 0.001) {
    //    o.clip.x = 20;
    //}
    
    o.pos = float3(pos.xy, -pos.z);
    return o;
}

float percentToDepth(float p) {
    if (p < 0) {
        return lerp(_DepthZero + _DepthMinOffset, _DepthZero, 1 + p);
    } else {
        return lerp(_DepthZero, _DepthZero - _DepthMaxOffset, p);
    }
}

