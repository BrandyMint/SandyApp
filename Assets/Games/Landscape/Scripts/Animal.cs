using System.Collections;
using UnityEngine;
using Utilities;
using Random = UnityEngine.Random;

namespace Games.Landscape {
    [RequireComponent(typeof(Animator))]
    public class Animal : MonoBehaviour {
        private static readonly int _ACCELERATION = Animator.StringToHash("acceleration");
        protected static readonly int _IDLE = Animator.StringToHash("idle");
        protected static readonly int _WALK = Animator.StringToHash("walk");
        protected static readonly int _EAT = Animator.StringToHash("eat");
        protected int[] _animStates;
        
        [SerializeField] protected float _minTimeState = 1f;
        [SerializeField] protected float _maxTimeState = 3f;

        protected Animator _anim;
        private IAnimalWalker _walker;
        private int _currState;

        private void Awake() {
            _anim = GetComponent<Animator>();
            _animStates = GetAnimStates();
            _walker = GetComponent<IAnimalWalker>();
        }

        protected virtual int[] GetAnimStates() {
            return new[] {_IDLE, _WALK, _EAT};
        }

        private void OnEnable() {
            StartAnimation();
        }

        public void StartAnimation() {
            StopCoroutine(nameof(Living));
            StartCoroutine(nameof(Living));
        }

        protected virtual IEnumerator Living() {
            SetAnimState(_IDLE);
            yield return new WaitForSeconds(Random.Range(0f, _maxTimeState / 2f));
            while (true) {
                var state = _animStates.Random();
                if (state == _WALK) {
                    yield return WalkRandom();
                } else {
                    SetAnimState(state);
                    yield return WaitRandom();
                }
            }
        }

        protected virtual void Update() {
            CheckWalkAnim();
        }

        protected virtual void CheckWalkAnim() {
            var a = _walker.CurrentAcceleration();
            var isMoving = a > 0.1f;
            if (isMoving != (_currState == _WALK)) {
                SetAnimState(isMoving ? _WALK : _IDLE);
            }
            _anim.SetFloat(_ACCELERATION, a);
        }

        protected void SetAnimState(int state) {
            foreach (var s in _animStates) {
                if (s == state)
                    _anim.SetTrigger(s);
                else
                    _anim.ResetTrigger(s);
            }

            _currState = state;
        }
        
        protected IEnumerator WaitRandom() {
            yield return new WaitForSeconds(Random.Range(_minTimeState, _maxTimeState));
        }

        protected IEnumerator WalkRandom() {
            return _walker.WalkRandom();
        }
    }
}