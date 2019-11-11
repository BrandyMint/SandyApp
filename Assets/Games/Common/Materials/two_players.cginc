fixed4 _ColorPlayerLeft;
fixed4 _ColorPlayerRight;
float _PlayerColorAlpha;
int _FlipHorizontal;

fixed4 frag(v2f i);

fixed4 fragTwoPlayers(v2f i) : SV_Target {
    fixed4 c = frag(i);
    fixed x = i.screenPos.x / i.screenPos.w;
    if (x < 0.5 && _FlipHorizontal == 0 || x > 0.5 && _FlipHorizontal == 1) {
        c *= _ColorPlayerLeft * _PlayerColorAlpha;
    } else {
        c *= _ColorPlayerRight * _PlayerColorAlpha;
    }
    return c;
}