Shader "Sandbox/Game/PingPong" {
    Properties {
        _ColorHands ("Color Hands", Color) = (1, 1, 1, 0.6)
        _FieldTex ("Field", 2D) = "white" {}
        _PlayersTex ("Player Colors", 2D) = "white" {}
        _PlayerColorAlpha ("Player Color Aplpha", Float) = 1
        
        _ColorCenter ("Color Center", Color) = (1, 1, 1, 1)
        _ColorBg ("Color Bg", Color) = (0, 0, 1, 1)
        
        _LayoutTex ("Layout", 2D) = "white" {}
        _LayoutCenterTex ("Layout Center", 2D) = "white" {}        
        
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
            #pragma vertex vertFootball
            #pragma fragment frag

            #include "UnityCG.cginc"
            
            #define EXTENSION_V2F \
                float2 uvGrass : TEXCOORD3;       

            #include "Assets/DepthSensorSandbox/Resources/Materials/utils.cginc"
            #include "Assets/DepthSensorSandbox/Resources/Materials/sandbox.cginc"
            #include "Assets/DepthSensorSandbox/Resources/Materials/hands.cginc"
            #include "Assets/Games/Common/Materials/multi_players.cginc"
            
            sampler2D _GrassTex; float4 _GrassTex_ST;
                        
            sampler2D _LayoutTex; 
            sampler2D _LayoutCenterTex;
            fixed4 _ColorCenter;
            fixed4 _ColorBg;
            fixed4 _ColorHands;
            
            v2f vertFootball (appdata v) {
                v2f o = vert(v);
                o.uvGrass = TRANSFORM_TEX(o.uv, _GrassTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {                
                fixed2 uv = i.screenPos.xy / i.screenPos.w;
                float scrAspect = _ScreenParams.x / _ScreenParams.y;
                fixed4 c = lerp(_ColorCenter, _ColorBg, min(1, length(fixed2((uv.x - 0.5) * scrAspect, uv.y - 0.5))));
                
                float z = i.vpos.z;
                float k = inverseLerp( _DepthZero - _DepthMaxOffset, _DepthZero + _DepthMinOffset, z);
                fixed4 player = colorMultiPlayers(i);
                c = lerp(c, player, player.a * lerp(0, k / 2 + 0.5, pow(abs(uv.x - 0.5) * 2, 3)));
                c.a = 1;
                
                fixed2 uvc = uv;                
                float newAspect = scrAspect * 3 / 4;
                if (uv.x < 0.5)
                    uv.x *= newAspect;
                else
                    uv.x = 1 - (1 - uv.x) * newAspect;
                uvc.x = (uvc.x - 0.5) * newAspect + 0.5; 
                fixed4 l = tex2D(_LayoutTex, uv);
                fixed4 lc = tex2D(_LayoutCenterTex, uvc);
                l.rgb = lerp(l.rgb * l.a, lc.rgb * lc.a, lc.a);
                l.a = max(l.a, lc.a);
                c = lerp(c, l, l.a);
                c.a = 1;
                
                c.rgb = lerp(c.rgb, _ColorHands.rgb, _ColorHands.a * handsInteractAlpha(i));
                return c;
            }            
            

            ENDCG
        }
        UsePass "Sandbox/ShadowCaster"
    }
}
