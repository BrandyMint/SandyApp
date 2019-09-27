Shader "Sandbox/FluidWithDraw" {
    Properties {
        _FluxAcceleration ("Flux Acceleration", Float) = 9.8
        _DepthZero ("Depth Zero", Float) = 1.6
        _CellArea ("Cell Area", Float) = 1
        _CellHeight ("Cell Height", Float) = 1
    }

    SubShader {
        Tags { "RenderType"="Opaque" }
        
		Lighting Off

        Pass {
            CGPROGRAM
            #pragma multi_compile _ CALC_DEPTH
            #pragma multi_compile __ CLEAR_FLUID
            #pragma vertex vert
            #pragma fragment fragFluid
       
            #define FORCE_POINT_SAMPLER
            #define MODIFY_FLUID

            #include "UnityCG.cginc"
            #include "Assets/DepthSensorSandbox/Resources/Materials/sandbox.cginc"
            #include "Assets/DepthSensorSandbox/Resources/Materials/fluid.cginc"
            
            float4 _Instrument;
            #define INSTRUMENT_POS(i) (i.xy)
            #define INSTRUMENT_SIZE(i) (i.z)            
            #define INSTRUMENT_STRENGTH(i) (i.w)
            int _InstrumentType;
            #define INSTRUMENT_TYPE_NONE 0
            #define INSTRUMENT_TYPE_REMOVE_TERRAIN 1
            #define INSTRUMENT_TYPE_ADD_TERRAIN 2
            
            void modifyFluid(v2f i, inout TYPE_HEIGHT height, inout TYPE_FLUX flux,
                TYPE_HEIGHT h, TYPE_HEIGHT hl, TYPE_HEIGHT hr, TYPE_HEIGHT ht, TYPE_HEIGHT hb,
                TYPE_FLUX f, TYPE_FLUX fl, TYPE_FLUX fr, TYPE_FLUX ft, TYPE_FLUX fb
            ) {
                TERRAIN_H(height) = TERRAIN_H(h);                
                if (_InstrumentType == INSTRUMENT_TYPE_NONE)
                    return;
                    
                float2 uv = i.screenPos.xy / i.screenPos.w * _ScreenParams.xy;
                float dist = length(uv - INSTRUMENT_POS(_Instrument));
                if (dist < INSTRUMENT_SIZE(_Instrument)) {
                    float k = smoothstep(0, 1, 1 - dist / INSTRUMENT_SIZE(_Instrument));
                    float dStrength = unity_DeltaTime.x * k * INSTRUMENT_STRENGTH(_Instrument);
                    if (_InstrumentType == INSTRUMENT_TYPE_ADD_TERRAIN) {
                        TERRAIN_H(height) -= dStrength;
                    } else
                    if (_InstrumentType == INSTRUMENT_TYPE_REMOVE_TERRAIN) {
                        TERRAIN_H(height) += dStrength;
                    }
                }
            }

            ENDCG
        }
    }    
    Fallback "Mobile/VertexLit"
}