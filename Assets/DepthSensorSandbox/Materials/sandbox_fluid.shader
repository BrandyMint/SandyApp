Shader "Sandbox/Fluid" {
    Properties {
        _DepthZero ("Depth Zero", Float) = 1.6
        _Water ("Depth Water", Float) = 3.0
    }

    SubShader {
        Tags { "RenderType"="Opaque" }
        
		Lighting Off

        Pass {
            CGPROGRAM
            #pragma multi_compile _ CALC_DEPTH
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "sandbox.cginc"

            float _DepthZero;
            float _Water;
            sampler2D _FluidPrevTex;
            
            struct FragmentOutput {
                fixed4 color : SV_Target0;
                half4 fluid : SV_Target1;
            };

            FragmentOutput frag (v2f i) {
                FragmentOutput o;
                half4 fluid = tex2D(_FluidPrevTex, i.screenPos.xy/i.screenPos.w);
                half color = fluid.z / _Water;
                o.color = fixed4(color, 0, 0, 1);
                
                float d = i.pos.z;
                half height = max(0, d - _DepthZero);
                o.fluid = half4(0, 0, height, 0);
                return o;
            }
            ENDCG
        }
    }
    Fallback "Mobile/VertexLit"
}