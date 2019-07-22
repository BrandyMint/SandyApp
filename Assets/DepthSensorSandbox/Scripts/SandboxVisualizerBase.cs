using UnityEngine;

namespace DepthSensorSandbox {
    [RequireComponent(typeof(SandboxMesh))]
    public class SandboxVisualizerBase : MonoBehaviour {
        private static readonly int _DEPTH_ZERO = Shader.PropertyToID("_DepthZero");
        
        [SerializeField] protected Material _material;

        private SandboxMesh _sandbox;
        
        protected virtual void Awake() {
            _sandbox = GetComponent<SandboxMesh>();
        }

        protected void OnDestroy() {
            SetEnable(false);
        }

        public virtual void SetEnable(bool enable) {
            if (enable) {
                Prefs.Calibration.OnChanged += OnCalibrationChange;
                OnCalibrationChange();
                _sandbox.Material = _material;
            } else {
                Prefs.Calibration.OnChanged -= OnCalibrationChange;
            }
        }
        
        protected virtual void OnCalibrationChange() {
            _material.SetFloat(_DEPTH_ZERO, Prefs.Calibration.ZeroDepth);
        }
    }
}