using System.Collections;
using UnityEngine;

namespace Games.Common.Navigation {
    public class MagnetToNavMesh : SpawningToNavMesh {
        [Header("Magnet")]
        [SerializeField] protected float _timeToCheck = 0.5f;
        [SerializeField] protected float _timeToShow = 1f;
        [SerializeField] protected float _maxPosDifference = 0.01f;

        protected float _showingAccel;
        protected float _show;
        protected Vector3 _initScale;

        protected Coroutine _showing;
        protected Coroutine _magneting;

        private void Awake() {
            OnSpawned += OnSpawnedSuccess;
            _initScale = transform.localScale;
        }

        public void Init(Transform field) {
            _spawnField = field;
        }

        public void AcceptScale(Vector3 scale) {
            _initScale = scale;
            if (_showing == null) {
                transform.localScale = scale;
            }
        }

        private void OnSpawnedSuccess() {
            StartShowHide(1f, true);
            StartMagneting();
        }

        protected override void OnWaitSpawnNextIteration() {
            StopShowHide();
            StopMagneting();
            base.OnWaitSpawnNextIteration();
        }

        private void OnMagnetFail() {
            StartShowHide(-1f);
        }

        private void OnHided() {
            RandomSpawn();
        }

        private void StartShowHide(float accel, bool restart = false) {
            _showingAccel = accel;
            if (restart)
                _show = accel > 0f ? 0f : 1f;
            if (_showing == null) {
                _showing = StartCoroutine(Showing());
            }
        }
        
        private void StopShowHide() {
            if (_showing != null) {
                StopCoroutine(_showing);
                _showing = null;
            }
        }

        private IEnumerator Showing() {
            bool hided = false;
            bool showed = false;
            while (true) {
                _show += _showingAccel * Time.deltaTime / _timeToShow;
                showed = _showingAccel > 0f && _show >= 1f;
                hided = _showingAccel < 0f && _show <= 0f;
                _show = Mathf.Clamp01(_show);
                transform.localScale = _initScale * _show;
                if (showed || hided)
                    break;
                yield return null;
            }
            _showing = null;
            
            if (hided)
                OnHided();
        }

        private void StartMagneting() {
            if (_magneting == null) {
                _magneting = StartCoroutine(Magneting());
            }
        }

        private void StopMagneting() {
            if (_magneting != null) {
                StopCoroutine(_magneting);
                _magneting = null;
            }
        }

        protected IEnumerator Magneting() {
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