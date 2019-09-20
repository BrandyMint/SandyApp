Shader "Sandbox/FluidClear" {
    Properties {
        _DepthZero ("Depth Zero", Float) = 1.6
    }

    SubShader {
        Tags { "RenderType"="Opaque" }
        
		Lighting Off

        Pass {
            CGPROGRAM
            #pragma multi_compile _ CALC_DEPTH
            #pragma vertex vert
            #pragma fragment fragFluidClear

            #include "UnityCG.cginc"
            #include "sandbox.cginc"
            #include "fluid.cginc"
            ENDCG
        }
    }
    Fallback "Mobile/VertexLit"
}