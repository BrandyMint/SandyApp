using UnityEngine;

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
            GL.invertCulling = (horizontal || vertical) && !(horizontal && vertical);
        }
        
        private void OnPostRender() {
            GL.invertCulling = false;
        }
    }
}