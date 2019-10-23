using System.Collections;
using DepthSensor.Buffer;
using UnityEngine;

namespace DepthSensorSandbox.Visualisation {
    [RequireComponent(typeof(Camera))]
    public class OneMomentBillboard : MonoBehaviour {
        [SerializeField] private bool _hideByTimer = false;
        [SerializeField] private float _timer = 2f;

        private Camera _cam;
        private bool _isDepthValid;

        private void Awake() {
            _cam = GetComponent<Camera>();
            DepthSensorSandboxProcessor.OnNewFrame += OnNewFrame;
        }
        
        private void OnDestroy() {
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
            float t = 0;
            while (!_isDepthValid) {
                yield return null;
                t += Time.unscaledDeltaTime;
            }
            yield return null;
            _cam.clearFlags = CameraClearFlags.Depth;
            
            while (_hideByTimer && t < _timer) {
                yield return null;
                t += Time.unscaledDeltaTime;
            }
            Destroy(gameObject);
        }
    }
}