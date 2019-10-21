using System.Linq;
using DepthSensor.Buffer;
using UnityEngine;

namespace DepthSensorSandbox.Visualisation {
    [RequireComponent(typeof(Canvas))]
    public class OneMomentBillboard : MonoBehaviour {
        private void Awake() {
            var uiLayer = LayerMask.GetMask("UI");
            var uiCamera = Camera.allCameras.First(c => (c.cullingMask & uiLayer) == uiLayer);
            GetComponent<Canvas>().worldCamera = uiCamera;
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