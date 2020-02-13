static const float PI = 3.141592653589793238462;

#ifndef RGBtoHSV
    half3 RGBtoHSV(half3 arg1) {
        half4 K = half4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
        half4 P = lerp(half4(arg1.bg, K.wz), half4(arg1.gb, K.xy), step(arg1.b, arg1.g));
        half4 Q = lerp(half4(P.xyw, arg1.r), half4(arg1.r, P.yzx), step(P.x, arg1.r));
        half D = Q.x - min(Q.w, Q.y);
        half E = 1e-10;
        return half3(abs(Q.z + (Q.w - Q.y) / (6.0 * D + E)), D / (Q.x + E), Q.x);
    }
#endif

#ifndef HSVtoRGB
    half3 HSVtoRGB(half3 arg1) {
        half4 K = half4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
        half3 P = abs(frac(arg1.xxx + K.xyz) * 6.0 - K.www);
        return arg1.z * lerp(K.xxx, saturate(P - K.xxx), arg1.y);
    }
#endif

inline fixed4 adjust(fixed4 c, fixed3 modHSV) {
    c.rgb = HSVtoRGB(RGBtoHSV(c) + modHSV);
    return c;
}

inline float smooth(float dmin, float dmax, float z) {
    return smoothstep(0, 1, (z - dmin) / (dmax - dmin));
}

float inverseLerp(float a, float b, float k) {
    return (k - a) / (b - a);
}

float angleSigned(float3 v1, float3 v2, float3 n) {
    return atan2(dot(n, cross(v1, v2)), dot(v1, v2));
}