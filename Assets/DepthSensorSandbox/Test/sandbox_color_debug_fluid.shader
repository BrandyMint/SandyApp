Shader "Sandbox/ColorDebugFluid" {
    Properties {
        _TerrainMin ("Terrain Min", Float) = 2
        _TerrainMax ("Terrain Max", Float) = 0.1
        _WaterMin ("Water Min", Float) = 1
        _WaterMax ("Water Max", Float) = 0
        _ColorBright ("Color Bright", Float) = 0.5
        _FluxBright ("_FluxBright", Float) = 100
    }

    SubShader {
        Tags { "RenderType"="Opaque" }
        
		Lighting Off

        Pass {
            CGPROGRAM
            #pragma multi_compile _ CALC_DEPTH
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "Assets/DepthSensorSandbox/Resources/Materials/utils.cginc"
            #include "Assets/DepthSensorSandbox/Resources/Materials/sandbox.cginc"
            #include "Assets/DepthSensorSandbox/Resources/Materials/fluid.cginc"
            
            float _TerrainMin;
            float _TerrainMax;
            float _WaterMin;
            float _WaterMax;
            float _ColorBright;
            float _FluxBright;

            fixed4 frag (v2f i) : SV_Target {
                TYPE_HEIGHT h = HEIGHT_SAMPLE(CURR);
                TYPE_FLUX flux = FLUX_SAMPLE(CURR);
                float water = WATER_H(h);
                float terrain = TERRAIN_H(h);
                terrain = inverseLerp(_TerrainMin, _TerrainMax, terrain);
                water = inverseLerp(_WaterMin, _WaterMax, water) * water;
                
                fixed4 c = flux * _FluxBright;
                c.g += terrain * _ColorBright;
                c.b += water * _ColorBright;
                return c;
            }

            ENDCG
        }
    }    
    Fallback "Mobile/VertexLit"
}