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

#define DEPTH_TO_FLOAT 65.535

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
        float d = tex2Dlod(_DepthTex, float4(uv, 0, 0)).r * DEPTH_TO_FLOAT;
        return float4(p.xy * d, d, 0);
    }
#endif


#ifdef CALC_NORMAL
    #define MODIFY_NORMAL(v, uv) \
        v.normal = normalize( \
            cross(v.vertex.xyz - sampleDepth(uv, L), B3) + \
            cross(v.vertex.xyz - sampleDepth(uv, T), R3) + \
            cross(v.vertex.xyz - sampleDepth(uv, R), T3) + \
            cross(v.vertex.xyz - sampleDepth(uv, B), L3) \
        );
#else
    #define MODIFY_NORMAL(v, uv)
#endif

#ifdef CALC_DEPTH 
    #define MODIFY_VERT(v, uv) \
        v.vertex = sampleDepth(uv, CURR); \
        MODIFY_NORMAL(v, uv)
#else
    #define MODIFY_VERT(v, uv)
#endif

#ifdef UNITY_LIGHTING_COMMON_INCLUDED
    fixed4 calcLight(float3 normal, float3 lightDir) {
        half4 nl = max(0, dot(normal, lightDir));
        return nl * _LightColor0;
    }
#endif

v2f vert (appdata v) {
    v2f o;
    o.uv = v.uv;
    MODIFY_VERT(v, o.uv)
    float3 pos = UnityObjectToViewPos(v.vertex);
    
    //fix over near clip
    if (pos.z > -_ProjectionParams.y) pos.z = -_ProjectionParams.y - 0.01;
    
    o.pos = mul(UNITY_MATRIX_P, float4(pos, 1.0));
    o.screenPos = ComputeScreenPos(o.pos);
#ifdef CALC_NORMAL
    o.normal = COMPUTE_VIEW_NORMAL;
#endif
#ifdef CALC_LIGHT
    o.light = calcLight(v.normal, ObjSpaceLightDir(float4(pos, 1.0)));
//    o.light.rgb += ShadeSH9(half4(UnityObjectToWorldNormal(normal),1));
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

#ifndef EXTENSION_INPUT
    #define EXTENSION_INPUT
#endif

struct Input {
    float3 vpos;
    float4 screenPos;
    float2 texcoord;
    EXTENSION_INPUT
#ifdef INCLUDE_INPUT_WORLD_NORMAL
    float3 worldNormal;
    INTERNAL_DATA
#endif
};

#ifdef INCLUDE_INPUT_WORLD_NORMAL
    float3 WorldToTangentNormalVector(Input IN, float3 normal) {
        float3 t2w0 = WorldNormalVector(IN, float3(1,0,0));
        float3 t2w1 = WorldNormalVector(IN, float3(0,1,0));
        float3 t2w2 = WorldNormalVector(IN, float3(0,0,1));
        float3x3 t2w = float3x3(t2w0, t2w1, t2w2);
        return normalize(mul(t2w, normal));
    }
#endif

void vertSurf(inout appdata_full v, out Input o) {
    MODIFY_VERT(v, v.texcoord.xy)
    UNITY_INITIALIZE_OUTPUT(Input,o);
    float3 pos = UnityObjectToViewPos(v.vertex);
    
    //fix over near clip
    if (pos.z > -_ProjectionParams.y) pos.z = -_ProjectionParams.y - 0.01;

    o.texcoord = v.texcoord.xy;
    o.screenPos = ComputeScreenPos(mul(UNITY_MATRIX_P, float4(pos, 1.0)));
    o.vpos = float3(pos.xy, -pos.z);
}

