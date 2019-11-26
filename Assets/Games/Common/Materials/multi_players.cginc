sampler2D _FieldTex;
sampler2D _PlayersTex; float4 _PlayersTex_ST;
float _PlayerColorAlpha;

fixed4 frag(v2f i);

fixed4 fragMultiPlayers(v2f i) : SV_Target {
    fixed4 c = frag(i);
    fixed2 uv = i.screenPos.xy / i.screenPos.w;
    fixed playerID = tex2D(_FieldTex, uv).a;
    fixed4 playerColor = tex2D(_PlayersTex, playerID);
    c *= playerColor;
    if (playerID < 1 - _PlayersTex_ST.x / 4)
        return c * playerColor * _PlayerColorAlpha;    
    return playerColor;
}