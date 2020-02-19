using DepthSensor.Buffer;
using UnityEngine;

namespace DepthSensorSandbox.Visualisation {
    
    [RequireComponent(typeof(SandboxMesh))]
    public class SandboxHands : MonoBehaviour {
        private static readonly int _HANDS_MASK_TEX = Shader.PropertyToID("_HandsMaskTex");
        
        [SerializeField] protected bool _enableOnStart = true;
        
        protected SandboxMesh _sandbox;
        private SandboxParams _sandboxParams;

        protected virtual void Awake() {
            _sandbox = GetComponent<SandboxMesh>();
        }

        private void Start() {
            SetEnable(_enableOnStart);
        }

        protected virtual void OnDestroy() {
            SetEnable(false);
        }

        public virtual void SetEnable(bool enable) {
            enabled = enable;
            if (enable) {
                DepthSensorSandboxProcessor.OnNewFrame += OnNewFrame;
            } else {
                DepthSensorSandboxProcessor.OnNewFrame -= OnNewFrame;
            }
        }

        private void OnNewFrame(DepthBuffer depth, MapDepthToCameraBuffer map) {
            if (_sandbox == null)
                return;
            var props = _sandbox.PropertyBlock;
            var mask = DepthSensorSandboxProcessor.Instance.Hands.HandsMask.GetNewest();
            mask.UpdateTexture();
            props.SetTexture(_HANDS_MASK_TEX, mask.texture);
            _sandbox.PropertyBlock = props;
        }
    }
}