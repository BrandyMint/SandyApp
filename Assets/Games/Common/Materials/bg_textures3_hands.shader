Shader "Sandbox/Game/BGTextures3Hands" {
    Properties {        
        _ColorHands ("Color Hands", Color) = (1, 1, 1, 0.8)
        
        _1Tex ("Tex 1", 2D) = "black" {}
        _1TexNormal ("Tex 1 Normal", 2D) = "bump" {}
                
        _12MixDepth ("1-2 Mix Depth Percent", Float) = 0.4
        
        _2Tex ("Tex 2", 2D) = "white" {}
        _2TexNormal ("Tex 2 Normal", 2D) = "bump" {}
        _2Depth ("Depth 2", Float) = -0.2
        
        
        _23MixDepth ("2-3 Mix Depth Percent", Float) = 0.2
        
        _3Tex ("Tex 3", 2D) = "white" {}
        _3TexNormal ("Tex 3 Normal", 2D) = "bump" {}
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
        #pragma surface surf Lambert vertex:vertSurf

        #define CALC_NORMAL
        #define EXTENSION_INPUT \
            float2 uv_1Tex; \
            float2 uv_2Tex; \
            float2 uv_3Tex;
        
        #include "Assets/DepthSensorSandbox/Resources/Materials/utils.cginc"
        #include "Assets/DepthSensorSandbox/Resources/Materials/sandbox.cginc"
        #include "Assets/DepthSensorSandbox/Resources/Materials/hands.cginc"
        
        sampler2D _1Tex;
        sampler2D _1TexNormal;
        float _2Depth;
        sampler2D _2Tex;
        sampler2D _2TexNormal;
        float _3Depth;
        sampler2D _3Tex;
        sampler2D _3TexNormal;
        float _12MixDepth;
        float _23MixDepth;
        fixed4 _ColorHands;
        
        inline void addSample(inout fixed4 c, sampler2D t, float2 uv, float d, float z, float mix) {
            c = lerp(tex2D(t, uv), c, smooth(d - mix, d + mix, z));
        }
        
        void surf (Input IN, inout SurfaceOutput o) {
            float z = IN.vpos.z;
            
            float d2 = percentToDepth(_2Depth);
            float d3 = percentToDepth(_3Depth);
            float mix12 = (_DepthMaxOffset + _DepthMinOffset) / 2 * _12MixDepth;
            float mix23 = (_DepthMaxOffset + _DepthMinOffset) / 2 * _23MixDepth;
            
            fixed4 c = tex2D(_1Tex, IN.uv_1Tex);
            addSample(c, _2Tex, IN.uv_2Tex, d2, z, mix12);
            addSample(c, _3Tex, IN.uv_3Tex, d3, z, mix23);
            c.rgb = lerp(c.rgb, _ColorHands.rgb, _ColorHands.a * handsInteractAlpha(IN.texcoord));
            o.Albedo = c;
            
            fixed4 n = tex2D(_1TexNormal, IN.uv_1Tex);
            addSample(n, _2TexNormal, IN.uv_2Tex, d2, z, mix12);
            addSample(n, _3TexNormal, IN.uv_3Tex, d3, z, mix23);
            o.Normal = UnpackNormal (n);
        }
        ENDCG
        
        UsePass "Sandbox/ShadowCaster"
    }
}
