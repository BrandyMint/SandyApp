using UnityEngine;

namespace Games.Common {
    [RequireComponent(typeof(ParticleSystemRenderer))]
    public class CorrectFlipParticles : MonoBehaviour {
        [SerializeField] protected bool _byUIFlip = true;
        [SerializeField] protected bool _bySandboxFlip = true;

        private ParticleSystemRenderer _particleSystem;
        
        private void Awake() {
            _particleSystem = GetComponent<ParticleSystemRenderer>();
            Prefs.App.OnChanged += OnAppChanged;
            OnAppChanged();
        }

        private void OnDestroy() {
            Prefs.App.OnChanged -= OnAppChanged;
        }

        protected virtual void OnAppChanged() {
            var flipY = (Prefs.App.FlipVertical && _byUIFlip) ^ (Prefs.App.FlipVerticalSandbox && _bySandboxFlip);
            var flipX = (Prefs.App.FlipHorizontal && _byUIFlip) ^ (Prefs.App.FlipHorizontalSandbox && _bySandboxFlip);
            
            var flip = _particleSystem.flip;
            flip.x = flipX ? 1f : 0f;
            flip.y = flipY ? 1f : 0f;
            _particleSystem.flip = flip;
        }
    }
}