Shader "Sandbox/Game/BalloonsSky" {
    Properties {
        _NoiseSize ("Perlin Size", Float) = 1
        _NoiseStrength ("Perlin Strength", Float) = 1
        
        _ColorMin ("Color Min", Color) = (0, 0, 1, 1)
        _ColorMax ("Color Max", Color) = (1, 1, 1, 1)
        _Mix ("Mix", Float) = 0.5 
        
        _DepthZero ("Depth Zero", Float) = 2
        _DepthMinOffset ("Depth Min Offset", Float) = 0.5
        _DepthMaxOffset ("Depth Max Offset", Float) = 0.5
        _DepthSliceOffset ("Depth Slice", Float) = 0.05
    }
    
    SubShader {
        Tags { "RenderType"="Opaque" "Queue" = "Background"}
        
		Lighting Off
		ZWrite Off
		ZTest Off
		//Blend One OneMinusSrcAlpha

        Pass {
            CGPROGRAM
            #pragma multi_compile _ CALC_DEPTH
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            
            fixed4 _ColorMin;
            fixed4 _ColorMax;
            float _NoiseSize;
            float _NoiseStrength;
            float _DepthSliceOffset;

            #include "Assets/DepthSensorSandbox/Resources/Materials/utils.cginc"
            #include "Assets/DepthSensorSandbox/Resources/Materials/sandbox.cginc"
            #include "Assets/DepthSensorSandbox/Resources/Materials/perlin.cginc"            

            fixed4 frag (v2f i) : SV_Target {
                float z = i.vpos.z;
                float noise = (perlin(i.uv * _NoiseSize) * 2 - 1) * _NoiseStrength;
                
                float max = _DepthZero - _DepthMaxOffset;
                float min = _DepthZero + _DepthMinOffset;
                float hands = max - _DepthSliceOffset;
                if (hands > z)
                    return fixed4(0, 0, 0, 1);
                
                float k = inverseLerp(min, max, z) + noise;                
                fixed4 c = lerp(_ColorMin, _ColorMax, k);
                return c;
            }
            ENDCG
        }
        UsePass "Sandbox/ShadowCaster"
    }
}
