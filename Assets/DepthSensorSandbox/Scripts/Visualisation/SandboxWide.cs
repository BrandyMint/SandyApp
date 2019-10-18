using UnityEngine;

namespace DepthSensorSandbox.Visualisation {
    public class SandboxWide : MonoBehaviour {
        private Vector3 _initialScale;
        
        private void Start() {
            _initialScale = transform.localScale;
            Prefs.Calibration.OnChanged += OnCalibrationChanged;
            OnCalibrationChanged();
        }

        private void OnDestroy() {
            Prefs.Calibration.OnChanged -= OnCalibrationChanged;
        }

        private void OnCalibrationChanged() {
            var scale = _initialScale;
            scale.x = Prefs.Calibration.WideMultiply;
            transform.localScale = scale;
        }
    }
}