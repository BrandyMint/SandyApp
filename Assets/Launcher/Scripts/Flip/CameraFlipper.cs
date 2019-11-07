using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Launcher.Flip {
    [RequireComponent(typeof(Camera))]
    public class CameraFlipper : MonoBehaviour {
        public bool horizontal;
        public bool vertical;
        public Vector2 obliqueness;
        
        private Camera _cam;

        private void Start() {
            _cam = GetComponent<Camera>();
            OnAppParamChanged();
            Prefs.App.OnChanged += OnAppParamChanged;
            Prefs.Calibration.OnChanged += OnAppParamChanged;
        }
        
        private void OnDestroy() {
            if (Prefs.App != null)
                Prefs.App.OnChanged -= OnAppParamChanged;
            if (Prefs.Calibration != null)
                Prefs.Calibration.OnChanged -= OnAppParamChanged;
        }

        private void OnAppParamChanged() {
            horizontal = Prefs.App.FlipHorizontalSandbox;
            vertical = Prefs.App.FlipVerticalSandbox;
            obliqueness = new Vector2(0f, Prefs.Calibration.Oblique);
            SetObliqueness(obliqueness);
        }

        private void OnPreCull() {
            _cam.ResetWorldToCameraMatrix();
            _cam.ResetProjectionMatrix();
            SetObliqueness(obliqueness);
            if (horizontal || vertical) {
                _cam.projectionMatrix *= Matrix4x4.Scale(new Vector3(
                    horizontal ? -1f : 1f,
                    vertical ? -1f : 1f,
                    1f
                ));
            }
        }
        
        private void SetObliqueness(Vector2 o) {
            var mat  = _cam.projectionMatrix;
            mat[0, 2] = o.x;
            mat[1, 2] = o.y;
            _cam.projectionMatrix = mat;
        }
        
        private void OnPreRender() {
            GL.invertCulling = GetInvertCulling();
        }
        
        private void OnPostRender() {
            GL.invertCulling = false;
        }

        public bool GetInvertCulling() {
            return GetInvertCullingForFlip(horizontal, vertical);
        }
        
        public bool GetInvertCulling(CameraEvent ev) {
            return GetInvertCullingFor(ev, horizontal, vertical);
        }

        public static bool GetInvertCullingForFlip(bool horizontal, bool vertical) {
            return (horizontal || vertical) && !(horizontal && vertical);
        }

        public static bool GetInvertCulling(Camera cam) {
            if (cam.TryGetComponent(out CameraFlipper flipper)) {
                return flipper.GetInvertCulling();
            }
            return false;
        }
        
        public static bool GetInvertCulling(Camera cam, CameraEvent ev) {
            if (cam.TryGetComponent(out CameraFlipper flipper)) {
                return flipper.GetInvertCulling(ev);
            }
            return false;
        }

        public static bool GetInvertCullingFor(CameraEvent ev, bool horizontal, bool vertical) {
            var invert = GetInvertCullingForFlip(horizontal, vertical);
            switch (ev) {
                //OnPreRender
                case CameraEvent.BeforeDepthTexture:
                case CameraEvent.AfterDepthTexture:
                case CameraEvent.BeforeDepthNormalsTexture:
                case CameraEvent.AfterDepthNormalsTexture:
                case CameraEvent.BeforeGBuffer:
                case CameraEvent.AfterGBuffer:
                case CameraEvent.BeforeLighting:
                case CameraEvent.AfterLighting:
                case CameraEvent.BeforeFinalPass:
                case CameraEvent.AfterFinalPass:
                case CameraEvent.BeforeReflections:
                case CameraEvent.AfterReflections:
                    return invert;
                //OnPostRender
                case CameraEvent.BeforeForwardOpaque:
                case CameraEvent.AfterForwardOpaque:
                case CameraEvent.BeforeImageEffectsOpaque:
                case CameraEvent.AfterImageEffectsOpaque:
                case CameraEvent.BeforeSkybox:
                case CameraEvent.AfterSkybox:
                case CameraEvent.BeforeForwardAlpha:
                case CameraEvent.AfterForwardAlpha:
                case CameraEvent.BeforeImageEffects:
                case CameraEvent.AfterImageEffects:
                case CameraEvent.AfterEverything:
                case CameraEvent.BeforeHaloAndLensFlares:
                case CameraEvent.AfterHaloAndLensFlares:
                    return !invert;
                default:
                    throw new ArgumentOutOfRangeException(nameof(ev), ev, null);
            }
        }
    }
}