Shader "Sandbox/Color" {
    Properties {
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
                float2 uv : TEXCOORD0;
                float4 clip : SV_POSITION;
            };

            sampler2D _DepthToColorTex; float4 _DepthToColorTex_ST;
            sampler2D _ColorTex; float4 _ColorTex_TexelSize;

            v2f vert (appdata v) {
                v2f o;
                o.clip = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _DepthToColorTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                float2 uv = tex2D(_DepthToColorTex, i.uv).rg * _ColorTex_TexelSize.xy;
                return tex2D(_ColorTex, uv);
            }
            ENDCG
        }
    }
}