using DepthSensor.Stream;
using Unity.Collections;
using UnityEngine;

namespace DepthSensorSandbox {
    [RequireComponent(typeof(MeshRenderer))]
    public class SandboxColor : MonoBehaviour {
        private static readonly int _DEPTH_ZERO = Shader.PropertyToID("_DepthZero");
        private static readonly int _DEPTH_TO_COLOR_TEX = Shader.PropertyToID("_DepthToColorTex");
        private static readonly int _COLOR_TEX = Shader.PropertyToID("_ColorTex");
        
        private Material _mat;
        private NativeArray<ushort> _depthToColorNative;
        private bool _isColorSetted;
        private bool _isDepthToColorSetted;

        private void Awake() {
            _mat = GetComponent<MeshRenderer>().material;
            Prefs.Calibration.OnChanged += OnCalibrationChange;
            OnCalibrationChange();
        }

        private void Start() {
            DepthSensorSandboxProcessor.OnDepthToColor += OnDepthToColor;
            DepthSensorSandboxProcessor.OnColor += OnColor;
        }

        private void OnDestroy() {
            DepthSensorSandboxProcessor.OnColor -= OnColor;
            DepthSensorSandboxProcessor.OnDepthToColor -= OnDepthToColor;
            Prefs.Calibration.OnChanged -= OnCalibrationChange;
        }

        private void OnCalibrationChange() {
            _mat.SetFloat(_DEPTH_ZERO, Prefs.Calibration.ZeroDepth);
        }

        private void OnDepthToColor(DepthSensorSandboxProcessor.DepthToColorStream d) {
            if (!_isDepthToColorSetted) {
                _mat.SetTexture(_DEPTH_TO_COLOR_TEX, d.texture);
                _isDepthToColorSetted = true;
            }
            d.texture.Apply(false);
        }

        private void OnColor(ColorStream color) {
            if (!_isColorSetted) {
                _mat.SetTexture(_COLOR_TEX, color.texture);
                _isColorSetted = true;
            }
            color.texture.Apply(false);
        }
    }
}