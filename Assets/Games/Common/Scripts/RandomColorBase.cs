using UnityEngine;

namespace Games.Common {
    public abstract class RandomColorBase : MonoBehaviour {
        [SerializeField] protected bool _randomOnAwake = true;

        protected Color _color;

        private void Awake() {
            if (_randomOnAwake)
                SetRandomColor();
        }

        protected abstract Color GetObjectColor();

        public abstract void SetColor(Color color);

        public void SetRandomColor() {
            _color = RandomColor(GetObjectColor());
            SetColor(_color);
        }

        public Color GetColor() {
            return _color;
        }

        protected Color RandomColor(Color color) {
            Color.RGBToHSV(color, out _, out var s, out var v);
            return Color.HSVToRGB(Random.value, s, v);
        }
    }
}