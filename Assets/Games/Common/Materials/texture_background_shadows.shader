﻿Shader "Unlit/TextureBackgroundShadows"
{
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _ShadowIntensity ("Shadow Intensity", Range (0, 1)) = 0.6
    }
    SubShader
    {
        Tags { "LightMode" = "ForwardBase" "RenderType"="Opaque" "Queue" = "Background-1"}
        
        //Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZTest Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            
            #include "UnityCG.cginc"
            #include "AutoLight.cginc"            

            struct appdata {
                float4 pos : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                SHADOW_COORDS(1)
            };

            sampler2D _MainTex; float4 _MainTex_ST;
            uniform float _ShadowIntensity;
            
            v2f vert (appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.pos);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                TRANSFER_SHADOW(o);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target {
                fixed4 c = tex2D(_MainTex, i.uv);
                float shadow = SHADOW_ATTENUATION(i);
                c.rgb *= lerp(1, shadow, _ShadowIntensity);
                return c;
            }
            ENDCG
        }
    }
    Fallback "VertexLit"
}