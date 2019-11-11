using UnityEngine;

namespace DepthSensorSandbox.Visualisation {
    [RequireComponent(typeof(SandboxMesh))]
    public class SandboxVisualizerBase : MonoBehaviour {
        private static readonly int _DEPTH_ZERO = Shader.PropertyToID("_DepthZero");
        private static readonly int _DEPTH_MAX_OFFSET = Shader.PropertyToID("_DepthMaxOffset");
        private static readonly int _DEPTH_MIN_OFFSET = Shader.PropertyToID("_DepthMinOffset");
        private static readonly int _FLIP_HORIZONTAL = Shader.PropertyToID("_FlipHorizontal");
        
        [SerializeField] protected Material _material;
        [SerializeField] protected bool _instantiateMaterial = true;
        [SerializeField] protected bool _enableOnStart = true;

        protected SandboxMesh _sandbox;
        private SandboxParams _sandboxParams;

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
            OverrideParamsSource(null);
        }

        public virtual void SetEnable(bool enable) {
            enabled = enable;
            if (enable) {
                SetDefaultParamsSourceIfNeed();   
                OnSandboxParamsChange();
                Prefs.App.OnChanged += OnPrefsAppChanged;
                OnPrefsAppChanged();
                _sandbox.Material = _material;
            } else {
                Prefs.App.OnChanged -= OnPrefsAppChanged;
            }
        }

        private void SetDefaultParamsSourceIfNeed() {
            if (_sandboxParams == null) {
                OverrideParamsSource(Prefs.Sandbox);
            }
        }

        public void OverrideParamsSource(SandboxParams overrideParams) {
            if (_sandboxParams != null) {
                _sandboxParams.OnChanged -= OnSandboxParamsChange;
            }
            _sandboxParams = overrideParams;
            if (_sandboxParams != null) {
                _sandboxParams.OnChanged += OnSandboxParamsChange;
                OnSandboxParamsChange();
            }
        }

        private void OnSandboxParamsChange() {
            if (enabled)
                OnSandboxParamsChange(_sandboxParams);
        }
        
        protected virtual void OnSandboxParamsChange(SandboxParams sandboxParams) {
            if (_sandbox == null)
                return;
            var props = _sandbox.PropertyBlock;
            props.SetFloat(_DEPTH_ZERO, sandboxParams.ZeroDepth);
            props.SetFloat(_DEPTH_MIN_OFFSET, sandboxParams.OffsetMinDepth);
            props.SetFloat(_DEPTH_MAX_OFFSET, sandboxParams.OffsetMaxDepth);
            _sandbox.PropertyBlock = props;
        }
        
        

        private void OnPrefsAppChanged() {
            if (_sandbox == null)
                return;
            var props = _sandbox.PropertyBlock;
            props.SetInt(_FLIP_HORIZONTAL, Prefs.App.FlipHorizontal ? 1 : 0);
            _sandbox.PropertyBlock = props;
        }
    }
}