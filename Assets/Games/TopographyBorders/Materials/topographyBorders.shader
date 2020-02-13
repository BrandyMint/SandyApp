Shader "Sandbox/Game/TopographyBorders" {
    Properties {
        _UpperLayers ("Upper layers", Float) = 2
        _LowerLayers ("Lower layers", Float) = 2
        _Color ("Color", Color) = (0, 0, 0, 1)
        _BorderColor ("Border Color", Color) = (1, 1, 1, 1)
        _BorderWidth ("Border Width", Float) = 0.01
        _BorderZeroMult ("Border Zero Mult", Float) = 3
        _DepthZero ("Depth Zero", Float) = 1.0
        _DepthMaxOffset ("Max Offset", Float) = 0.1
        _DepthMinOffset ("Min Offset", Float) = 0.1
    }
    
    SubShader {
        Tags { "RenderType"="Opaque" }
        
		Lighting Off
		//Blend One OneMinusSrcAlpha

        Pass {
            CGPROGRAM
            #pragma multi_compile _ CALC_DEPTH 
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            #define CALC_NORMAL

            #include "Assets/DepthSensorSandbox/Resources/Materials/utils.cginc"
            #include "Assets/DepthSensorSandbox/Resources/Materials/sandbox.cginc"
            
            float _UpperLayers;
            float _LowerLayers;
            float _BorderWidth;
            float _BorderZeroMult;
            fixed4 _BorderColor;
            fixed4 _Color;
            
            float calcBorder(float max, float d, float layers, float3 normal) {
                float layer = clamp(inverseLerp(0, max, d) * layers, 0, layers + 0.5);
                float dist = abs(layer - round(layer)) / layers * max;
                float a = acos(dot(normal, float3(0, 0, 1)));
                dist /= tan(a);
                float width = layer < 0.5 ? _BorderZeroMult * _BorderWidth : _BorderWidth;
                return inverseLerp(width, 0, dist);
            }
            
            fixed4 frag (v2f i) : SV_Target {
                float d = i.vpos.z;
                float k;
                float border;
                if (d < _DepthZero) { //Upper
                    border = calcBorder(_DepthMaxOffset, _DepthZero - d, _UpperLayers, i.normal);
                } else { //Lower
                    border = calcBorder(_DepthMinOffset, d - _DepthZero, _LowerLayers, i.normal);
                }
                return lerp(_Color, _BorderColor, border);
            }
            ENDCG
        }
        UsePass "Sandbox/ShadowCaster"
    }
}
