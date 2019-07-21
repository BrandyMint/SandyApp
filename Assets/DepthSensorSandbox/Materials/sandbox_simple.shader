Shader "Sandbox/Simple" {
    Properties {
        _Color1 ("Color 1", Color) = (1, 1, 1, 1)
        _Depth1 ("Depth 1", Float) = 4.46        
        _Color2 ("Color 2", Color) = (0, 1, 0, 1)
        _Depth2 ("Depth 2", Float) = 4.56
        _Color3 ("Color 3", Color) = (0, 0, 1, 1)
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

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float4 clip : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 pos : TEXCOORD1;
            };
                        
            #include "sandbox.cginc"

            fixed4 _Color1;
            float _Depth1;
            fixed4 _Color2;
            float _Depth2;
            fixed4 _Color3;

            fixed4 frag (v2f i) : SV_Target {
                float d = i.pos.z;
                if (d < _Depth1)
                    return _Color1;
                if (d < _Depth2)
                    return _Color2;
                return _Color3;
            }
            ENDCG
        }
    }
    Fallback "Mobile/VertexLit"
}
