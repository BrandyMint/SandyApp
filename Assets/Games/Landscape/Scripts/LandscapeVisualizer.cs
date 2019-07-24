using DepthSensorSandbox;
using UnityEngine;

namespace Games.Landscape {
    public class LandscapeVisualizer : SandboxVisualizerBase {
        private static readonly int _DEPTH_SEA_BOTTOM = Shader.PropertyToID("_DepthSeaBottom");
        private static readonly int _DEPTH_SEA = Shader.PropertyToID("_DepthSea");
        private static readonly int _DEPTH_GROUND = Shader.PropertyToID("_DepthGround");
        private static readonly int _DEPTH_MOUNTAINS = Shader.PropertyToID("_DepthMountains");

        private void Start() {
            SetEnable(true);
        }

        public override void SetEnable(bool enable) {
            base.SetEnable(enable);
            if (enable) {
                Prefs.Landscape.OnChanged += OnPrefsChanged;
                OnPrefsChanged();
            } else {
                Prefs.Landscape.OnChanged -= OnPrefsChanged;
            }
        }

        private void OnPrefsChanged() {
            _material.SetFloat(_DEPTH_SEA_BOTTOM, Prefs.Landscape.DepthSeaBottom);
            _material.SetFloat(_DEPTH_SEA, Prefs.Landscape.DepthSea);
            _material.SetFloat(_DEPTH_GROUND, Prefs.Landscape.DepthGround);
            _material.SetFloat(_DEPTH_MOUNTAINS, Prefs.Landscape.DepthMountains);
        }
    }
}