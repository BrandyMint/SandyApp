Shader "Sandbox/Game/BGTextures3HandsRoad" {
    Properties {        
        _ColorHands ("Color Hands", Color) = (1, 1, 1, 0.8)
        
        _1Tex ("Tex 1", 2D) = "black" {}                
        _12MixDepth ("1-2 Mix Depth Percent", Float) = 0.4
        
        _2Tex ("Tex 2", 2D) = "white" {}
        _2Depth ("Depth 2", Float) = -0.2
        _23MixDepth ("2-3 Mix Depth Percent", Float) = 0.2
        
        _3Tex ("Tex 3", 2D) = "white" {}
        _3Depth ("Depth 3", Float) = 0.4
        
        _DepthZero ("Depth Zero", Float) = 2
        _DepthMinOffset ("Depth Min Offset", Float) = 0.5
        _DepthMaxOffset ("Depth Max Offset", Float) = 0.5
        
        _ZWrite ("ZWrite", Int) = 1
    }
    
    SubShader {
        Tags { "RenderType" = "Opaque" }
        ZWrite [_ZWrite]

        CGPROGRAM
        #pragma target 3.5
        #pragma multi_compile _ CALC_DEPTH
        #pragma surface surf Standard vertex:vertSurfRoad

        #define CALC_NORMAL
        #define EXTENSION_INPUT \
            float2 texcoord_Road; \
            float2 uv_1Tex; \
            float2 uv_2Tex; \
            float2 uv_3Tex;
        
        #include "Assets/DepthSensorSandbox/Resources/Materials/utils.cginc"
        #include "Assets/DepthSensorSandbox/Resources/Materials/sandbox.cginc"
        #include "Assets/DepthSensorSandbox/Resources/Materials/hands.cginc"
        
        sampler2D _RoadTex;
        float4x4 _RoadTex_Proj;
        sampler2D _1Tex;
        float _2Depth;
        sampler2D _2Tex;
        float _3Depth;
        sampler2D _3Tex;
        float _12MixDepth;
        float _23MixDepth;
        fixed4 _ColorHands;
        
        inline void addSample(inout fixed4 c, fixed4 c2, float d, float z, float mix) {
            c = lerp(c2, c, smooth(d - mix, d + mix, z));
        }
        
        void vertSurfRoad(inout appdata_full v, out Input o) {
            vertSurf(v, o);
            o.texcoord_Road = mul(_RoadTex_Proj, float4(mul(unity_ObjectToWorld, v.vertex).xyz, 1.0)).xy;
        }
        
        void surf (Input IN, inout SurfaceOutputStandard o) {
            float z = IN.vpos.z;
            
            float d2 = percentToDepth(_2Depth);
            float d3 = percentToDepth(_3Depth);
            float mix12 = (_DepthMaxOffset + _DepthMinOffset) / 2 * _12MixDepth;
            float mix23 = (_DepthMaxOffset + _DepthMinOffset) / 2 * _23MixDepth;
            
            fixed4 c = tex2D(_1Tex, IN.uv_1Tex);
            fixed4 c2 = tex2D(_2Tex, IN.uv_2Tex);
            fixed4 c3 = tex2D(_3Tex, IN.uv_3Tex);
            addSample(c, c2, d2, z, mix12);
            addSample(c, c3, d3, z, mix23);
            
            fixed4 road = tex2D(_RoadTex, IN.texcoord_Road);
            c.rgb = lerp(c.rgb, road.rgb, road.a);
            c.rgb = lerp(c.rgb, _ColorHands.rgb, _ColorHands.a * handsInteractAlpha(IN.texcoord));
            o.Albedo = c;
        }
        ENDCG
        
        UsePass "Sandbox/ShadowCaster"
    }
}
