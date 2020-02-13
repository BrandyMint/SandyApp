Shader "Unlit/GradientDepth"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _GradientTex ("Gradient", 2D) = "white" {}
        _MinDepth ("Min Depth", float) = 0.3
        _MaxDepth ("Min Depth", float) = 2.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off

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

            sampler2D _MainTex; float4 _MainTex_ST;
            sampler2D _GradientTex;
            float _MinDepth;
            float _MaxDepth;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                float d = tex2D(_MainTex, i.uv).r * 65.535;
                if (d < 0.001)
                    return fixed4(0, 0, 0, 1);
                float k = inverseLerp(_MinDepth, _MaxDepth, d);
                return tex2D(_GradientTex, k);
            }
            ENDCG
        }
    }
}
