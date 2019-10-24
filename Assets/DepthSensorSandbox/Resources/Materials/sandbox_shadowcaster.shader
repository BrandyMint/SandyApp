Shader "Sandbox"
{
    SubShader
    {        
        Pass {
            Tags { "LightMode" = "ShadowCaster" }
            Name "ShadowCaster"
            
            CGPROGRAM
            #pragma multi_compile _ CALC_DEPTH 
            #pragma multi_compile_shadowcaster
            #pragma vertex vertShadowCast
            #pragma fragment frag
            #pragma target 2.0        
            #include "UnityCG.cginc"
            
            #include "sandbox.cginc"
            
            struct v2f_shadow {
                V2F_SHADOW_CASTER;
            };
            
            v2f_shadow vertShadowCast( appdata v ) {
#ifdef CALC_DEPTH
                v.vertex = sampleDepth(v.uv, CURR);
#endif
                v2f_shadow o;
                TRANSFER_SHADOW_CASTER(o)
                return o;
            }
            
            float4 frag( v2f_shadow i ) : SV_Target {
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
}