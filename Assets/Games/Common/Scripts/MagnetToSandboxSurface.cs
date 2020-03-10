using DepthSensorSandbox.Projecting;
using UnityEngine;

namespace Games.Common {
    public class MagnetToSandboxSurface : MonoBehaviour {
        [SerializeField] private float _factor = 3f;

        private bool _wasUpdateTarget;
        private Vector3 _target;

        public void ResetMagnet() {
            _wasUpdateTarget = false;
        }

        private void FixedUpdate() {
            _target = SensorProjector.OnSandbox(transform.position);
            _wasUpdateTarget = true;
        }

        private void Update() {
            if (_wasUpdateTarget) {
                var k = Mathf.Min(1f, _factor * Time.deltaTime);
                transform.position = Vector3.Lerp(transform.position, _target, k);
                _wasUpdateTarget = true;
            }
        }
    }
}