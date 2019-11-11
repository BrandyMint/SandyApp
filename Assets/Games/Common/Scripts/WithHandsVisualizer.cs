using DepthSensorSandbox.Visualisation;
using UnityEngine;

namespace Games.Balloons {
    public class WithHandsVisualizer : SandboxVisualizerBase {
        private static readonly int _DEPTH_SLICE_OFFSET = Shader.PropertyToID("_DepthSliceOffset");
        
        [SerializeField] private float _handsOffset = 0.05f;

        public override void SetEnable(bool enable) {
            base.SetEnable(enable);
            if (enable) {
                var props = _sandbox.PropertyBlock;
                props.SetFloat(_DEPTH_SLICE_OFFSET, _handsOffset);
                _sandbox.PropertyBlock = props;
            }
        }
    }
}