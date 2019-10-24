#ifndef EXTENSION_V2F
    #define EXTENSION_V2F
#endif

#ifdef CALC_LIGHT
    #ifndef CALC_NORMAL
        #define CALC_NORMAL
    #endif
#endif

float _DepthZero;
float _DepthMaxOffset;
float _DepthMinOffset;

struct appdata {
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    float2 uv : TEXCOORD0;    
};

#define CURR float2(0, 0)
#define L float2(-1, 0)
#define R float2(1, 0)
#define T float2(0, 1)
#define B float2(0, -1)

#define CURR3 float3(0, 0, 0)
#define L3 float3(-1, 0, 0)
#define R3 float3(1, 0, 0)
#define T3 float3(0, 1, 0)
#define B3 float3(0, -1, 0)

struct v2f {
    float4 pos : SV_POSITION;
#ifdef CALC_NORMAL
    float3 normal : NORMAL;
#endif
    float2 uv : TEXCOORD0;
    float3 vpos : TEXCOORD1;
    float4 screenPos: TEXCOORD2;
#ifdef CALC_LIGHT
    fixed4 light : COLOR0;
#endif
#ifdef SHADOW_COORDS
    SHADOW_COORDS(7)
#endif
    EXTENSION_V2F
};

#ifdef CALC_DEPTH
    #pragma target 4.0
    sampler2D _DepthTex; float4 _DepthTex_ST; float4 _DepthTex_TexelSize;
    sampler2D _MapToCameraTex; float4 _MapToCameraTex_ST;
    
    float4 sampleDepth (float2 uv, float2 offset) {
        uv += offset * _DepthTex_TexelSize.xy;
        clamp(uv, 0, 1);
        float2 p = tex2Dlod(_MapToCameraTex, float4(uv, 0, 0)).rg;
        float d = tex2Dlod(_DepthTex, float4(uv, 0, 0)).r * 65.535;
        return float4(p.xy * d, d, 0);
    }
#endif

#ifdef UNITY_LIGHTING_COMMON_INCLUDED
    fixed4 calcLight(float3 normal, float3 lightDir) {
        half nl = max(0, dot(normal, lightDir));
        return nl * _LightColor0;
    }
#endif

v2f vert (appdata v) {
    v2f o;
    o.uv = v.uv;
#ifdef CALC_DEPTH
    float4 vertex = sampleDepth(o.uv, CURR);
    #ifdef CALC_NORMAL
        v.normal = normalize(
            cross(vertex.xyz - sampleDepth(o.uv, L), B3) +
            cross(vertex.xyz - sampleDepth(o.uv, T), R3) +
            cross(vertex.xyz - sampleDepth(o.uv, R), T3) +
            cross(vertex.xyz - sampleDepth(o.uv, B), L3)
        );
    #endif
#else
    float4 vertex = v.vertex;
#endif
    float3 pos = UnityObjectToViewPos(vertex);
    
    //fix over near clip
    if (pos.z > -_ProjectionParams.y) pos.z = -_ProjectionParams.y - 0.01;
    
    o.pos = mul(UNITY_MATRIX_P, float4(pos, 1.0));
    o.screenPos = ComputeScreenPos(o.pos);
#ifdef CALC_NORMAL
    o.normal = COMPUTE_VIEW_NORMAL;
#endif
#ifdef CALC_LIGHT
    o.light = calcLight(v.normal, ObjSpaceLightDir(float4(pos, 1.0)));
    //o.light.rgb += ShadeSH9(half4(worldNormal,1));
#endif    
#ifdef TRANSFER_SHADOW
    TRANSFER_SHADOW(o)
#endif
    o.vpos = float3(pos.xy, -pos.z);
    return o;
}

float percentToDepth(float p) {
    if (p < 0) {
        return lerp(_DepthZero + _DepthMinOffset, _DepthZero, 1 + p);
    } else {
        return lerp(_DepthZero, _DepthZero - _DepthMaxOffset, p);
    }
}

