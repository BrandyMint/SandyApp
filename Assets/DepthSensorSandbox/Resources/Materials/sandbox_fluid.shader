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
            #pragma fragment fragFluid
       
            #define FORCE_POINT_SAMPLER

            #include "UnityCG.cginc"
            #include "sandbox.cginc"
            #include "fluid.cginc"
            ENDCG
        }
    }    
    Fallback "Mobile/VertexLit"
}