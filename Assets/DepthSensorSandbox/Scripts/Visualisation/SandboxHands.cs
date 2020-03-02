using DepthSensor.Buffer;
using UnityEngine;

namespace DepthSensorSandbox.Visualisation {
    
    [RequireComponent(typeof(SandboxMesh))]
    public class SandboxHands : MonoBehaviour {
        private static readonly int _HANDS_MASK_TEX = Shader.PropertyToID("_HandsMaskTex");
        private static readonly int _HANDS_DEPTH_TEX = Shader.PropertyToID("_HandsDepthTex");
        private static readonly int _HANDS_DEPTH_MAX = Shader.PropertyToID("_HandsDepthMax");
        
        [SerializeField] protected bool _enableOnStart = true;
        [SerializeField] public bool UpdateMask = true;
        [SerializeField] public bool UpdateHandsDepth = true;
        
        protected SandboxMesh _sandbox;
        private SandboxParams _sandboxParams;
        private float _handsDepthMax = 0.04f;

        protected virtual void Awake() {
            _sandbox = GetComponent<SandboxMesh>();
        }

        private void Start() {
            SetEnable(_enableOnStart);
        }

        protected virtual void OnDestroy() {
            SetEnable(false);
        }

        public float HandsDepthMax {
            get => _handsDepthMax;
            set {
                _handsDepthMax = value;
                UpdateHandsDepthMax(value);
            }
        }

        private void UpdateHandsDepthMax(float value) {
            if (_sandbox == null)
                return;
            var props = _sandbox.PropertyBlock;
            props.SetFloat(_HANDS_DEPTH_MAX, value);
            _sandbox.PropertyBlock = props;
        }

        public virtual void SetEnable(bool enable) {
            enabled = enable;
            if (enable) {
                DepthSensorSandboxProcessor.OnNewFrame += OnNewFrame;
                HandsDepthMax = _handsDepthMax;
            } else {
                DepthSensorSandboxProcessor.OnNewFrame -= OnNewFrame;
            }
        }

        private void OnNewFrame(DepthBuffer depth, MapDepthToCameraBuffer map) {
            if (_sandbox == null)
                return;
            var props = _sandbox.PropertyBlock;
            if (UpdateMask) {
                var mask = DepthSensorSandboxProcessor.Instance.Hands.HandsMask.GetNewest();
                mask.UpdateTexture();
                props.SetTexture(_HANDS_MASK_TEX, mask.texture);
            }
            if (UpdateHandsDepth) {
                var hands = DepthSensorSandboxProcessor.Instance.Hands.HandsDepth.GetNewest();
                /*if (hands.texture.filterMode != FilterMode.Point)
                    hands.texture.filterMode = FilterMode.Point;*/
                hands.UpdateTexture();
                props.SetTexture(_HANDS_DEPTH_TEX, hands.texture);
            }
            _sandbox.PropertyBlock = props;
        }
    }
}