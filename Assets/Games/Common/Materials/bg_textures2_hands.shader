Shader "Sandbox/Game/BGTextures2Hands" {
    Properties {        
        _ColorHands ("Color Hands", Color) = (1, 1, 1, 0.8)
        _MinTex ("Min", 2D) = "black" {}
        _MinTexNormal ("Min Normal", 2D) = "bump" {}
        _MaxTex ("Max", 2D) = "white" {}
        _MaxTexNormal ("Max Normal", 2D) = "bump" {}
        
        _MixDepthPercent ("Mix Depth Percent", Float) = 0.2
        _DepthZero ("Depth Zero", Float) = 2
        _DepthMinOffset ("Depth Min Offset", Float) = 0.5
        _DepthMaxOffset ("Depth Max Offset", Float) = 0.5
        
        _ZWrite ("ZWrite", Int) = 1
    }
    
    SubShader {
        Tags { "RenderType" = "Opaque" }
        ZWrite [_ZWrite]

        CGPROGRAM
        #pragma target 3.0
        #pragma multi_compile _ CALC_DEPTH
        #pragma surface surf Lambert vertex:vertSurf

        #define CALC_NORMAL
        #define EXTENSION_INPUT \
            float2 uv_MinTex; \
            float2 uv_MaxTex;
        
        #include "Assets/DepthSensorSandbox/Resources/Materials/utils.cginc"
        #include "Assets/DepthSensorSandbox/Resources/Materials/sandbox.cginc"
        #include "Assets/DepthSensorSandbox/Resources/Materials/hands.cginc"
        
        sampler2D _MinTex;
        sampler2D _MaxTex;
        sampler2D _MinTexNormal;
        sampler2D _MaxTexNormal;
        float _MixDepthPercent;
        fixed4 _ColorHands;
        
        float smooth(float d, float z) {
            float mix = (_DepthMaxOffset + _DepthMinOffset) / 2 * _MixDepthPercent;
            return smooth(d - mix, d + mix, z);
        }
        
        inline void addSample(inout fixed4 c, sampler2D t, float2 uv, float d, float z) {
            c = lerp(tex2D(t, uv), c, smooth(d, z));
        }
        
        void surf (Input IN, inout SurfaceOutput o) {
            float z = IN.vpos.z;
            
            fixed4 c = tex2D(_MinTex, IN.uv_MinTex);
            addSample(c, _MaxTex, IN.uv_MinTex, _DepthZero, z);
            c.rgb = lerp(c.rgb, _ColorHands.rgb, _ColorHands.a * handsInteractAlpha(IN.texcoord));
            o.Albedo = c;
            
            fixed4 n = tex2D(_MinTexNormal, IN.uv_MinTex);
            addSample(n, _MaxTexNormal, IN.uv_MinTex, _DepthZero, z);
            o.Normal = UnpackNormal (n);
        }
        ENDCG
        
        UsePass "Sandbox/ShadowCaster"
    }
}
