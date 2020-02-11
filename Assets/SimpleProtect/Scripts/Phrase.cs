#if BUILD_PROTECT_COPY || BUILD_ACTIVATOR
using System.Text;

namespace SimpleProtect {
    public static class Phrase {
        private static readonly string _PHRASE = @"rj<ys6=tG}6*xvEMxiIML|gXwFly1tx|NdS2Ir2|p$FyallrvR:Q)f0gk59JNegVq!QrUL0dK2DDHKDu||o:$U56*r<s#-o#?:8?qII@JzySx(mU2R-?c@rRmRjni4&gP578-aS=e!2It-}{{$:nzD@LThfnK-A>3S=@yjoY)d}Z5mRw<@pUD<qB#>GoyOKJ!?Z7S4LJJ>ymvfY@{u^MxZ5|$7xjMl(T|w>e4o&LkNV|L1OgJ$Pq3iO=(Q^_r!pWed{O6H<d_Ba<F5*QBBC-?uvsAvh^rURM%M&wVB}cL}?>nRGi32ow%5_>fg=x#fT-?!}TBZ*M}}t!@Q(jJSc7}2B-<+kIj*DtF-npg^x43i4a6aqB_Zb@?dg6yRUhaB|VAwER8l8n%2jV7->#rO8&W:xb(R?99VG#G3peklct+J%Fi@*aV#m5m<)+F3$XrjYE51mPzsl6zVQA&6YlI$U!fu%Ye%RxJetGOdppODI9Vrx-fH8d&Cue6E>U&ZXG*?gE*7&zF+GS2G?nVsxQ>)@$6C@iXD2S|N%:hsb=spJuGD|x*qGL50T9RRC:2729t|mTstLVL{GPQSGzHVQb:6+QZ<!bRbPvGIB+=)qSb}?&n51!gKrfN57{m$9ov)f2bMx>i$%jNby>Lk-2$>Y&4sF3&9}(ZmqlF#EyJg*AI@72Y9Aw)jG:db-ZtC^9Y+}fSC7{slXr-M8xTVO-}(5lAWSefJ:BrDa|UJ!J_=kovz)721<!h4x&0lJ4M+lX$=-eUaZy%l4AKd(DK7db4Is_OnXPwO-fZ7R3QiTyJh0!z9gr>kR{VavKhIABx|W-?}HOBs:rOPsT=5@sI|@b9VH0&swGt>aufS{:J$vjGb2qE28>^V)Fd5oPcf}:|ND>YDrvG8e8PQ(:coWXT|J%sxtt@5)N#5VZM0UW>2*}4m6<5SKl6&_<$a4h97D$?aqkE&TpQ^5+rUNwx5HGJ_x0z<FC*U?yQ_Er6zLUZOu61f2Xk^<1n8t)BMe_{%Of(75$^yx^c44U";
        private static readonly int[] _IDS = {3, 2, 9, 8, 7, 8, 5, 1, 2, 3, 4, 5, 9, 12, 2, 4, 1};

        public static string Get() {
            var s = new StringBuilder();
            var id = 0;
            var i = 0;
            var next = "";
            while ((next = GetNext(ref id, ref i)) != null) {
                s.Append(next);
            }
            return s.ToString();
        }

        private static string GetNext(ref int i, ref int start) {
            i %= _IDS.Length;
            start += _IDS[i];
            var len = _IDS[++i % _IDS.Length];
            if (start + len < _PHRASE.Length)
                return _PHRASE.Substring(start, len);
            return null;
        }
    }
}
#endif