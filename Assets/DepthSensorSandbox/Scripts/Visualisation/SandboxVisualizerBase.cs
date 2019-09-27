using UnityEngine;

namespace DepthSensorSandbox.Visualisation {
    [RequireComponent(typeof(SandboxMesh))]
    public class SandboxVisualizerBase : MonoBehaviour {
        private static readonly int _DEPTH_ZERO = Shader.PropertyToID("_DepthZero");
        
        [SerializeField] protected Material _material;
        [SerializeField] protected bool _instantiateMaterial = true;

        protected SandboxMesh _sandbox;
        
        protected virtual void Awake() {
            if (_instantiateMaterial)
                _material = new Material(_material);
            _sandbox = GetComponent<SandboxMesh>();
        }

        protected virtual void OnDestroy() {
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
            enabled = enable;
        }
        
        protected virtual void OnCalibrationChange() {
            var props = _sandbox.PropertyBlock;
            props.SetFloat(_DEPTH_ZERO, Prefs.Calibration.ZeroDepth);
            _sandbox.PropertyBlock = props;
        }
    }
}