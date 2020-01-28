Shader "Unlit/ClockFill"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _StartVec ("Start Vector", Vector) = (0,1,0,0)
        _Fill ("Fill", Float) = 0.8
        _Clockwise ("Fill", Float) = 1
        _FadeTrashold("Fade Trashold", Float) = 0.05 
    }
    SubShader
    {
        Tags {"RenderType"="Transparent" "Queue" = "Transparent+1"}        
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZTest Off
        Zwrite Off
        
        Pass
        {
            CGPROGRAM
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

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float3 _StartVec;
            float _Fill;
            float _Clockwise;
            float _FadeTrashold;
            
            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target {
                float a = 0.5 - angleSigned(_StartVec, float3(i.uv - 0.5, 0), float3(0, 0, _Clockwise)) / 2 / PI;
                if (a > _Fill)
                    return fixed4(0, 0, 0, 0);
                fixed fade = min(1, min(1 - min(_Fill, 1), _Fill) / _FadeTrashold);
                fixed4 col = tex2D(_MainTex, i.uv) * fade;
                return col;
            }
            ENDCG
        }
    }
}