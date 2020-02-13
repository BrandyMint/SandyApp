using System.Collections;
using BezierSolution;
using Games.Common;
using Unity.Mathematics;
using UnityEngine;
using Utilities;
using Random = UnityEngine.Random;

namespace Games.Landscape {
    [RequireComponent(typeof(Animator), typeof(BezierWalkerWithSpeed))]
    public class Animal : AbstractAnimal {
        protected override IEnumerator Living() {
            SetAnimState(_IDLE);
            yield return new WaitForSeconds(Random.Range(0f, _maxTimeState / 2f));
            while (true) {
                var state = _animStates.Random();
                SetAnimState(state);
                if (state == _WALK) {
                    yield return WalkRandom();
                } else {
                    yield return WaitRandom();
                }
            }
        }
    }
}