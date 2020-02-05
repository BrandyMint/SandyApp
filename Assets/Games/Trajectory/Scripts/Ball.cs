using Games.Common.GameFindObject;
using UnityEngine;

namespace Games.Trajectory {
    public class Ball : InteractableModel {
        [SerializeField] private float _minSoundVolume = 0.4f;
        [SerializeField] private float _soundPeriod = 0.3f;
        
        private float _lastSoundTime;
        private float _maxImpulse = -1f;
        
        private void Start() {
            _lastSoundTime = Time.time;
        }

        private void OnCollisionEnter(Collision other) {
            if (Time.time < _lastSoundTime + _soundPeriod)
                return;

            var impulse = other.impulse.magnitude;
            if (_maxImpulse < 0) _maxImpulse = impulse;
            var volume = Mathf.Min(impulse / _maxImpulse, 1f);
            volume *= volume;
            if (volume > _minSoundVolume) {
                PlayAudioBang(false, volume);
            }
        }
        
        public override void PlayAudioBang(bool isRight) {
            PlayAudioBang(isRight, 1f);
        }

        private void PlayAudioBang(bool isRight, float volume) {
            if (_audioSource != null) {
                _audioSource.volume = volume;
                base.PlayAudioBang(isRight);
            }
        }
    }
}