using DepthSensor.Buffer;
using UnityEngine;

namespace DepthSensorSandbox {
    [RequireComponent(typeof(SandboxMesh))]
    public class SandboxVisualizerColor : SandboxVisualizerBase {
        private static readonly int _DEPTH_TO_COLOR_TEX = Shader.PropertyToID("_DepthToColorTex");
        private static readonly int _COLOR_TEX = Shader.PropertyToID("_ColorTex");

        public override void SetEnable(bool enable) {
            base.SetEnable(enable);
            if (enable) {
                DepthSensorSandboxProcessor.OnDepthToColor += OnDepthToColor;
                DepthSensorSandboxProcessor.OnColor += OnColor;
            } else {
                DepthSensorSandboxProcessor.OnColor -= OnColor;
                DepthSensorSandboxProcessor.OnDepthToColor -= OnDepthToColor;
            }
        }

        private void OnDepthToColor(DepthSensorSandboxProcessor.DepthToColorBuffer d) {
            _material.SetTexture(_DEPTH_TO_COLOR_TEX, d.texture);
        }

        private void OnColor(ColorBuffer color) {
            _material.SetTexture(_COLOR_TEX, color.texture);
        }
    }
}