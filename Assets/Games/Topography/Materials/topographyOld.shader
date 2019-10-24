Shader "Sandbox/Game/TopographyOld" {
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

            #include "Assets/DepthSensorSandbox/Resources/Materials/utils.cginc"
            #include "Assets/DepthSensorSandbox/Resources/Materials/sandbox.cginc"
            
            float _UpperLayers;
            float _LowerLayers;
            float _Middle;
            float _BorderWidth;
            fixed4 _BorderColor;
            sampler2D _ColorTex;
                        
            float calcLayerColorIndex(float min, float max, float d, float layers) {
                float layerId = floor(saturate(inverseLerp(min, max, d)) * layers);
                return saturate(layerId / layers);
            }
            
            float calcBorder(float min, float max, float d, float layers) {
                float layer = clamp(inverseLerp(min, max, d) * layers, 0.5, layers + 0.5);
                float dist = abs(layer - round(layer));
                return step(dist, _BorderWidth);
            }
            
            void getColorAndBorder(float min, float max, float d, float layers, out float color, out float border) {
                color = calcLayerColorIndex(min, max, d, layers);
                border = calcBorder(min, max, d, layers);
            }
            
            fixed4 frag (v2f i) : SV_Target {
                float d = i.vpos.z;
                float max = _DepthZero - _DepthMaxOffset;
                float min = _DepthZero + _DepthMinOffset;
                float k;
                float border;
                if (d < _DepthZero) { //Upper
                    getColorAndBorder(_DepthZero, max, d, _UpperLayers - 1, k, border);
                    k = lerp(_Middle, 1, k);
                } else { //Lower
                    getColorAndBorder(min, _DepthZero, d, _LowerLayers, k, border);
                    k = lerp(0, _Middle, k);
                }
                return lerp(tex2D(_ColorTex, k), _BorderColor, border);
            }
            ENDCG
        }
    }
    Fallback "Mobile/VertexLit"
}
