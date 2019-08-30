using System.Collections.Generic;
using DepthSensorSandbox.Visualisation;
using UnityEngine;

namespace Games.Landscape {
    public class LandscapeVisualizer : SandboxVisualizerBase {
        private static readonly int _DEPTH_SEA_BOTTOM = Shader.PropertyToID("_DepthSeaBottom");
        private static readonly int _DEPTH_SEA = Shader.PropertyToID("_DepthSea");
        private static readonly int _DEPTH_GROUND = Shader.PropertyToID("_DepthGround");
        private static readonly int _DEPTH_MOUNTAINS = Shader.PropertyToID("_DepthMountains");
        private static readonly int _DEPTH_ICE = Shader.PropertyToID("_DepthIce");
        private static readonly int _MIX_DEPTH = Shader.PropertyToID("_MixDepth");
        private static readonly int _MIX_NOISE_STRENGTH = Shader.PropertyToID("_MixNoiseStrength");

        private readonly int[] _DetailSizeFloats = new[] {
            _MIX_DEPTH,
            _MIX_NOISE_STRENGTH,
            Shader.PropertyToID("_MixNoiseSize"),
        };
        private readonly int[] _DetailSizeTexScales = new[] {
            Shader.PropertyToID("_MountainsTex"),
            Shader.PropertyToID("_GroundTex"),
            Shader.PropertyToID("_SandTex"),
        };
        private readonly Dictionary<int, float> _detailSizeFloatDefaults = new Dictionary<int, float>();
        private readonly Dictionary<int, Vector2> _detailSizeTexScaleDefaults = new Dictionary<int, Vector2>();

        private void Start() {
            foreach (var propId in _DetailSizeFloats) {
                _detailSizeFloatDefaults.Add(propId, _material.GetFloat(propId));
            }
            foreach (var propId in _DetailSizeTexScales) {
                _detailSizeTexScaleDefaults.Add(propId, _material.GetTextureScale(propId));
            }
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
            _material.SetFloat(_DEPTH_ICE, Prefs.Landscape.DepthIce);

            var size = Prefs.Landscape.DetailsSize;
            foreach (var texScale in _detailSizeTexScaleDefaults) {
                _material.SetTextureScale(texScale.Key, texScale.Value / size);
            }
            
            foreach (var floatProp in _detailSizeFloatDefaults) {
                if (floatProp.Key != _MIX_DEPTH && floatProp.Key != _MIX_NOISE_STRENGTH) {
                    _material.SetFloat(floatProp.Key, floatProp.Value * size);
                }
            }

            var mixDepthDef = _detailSizeFloatDefaults[_MIX_DEPTH];
            var minD = Mathf.Min(Prefs.Landscape.DepthGround, Prefs.Landscape.DepthMountains, Prefs.Landscape.DepthIce);
            var mixDepth = Mathf.Min(mixDepthDef * size, minD);
            _material.SetFloat(_MIX_DEPTH, mixDepth);
            
            var noiseStrength = _detailSizeFloatDefaults[_MIX_NOISE_STRENGTH];
            _material.SetFloat(_MIX_NOISE_STRENGTH, noiseStrength / mixDepthDef * mixDepth);
        }
    }
}