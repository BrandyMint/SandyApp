using UnityEngine;
using UnityEngine.UI;

namespace Games.Common {
    [RequireComponent(typeof(Text))]
    public class RandomColorText : RandomColorBase {
        private Text _t;
        private Text Txt {
            get {
                if (_t == null) {
                    _t = GetComponent<Text>();
                }
                return _t;
            }
        }

        protected override Color GetObjectColor() {
            return Txt.color;
        }

        public override void SetColor(Color color) {
            _color = color;
            Txt.color = color;
        }
    }
}