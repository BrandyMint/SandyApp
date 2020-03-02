sampler2D _HandsMaskTex; float4 _HandsMaskTex_TexelSize;
sampler2D _HandsDepthTex; float4 _HandsDepthTex_TexelSize;
float _HandsDepthMax;

#define HANDS_CELL_ERROR_AURA 1.0/256.0
#define HANDS_CELL_COLOR 2.0/256.0
#define HANDS_CELL_EMPTY
#ifndef DEPTH_TO_FLOAT
    #define DEPTH_TO_FLOAT 65.535
#endif

fixed handsMaskAlpha(float2 uv) {
    fixed hands = tex2D(_HandsMaskTex, uv).r;
    return smoothstep(HANDS_CELL_ERROR_AURA, HANDS_CELL_COLOR, hands);
}

fixed handsMaskAlpha(v2f i) {
    return handsMaskAlpha(i.uv);
}

float sampleHandsDepth(float2 uv) {
    return tex2D(_HandsDepthTex, uv).r * DEPTH_TO_FLOAT;
}

fixed handsDepthToInteractAlpha(float depth) {
    if (depth > 0.001)
        return smoothstep(_HandsDepthMax*1.5, _HandsDepthMax/1.5, depth);
    return 0;
}

fixed handsInteractAlpha(float2 uv) {
    fixed a = handsMaskAlpha(uv);
    if (a > 0.99) {
        return handsDepthToInteractAlpha(sampleHandsDepth(uv)) * a;
    }
    return a;
}

fixed handsInteractAlpha(v2f i) {
    return handsInteractAlpha(i.uv);
}