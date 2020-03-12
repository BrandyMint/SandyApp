using DepthSensor;
using UnityEngine;

namespace DepthSensorSandbox.Projecting {
    public abstract class SensorProjector : MonoBehaviour {
        
        public static SensorProjector Instance { get; protected set; }
        
        protected virtual void Awake() {
            Instance = this;
        }

        public static Vector3 OnSandbox(Vector3 worldPos) {
            if (Instance != null)
                return Instance.PlaceOnSandbox(worldPos);
            return worldPos;
        }

        public static Vector2 UVFrom(Vector3 worldPos) {
            if (Instance != null)
                return Instance.GetUVFrom(worldPos);
            return worldPos;
        }

        public abstract Vector3 PlaceOnSandbox(Vector3 worldPos);
        
        public Vector2 GetUVFrom(Vector3 worldPos) {
            var device = DepthSensorManager.Instance?.Device;
            if (device != null && device.IsAvailable()) {
                var pos = transform.InverseTransformPoint(worldPos);
                var p = device.CameraPosToDepthMapPos(pos);
                var depth = device.Depth.GetNewest();
                p.x /= depth.width;
                p.y /= depth.height;
                return p;
            }

            return worldPos;
        }
    }
}