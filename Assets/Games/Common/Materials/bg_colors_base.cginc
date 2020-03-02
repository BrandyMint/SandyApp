#include "UnityCG.cginc"

#include "Assets/DepthSensorSandbox/Resources/Materials/utils.cginc"
#include "Assets/DepthSensorSandbox/Resources/Materials/sandbox.cginc"
#include "Assets/DepthSensorSandbox/Resources/Materials/hands.cginc"

fixed4 _ColorMin;
fixed4 _ColorMax;
fixed4 _ColorHands;

#ifdef ENABLE_NOISE
    #include "Assets/DepthSensorSandbox/Resources/Materials/perlin.cginc"
    float _NoiseSize;
    float _NoiseStrength;
#endif
#ifdef ENABLE_MULTIPLAYERS
    #include "Assets/Games/Common/Materials/multi_players.cginc"
#endif

fixed4 basFragColor (v2f i) {
    float z = i.vpos.z;
    float max = _DepthZero - _DepthMaxOffset;
    float min = _DepthZero + _DepthMinOffset;
    float k = inverseLerp(min, max, z);
#ifdef ENABLE_NOISE
    float noise = (perlin(i.uv * _NoiseSize) * 2 - 1) * _NoiseStrength;
    k += noise;
#endif
    return lerp(_ColorMin, _ColorMax, k);
}

fixed4 frag (v2f i) : SV_Target {
    fixed4 c = basFragColor(i);
    
#ifdef ENABLE_MULTIPLAYERS
    fixed4 player = colorMultiPlayers(i);
    if (player.a < 1)
        c = c * player * player.a;
    else
        c = player;
#endif
    
    c.rgb = lerp(c.rgb, _ColorHands.rgb, _ColorHands.a * handsInteractAlpha(i));
    return c;
}