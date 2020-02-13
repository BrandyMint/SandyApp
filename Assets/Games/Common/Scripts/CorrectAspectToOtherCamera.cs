using UnityEngine;

namespace Games.Common {
    public class CorrectAspectToOtherCamera : MonoBehaviour {
        [SerializeField] private Camera _cam;

        private float _prevAspect; 

        private void Start() {
            _prevAspect = transform.localScale.x / transform.localScale.y; 
            Prefs.Projector.OnChanged += OnProjectorChanged;
            OnProjectorChanged();
        }

        private void OnDestroy() {
            Prefs.Projector.OnChanged -= OnProjectorChanged;
        }

        private void OnProjectorChanged() {
            var projAspect = Prefs.Projector.Width / Prefs.Projector.Height;
            var aspect = projAspect / _cam.aspect / _prevAspect;
            _prevAspect = aspect;
            var scale = transform.localScale;
            scale.x = scale.y / aspect;
            transform.localScale = scale;
        }
    }
}