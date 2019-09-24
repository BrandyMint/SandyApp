Shader "Sandbox/Fluid" {
    Properties {
        _FluidSpeed ("Fluid Speed", Float) = 1
        _FluidViscosity ("Fluid Viscosity", Float) = 1
        _DepthZero ("Depth Zero", Float) = 1.6
        _Thickness ("Thickness", Float) = 0
        _ThicknessRunoff ("Thickness runoff", Float) = 0.01
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

            #include "UnityCG.cginc"
            #include "sandbox.cginc"
            #include "fluid.cginc"
            
            fixed4 fragColor (v2f i, float4 fluid) {
                float color = fluid.z;
                if (color > 0) color += 0.3;
                return fixed4(abs(fluid.x) * 100, abs(fluid.y) * 100, color * 2, 1);
                if (color > 0)
                    return fixed4(0, 0, color, 1);
                return fixed4(0, 1, 0, 1);
            }
            ENDCG
        }
    }    
    Fallback "Mobile/VertexLit"
}