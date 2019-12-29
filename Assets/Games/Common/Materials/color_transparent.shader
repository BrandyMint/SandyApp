Shader "Unlit/ColorTrasnparent"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _Alpha ("Alpha", float) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderType"="Transparent" "Queue" = "Transparent"}
        
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
            };

            fixed4 _Color;
            float _Alpha;
            
            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target {
                return _Color * _Alpha;
            }
            ENDCG
        }
    }
}