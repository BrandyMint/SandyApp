using System;
using DepthSensor.Stream;
using UnityEngine;
using UnityEngine.UI;

namespace DepthSensorSandbox {
    [RequireComponent(typeof(RawImage), typeof(AspectRatioFitter))]
    public class ColorMonitor : MonoBehaviour {
        private AspectRatioFitter _aspect;
        private RawImage _img;

        private void Awake() {
            _img = GetComponent<RawImage>();
            _aspect = GetComponent<AspectRatioFitter>();
        }

        private void OnEnable() {
            DepthSensorSandboxProcessor.OnColor += OnColor;
        }

        private void OnDisable() {
            DepthSensorSandboxProcessor.OnColor -= OnColor;
        }

        private void OnDestroy() {
            DepthSensorSandboxProcessor.OnColor -= OnColor;
        }

        private void OnColor(ColorStream s) {
            _img.texture = s.texture;
            var aspect = (float) s.width / s.height;
            if (Math.Abs(_aspect.aspectRatio - aspect) > 0.01f)
                _aspect.aspectRatio = aspect;
        }
    }
}