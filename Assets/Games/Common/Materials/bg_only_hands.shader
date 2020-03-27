Shader "Sandbox/Game/BGOnlyHands" {
    Properties {
        _ColorHands ("Color Hands", Color) = (0.3, 0.3, 0.3, 1)
    }
    
    SubShader {
        Tags { "RenderType"="Transparent" "Queue" = "Background"}        
        Blend SrcAlpha OneMinusSrcAlpha
        
		Lighting Off
		ZWrite Off
		ZTest Off

        Pass {
            CGPROGRAM
            #pragma multi_compile _ CALC_DEPTH
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            
            #include "Assets/DepthSensorSandbox/Resources/Materials/sandbox.cginc"
            #include "Assets/DepthSensorSandbox/Resources/Materials/hands.cginc"
            
            fixed4 _ColorMin;
            fixed4 _ColorMax;
            fixed4 _ColorHands;

            fixed4 frag (v2f i) : SV_Target {
                fixed4 c = _ColorHands;
                c.a *= handsInteractAlpha(i);
                return c;
            }
            ENDCG
        }
        UsePass "Sandbox/ShadowCaster"
    }
}
