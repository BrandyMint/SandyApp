Shader "Sandbox/Game" {
    Properties {
        _DepthSliceOffset ("Depth Slice", Float) = 0.05        
        _DotSlice("Dot Slice", Float) = 0.7
        
        _DepthZero ("Depth Zero", Float) = 2
        _DepthMinOffset ("Depth Min Offset", Float) = 0.5
        _DepthMaxOffset ("Depth Max Offset", Float) = 0.5
    }
    
    SubShader {
        Tags { "RenderType"="Opaque" }
        
		Lighting Off
		ZWrite Off
		ZTest Off
		//Blend One OneMinusSrcAlpha

        Pass {
            Name "DepthSlice"

            CGPROGRAM
            #pragma multi_compile _ CALC_DEPTH
            #pragma vertex vert
            #pragma fragment fragSlice
            
            #define CALC_NORMAL

            #include "UnityCG.cginc"
                        
            #include "Assets/DepthSensorSandbox/Resources/Materials/sandbox.cginc"
            #include "depth_slice.cginc"
            ENDCG
        }        
    }
}
