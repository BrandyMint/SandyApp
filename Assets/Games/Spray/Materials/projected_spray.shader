Shader "Projector/ProjectedSpray"
{
	Subshader {
		Pass {
			AlphaTest Off
	
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			
			#pragma multi_compile _ CALC_DEPTH
                
            #include "Assets/DepthSensorSandbox/Resources/Materials/sandbox.cginc"
			
			fixed4 frag (v2f i) : SV_Target {
				return fixed4(0, 0, 0, 0);
			}
			ENDCG
		}
		UsePass "Sandbox/ShadowCaster"
    }
}