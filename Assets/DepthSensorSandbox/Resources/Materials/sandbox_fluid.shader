Shader "Sandbox/Fluid" {
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
            #pragma multi_compile _ CALC_DEPTH CLEAR_FLUID
            #pragma vertex vert
            #pragma fragment frag
            
            #define USE_MRT_FLUID
            //#define PROVIDE_FLUX

            #include "UnityCG.cginc"
            #include "sandbox.cginc"
            #include "fluid.cginc"

#ifdef PROVIDE_FLUX
            fixed4 fragColor (v2f i, TYPE_HEIGHT h, TYPE_FLUX flux) {
                float water = WATER_H(h);
                float terrain = TERRAIN_H(h);
                if (water > 0) water += 0.3;
                terrain /= 8;
                flux *= 1000;
                flux.g += terrain;
                flux.b += water;
                return flux;
            }
#else
            fixed4 fragColor (v2f i, TYPE_HEIGHT h) {
                float water = WATER_H(h) * 3;
                float terrain = TERRAIN_H(h);
                if (water > 0) water += 0.3;
                terrain /= 6;
                return fixed4(0, terrain, water, 1);
            }
#endif
            ENDCG
        }
    }    
    Fallback "Mobile/VertexLit"
}