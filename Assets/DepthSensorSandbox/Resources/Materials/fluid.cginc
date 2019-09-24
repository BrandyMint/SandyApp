sampler2D_float _FluidPrevTex;
float4 _FluidPrevTex_TexelSize;

float _DepthSea;
float _DepthZero;
float _FluidSpeed;
float _FluidViscosity;
float4 _Neighbours[8];
float _Thickness;
float _ThicknessRunoff;

float4 fragFluidClear (v2f i) : SV_Target {
    float d = i.pos.z;
    float dSea = _DepthZero - _DepthSea;
    float water = max(0, d - dSea);
    return float4(0, 0, water, d);
}

inline float4 getFluid(float4 screenPos, float2 dxy) {
    float2 d = dxy * _FluidPrevTex_TexelSize.xy; 
    return tex2D(_FluidPrevTex, (screenPos.xy) / screenPos.w + d);
}

inline void fluidMoving(float3 f, float2 dxy, float k, float dSpeed, inout float3 impulse) {
    if (f.z > _Thickness) {
        float proj = dot(f.xy, dxy);
        float water = max(0, min(f.z, proj * dSpeed));
        impulse.z += water * k;
        impulse.xy += f.xy * water;
    }
}

float4 fragFluid (v2f i) : SV_Target {
    float4 fluid = getFluid(i.screenPos, float2(0, 0));
    fluid.w = i.pos.z;
    float dSpeed = _FluidSpeed * unity_DeltaTime.x;
    float dViscosity = _FluidViscosity * unity_DeltaTime.x;
    float2 innerImpulse = fluid.xy * fluid.z;
    float3 outerImpulse = float3(0, 0, 0);
    float2 runoff = float2(0, 0);
    for (int j = 0; j < 8; ++j) {
        float3 n = _Neighbours[j].xyz;
        float4 neighbour = getFluid(i.screenPos, n.xy);
        n.xy /= n.z;
        fluidMoving(fluid.xyz, n.xy, -1, dSpeed, outerImpulse);
        fluidMoving(neighbour.xyz, -n.xy, 1, dSpeed, outerImpulse);
        if (fluid.z > _Thickness && (neighbour.w - neighbour.z) - (fluid.w - fluid.z) > _ThicknessRunoff) {
            runoff += dViscosity * n.xy;
        }
    }
    
    fluid.z = max(0, fluid.z + outerImpulse.z);
    if (fluid.z > _Thickness) {
        fluid.xy =  (outerImpulse.xy * dViscosity + innerImpulse) / fluid.z;
        fluid.xy += runoff;
        fluid.xy *= max(0, 1 - dViscosity);        
    } else {
        fluid.xy = float2(0, 0);
    }
    return fluid;
}

#ifdef USE_MRT_FLUID
    #pragma require mrt4
    
    struct FragColorFluid {
        fixed4 color : SV_Target0;
        float4 fluid : SV_Target1;
    };
    
    fixed4 fragColor (v2f i, float4 fluid);
    
    FragColorFluid frag (v2f i) {
        FragColorFluid o;
#ifdef CLEAR_FLUID 
        float4 fluid = fragFluidClear(i);
#else
        float4 fluid = fragFluid(i);
#endif
        o.color = fragColor(i, fluid);
        o.fluid = fluid;
        return o;
    }
#endif

