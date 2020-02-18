using System;
using UnityEngine;

namespace DepthSensorSandbox.Visualisation {
    [RequireComponent(typeof(Camera))]
    public class SandboxCamera : MonoBehaviour {
        private const float _MAX_VALID_ASPECT = 3f;

        private Camera _cam;

        public static Action<SandboxCamera> AfterCalibrationUpdated;

        private void Awake() {
            _cam = GetComponent<Camera>();
            Prefs.Calibration.OnChanged += OnCalibrationChanged;
            Prefs.Projector.OnChanged += OnCalibrationChanged;
            OnCalibrationChanged();
        }

        private void OnDestroy() {
            Prefs.Calibration.OnChanged -= OnCalibrationChanged;
            Prefs.Projector.OnChanged -= OnCalibrationChanged;
        }

        public void OnCalibrationChanged() {
            transform.localPosition = Prefs.Calibration.Position;
            transform.localRotation = Prefs.Calibration.Rotation;
            
            var aspect = Prefs.Projector.Width / Prefs.Projector.Height;
            if (aspect > 1f / _MAX_VALID_ASPECT && aspect < _MAX_VALID_ASPECT) {
                _cam.aspect = aspect;
            } else {
                _cam.ResetAspect();
            }
            _cam.fieldOfView = Prefs.Calibration.Fov;

            AfterCalibrationUpdated?.Invoke(this);
        }

        public void ResetToCalibration() {
            OnCalibrationChanged();
        }

        public Camera GetCamera() {
            return _cam;
        }
    }
}