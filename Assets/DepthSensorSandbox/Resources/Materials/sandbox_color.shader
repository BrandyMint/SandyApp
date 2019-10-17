Shader "Sandbox/Color" {
    Properties {
        //_ColorZero ("Color Zero Depth", Color) = (0.3, 0.6, 1, 1)
    }
    
    SubShader {
        Tags { "RenderType"="Opaque" }
        
		Lighting Off
		//Blend One OneMinusSrcAlpha

        Pass {
            CGPROGRAM
            #pragma multi_compile _ CALC_DEPTH
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "sandbox.cginc"

            sampler2D _DepthToColorTex;
            sampler2D _ColorTex; float4 _ColorTex_TexelSize;

            fixed4 frag (v2f i) : SV_Target {
                float2 uv = tex2D(_DepthToColorTex, i.uv).rg * _ColorTex_TexelSize.xy;
                fixed4 col = tex2D(_ColorTex, uv);
                float d = i.pos.z;
                /*if (d > _DepthZero)
                    col *= _ColorZero;*/
                return col;
            }
            ENDCG
        }
    }
    Fallback "Mobile/VertexLit"
}