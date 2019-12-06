using UnityEngine;
using UnityEngine.UI;

namespace Games.Planet {
    public class CircleHP : MonoBehaviour {
        
        [SerializeField] private Text _text;
        [SerializeField] private Image _circle;

        public int Max;
        private int _val = -1;

        public int Val {
            get => _val;
            set {
                if (_val != value) {
                    _val = value;
                    UpdateHP();
                }
            }
        }

        private void UpdateHP() {
            if (_text != null) {
                _text.text = _val.ToString();
            }
            
            if (_circle != null)
                _circle.fillAmount = (float)_val / Max;
        }
    }
}