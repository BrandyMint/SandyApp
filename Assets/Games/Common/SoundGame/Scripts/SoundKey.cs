using Games.Common.GameFindObject;
using UnityEngine;

namespace Games.Common.SoundGame {
    [RequireComponent(typeof(Animator))]
    public class SoundKey : InteractableModel {
        private static readonly int _PLAY = Animator.StringToHash("play");
        private Animator _animator;

        public virtual string GetNoteName() {
            return _audioRight.name;
        }

        protected override void Awake() {
            _animator = GetComponent<Animator>();
            base.Awake();
        }
        
        public override void Bang(bool isRight) {
            if (hideOnBang)
                Show(false);
            Play(isRight);
        }

        public void Play(bool isRight = true) {
            if (!Selected) {
                PlayAudioBang(isRight);
                Selected = true;
            }
        }

        public bool Selected {
            get => _animator.GetBool(_PLAY);
            set => _animator.SetBool(_PLAY, value);
        }

        public void Stop() {
            //_audioSource.Stop();
            Selected = false;
        }
    }
}