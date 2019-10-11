Shader "Sandbox/Topography" {
    Properties {
        _ScaleTex ("Scale texture", 2D) = "white" {}
        _DepthZero ("Depth Zero", Float) = 1.0
        _DepthMaxOffset ("Max Offset", Float) = 0.1
        _DepthMinOffset ("Min Offset", Float) = 0.1
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
            #include "utils.cginc"
            #include "sandbox.cginc"

            float _DepthZero;
            float _DepthMaxOffset;
            float _DepthMinOffset;
            sampler2D _ScaleTex;

            fixed4 frag (v2f i) : SV_Target {
                float d = i.pos.z;
                float max = _DepthZero - _DepthMaxOffset;
                float min = _DepthZero + _DepthMinOffset;
                float k = inverseLerp(min, max, d);
                return tex2D(_ScaleTex, k);
            }
            ENDCG
        }
    }
    Fallback "Mobile/VertexLit"
}
