using System.Collections;
using Games.Common.GameFindObject;
using UnityEngine;

namespace Games.Tubs {
    public class Steam : InteractableModel {
        private static readonly int _FILL = Shader.PropertyToID("_Fill");
        
        [SerializeField] private ParticleSystem _steamParticles;
        [SerializeField] private float _timeToFix = 2f;
        [SerializeField] private Renderer _clock;

        private float _steamFactor = 1f;
        private float _maxSteam;
        private MaterialPropertyBlock _clockProps;

        protected override void Awake() {
            _clockProps = new MaterialPropertyBlock();
            _clock.gameObject.SetActive(true);
            base.Awake();
        }

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
            var time = 1f;

            while (true) {
                time += _steamFactor * Time.deltaTime / _timeToFix;
                time = Mathf.Clamp01(time);
                startLifetime.constant = time * _maxSteam;
                _clock.GetPropertyBlock(_clockProps);
                _clockProps.SetFloat(_FILL, time);
                _clock.SetPropertyBlock(_clockProps);
                module.startLifetime = startLifetime;
                if (time <= 0f)
                    break;
                yield return null;
            }
            
            _steamParticles.Stop();
            PlayAudioBang(true);
            StartCoroutine(PlayParticlesAndDead(_rightBang));
        }
    }
}