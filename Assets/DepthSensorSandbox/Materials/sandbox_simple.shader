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
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                //float2 uv : TEXCOORD0;
            };

            struct v2f {
                //float2 uv : TEXCOORD0;
                float4 clip : SV_POSITION;
                float3 pos : TEXCOORD1;
            };

            fixed4 _Color1;
            float _Depth1;
            fixed4 _Color2;
            float _Depth2;
            fixed4 _Color3;

            v2f vert (appdata v) {
                v2f o;
                o.clip = UnityObjectToClipPos(v.vertex);
                o.pos = UnityObjectToViewPos(v.vertex);
                //o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
#if UNITY_REVERSED_Z 
                float d = -i.pos.z;
#else
                float d = i.pos.z;
#endif
                if (d < _Depth1)
                    return _Color1;
                if (d < _Depth2)
                    return _Color2;
                return _Color3;
            }
            ENDCG
        }
    }
}
