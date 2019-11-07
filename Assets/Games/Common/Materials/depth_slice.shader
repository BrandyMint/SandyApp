Shader "Sandbox/Game/DepthSlice" {
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
            CGPROGRAM
            #pragma multi_compile _ CALC_DEPTH
            #pragma vertex vert
            #pragma fragment frag
            
            #define CALC_NORMAL

            #include "UnityCG.cginc"
            #include "Assets/DepthSensorSandbox/Resources/Materials/utils.cginc"
            #include "Assets/DepthSensorSandbox/Resources/Materials/sandbox.cginc"
            
            float _DepthSliceOffset;
            float _DotSlice;

            float frag (v2f i) : SV_Target {
                float z = i.vpos.z;
                z = _DepthZero - _DepthMaxOffset - _DepthSliceOffset - z;
                float d = dot(i.normal, float3(0, 0, 1));
                if (d < _DotSlice)
                    return 0;
                return max(0, z);
            }
            ENDCG
        }        
    }
}
