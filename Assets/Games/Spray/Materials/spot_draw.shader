Shader "Projector/Spray" {
	Properties {
		_Color ("Main Color", Color) = (1,1,1,1)
		_ShadowTex ("Cookie", 2D) = "" {}
		_FalloffTex ("FallOff", 2D) = "" {}
	}
	
	Subshader {
		Tags {"Queue"="Transparent"}
		Pass {
			ZWrite Off
			ColorMask RGB
			Blend DstColor One
			Offset -1, -1
	
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			
			#pragma multi_compile _ CALC_DEPTH
			#define EXTENSION_V2F \
                float2 uvShadow : TEXCOORD4; \
                float2 uvFalloff : TEXCOORD3;
                
            #include "Assets/DepthSensorSandbox/Resources/Materials/sandbox.cginc"
            
			float4x4 unity_Projector;
			float4x4 unity_ProjectorClip;
			fixed4 _Color;
			sampler2D _ShadowTex;
			sampler2D _FalloffTex;
			sampler2D _MainTex;
			
			v2f vertSpray (appdata v) {
				v2f o = vert(v);
				o.uvShadow = mul (unity_Projector, vertex);
				o.uvFalloff = mul (unity_ProjectorClip, vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target {
				fixed4 texS = tex2Dproj (_ShadowTex, UNITY_PROJ_COORD(i.uvShadow));
				texS.rgb *= _Color.rgb;
				texS.a = 1.0 - texS.a;
	
				fixed4 texF = tex2Dproj (_FalloffTex, UNITY_PROJ_COORD(i.uvFalloff));
				fixed4 res = texS * texF.a;
				
				fixed4 base = tex2Dproj (_ShadowTex, UNITY_PROJ_COORD(i.uvShadow));
				return res;
			}
			ENDCG
		}
	}
}
