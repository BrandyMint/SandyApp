﻿Shader "Unlit/BlendSpray" {
    SubShader
    {
        Tags { "RenderType"="Transporent" }
        
        GrabPass {
            "_PrevTex"
        }

        Pass
        {
            ZWrite Off
            ZTest Off
            Cull Off
            //Blend SrcAlpha OneMinusSrcAlpha, One One
        
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
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex; float4 _MainTex_ST;
            sampler2D _PrevTex;
            
            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target {
                fixed4 newC = tex2D(_MainTex, i.uv);
                fixed4 oldC = tex2D(_PrevTex, i.uv);
                fixed4 c = lerp(oldC, newC, newC.a);
                c.a = saturate(newC.a + oldC.a);
                return c;
            }
            ENDCG
        }
    }
}