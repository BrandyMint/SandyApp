Shader "Sandbox/Game/Football" {
    Properties {
        _FieldTex ("Field", 2D) = "white" {}
        _PlayersTex ("Player Colors", 2D) = "white" {}
        _PlayerColorAlpha ("Player Color Aplpha", Float) = 1
        
        _GrassTex ("Grass", 2D) = "white" {}
        _hsvGrass1 ("HSV Grass 1", Vector) = (0, 0, 0, 0)
        _hsvGrass2 ("HSV Grass 2", Vector) = (0, 0, 0, 0)
        _stripesCount ("Stripes Count", Float) = 15 
        
        _LayoutTex ("Layout", 2D) = "white" {}
        _LayoutCenterTex ("Layout", 2D) = "white" {}        
        
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
            #pragma vertex vertFootball
            #pragma fragment frag

            #include "UnityCG.cginc"
            
            #define CALC_NORMAL
            
            #define EXTENSION_V2F \
                float2 uvGrass : TEXCOORD3;       

            #include "Assets/DepthSensorSandbox/Resources/Materials/utils.cginc"
            #include "Assets/DepthSensorSandbox/Resources/Materials/sandbox.cginc"
            #include "Assets/Games/Common/Materials/depth_slice.cginc"
            #include "Assets/Games/Common/Materials/multi_players.cginc"
            
            float _DotSliceOverride;
            sampler2D _GrassTex; float4 _GrassTex_ST;
            fixed3 _hsvGrass1;
            fixed3 _hsvGrass2;
            float _stripesCount;
                        
            sampler2D _LayoutTex; 
            sampler2D _LayoutCenterTex;            
            
            v2f vertFootball (appdata v) {
                v2f o = vert(v);
                o.uvGrass = TRANSFORM_TEX(o.uv, _GrassTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                float hands = fragSliceDot(i, _DotSliceOverride);
                if (hands > 0)
                    return fixed4(0, 0, 0, 1);
                
                fixed2 uv = i.screenPos.xy / i.screenPos.w;
                fixed3 hsvGrass = _hsvGrass1;
                if (frac(uv.x / _ScreenParams.z  * _stripesCount / 2) > 0.5)
                    hsvGrass = _hsvGrass2;
                fixed4 c = adjust(tex2D(_GrassTex, i.uvGrass), hsvGrass);
                
                float z = i.vpos.z;
                float k = inverseLerp( _DepthZero - _DepthMaxOffset, _DepthZero + _DepthMinOffset, z);
                fixed4 player = colorMultiPlayers(i);
                c = lerp(c, player * c, lerp(0, k / 2 + 0.5, abs(uv.x - 0.5) * 2));
                c.rgba /= c.a;
                
                fixed2 uvc = uv; 
                float scrAspect = _ScreenParams.x / _ScreenParams.y;
                float newAspect = scrAspect * 3 / 4;
                if (uv.x < 0.5)
                    uv.x *= newAspect;
                else
                    uv.x = 1 - (1 - uv.x) * newAspect;
                uvc.x = (uvc.x - 0.5) * newAspect + 0.5; 
                fixed4 l = max(tex2D(_LayoutTex, uv), tex2D(_LayoutCenterTex, uvc));
                c = lerp(c, l, l.a);
                return c;
            }            
            

            ENDCG
        }
        UsePass "Sandbox/ShadowCaster"
    }
}
