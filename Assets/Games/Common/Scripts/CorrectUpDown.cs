using UnityEngine;

namespace Games.Common {
    public class CorrectUpDown : MonoBehaviour {
        public enum CorrectMethod {
            SCALE,
            POSITION,
            ROTATION
        }
        
        [SerializeField] protected bool _byUIFlip = true;
        [SerializeField] protected bool _bySandboxFlip = true;
        [SerializeField] protected CorrectMethod _method = CorrectMethod.SCALE; 
        protected float _prev = 1f;
        
        private void Awake() {
            Prefs.App.OnChanged += OnAppChanged;
            OnAppChanged();
        }

        private void OnDestroy() {
            Prefs.App.OnChanged -= OnAppChanged;
        }

        protected virtual void OnAppChanged() {
            var flip = (Prefs.App.FlipVertical && _byUIFlip) ^ (Prefs.App.FlipVerticalSandbox && _bySandboxFlip);
            var y = flip ? -1f : 1f;
            switch (_method) {
                case CorrectMethod.SCALE:
                    var scale = transform.localScale;
                    scale.y *= y * _prev;
                    transform.localScale = scale;
                    break;
                case CorrectMethod.POSITION:
                    var pos = transform.localPosition;
                    pos.y *= y * _prev;
                    transform.localPosition = pos;
                    break;
                case CorrectMethod.ROTATION:
                    var r = y * _prev < 1f ? Quaternion.AngleAxis(180f, Vector3.back) : Quaternion.identity;
                    var rot = transform.localRotation;
                    transform.localRotation = rot * r;
                    break;
            }
            _prev = y;
        }
    }
}