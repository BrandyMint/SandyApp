float _DepthSliceOffset;
#ifdef CALC_NORMAL
    float _DotSlice;
#endif

float fragSlice(v2f i) : SV_Target {
    float z = i.vpos.z;
    z = _DepthZero - _DepthMaxOffset - _DepthSliceOffset - z;
#ifdef CALC_NORMAL
    float d = dot(i.normal, float3(0, 0, 1));
    if (d < _DotSlice)
        return 0;
#endif
    return max(0, z);
}