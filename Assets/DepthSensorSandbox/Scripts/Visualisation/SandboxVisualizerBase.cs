using UnityEngine;

namespace DepthSensorSandbox.Visualisation {
    [RequireComponent(typeof(SandboxMesh))]
    public class SandboxVisualizerBase : MonoBehaviour {
        private static readonly int _DEPTH_ZERO = Shader.PropertyToID("_DepthZero");
        private static readonly int _DEPTH_MAX_OFFSET = Shader.PropertyToID("_DepthMaxOffset");
        private static readonly int _DEPTH_MIN_OFFSET = Shader.PropertyToID("_DepthMinOffset");
        
        [SerializeField] protected Material _material;
        [SerializeField] protected bool _instantiateMaterial = true;
        [SerializeField] protected bool _enableOnStart = true;

        protected SandboxMesh _sandbox;
        
        protected virtual void Awake() {
            if (_instantiateMaterial)
                _material = new Material(_material);
            _sandbox = GetComponent<SandboxMesh>();
        }

        private void Start() {
            Init();
            SetEnable(_enableOnStart);
        }

        protected virtual void Init() { }

        protected virtual void OnDestroy() {
            SetEnable(false);
        }

        public virtual void SetEnable(bool enable) {
            if (enable) {
                Prefs.Sandbox.OnChanged += OnSandboxParamsChange;
                OnSandboxParamsChange();
                _sandbox.Material = _material;
            } else {
                Prefs.Sandbox.OnChanged -= OnSandboxParamsChange;
            }
            enabled = enable;
        }
        
        protected virtual void OnSandboxParamsChange() {
            var props = _sandbox.PropertyBlock;
            props.SetFloat(_DEPTH_ZERO, Prefs.Sandbox.ZeroDepth);
            props.SetFloat(_DEPTH_MIN_OFFSET, Prefs.Sandbox.OffsetMinDepth);
            props.SetFloat(_DEPTH_MAX_OFFSET, Prefs.Sandbox.OffsetMaxDepth);
            _sandbox.PropertyBlock = props;
        }
    }
}