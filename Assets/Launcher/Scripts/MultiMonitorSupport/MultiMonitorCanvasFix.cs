using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using Utilities;

namespace Launcher.MultiMonitorSupport {
    [RequireComponent(typeof(Canvas), typeof(CanvasScaler))]
    public class MultiMonitorCanvasFix : MonoBehaviour {
        private Canvas _canvas;
        private CanvasScaler _scaler;

        private Vector2 _referenceResolution;
        private int _prevDispTarget = -1;
        private Camera _prevCamera;

        private void Awake() {
            Assert.IsTrue(MultiMonitor.UseMultiMonitorFix(), 
                "MultiMonitorCanvasFix must be used when MultiMonitor.UseMultiMonitorFix() == true");
            Assert.IsFalse(_canvas.renderMode == RenderMode.ScreenSpaceOverlay,
                "MultiMonitorCanvasFix dont support RenderMode.ScreenSpaceOverlay!");
            _canvas = GetComponent<Canvas>();
            _scaler = GetComponent<CanvasScaler>();
            _referenceResolution = _scaler.referenceResolution;
            
            FixCanvasIfNeed();
        }

        private void LateUpdate() {
            FixCanvasIfNeed();
        }

        private void FixCanvasIfNeed() {
            if (_canvas.renderMode == RenderMode.ScreenSpaceCamera) {
                var dispTarget = MultiMonitor.GetTargetDisplay(_canvas.worldCamera);
                if (_prevDispTarget != dispTarget || _prevCamera != _canvas.worldCamera) {
                    if (MultiMonitor.GetMultiMonitorRect(out var multiRect)) {
                        _scaler.referenceResolution = MathHelper.Mul(_referenceResolution,
                            MathHelper.Div(multiRect.size, MultiMonitor.GetDisplayRect(dispTarget).size));
                        
                        _prevDispTarget = dispTarget;
                        _prevCamera = _canvas.worldCamera;
                    }
                }
            }
        } 
    }
}