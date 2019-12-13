using System.Collections;
using Games.Common.GameFindObject;
using UnityEngine;

namespace Games.Tubs {
    public class Steam : InteractableModel {
        [SerializeField] private ParticleSystem _steamParticles;
        [SerializeField] private float _timeToFix = 2f;

        private float _steamFactor = 1f;
        private float _maxSteam;

        private void Start() {
            _maxSteam = _steamParticles.main.startLifetime.constant;
            StartCoroutine(Steaming());
        }
        
        public override void Bang(bool isRight) {
            _steamFactor = isRight ? -1f : 1f;
        }

        private IEnumerator Steaming() {
            var module = _steamParticles.main;
            var startLifetime = module.startLifetime;
            _steamParticles.Play();

            while (true) {
                startLifetime.constant += _maxSteam / _timeToFix * _steamFactor * Time.deltaTime;
                if (startLifetime.constant < 0f)
                    break;
                startLifetime.constant = Mathf.Clamp(startLifetime.constant, 0f, _maxSteam);
                module.startLifetime = startLifetime;
                yield return null;
            }
            
            _steamParticles.Stop();
            PlayAudioBang(true);
            StartCoroutine(PlayParticlesAndDead(_rightBang));
        }
    }
}