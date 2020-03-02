using System.Collections;
using Games.Common.GameFindObject;
using UnityEngine;
using Utilities;

namespace Games.Moles {
    [RequireComponent(typeof(Renderer), typeof(ParticleSystem))]
    public class Mole : MonoBehaviour, IInteractable {
        [SerializeField] private AudioClip[] _audioHit;
        [SerializeField] private AudioClip[] _audioDamage;
        
        private static readonly int _SHOW = Animator.StringToHash("show");
        private static readonly int _HIDE = Animator.StringToHash("hide");

        public int ItemType { get; set; }
        
        private ParticleSystem _particles;
        private Collider _collider;
        private float _showTime;
        private Animator _anim;
        private AudioSource _audioSourceHit;
        private AudioSource _audioSourceDamage;

        private void Awake() {
            _particles = GetComponent<ParticleSystem>();
            _collider = GetComponent<Collider>();
            _anim = GetComponent<Animator>();
            _audioSourceHit = gameObject.AddComponent<AudioSource>();
            _audioSourceDamage = gameObject.AddComponent<AudioSource>();

            Hide(true);
        }
        
        public MoleState State { get; private set; }

        public void Show(float time, bool noAnimation = false) {
            State = MoleState.TRANSITION_TO_SHOW;
            _showTime = time;
            if (noAnimation)
                OnShowed();
            else
                _anim.SetTrigger(_SHOW);
        }

        public void Hide(bool noAnimation = false) {
            StopCoroutine(nameof(Showing));
            State = MoleState.TRANSITION_TO_HIDE;
            if (noAnimation)
                OnHided();
            else
                _anim.SetTrigger(_HIDE);
        }

        public void Bang(bool isRight) {
            SetInteractive(false);
            Hide();

            //GetComponent<AudioSource>().Play();
            _particles.Play();
            PlaySound(_audioSourceHit, _audioHit);
            PlaySound(_audioSourceDamage, _audioDamage);
        }

        private void PlaySound(AudioSource audioSource, AudioClip[] clips) {
            audioSource.clip = clips.Random();
            audioSource.Play();
        }

        private IEnumerator Showing(float time) {
            yield return new WaitForSeconds(time);
            Hide();
        }

        // ReSharper disable once UnusedMember.Local
        private void OnTransition() {
            switch (State) {
                case MoleState.TRANSITION_TO_SHOW:
                    OnShowed();
                    break;
                case MoleState.TRANSITION_TO_HIDE:
                    OnHided();
                    break;
            }
        }

        private void OnShowed() {
            State = MoleState.SHOWED;
            SetInteractive(true);
            StartCoroutine(nameof(Showing), _showTime);
        }

        private void OnHided() {
            State = MoleState.HIDED;
            SetInteractive(false);
        }

        private void SetInteractive(bool interactive) {
            //gameObject.layer = interactive ? _interactiveLayer : 0;
            _collider.enabled = interactive;
        }

        public void Dead() {
            gameObject.layer = 0;
            Destroy(gameObject);
        }

        public void PlayAudioBang(bool isRight) {
            throw new System.NotImplementedException();
        }

        public void Show(bool show) {
            throw new System.NotImplementedException();
        }
    }
}