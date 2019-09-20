sampler2D _FluidPrevTex;

float _DepthSea;
float _DepthZero;

half4 fragFluidClear (v2f i) : SV_Target {
    float d = i.pos.z;
    float dSea = _DepthZero - _DepthSea;
    half height = max(0, d - dSea);
    return half4(0, 0, height, 0);
}

half4 fragFluid (v2f i) : SV_Target {
    return tex2D(_FluidPrevTex, i.screenPos.xy/i.screenPos.w);
}

#ifdef USE_MRT_FLUID
    #pragma require mrt4
    
    struct FragColorFluid {
        fixed4 color : SV_Target0;
        half4 fluid : SV_Target1;
    };
    
    fixed4 fragColor (v2f i, half4 fluid);
    
    FragColorFluid frag (v2f i) {
        FragColorFluid o;
        half4 fluid = fragFluid(i);
        o.color = fragColor(i, fluid);
        o.fluid = fluid;
        return o;
    }
#endif

