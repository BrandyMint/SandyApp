using UnityEngine;

namespace Games.Common {
    [RequireComponent(typeof(Renderer))]
    public class RandomColorRenderer : RandomColorBase {
        private static readonly int _COLOR = Shader.PropertyToID("_Color");
        [SerializeField] private int _onlyForMaterialId = -1;
        
        private Renderer _r;
        private Renderer Rend {
            get {
                if (_r == null) {
                    _r = GetComponent<Renderer>();
                }
                return _r;
            }
        }

        protected override Color GetObjectColor() {
            return Rend.material.GetColor(_COLOR);
        }

        public override void SetColor(Color color) {
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
    }
}