Shader "Sandbox/Game/BGColors2HandsPerlin" {
    Properties {
        _NoiseSize ("Perlin Size", Float) = 1
        _NoiseStrength ("Perlin Strength", Float) = 1
        
        _ColorMin ("Color Min", Color) = (0, 0, 1, 1)
        _ColorMax ("Color Max", Color) = (1, 1, 1, 1)
        _ColorHands ("Color Hands", Color) = (1, 1, 1, 0.6)
        
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
            
            #define ENABLE_NOISE
            
            #include "bg_colors_base.cginc"
            ENDCG
        }
        UsePass "Sandbox/ShadowCaster"
    }
}
