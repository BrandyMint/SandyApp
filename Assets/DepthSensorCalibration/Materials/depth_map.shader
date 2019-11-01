Shader "Unlit/DepthMap"
{
    Properties
    {
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        
		Lighting Off
		Cull Off ZWrite Off ZTest Off
		//Blend One OneMinusSrcAlpha

        Pass {
        ZWrite Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float4 scrPos : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
            
            sampler2D _CameraDepthTexture;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.scrPos = ComputeScreenPos(o.vertex);
                return o;
            }

            float frag (v2f i) : SV_Target
            {
                float depth = tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(i.scrPos)).r;
                depth = LinearEyeDepth(depth);
                return depth / 65.535;
            }
            ENDCG
        }
    }
}