sampler2D _FieldTex;
sampler2D _PlayersTex; float4 _PlayersTex_ST;
float _PlayerColorAlpha;

fixed4 colorMultiPlayers(v2f i) {
    fixed2 uv = i.screenPos.xy / i.screenPos.w;
    fixed playerID = tex2D(_FieldTex, uv).a;
    fixed4 playerColor = tex2D(_PlayersTex, playerID);
    if (playerID < 1 - _PlayersTex_ST.x / 4)
        playerColor.a *= _PlayerColorAlpha;
    return playerColor;
}