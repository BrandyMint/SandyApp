Shader "Sandbox/Game/Eatable" {
    Properties {
        _FieldTex ("Field", 2D) = "white" {}
        _PlayersTex ("Player Colors", 2D) = "white" {}
        _PlayerColorAlpha ("Player Color Aplpha", Float) = 1
        
        _ColorMin ("Color Min", Color) = (0, 0, 0, 1)
        _ColorMax ("Color Max", Color) = (1, 1, 1, 1)
        
        _DepthSliceOffset ("Depth Slice", Float) = 0.05        
        _DotSlice ("Dot Slice", Float) = 0.7        
        _DotSliceOverride ("Dot Slice Override", Float) = 0.5
        
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

            #include "UnityCG.cginc"
            
            #define CALC_NORMAL

            #include "Assets/DepthSensorSandbox/Resources/Materials/utils.cginc"
            #include "Assets/DepthSensorSandbox/Resources/Materials/sandbox.cginc"
            #include "Assets/Games/Common/Materials/depth_slice.cginc"            
            #include "Assets/Games/Common/Materials/multi_players.cginc"

            fixed4 _ColorMin;
            fixed4 _ColorMax;
            float _DotSliceOverride;

            fixed4 frag (v2f i) : SV_Target {
                float hands = fragSliceDot(i, _DotSliceOverride);
                if (hands > 0)
                    return fixed4(0, 0, 0, 1);
                
                float z = i.vpos.z;
                float max = _DepthZero - _DepthMaxOffset;
                float min = _DepthZero + _DepthMinOffset;
                float k = inverseLerp(min, max, z);
                fixed4 c = lerp(_ColorMin, _ColorMax, k);
                fixed4 player = colorMultiPlayers(i);
                if (player.a < 1)
                    return c * player * player.a;
                return player;
            }

            ENDCG
        }
        UsePass "Sandbox/ShadowCaster"
    }
}
