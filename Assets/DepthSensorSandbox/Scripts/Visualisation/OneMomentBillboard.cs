using System;
using System.Collections;
using DepthSensor.Buffer;
using Launcher;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DepthSensorSandbox.Visualisation {
    [RequireComponent(typeof(Camera))]
    public class OneMomentBillboard : MonoBehaviour {
        public static event Action OnReady {
            add {
                if (_instances > 0) {
                    _onReady += value;
                } else {
                    value?.Invoke();
                }
            }
            remove => _onReady -= value;
        }
        
        
        [SerializeField] private bool _hideByTimer = false;
        [SerializeField] private float _timer = 2f;
        [SerializeField] private bool _simulate;
        [SerializeField] private bool _keepClearFlags;

        private Camera _cam;
        private bool _isDepthValid;

        private static int _instances;
        private static event Action _onReady;

        private void Awake() {
            ++_instances;
            _cam = GetComponent<Camera>();
            _isDepthValid = _simulate;
        }
        
        private void OnDestroy() {
            --_instances;
            StopAllCoroutines();
            DepthSensorSandboxProcessor.OnNewFrame -= OnNewFrame;
        }

        private void Start() {
            StartCoroutine(Hidding());
        }

        private void OnNewFrame(DepthBuffer depth, MapDepthToCameraBuffer map) {
            if (depth.IsDepthValid()) {
                _isDepthValid = true;
            }
        }

        private IEnumerator Hidding() {
            var start = Time.unscaledTime;
            yield return new WaitUntil(() => Scenes.LoadingOrActiveScenePath == SceneManager.GetActiveScene().path);
            
            DepthSensorSandboxProcessor.OnNewFrame += OnNewFrame;
            yield return new WaitUntil(() => _isDepthValid);
            yield return null;

            if (!_keepClearFlags) {
                _cam.clearFlags = CameraClearFlags.Depth;
            }

            yield return new WaitWhile(() => _hideByTimer && Time.unscaledTime - start < _timer);
            Hide();
        }

        public void Hide() {
            Destroy(gameObject);
            _onReady?.Invoke();
        }
    }
}