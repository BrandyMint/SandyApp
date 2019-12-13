using UnityEngine;

namespace Games.Tubs {
    public class TubeFragment : MonoBehaviour {
        private const int _ITERATIONS = 20;
        
        [HideInInspector]
        [SerializeField] private TubeDir _dirs = 0;
        [HideInInspector]
        [SerializeField] private bool _hasValve;

        [SerializeField] private Collider _collider;
        [SerializeField] private float _steamMinAngle = 20f;
        [SerializeField] private float _steamMaxAngle = 60f;
        
        public TubeDir Dirs => _dirs;

        public bool HasValve => _hasValve;

        public void Init() {
            _hasValve = name.Contains("V");
            
            _dirs = 0;
            foreach (var dVal in Tube.DIRS) {
                if (name.Contains(dVal.ToString())) {
                    _dirs |= dVal;
                }
            }
        }

        public bool GetRandomSteamSpawn(out Vector3 worldPos, out Quaternion worldRot) {
            worldPos = Vector3.zero;
            worldRot = Quaternion.identity;
            _collider.enabled = true;
            bool success = false;
            for (int i = 0; i < _ITERATIONS; ++i) {
                var rayStart = new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f), -1f);
                var rayDirWorld = transform.TransformDirection(Vector3.forward);
                var ray = new Ray(transform.TransformPoint(rayStart),  rayDirWorld);
                if (Physics.Raycast(ray, out var hit)) {
                    var angle = Vector3.Angle(hit.normal, -rayDirWorld);
                    if (angle >= _steamMinAngle && angle <= _steamMaxAngle) {
                        worldPos = hit.point;
                        worldRot = Quaternion.LookRotation(hit.normal, -rayDirWorld);
                        success = true;
                        break;
                    }
                }
            }

            return success;
        }
    }
}