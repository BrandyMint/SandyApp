using System;
using System.Collections;
using System.Collections.Generic;
using Games.Common.GameFindObject;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Games.Slimes {
    public class Slime : InteractableModel {
        private static readonly int _COLOR = Shader.PropertyToID("_Color");
        private static readonly int _SMASH = Animator.StringToHash("smash");
        private static readonly int _DO_SMASH = Animator.StringToHash("doSmash");
        private static readonly int _WAIT = Animator.StringToHash("wait");
        private static readonly int _REVIVE = Animator.StringToHash("revive");
        
        [SerializeField] private float _timeToRevived = 4f;
        [SerializeField] private float _timeToNewColor = 5f;
        [SerializeField] private Animator _animator;
        
        public static event Action<Slime> OnNeedNewColor;
        
        private List<Rigidbody> _blobs;
        private int _modelLayer;

        private void Start() {
            _modelLayer = _model.layer;
            _animator.SetFloat(_WAIT, _timeToRevived);
            StartCoroutine(nameof(WaitNewColor));
        }
        
        public override void Bang(bool isRight) {
            StartCoroutine(PlayParticlesAndDead(isRight ? _rightBang : _wrongBang));
        }

        protected override IEnumerator PlayParticlesAndDead(ParticleSystem particles) {
            StopCoroutine(nameof(WaitNewColor));
            _model.layer = 0;
            particles.Play();
            _animator.SetInteger(_SMASH, Random.Range(0, 3));
            _animator.SetTrigger(_DO_SMASH);
            yield return new WaitForSeconds(_timeToRevived);
            _animator.SetTrigger(_REVIVE);
            _model.layer = _modelLayer;
            OnNeedNewColor?.Invoke(this);
            StartCoroutine(nameof(WaitNewColor));
        }

        private IEnumerator WaitNewColor() {
            while (true) {
                OnNeedNewColor?.Invoke(this);
                yield return new WaitForSeconds(_timeToNewColor);
            }
        }

        public void SetColor(Color color) {
            var props = new MaterialPropertyBlock();
            _r.GetPropertyBlock(props);
            props.SetColor(_COLOR, color);
            _r.SetPropertyBlock(props);
        }
    }
}