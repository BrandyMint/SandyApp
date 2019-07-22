Shader "Sandbox/Simple" {
    Properties {
        _Color2 ("Color 2", Color) = (1, 1, 1, 1)        
        _Color1 ("Color 1", Color) = (0, 1, 0, 1)
        _Depth1 ("Depth 1", Float) = 0.1
        _ColorZero ("Color Zero", Color) = (0, 0, 1, 1)
        _DepthZero ("Depth Zero", Float) = 3.0
    }
    
    SubShader {
        Tags { "RenderType"="Opaque" }
        
		Lighting Off
		//Blend One OneMinusSrcAlpha

        Pass {
            CGPROGRAM
            #pragma multi_compile _ CALC_DEPTH
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "sandbox.cginc"

            fixed4 _Color2;
            fixed4 _Color1;
            float _Depth1;
            fixed4 _ColorZero;
            float _DepthZero;

            fixed4 frag (v2f i) : SV_Target {
                float d = i.pos.z;
                if (d > _DepthZero)
                    return _ColorZero;
                if (d > _DepthZero - _Depth1)
                    return _Color1;
                return _Color2;
            }
            ENDCG
        }
    }
    Fallback "Mobile/VertexLit"
}
