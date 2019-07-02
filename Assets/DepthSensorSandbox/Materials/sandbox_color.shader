Shader "Sandbox/Color" {
    Properties {
        _ColorZero ("Color Zero Depth", Color) = (0.3, 0.6, 1, 1)
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
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float4 clip : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 pos : TEXCOORD1;
            };

            fixed4 _ColorZero;
            float _DepthZero;
            sampler2D _DepthToColorTex; float4 _DepthToColorTex_ST;
            sampler2D _ColorTex; float4 _ColorTex_TexelSize;

            v2f vert (appdata v) {
                v2f o;
                o.clip = UnityObjectToClipPos(v.vertex);
                float3 pos = UnityObjectToViewPos(v.vertex);
                o.pos = float3(pos.xy, -pos.z);
                o.uv = TRANSFORM_TEX(v.uv, _DepthToColorTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                float2 uv = tex2D(_DepthToColorTex, i.uv).rg * _ColorTex_TexelSize.xy;
                fixed4 col = tex2D(_ColorTex, uv);
                float d = i.pos.z;
                if (d > _DepthZero)
                    col *= _ColorZero;
                return col;
            }
            ENDCG
        }
    }
}