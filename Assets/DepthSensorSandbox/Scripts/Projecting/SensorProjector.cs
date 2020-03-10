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

        public abstract Vector3 PlaceOnSandbox(Vector3 worldPos);
    }
}