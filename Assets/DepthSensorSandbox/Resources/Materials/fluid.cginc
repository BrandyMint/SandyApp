sampler2D_float _FluxPrevTex; float4 _FluxPrevTex_TexelSize;
sampler2D_float _HeightPrevTex; float4 _HeightPrevTex_TexelSize;

float _DepthSea;
float _DepthZero;
float _FluxAcceleration;
float _CellArea;
float _CellHeight;

#define TYPE_HEIGHT float2
#define TYPE_FLUX float4

#define TERRAIN_H(col) (col.r)
#define WATER_H(col) (col.g)
#define HEIGHT_FULL(col) ((TERRAIN_H(col) - WATER_H(col)))

#define SAMPLE_FROM_SCREEN_POS(tex, screenPos, offset) \
    (tex2D(tex, screenPos.xy / screenPos.w + offset * tex##_TexelSize.xy))
#define SAMPLE_OFFSET(tex, offset) (SAMPLE_FROM_SCREEN_POS(tex, i.screenPos, offset))
#define FLUX_SAMPLE(offset) (SAMPLE_OFFSET(_FluxPrevTex, offset))
#define HEIGHT_SAMPLE(offset) (SAMPLE_OFFSET(_HeightPrevTex, offset).rg)

#define CURR float2(0, 0)
#define L float2(-1, 0)
#define R float2(1, 0)
#define T float2(0, 1)
#define B float2(0, -1)

#define L_FLUX(col) (col.r)
#define R_FLUX(col) (col.g)
#define T_FLUX(col) (col.b)
#define B_FLUX(col) (col.a)

#define SUM_C(col) ((col.x + col.y + col.z + col.w))

#define BOUNDARY_MIN(tex, col, x) ((col.##x < tex##_TexelSize.##x / 2))
#define BOUNDARY_MAX(tex, col, x) ((col.##x > 1 - tex##_TexelSize.##x / 2))
#define BOUNDARY(type, h) (!(type##_H(h) > 0))

float2 fragHeightClear (v2f i) : SV_Target {
    float d = i.pos.z;
    float dSea = _DepthZero - _DepthSea;
    float water = max(0, d - dSea);
    float2 h;
    TERRAIN_H(h) = d;
    WATER_H(h) = water;
    return h;    
}

float4 fragFluxClear (v2f i) : SV_Target {
    return float4(0, 0, 0, 0);
}

TYPE_FLUX calcFlux (float2 xy, TYPE_FLUX flux, TYPE_HEIGHT h, TYPE_HEIGHT hl, TYPE_HEIGHT hr, TYPE_HEIGHT ht, TYPE_HEIGHT hb) {
    TYPE_FLUX heightDiff = -HEIGHT_FULL(h);
    L_FLUX(heightDiff) += HEIGHT_FULL(hl);
    R_FLUX(heightDiff) += HEIGHT_FULL(hr);
    T_FLUX(heightDiff) += HEIGHT_FULL(ht);
    B_FLUX(heightDiff) += HEIGHT_FULL(hb);
    
    flux = max(0, flux + unity_DeltaTime.x * _FluxAcceleration * _CellArea * heightDiff / _CellHeight);
    flux *= min(1, WATER_H(h) * _CellArea / (SUM_C(flux) * unity_DeltaTime.x));
    
    if (BOUNDARY_MIN(_FluxPrevTex, xy, x) || BOUNDARY(TERRAIN, hl)) L_FLUX(flux) = 0;
    if (BOUNDARY_MAX(_FluxPrevTex, xy, x) || BOUNDARY(TERRAIN, hr)) R_FLUX(flux) = 0;
    if (BOUNDARY_MIN(_FluxPrevTex, xy, y) || BOUNDARY(TERRAIN, hb)) B_FLUX(flux) = 0;
    if (BOUNDARY_MAX(_FluxPrevTex, xy, y) || BOUNDARY(TERRAIN, ht)) T_FLUX(flux) = 0;
    
    return max(0, flux);
}

TYPE_HEIGHT calcHeight(v2f i, TYPE_HEIGHT h, TYPE_FLUX f, TYPE_FLUX fl, TYPE_FLUX fr, TYPE_FLUX ft, TYPE_FLUX fb) {
    TYPE_FLUX outFlux = f;
    TYPE_FLUX inFlux;
    L_FLUX(inFlux) = R_FLUX(fl);
    R_FLUX(inFlux) = L_FLUX(fr);
    T_FLUX(inFlux) = B_FLUX(fb);
    B_FLUX(inFlux) = T_FLUX(ft);
    
    float waterDiff = SUM_C(inFlux) - SUM_C(outFlux);
    
    TERRAIN_H(h) = i.pos.z;
    WATER_H(h) = max(0, WATER_H(h) + unity_DeltaTime.x * waterDiff / _CellArea);    
    return h;
}

void calcFluid(v2f i, out TYPE_HEIGHT height, out TYPE_FLUX flux) {
    float2 xy = i.screenPos.xy / i.screenPos.w;
    TYPE_HEIGHT h = HEIGHT_SAMPLE(CURR);    
    TYPE_HEIGHT hl = HEIGHT_SAMPLE(L);
    TYPE_HEIGHT hr = HEIGHT_SAMPLE(R);
    TYPE_HEIGHT ht = HEIGHT_SAMPLE(T);
    TYPE_HEIGHT hb = HEIGHT_SAMPLE(B);
    TYPE_FLUX f = FLUX_SAMPLE(CURR);
    TYPE_FLUX fl = FLUX_SAMPLE(L);
    TYPE_FLUX fr = FLUX_SAMPLE(R);
    TYPE_FLUX ft = FLUX_SAMPLE(T);
    TYPE_FLUX fb = FLUX_SAMPLE(B);
    flux = calcFlux(xy, f, h, hl, hr, ht, hb);
    height = calcHeight(i, h, f, fl, fr, ft, fb);
}

#ifdef USE_MRT_FLUID
    #pragma require mrt4
    
    struct FragColorFluid {
        fixed4 color : SV_Target0;
        TYPE_HEIGHT height : SV_Target1;
        TYPE_FLUX flux : SV_Target2;
    };

#ifdef PROVIDE_FLUX
    fixed4 fragColor (v2f i, TYPE_HEIGHT height, TYPE_FLUX flux);
#else
    fixed4 fragColor (v2f i, TYPE_HEIGHT height);
#endif
    
    FragColorFluid frag (v2f i) {
        FragColorFluid o;
#ifdef CLEAR_FLUID 
        o.flux = fragFluxClear(i);
        o.height = fragHeightClear(i);
#else
        calcFluid(i, o.height, o.flux);
#endif
#ifdef PROVIDE_FLUX
        o.color = fragColor(i, o.height, o.flux);
#else
        o.color = fragColor(i, o.height);
#endif
        return o;
    }
#endif

