Shader "Sandbox/ColorHands" {
    Properties {
        _ColorErrorAura ("Color Error Aura", Color) = (0.8, 0.2, 0, 0.5)
        _ColorHands ("Color Hands", Color) = (0, 0.8, 0.1, 0.5)
        _ColorHandsInteract ("Color Hands Interact", Color) = (0, 0.2, 0.8, 0.5)
        _HandsDepthMaxDebug ("Hands Depth max", Float) = 0.04
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
            sampler2D _HandsDepthTex;
            fixed4 _ColorErrorAura;
            fixed4 _ColorHands;
            fixed4 _ColorHandsInteract;
            float _HandsDepthMax;
            float _HandsDepthMaxDebug;

            fixed4 frag (v2f i) : SV_Target {
                float2 uv = tex2D(_DepthToColorTex, i.uv).rg * _ColorTex_TexelSize.xy;
                fixed4 col = tex2D(_ColorTex, uv);
                fixed hands = tex2D(_HandsMaskTex, i.uv).r * 256;
                /*float handsDepth = tex2D(_HandsDepthTex, i.uv).r * DEPTH_TO_FLOAT;
                if (handsDepth > 0) {
                    fixed4 handColor = handsDepth < _HandsDepthMaxDebug ? _ColorHandsInteract : _ColorHands;
                    col.rgb = lerp(col.rgb, handColor.rgb, 1 - handColor.a);
                }
                return col;*/
                if (hands >= 2) {
                    float handsDepth = tex2D(_HandsDepthTex, i.uv).r * DEPTH_TO_FLOAT;
                    fixed4 handColor = handsDepth > 0 && handsDepth < _HandsDepthMax ? _ColorHandsInteract : _ColorHands;
                    col.rgb = lerp(col.rgb, handColor.rgb, 1 - handColor.a);
                }
                else if (hands >= 1)
                    col.rgb = lerp(col.rgb, _ColorErrorAura.rgb, 1 - _ColorErrorAura.a);
                return col;
            }
            ENDCG
        }
        UsePass "Sandbox/ShadowCaster"
    }
}