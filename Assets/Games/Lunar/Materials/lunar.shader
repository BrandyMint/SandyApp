Shader "Sandbox/Game/Lunar" {
    Properties {
        _Color ("Color", Color) = (0.5, 0.5, 0.5, 1)
        _DepthZero ("Depth Zero", Float) = 1.0
        _DepthMaxOffset ("Max Offset", Float) = 0.1
        _DepthMinOffset ("Min Offset", Float) = 0.1
    }
    
    SubShader {
        Tags { "LightMode"="ForwardBase" }
        
		Lighting Off
		//Blend One OneMinusSrcAlpha

        Pass {
            CGPROGRAM
            #pragma multi_compile _ CALC_DEPTH 
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
            #include "AutoLight.cginc"
            
            #define CALC_LIGHT

            #include "Assets/DepthSensorSandbox/Resources/Materials/utils.cginc"
            #include "Assets/DepthSensorSandbox/Resources/Materials/sandbox.cginc"

            fixed4 _Color;
            
            fixed4 frag (v2f i) : SV_Target {
                float d = i.vpos.z;
                float max = _DepthZero - _DepthMaxOffset;
                float min = _DepthZero + _DepthMinOffset;
                
                fixed4 col = _Color;
                
                fixed shadow = SHADOW_ATTENUATION(i);
                fixed3 light = i.light.rgb;
                col.rgb *= light * shadow;
                return col;
            }
            ENDCG
        }
        
        UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
    }
    Fallback "Mobile/VertexLit"
}
