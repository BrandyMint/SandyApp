Shader "Sandbox/Game/Landscape" {
    Properties {
        _MixDepth ("Mix Depth", Float) = 0.01
        _MixNoiseSize ("Perlin Size", Float) = 1
        _MixNoiseStrength ("Perlin Strength", Float) = 1
        
        _ColorIce ("Color Ice", Color) = (1, 1, 1, 1)
        _DepthIce ("Depth Ice", Float) = 1
        
        _MountainsTex ("Mountains", 2D) = "white" {}
        _hsvMountains ("HSV Mountains", Vector) = (0, 0, 0, 0)
        _DepthMountains ("Depth Mountains", Float) = 0.6
        
        _DirtTex ("Dirt", 2D) = "white" {}
        _hsvDirt ("HSV Dirt", Vector) = (0, 0, 0, 0)
        _DepthDirt ("Depth Dirt", Float) = 0.3
        
        _GroundTex ("Ground", 2D) = "white" {}
        _hsvGround ("HSV Ground", Vector) = (0, 0, 0, 0)
        _DepthGround ("Depth Ground", Float) = 0.01
        
        _SandTex ("Sand", 2D) = "white" {}
        _hsvSand ("HSV Sand", Vector) = (0, 0, 0, 0)
        
        _Metallic ("Ground Metallic", Float) = 0.0
                
        _ColorSeaMin ("Color Sea Min", Color) = (0, 1, 1, 1)
        _DepthSea ("Depth Sea", Float) = 0
        
        _ColorSeaMax ("Color Sea Max", Color) = (0, 0, 1, 1)
        _DepthSeaBottom ("Depth Sea Bottom", Float) = -0.2
        
        _SeaMetallic ("Sea Metallic", Float) = 0.5
        //_SeaSmoothness ("Sea Smoothness", Float) = 1.0
        
        _DepthZero ("Depth Zero", Float) = 2
        _DepthMinOffset ("Depth Min Offset", Float) = 0.5
        _DepthMaxOffset ("Depth Max Offset", Float) = 0.5
    }
    
    SubShader {
        Tags { "RenderType"="Opaque" "Queue" = "Background"}
		ZWrite Off

        CGPROGRAM
        #pragma target 4.0
        #pragma multi_compile _ CALC_DEPTH 
        #pragma multi_compile __ DYNAMIC_FLUID
        #pragma multi_compile ___ DRAW_LANDSCAPE
        #pragma surface surf Standard vertex:vertSurf
        
        sampler2D _MountainsTex;
        fixed3 _hsvMountains;
        sampler2D _DirtTex;
        fixed3 _hsvDirt;
        sampler2D _GroundTex;
        fixed3 _hsvGround;
        sampler2D _SandTex;
        fixed3 _hsvSand;
        fixed4 _ColorSeaMin;
        fixed4 _ColorSeaMax;
        fixed4 _ColorIce;
        float _MixDepth;
        float _MixNoiseSize;
        float _MixNoiseStrength;
        float _DepthIce;
        float _DepthDirt;
        float _DepthMountains;
        float _DepthGround;
#ifndef DYNAMIC_FLUID
        float _DepthSea;            
#endif
        float _DepthSeaBottom;
        float _SeaMetallic;
        float _Metallic;
        //float _SeaSmoothness;
        
        //#define CALC_NORMAL
        #define INCLUDE_INPUT_WORLD_NORMAL
        #define EXTENSION_INPUT \
            float2 uv_MountainsTex; \
            float2 uv_DirtTex; \
            float2 uv_GroundTex; \
            float2 uv_SandTex;              
        
        #include "Assets/DepthSensorSandbox/Resources/Materials/utils.cginc"
        #include "Assets/DepthSensorSandbox/Resources/Materials/sandbox.cginc"
        #include "Assets/DepthSensorSandbox/Resources/Materials/perlin.cginc"
#ifdef DYNAMIC_FLUID
        #include "Assets/DepthSensorSandbox/Resources/Materials/fluid.cginc"
#endif

        float smooth(float d, float z) {
            return smooth(d - _MixDepth, d + _MixDepth, z);
        }
        
        float smoothNoise(float d, float z, float noise) {
            float mix = _MixDepth * noise;
            return smooth(d - mix, d + _MixDepth, z);
        }
        
        inline void addSample(inout fixed4 c, sampler2D t, fixed3 modHSV, 
            float2 uv, float d, float z, float noise) 
        {
            c = lerp(adjust(tex2D(t, uv), modHSV), c, smoothNoise(d, z, noise));
        }
        
        void surf (Input i, inout SurfaceOutputStandard o) {
            float z = i.vpos.z;
#ifdef DYNAMIC_FLUID
            TYPE_HEIGHT h = HEIGHT_SAMPLE(CURR);
    #ifdef DRAW_LANDSCAPE
            z = TERRAIN_H(h);
    #endif
#endif
            float dSeaBottom = percentToDepth(_DepthSeaBottom);
            float dSea = percentToDepth(_DepthSea);
            float dGround = percentToDepth(_DepthGround);
            float dDirt = percentToDepth(_DepthDirt);
            float dMountains = percentToDepth(_DepthMountains);
            float dIce = percentToDepth(_DepthIce * 1.5);
            float noise = perlin(i.texcoord * _MixNoiseSize) * _MixNoiseStrength;
            
            fixed4 c = adjust(tex2D(_SandTex, i.uv_SandTex), _hsvSand);
            addSample(c, _GroundTex, _hsvGround, i.uv_GroundTex , dGround, z, noise);
            addSample(c, _DirtTex, _hsvDirt, i.uv_DirtTex, dDirt, z, noise);
            addSample(c, _MountainsTex, _hsvMountains, i.uv_MountainsTex, dMountains, z, noise);
            c.rgb = lerp(c.rgb, _ColorIce.rgb, _ColorIce.a * smooth(dMountains, dIce, z));
                
#ifdef DYNAMIC_FLUID
            dSeaBottom = dSeaBottom - dSea;
            dSea = 0;
            z = WATER_H(h);
#endif
            float kSeaColor = smooth(dSea, dSeaBottom, z);
            float kSeaAlptha = smooth(dSea, dSea + _MixDepth / 2, z);
            fixed4 sea = lerp(_ColorSeaMin, _ColorSeaMax, kSeaColor);
            c.rgb = lerp(c.rgb, sea.rgb, sea.a * kSeaAlptha);
            
            o.Metallic = lerp(_Metallic, _SeaMetallic, kSeaAlptha);
            //o.Smoothness = kSeaAlptha * _SeaSmoothness;
            //o.Normal = lerp(o.Normal, WorldToTangentNormalVector(i, float3(0, 1, 0)), smoothstep(0, 0.1, kSeaColor));
            o.Albedo = c;
        }
        ENDCG 
        
        UsePass "Sandbox/ShadowCaster"
    }
}
