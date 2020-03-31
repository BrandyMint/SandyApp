Shader "Sandbox/Game/BGColors2LowPolyHandsMultiplayer" {
    Properties {
        _FieldLight ("Field Light", Float) = 1
        _FieldTex ("Field", 2D) = "white" {}
        _PlayersTex ("Player Colors", 2D) = "white" {}
        _PlayerColorAlpha ("Player Color Aplpha", Float) = 1
        
        _ColorMin ("Color Min", Color) = (0, 0, 0, 1)
        _ColorMax ("Color Max", Color) = (0.1, 0.1, 0.1, 1)        
        _ColorHands ("Color Hands", Color) = (1, 1, 1, 0.8)
        
        _DepthZero ("Depth Zero", Float) = 2
        _DepthMinOffset ("Depth Min Offset", Float) = 0.5
        _DepthMaxOffset ("Depth Max Offset", Float) = 0.5
        
        _ZWrite ("ZWrite", Int) = 1
    }
    
    SubShader {
        Tags { "RenderType" = "Opaque" }
        ZWrite [_ZWrite]

        CGPROGRAM
        #pragma target 3.0
        #pragma multi_compile _ CALC_DEPTH
        #pragma surface surf Lambert vertex:vertSurfFlat
        
        #define INCLUDE_INPUT_WORLD_NORMAL
        #define EXTENSION_INPUT \
            float3 worldPos;
        
        #include "Assets/DepthSensorSandbox/Resources/Materials/utils.cginc"
        #include "Assets/DepthSensorSandbox/Resources/Materials/sandbox.cginc"
        #include "Assets/DepthSensorSandbox/Resources/Materials/hands.cginc"
        #include "Assets/Games/Common/Materials/multi_players.cginc"
        
        void vertSurfFlat(inout appdata_full v, out Input o) {
            vertSurf(v, o);
            o.worldPos = mul(unity_ObjectToWorld, v.vertex);
        }
        
        fixed4 _ColorMin;
        fixed4 _ColorMax;
        fixed4 _ColorHands;
        float4 _CameraFlip;
        float _FieldLight;
        
        void surf (Input IN, inout SurfaceOutput o) {
            float z = IN.vpos.z;
            float max = _DepthZero - _DepthMaxOffset;
            float min = _DepthZero + _DepthMinOffset;
            float k = inverseLerp(min, max, z);            
            fixed4 c = lerp(_ColorMin, _ColorMax, k);
            c.rgb = lerp(c.rgb, _ColorHands.rgb, _ColorHands.a * handsInteractAlpha(IN.texcoord));
            
            fixed4 p = colorMultiPlayers(IN.screenPos);
            if (p.a < 1) {
                fixed2 uv = IN.screenPos.xy / IN.screenPos.w;
                float z = IN.vpos.z;
                float k = inverseLerp( _DepthZero - _DepthMaxOffset, _DepthZero + _DepthMinOffset, z);
                float len = length(uv - 0.5);
                float a = lerp(0, k / 2 + 0.5, len * len);
                p.a = a * _FieldLight;
            }
            c.rgb = lerp(c.rgb, p.rgb, p.a);
            
            o.Albedo = c;
            
            float3 pos = IN.worldPos;
            float3 x = ddx(pos) * _CameraFlip.x;
            float3 y = ddy(pos) * _CameraFlip.y;
            float3 normal = normalize(cross(x, y));
            o.Normal = -WorldToTangentNormalVector(IN, normal);
        }
        ENDCG
        
        UsePass "Sandbox/ShadowCaster"
    }
}
