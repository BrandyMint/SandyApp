Shader "Sandbox/Game/BalloonsSkyNew" {
    Properties {
        _MainTex("Background", 2D) = "white" {} 
    
        _DepthSliceOffset ("Depth Slice", Float) = 0.05        
        _DotSlice ("Dot Slice", Float) = 0.7        
        _DotSliceOverride ("Dot Slice Override", Float) = 0.5
        
        _DepthZero ("Depth Zero", Float) = 2
        _DepthMinOffset ("Depth Min Offset", Float) = 0.5
        _DepthMaxOffset ("Depth Max Offset", Float) = 0.5
    }
    
    SubShader {
        Tags { "RenderType"="Opaque" "Queue" = "Background"}
        
		Lighting Off
		ZWrite Off
		ZTest Off

        Pass {
            CGPROGRAM
            #pragma multi_compile _ CALC_DEPTH
            #pragma vertex vert
            #pragma fragment frag

            #define CALC_NORMAL
            
            #include "UnityCG.cginc"
            #include "Assets/DepthSensorSandbox/Resources/Materials/utils.cginc"
            #include "Assets/DepthSensorSandbox/Resources/Materials/sandbox.cginc"
            #include "Assets/Games/Common/Materials/depth_slice.cginc"
            #include "Assets/DepthSensorSandbox/Resources/Materials/perlin.cginc"
            
            float _DotSliceOverride;
            sampler2D _MainTex; float4 _MainTex_TexelSize;
            

            fixed4 frag (v2f i) : SV_Target {
                float hands = fragSliceDot(i, _DotSliceOverride);
                if (hands > 0)
                    return fixed4(0, 0, 0, 1);
                
                fixed2 uv = i.screenPos.xy / i.screenPos.w;
                //float aspect = _ScreenParams.x / _ScreenParams.y * _MainTex_TexelSize.x / _MainTex_TexelSize.y;
                float aspect = 1;
                fixed4 c = tex2D(_MainTex, fixed2((uv.x - 0.5) * aspect + 0.5, uv.y)); 
                return c;
            }
            ENDCG
        }
        UsePass "Sandbox/ShadowCaster"
    }
}
