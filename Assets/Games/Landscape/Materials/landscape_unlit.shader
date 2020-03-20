Shader "Sandbox/Game/LandscapeUnlit" {
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
                
        _ColorSeaMin ("Color Sea Min", Color) = (0, 1, 1, 1)
        _DepthSea ("Depth Sea", Float) = 0
        
        _ColorSeaMax ("Color Sea Max", Color) = (0, 0, 1, 1)
        _DepthSeaBottom ("Depth Sea Bottom", Float) = -0.2
        
        _DepthZero ("Depth Zero", Float) = 2
        _DepthMinOffset ("Depth Min Offset", Float) = 0.5
        _DepthMaxOffset ("Depth Max Offset", Float) = 0.5
    }
    
    SubShader {
        Tags { "RenderType"="Opaque" "Queue" = "Background"}
        
		Lighting Off
		ZWrite Off
		//Blend One OneMinusSrcAlpha

        Pass {
            CGPROGRAM
            #pragma multi_compile _ CALC_DEPTH 
            #pragma multi_compile __ DYNAMIC_FLUID 
            #pragma multi_compile ___ DRAW_LANDSCAPE
            #pragma vertex vertLandscape
            #pragma fragment frag

            #include "UnityCG.cginc"
            
            sampler2D _MountainsTex; float4 _MountainsTex_ST;
            fixed3 _hsvMountains;
            sampler2D _DirtTex; float4 _DirtTex_ST;
            fixed3 _hsvDirt;
            sampler2D _GroundTex; float4 _GroundTex_ST;
            fixed3 _hsvGround;
            sampler2D _SandTex; float4 _SandTex_ST;
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
            
            #define EXTENSION_V2F \
                float2 uvMountains : TEXCOORD6; \
                float2 uvDirt : TEXCOORD5; \
                float2 uvGround : TEXCOORD4;\
                float2 uvSand : TEXCOORD3;                

            #include "Assets/DepthSensorSandbox/Resources/Materials/utils.cginc"
            #include "Assets/DepthSensorSandbox/Resources/Materials/sandbox.cginc"
            #include "Assets/DepthSensorSandbox/Resources/Materials/perlin.cginc"
#ifdef DYNAMIC_FLUID
            #include "Assets/DepthSensorSandbox/Resources/Materials/fluid.cginc"
#endif
            
            v2f vertLandscape (appdata v) {
                v2f o = vert(v);
                o.uvMountains = TRANSFORM_TEX(o.uv, _MountainsTex);
                o.uvDirt = TRANSFORM_TEX(o.uv, _DirtTex);
                o.uvGround = TRANSFORM_TEX(o.uv, _GroundTex);
                o.uvSand = TRANSFORM_TEX(o.uv, _SandTex);
                return o;
            }
            
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

            fixed4 frag (v2f i) : SV_Target {
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
                float noise = perlin(i.uv * _MixNoiseSize) * _MixNoiseStrength;
                
                fixed4 c = adjust(tex2D(_SandTex, i.uvSand), _hsvSand);
                addSample(c, _GroundTex, _hsvGround, i.uvGround , dGround, z, noise);
                addSample(c, _DirtTex, _hsvDirt, i.uvDirt, dDirt, z, noise);
                addSample(c, _MountainsTex, _hsvMountains, i.uvMountains, dMountains, z, noise);
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
                return c;
            }
            ENDCG
        }
        UsePass "Sandbox/ShadowCaster"
    }
}
