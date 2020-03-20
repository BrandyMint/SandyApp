using System.Collections;
using UnityEngine;

namespace Games.Common.Navigation {
    public class MagnetToNavMesh : MagnetToNavMeshAbstract {
        [SerializeField] protected float _timeToShow = 1f;

        private float _showingAccel;
        private float _show;
        private Vector3 _initScale;

        private Coroutine _showing;

        protected override void Awake() {
            _initScale = transform.localScale;
            base.Awake();
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

        protected override void OnSpawnedSuccess() {
            StartShowHide(1f, true);
            base.OnSpawnedSuccess();
        }

        protected override void OnWaitSpawnNextIteration() {
            StopShowHide();
            base.OnWaitSpawnNextIteration();
        }

        protected override void OnMagnetFail() {
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
    }
}