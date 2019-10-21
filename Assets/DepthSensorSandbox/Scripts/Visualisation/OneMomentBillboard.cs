using DepthSensor.Buffer;
using UnityEngine;

namespace DepthSensorSandbox.Visualisation {
    public class OneMomentBillboard : MonoBehaviour {
        private void Awake() {
            DepthSensorSandboxProcessor.OnNewFrame += OnNewFrame;
        }
        
        private void OnNewFrame(DepthBuffer depth, MapDepthToCameraBuffer map) {
            if (depth.IsDepthValid()) {
                Destroy(gameObject);
            }
        }

        private void OnDestroy() {
            DepthSensorSandboxProcessor.OnNewFrame -= OnNewFrame;
        }
    }
}