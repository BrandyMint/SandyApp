Shader "Sandbox/ColorHands" {
    Properties {
        _ColorHands ("Color Hands", Color) = (0.8, 0.2, 0, 0.5)
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
            #include "Assets/DepthSensorSandbox/Resources/Materials/sandbox.cginc"

            sampler2D _DepthToColorTex;
            sampler2D _ColorTex; float4 _ColorTex_TexelSize;
            sampler2D _HandsMaskTex;
            fixed4 _ColorHands;

            fixed4 frag (v2f i) : SV_Target {
                float2 uv = tex2D(_DepthToColorTex, i.uv).rg * _ColorTex_TexelSize.xy;
                fixed4 col = tex2D(_ColorTex, uv);
                fixed hands = tex2D(_HandsMaskTex, i.uv).r;
                if (hands > 0)
                    col.rgb = lerp(col.rgb, _ColorHands.rgb, 1 - _ColorHands.a);   
                return col;
            }
            ENDCG
        }
        UsePass "Sandbox/ShadowCaster"
    }
}