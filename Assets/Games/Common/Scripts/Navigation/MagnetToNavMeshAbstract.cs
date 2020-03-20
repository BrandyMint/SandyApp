using System.Collections;
using UnityEngine;

namespace Games.Common.Navigation {
    public abstract class MagnetToNavMeshAbstract : SpawningToNavMesh {
        [Header("Magnet")]
        [SerializeField] protected float _timeToCheck = 0.5f;
        [SerializeField] protected float _maxPosDifference = 0.01f;

        private Coroutine _magneting;

        protected virtual void Awake() {
            OnSpawned += OnSpawnedSuccess;
        }

        protected virtual void OnSpawnedSuccess() {
            StartMagneting();
        }

        protected override void OnWaitSpawnNextIteration() {
            StopMagneting();
            base.OnWaitSpawnNextIteration();
        }

        protected abstract void OnMagnetFail();

        protected void StartMagneting() {
            if (_magneting == null) {
                _magneting = StartCoroutine(Magneting());
            }
        }

        protected void StopMagneting() {
            if (_magneting != null) {
                StopCoroutine(_magneting);
                _magneting = null;
            }
        }

        private IEnumerator Magneting() {
            while (true) {
                var magnetFail = true;
                var oldPos = transform.position;
                var newPos = oldPos;
                if (SamplePositionWithScaledDistance(oldPos, out var hit, _maxSampleDistance)) {
                    newPos = hit.position;
                    var diff = (Vector2) _spawnField.InverseTransformVector(oldPos - newPos);
                    magnetFail = diff.magnitude > _maxPosDifference;
                }
                if (magnetFail) break;
                
                var t = _timeToCheck;
                do {
                    t -= Time.deltaTime;
                    transform.position = Vector3.Lerp(newPos, oldPos, t / _timeToCheck);
                    yield return null;
                } while (t > 0f);
            }

            _magneting = null;
            OnMagnetFail();
        }
    }
}