using UnityEngine;
using Random = UnityEngine.Random;

namespace Games.Common {
    [RequireComponent(typeof(Renderer))]
    public class RandomColorRenderer : MonoBehaviour {
        private static readonly int _COLOR = Shader.PropertyToID("_Color");

        private Color _color;
        private Renderer _r;

        private void Awake() {
            SetRandomColor();
        }

        public void SetRandomColor() {
            _r = GetComponent<Renderer>();
            _color = _r.material.GetColor(_COLOR);
            Color.RGBToHSV(_color, out _, out var s, out var v);
            _color = Color.HSVToRGB(Random.value, s, v);
            
            var props = new MaterialPropertyBlock();
            _r.GetPropertyBlock(props);
            props.SetColor(_COLOR, _color);
            _r.SetPropertyBlock(props);
        }

        public Color GetColor() {
            return _color;
        }
    }
}