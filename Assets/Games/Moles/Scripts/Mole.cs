using System.Collections;
using UnityEngine;

namespace Games.Moles {
    [RequireComponent(typeof(Renderer), typeof(ParticleSystem))]
    public class Mole : MonoBehaviour {
        private static readonly int _SHOW = Animator.StringToHash("show");
        private static readonly int _HIDE = Animator.StringToHash("hide");

        public int Player;
        
        private ParticleSystem _particles;
        private Collider _collider;
        private float _showTime;
        private Animator _anim;

        private void Awake() {
            _particles = GetComponent<ParticleSystem>();
            _collider = GetComponent<Collider>();
            _anim = GetComponent<Animator>();

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

        public void Bang() {
            SetInteractive(false);
            Hide();

            //GetComponent<AudioSource>().Play();
            _particles.Play();
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
    }
}