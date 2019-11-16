﻿Shader "Sandbox/Game/SprayBG" {
    Properties {        
        _ColorMin ("Color Min", Color) = (0, 0, 0, 1)
        _ColorMax ("Color Max", Color) = (0.1, 0.1, 0.1, 1)
        _ColorHands ("Color Hands", Color) = (0.3, 0.3, 0.3, 1)
        
        _DepthSliceOffset ("Depth Slice", Float) = 0.05        
        _DotSlice ("Dot Slice", Float) = 0.7        
        _DotSliceOverride ("Dot Slice Override", Float) = 0.5        
        
        _MixDepthPercent ("Mix Depth Percent", Float) = 0.2
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
            
            fixed4 _ColorMin;
            fixed4 _ColorMax;
            fixed4 _ColorHands;
            float _DotSliceOverride;

            fixed4 frag (v2f i) : SV_Target {
                float hands = fragSliceDot(i, _DotSliceOverride);
                if (hands > 0)
                    return _ColorHands;
                
                float z = i.vpos.z;
                float max = _DepthZero - _DepthMaxOffset;
                float min = _DepthZero + _DepthMinOffset;
                float k = inverseLerp(min, max, z);
                fixed4 c = lerp(_ColorMin, _ColorMax, k);
                return c;
            }
            ENDCG
        }
        UsePass "Sandbox/ShadowCaster"
    }
}
