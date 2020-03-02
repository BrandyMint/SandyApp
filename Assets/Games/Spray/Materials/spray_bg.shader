﻿Shader "Sandbox/Game/SprayBG" {
    Properties {
        _ProjectedTex ("Projected", 2D) = "clear" {}
        _ColorMin ("Color Min", Color) = (0, 0, 0, 1)
        _ColorMax ("Color Max", Color) = (0.1, 0.1, 0.1, 1)
        _ColorHands ("Color Hands", Color) = (0.3, 0.3, 0.3, 1)    
        
        _MixDepthPercent ("Mix Depth Percent", Float) = 0.2
        _DepthZero ("Depth Zero", Float) = 2
        _DepthMinOffset ("Depth Min Offset", Float) = 0.5
        _DepthMaxOffset ("Depth Max Offset", Float) = 0.5
    }
    
    SubShader {
        Tags { "RenderType"="Opaque" "Queue" = "Background" }
        
		Lighting Off
		ZWrite Off
		ZTest Off

        Pass {
            CGPROGRAM
            #pragma multi_compile _ CALC_DEPTH
            #pragma vertex vert
            #pragma fragment fragWithProjected
            #include "Assets/Games/Common/Materials/bg_colors_base.cginc"
            
            sampler2D _ProjectedTex;

            fixed4 fragWithProjected (v2f i) : SV_Target {
                fixed4 c = basFragColor(i);                
                fixed4 projected = tex2D(_ProjectedTex, i.screenPos.xy / i.screenPos.w);
                c.rgb = lerp(c.rgb, projected.rgb, projected.a);
                c.rgb = lerp(c.rgb, _ColorHands.rgb, _ColorHands.a * handsInteractAlpha(i));
                return c;
            }
            ENDCG
        }
        UsePass "Sandbox/ShadowCaster"
    }
}
