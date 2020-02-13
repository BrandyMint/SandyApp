using System.Collections;
using UnityEngine;

namespace Games.Landscape {
    public class AnimalFlying : AbstractAnimal {
        protected static readonly int _FLY = Animator.StringToHash("fly");
        protected static readonly int _FLITTER = Animator.StringToHash("flitter");

        [SerializeField] protected float _randomizeZFly = 0.5f;
        
        public float ZFly { get; set; }
        
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

        private IEnumerator Flittering() {
            var flitter = true;
            while (true) {
                yield return WaitRandom();
                flitter = !flitter;
                _anim.SetBool(_FLITTER, flitter);
            }
        }

        protected override Vector3 CorrectWalkEndPoint(Vector3 p) {
            p.z = ZFly * (1f + Random.Range(-_randomizeZFly, _randomizeZFly));
            return p;
        }
    }
}