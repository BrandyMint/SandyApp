using UnityEngine;
using Random = UnityEngine.Random;

namespace Games.Common {
    [RequireComponent(typeof(Renderer))]
    public class RandomColorRenderer : MonoBehaviour {
        private static readonly int _COLOR = Shader.PropertyToID("_Color");
        [SerializeField] private int _onlyForMaterialId = -1;
        [SerializeField] private bool _randomOnAwake = true;

        private Color _color;
        private Renderer _r;
        private Renderer Rend {
            get {
                if (_r == null) {
                    _r = GetComponent<Renderer>();
                }
                return _r;
            }
        }

        private void Awake() {
            if (_randomOnAwake)
                SetRandomColor();
        }

        public void SetRandomColor() {
            _color = Rend.material.GetColor(_COLOR);
            Color.RGBToHSV(_color, out _, out var s, out var v);
            _color = Color.HSVToRGB(Random.value, s, v);
            SetColor(_color);
        }

        public void SetColor(Color color) {
            _color = color;
            if (_onlyForMaterialId >= 0 && _onlyForMaterialId < Rend.materials.Length) {
                var materials = Rend.materials;
                var mat = new Material(materials[_onlyForMaterialId]);
                mat.SetColor(_COLOR, _color);
                materials[_onlyForMaterialId] = mat;
                Rend.materials = materials;
            } else {
                var props = new MaterialPropertyBlock();
                Rend.GetPropertyBlock(props);
                props.SetColor(_COLOR, _color);
                Rend.SetPropertyBlock(props);
            }
        }

        public Color GetColor() {
            return _color;
        }
    }
}