fixed4 _ColorPlayerLeft;
fixed4 _ColorPlayerRight;
float _PlayerColorAlpha;

fixed4 frag(v2f i);

fixed4 fragTwoPlayers(v2f i) : SV_Target {
    fixed4 c = frag(i);
    fixed x = i.screenPos.x / i.screenPos.w;
    if (x < 0.5) {
        c *= _ColorPlayerLeft * _PlayerColorAlpha;
    } else {
        c *= _ColorPlayerRight * _PlayerColorAlpha;
    }
    return c;
}