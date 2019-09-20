Shader "Sandbox/Fluid" {
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
            #pragma fragment frag
            
            #define USE_MRT_FLUID

            #include "UnityCG.cginc"
            #include "sandbox.cginc"
            #include "fluid.cginc"
            
            fixed4 fragColor (v2f i, half4 fluid) {
                half color = fluid.z;
                return fixed4(color, 0, 0, 1);
            }
            ENDCG
        }
    }    
    Fallback "Mobile/VertexLit"
}