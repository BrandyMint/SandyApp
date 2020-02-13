Shader "Sandbox/Game/FlowersAndMushrooms" {
    Properties {        
        _MinTex ("Min", 2D) = "black" {}
        _MaxTex ("Max", 2D) = "white" {}
        
        _DepthSliceOffset ("Depth Slice", Float) = 0.05        
        _DotSlice ("Dot Slice", Float) = 0.7        
        _DotSliceOverride ("Dot Slice Override", Float) = 0.5        
        
        _MixDepthPercent ("Mix Depth Percent", Float) = 0.2
        _DepthZero ("Depth Zero", Float) = 2
        _DepthMinOffset ("Depth Min Offset", Float) = 0.5
        _DepthMaxOffset ("Depth Max Offset", Float) = 0.5
    }
    
    SubShader {
        Tags { "RenderType"="Opaque" "Queue" = "Background"}
        
		Lighting Off
		ZWrite Off
		ZTest Off

        Pass {
            CGPROGRAM
            #pragma multi_compile _ CALC_DEPTH
            #pragma vertex vertCalcUV
            #pragma fragment frag

            #define CALC_NORMAL
            
            #define EXTENSION_V2F \
                float2 uvMinTex : TEXCOORD5; \
                float2 uvMaxTex : TEXCOORD4; 
            
            #include "UnityCG.cginc"
            #include "Assets/DepthSensorSandbox/Resources/Materials/utils.cginc"
            #include "Assets/DepthSensorSandbox/Resources/Materials/sandbox.cginc"
            #include "Assets/Games/Common/Materials/depth_slice.cginc"
            
            sampler2D _MinTex; float4 _MinTex_ST;
            sampler2D _MaxTex; float4 _MaxTex_ST;
            float _MixDepthPercent;
            float _NoiseSize;
            float _NoiseStrength;
            float _DotSliceOverride;
            
            v2f vertCalcUV (appdata v) {
                v2f o = vert(v);
                o.uvMinTex = TRANSFORM_TEX(o.uv, _MinTex);
                o.uvMaxTex = TRANSFORM_TEX(o.uv, _MaxTex);
                return o;
            }
            
            float smooth(float d, float z) {
                float mix = (_DepthMaxOffset + _DepthMinOffset) / 2 * _MixDepthPercent;
                return smooth(d - mix, d + mix, z);
            }
            
            inline void addSample(inout fixed4 c, sampler2D t, float2 uv, float d, float z) {
                c = lerp(tex2D(t, uv), c, smooth(d, z));
            }

            fixed4 frag (v2f i) : SV_Target {
                float hands = fragSliceDot(i, _DotSliceOverride);
                if (hands > 0)
                    return fixed4(0, 0, 0, 1);
                    
                float z = i.vpos.z;                
                fixed4 c = tex2D(_MinTex, i.uvMinTex);
                addSample(c, _MaxTex, i.uvMaxTex, _DepthZero, z);
                return c;
            }
            ENDCG
        }
        UsePass "Sandbox/ShadowCaster"
    }
}
