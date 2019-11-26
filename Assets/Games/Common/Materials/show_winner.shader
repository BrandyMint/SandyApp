Shader "Unlit/ShowWinner"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Winner ("Winner", Float) = 0.5  
        _Color ("Color Background", Color) = (0, 0, 0, 0.5)
        _ColorWinner ("Color Winner", Color) = (0, 1, 0, 1)
        _Trashold ("Trashold", Float) = 0.05
        _MinWinColor ("Min Win Color", Float) = 0.4
        _MaxWinColor ("Min Win Color", Float) = 0.7
        _Frequency ("Frequency", Float) = 1
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off

        Pass
        {
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
            fixed4 _Color;
            fixed4 _ColorWinner;
            float _Winner;
            float _Trashold;
            float _MinWinColor;
            float _MaxWinColor;
            float _Frequency;
            
            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target {
                fixed player = tex2D(_MainTex, i.uv).a;
                float k = smoothstep(_Trashold, 0, abs(player - _Winner));
                k *= lerp(_MinWinColor, _MaxWinColor, sin(_Time.y * _Frequency));
                return lerp(_Color, _ColorWinner, k);
            }
            ENDCG
        }
    }
}