using UnityEngine;

namespace Games.Common {
    public class CorrectUpDown : MonoBehaviour {
        private float _prevY = 1f;
        
        private void Awake() {
            Prefs.App.OnChanged += OnAppChanged;
            OnAppChanged();
        }

        private void OnDestroy() {
            Prefs.App.OnChanged -= OnAppChanged;
        }

        private void OnAppChanged() {
            var scale = transform.localScale;
            var y = Prefs.App.FlipVertical ? -1f : 1f;
            scale.y *= y * _prevY;
            transform.localScale = scale;
            _prevY = y;
        }
    }
}