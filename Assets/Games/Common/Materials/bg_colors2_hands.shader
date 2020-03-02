﻿Shader "Sandbox/Game/BGColors2Hands" {
    Properties {
        _ColorMin ("Color Min", Color) = (0, 0, 0, 1)
        _ColorMax ("Color Max", Color) = (0.1, 0.1, 0.1, 1)
        _ColorHands ("Color Hands", Color) = (0.3, 0.3, 0.3, 1)
        
        _DepthZero ("Depth Zero", Float) = 2
        _DepthMinOffset ("Depth Min Offset", Float) = 0.5
        _DepthMaxOffset ("Depth Max Offset", Float) = 0.5
    }
    
    SubShader {
        Tags { "RenderType"="Opaque" "Queue" = "Background"}
        
		Lighting Off
		ZWrite Off
		ZTest Off

        Pass {
            CGPROGRAM
            #pragma multi_compile _ CALC_DEPTH
            #pragma vertex vert
            #pragma fragment frag
            
            #include "bg_colors_base.cginc"
            
            ENDCG
        }
        UsePass "Sandbox/ShadowCaster"
    }
}
