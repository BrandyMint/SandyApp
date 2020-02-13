Shader "Sandbox/Game/Paint" {
    Properties {
        _MainTex ("Previus frame", 2D) = "black" {}
        _ColorsTex ("Color", 2D) = "white" {}
        _ColorScale ("Color Scale", Float) = 1
        _ColorMix ("Color Mix", Float) = 0.1
    
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

            #include "UnityCG.cginc"

            #include "Assets/DepthSensorSandbox/Resources/Materials/utils.cginc"
            
            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
            
            sampler2D _ColorsTex;            
            sampler2D _HandsTex;
            sampler2D _MainTex; float4 _MainTex_ST;
            float _ColorScale;
            float _ColorMix;
            
            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                float h = tex2D(_HandsTex, i.uv);
                float k = frac(h * _ColorScale);
                fixed4 hand = tex2D(_ColorsTex, k);
                fixed4 c = tex2D(_MainTex, i.uv);
                return lerp(c, hand, smoothstep(0, _ColorMix, h));
            }
            ENDCG
        }
    }
}
