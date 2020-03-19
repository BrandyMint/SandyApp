using System.Collections;
using UnityEngine;

namespace Games.Landscape {
    public class AnimalFlying : Animal {
        protected static readonly int _FLY = Animator.StringToHash("fly");
        protected static readonly int _FLITTER = Animator.StringToHash("flitter");

        protected override int[] GetAnimStates() {
            return new[] {_FLY};
        }

        protected override IEnumerator Living() {
            SetAnimState(_FLY);
            StartCoroutine(Flittering());
            while (true) {
                yield return WalkRandom();
            }
        }

        protected override void CheckWalkAnim() {
        }

        private IEnumerator Flittering() {
            var flitter = true;
            while (true) {
                yield return WaitRandom();
                flitter = !flitter;
                _anim.SetBool(_FLITTER, flitter);
            }
        }
    }
}