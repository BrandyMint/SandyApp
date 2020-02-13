using DepthSensor.Buffer;
using UnityEngine;
using Utilities;

namespace DepthSensorSandbox.Visualisation {
    [RequireComponent(typeof(SandboxMesh))]
    public class SandboxVisualizerColor : SandboxVisualizerBase {
        private static readonly int _DEPTH_TO_COLOR_TEX = Shader.PropertyToID("_DepthToColorTex");
        private static readonly int _COLOR_TEX = Shader.PropertyToID("_ColorTex");
        private bool _isFreezeColor;
        private RenderTexture _freezedColorTex;

        protected override void OnDestroy() {
            base.OnDestroy();
            if (_freezedColorTex != null)
                Destroy(_freezedColorTex);
        }

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
            var tex = color.texture;
            if (!_isFreezeColor) {
                TexturesHelper.ReCreateIfNeedCompatible(ref _freezedColorTex, tex);
                Graphics.Blit(tex, _freezedColorTex);
                _material.SetTexture(_COLOR_TEX, tex);
            }
        }

        public bool FreezeColor {
            get => _freezedColorTex;
            set {
                _isFreezeColor = value;
                if (_isFreezeColor) {
                    if (_freezedColorTex != null) {
                        _material.SetTexture(_COLOR_TEX, _freezedColorTex);
                    } else
                        _isFreezeColor = false;
                }
            }
        }
    }
}