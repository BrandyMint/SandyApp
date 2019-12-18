using UnityEngine;
using Random = UnityEngine.Random;

namespace Games.Common {
    [RequireComponent(typeof(Renderer))]
    public class RandomColorRenderer : MonoBehaviour {
        private static readonly int _COLOR = Shader.PropertyToID("_Color");
        [SerializeField] private int _onlyForMaterialId = -1;

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

            if (_onlyForMaterialId >= 0 && _onlyForMaterialId < _r.materials.Length) {
                var materials = _r.materials;
                var mat = new Material(materials[_onlyForMaterialId]);
                mat.SetColor(_COLOR, _color);
                materials[_onlyForMaterialId] = mat;
                _r.materials = materials;
            } else {
                var props = new MaterialPropertyBlock();
                _r.GetPropertyBlock(props);
                props.SetColor(_COLOR, _color);
                _r.SetPropertyBlock(props);
            }
        }

        public Color GetColor() {
            return _color;
        }
    }
}