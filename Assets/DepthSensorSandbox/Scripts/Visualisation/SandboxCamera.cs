using UnityEngine;

namespace DepthSensorSandbox.Visualisation {
    [RequireComponent(typeof(Camera))]
    public class SandboxCamera : MonoBehaviour {
        private Camera _cam;
        
        private void Awake() {
            _cam = GetComponent<Camera>();
            Prefs.Calibration.OnChanged += OnCalibrationChanged;
            OnCalibrationChanged();
        }

        private void OnDestroy() {
            Prefs.Calibration.OnChanged -= OnCalibrationChanged;
        }

        public void OnCalibrationChanged() {
            transform.localPosition = Prefs.Calibration.Position;
            transform.localRotation = Prefs.Calibration.Rotation;
            _cam.fieldOfView = Prefs.Calibration.Fov;
        }

        public void ResetToCalibration() {
            OnCalibrationChanged();
        }
    }
}