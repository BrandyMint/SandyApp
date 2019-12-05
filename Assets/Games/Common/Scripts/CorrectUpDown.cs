using UnityEngine;

namespace Games.Common {
    public class CorrectUpDown : MonoBehaviour {
        public enum CorrectMethod {
            SCALE,
            POSITION,
            ROTATION
        }
        
        [SerializeField] private bool _byUIFlip = true;
        [SerializeField] private bool _bySandboxFlip = true;
        [SerializeField] private CorrectMethod _method = CorrectMethod.SCALE; 
        private float _prevY = 1f;
        
        private void Awake() {
            Prefs.App.OnChanged += OnAppChanged;
            OnAppChanged();
        }

        private void OnDestroy() {
            Prefs.App.OnChanged -= OnAppChanged;
        }

        private void OnAppChanged() {
            var flip = (Prefs.App.FlipVertical && _byUIFlip) ^ (Prefs.App.FlipVerticalSandbox && _bySandboxFlip);
            var y = flip ? -1f : 1f;
            switch (_method) {
                case CorrectMethod.SCALE:
                    var scale = transform.localScale;
                    scale.y *= y * _prevY;
                    transform.localScale = scale;
                    break;
                case CorrectMethod.POSITION:
                    var pos = transform.localPosition;
                    pos.y *= y * _prevY;
                    transform.localPosition = pos;
                    break;
                case CorrectMethod.ROTATION:
                    var r = y * _prevY < 1f ? Quaternion.AngleAxis(180f, Vector3.back) : Quaternion.identity;
                    var rot = transform.localRotation;
                    transform.localRotation = rot * r;
                    break;
            }
            _prevY = y;
        }
    }
}