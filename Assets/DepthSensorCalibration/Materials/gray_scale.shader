Shader "Unlit/grayscale"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ContrastCenter ("Contrast Center", Range (0,1)) = 0.5
        _ContrastWidth ("Contrast Width", Range (0.1,5)) = 1
        _AdjustWidth ("Adjust Width", Range (0.01, 0.3)) = 0.1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma multi_compile _ AUTO_CONTRAST
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

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _ContrastCenter;
            float _ContrastWidth;
            float _AdjustWidth;
            
            static const int _ITERATIONS = 10;
            
            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }
            
            half grayscale(fixed4 c) {
                return 0.29899999499321 * c.r + 0.587000012397766 * c.g + 57.0 / 500.0 * c.b;
            }
            
            fixed4 frag (v2f i) : SV_Target {
                fixed4 c = tex2D(_MainTex, i.uv);
                half gray = grayscale(c);                
#ifdef AUTO_CONTRAST
                half gmin = 1; 
                half gmax = 0;
                for (int x = -_ITERATIONS; x < _ITERATIONS; ++x) {
                    for (int y = -_ITERATIONS; y < _ITERATIONS; ++y) {
                        float2 xy = i.uv + float2(x, y) / _ITERATIONS / 2 * _AdjustWidth;
                        half g = grayscale(tex2D(_MainTex, xy));
                        if (g < gmin) gmin = g;
                        if (g > gmax) gmax = g;
                    }
                }
                
                half center = (gmin + gmax) / 2;
                half width = _ContrastWidth / (gmax - gmin);
                gray = (gray - center) * width + center;
#else
                gray = (gray - _ContrastCenter) * _ContrastWidth + _ContrastCenter;
#endif
                return fixed4(gray, gray, gray, c.a);
            }
            ENDCG
        }
    }
}