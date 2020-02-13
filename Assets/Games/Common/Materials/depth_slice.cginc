float _DepthSliceOffset;
#ifdef CALC_NORMAL
    float _DotSlice;
#endif

float fragSliceDot(v2f i, float dotSlice) {
    float z = i.vpos.z;
    z = _DepthZero - _DepthMaxOffset - _DepthSliceOffset - z;
#ifdef CALC_NORMAL
    float d = dot(i.normal, float3(0, 0, 1));
    if (d < dotSlice)
        return 0;
#endif
    return max(0, z);
}

float fragSlice(v2f i) : SV_Target {
#ifdef CALC_NORMAL
    return fragSliceDot(i, _DotSlice);
#else
    return fragSliceDot(i, 0);    
#endif
}