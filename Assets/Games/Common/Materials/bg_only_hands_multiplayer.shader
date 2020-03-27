Shader "Sandbox/Game/BGOnlyHandsMultiplayer" {
    Properties {
        _BaseAlpha ("Base Alpha", Float) = 0.3
        _FieldLight ("Field Light", Float) = 1
        _ColorHands ("Color Hands", Color) = (1, 1, 1, 0.4)
        _FieldTex ("Field", 2D) = "white" {}
        _PlayersTex ("Player Colors", 2D) = "white" {}
        _PlayerColorAlpha ("Player Color Aplpha", Float) = 1
        
        _DepthZero ("Depth Zero", Float) = 2
        _DepthMinOffset ("Depth Min Offset", Float) = 0.5
        _DepthMaxOffset ("Depth Max Offset", Float) = 0.5
    }
    
    SubShader {
        Tags { "RenderType"="Transparent" "Queue" = "Background"}
        Blend SrcAlpha One 
        
		Lighting Off
		ZWrite Off
		ZTest Off

        Pass {
            CGPROGRAM
            #pragma multi_compile _ CALC_DEPTH
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            
            #include "Assets/DepthSensorSandbox/Resources/Materials/utils.cginc"
            #include "Assets/DepthSensorSandbox/Resources/Materials/sandbox.cginc"
            #include "Assets/DepthSensorSandbox/Resources/Materials/hands.cginc"
            #include "Assets/Games/Common/Materials/multi_players.cginc"
            
            float _BaseAlpha;
            float _FieldLight;
            fixed4 _ColorHands;

            fixed4 frag (v2f i) : SV_Target {                
                fixed4 c = colorMultiPlayers(i);
                if (c.a < 1) {
                    fixed2 uv = i.screenPos.xy / i.screenPos.w;
                    float z = i.vpos.z;
                    float k = inverseLerp( _DepthZero - _DepthMaxOffset, _DepthZero + _DepthMinOffset, z);
                    float len = length(uv - 0.5);
                    float a = _BaseAlpha * lerp(0, k / 2 + 0.5, len * len);
                    c.a = a;
                    c.a *= a * _FieldLight;
                }
                
                c.rgb = lerp(c.rgb, _ColorHands.rgb, _ColorHands.a * handsInteractAlpha(i));
                return c;
            }
            ENDCG
        }    
    }
    Fallback "Sandbox/ShadowCaster"
}
