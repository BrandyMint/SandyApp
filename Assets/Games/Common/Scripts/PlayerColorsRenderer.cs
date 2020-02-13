using System.Linq;
using UnityEngine;
using Utilities;

namespace Games.Common {
    [RequireComponent(typeof(Renderer))]
    public class PlayerColorsRenderer : MonoBehaviour {
        [SerializeField] private Color _delimeterColor = Color.white;
        
        private Renderer _r;
        private Texture2D _colors;
        private MaterialPropertyBlock _props;
        private static readonly int _PLAYERS_TEX = Shader.PropertyToID("_PlayersTex");

        private void Awake() {
            _r = GetComponent<Renderer>();
        }

        private void Start() {
            UpdateColors();
        }

        private void UpdateColors() {
            var colors = PlayerColors.Instance.Colors.Append(_delimeterColor);
            if (TexturesHelper.ReCreateIfNeed(ref _colors, PlayerColors.Instance.Count + 1, 1)) {
                _colors.wrapMode = TextureWrapMode.Clamp;
            }
            var x = 0;
            foreach (var color in colors) {
                _colors.SetPixel(x++, 0, color);
            }
            _colors.Apply();
            
            SetTexture(_colors);
        }

        private void SetTexture(Texture t) {
            if (_r == null) return;
            
            if (_props == null)
                _props = new MaterialPropertyBlock();
            _r.GetPropertyBlock(_props);
            _props.SetTexture(_PLAYERS_TEX, t);
            _r.SetPropertyBlock(_props);
        }

        private void OnDestroy() {
            if (_colors != null) {
                Destroy(_colors);
            }
        }
    }
}