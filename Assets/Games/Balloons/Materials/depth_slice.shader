Shader "Sandbox/Game/DepthSlice" {
    Properties {
        _DepthSliceOffset ("Depth Slice", Float) = 0.05
        
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
            
            float _DepthSliceOffset;

            #include "UnityCG.cginc"
            #include "Assets/DepthSensorSandbox/Resources/Materials/sandbox.cginc"            

            float frag (v2f i) : SV_Target {
                float z = i.vpos.z;
                z = _DepthZero - _DepthMaxOffset - _DepthSliceOffset - z;
                return step(0, z);
            }
            ENDCG
        }        
    }
}
