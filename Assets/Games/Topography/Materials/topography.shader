Shader "Sandbox/Game/Topography" {
    Properties {
        _ColorTex ("Scale texture", 2D) = "white" {}
        _Middle ("Middle", Float) = 0.5
        _UpperLayers ("Upper layers", Float) = 2
        _LowerLayers ("Lower layers", Float) = 2
        _BorderColor ("Border Color", Color) = (0, 0, 0, 1)
        _BorderWidth ("Border Width", Float) = 0.01
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
            float _Middle;
            float _BorderWidth;
            fixed4 _BorderColor;
            sampler2D _ColorTex;
                        
            float calcLayerColorIndex(float max, float d, float layers) {
                float layerId = floor(saturate(inverseLerp(0, max, d)) * layers);
                return saturate(layerId / layers);
            }
            
            float calcBorder(float max, float d, float layers, float3 normal) {
                float layer = clamp(inverseLerp(0, max, d) * layers, 0, layers + 0.5);
                float dist = abs(layer - round(layer)) / layers * max;
                float a = acos(dot(normal, float3(0, 0, 1)));
                dist /= tan(a);
                return step(dist, _BorderWidth);
            }
            
            void getColorAndBorder(float max, float d, float layers, float3 normal, out float color, out float border) {
                color = calcLayerColorIndex(max, d, layers);
                border = calcBorder(max, d, layers, normal);
            }
            
            fixed4 frag (v2f i) : SV_Target {
                float d = i.vpos.z;
                float k;
                float border;
                if (d < _DepthZero) { //Upper
                    getColorAndBorder(_DepthMaxOffset, _DepthZero - d, _UpperLayers, i.normal, k, border);
                    k = lerp(_Middle, 1, k);
                } else { //Lower
                    getColorAndBorder(_DepthMinOffset, d - _DepthZero, _LowerLayers, i.normal, k, border);
                    k = lerp(0, _Middle, 1 - k - 1/(_LowerLayers + 1));
                }
                return lerp(tex2D(_ColorTex, k), _BorderColor, border);
            }
            ENDCG
        }
        UsePass "Sandbox/ShadowCaster"
    }
}
