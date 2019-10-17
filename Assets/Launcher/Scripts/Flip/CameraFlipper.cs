using UnityEngine;

namespace Launcher.Flip {
    [RequireComponent(typeof(Camera))]
    public class CameraFlipper : MonoBehaviour {
        public bool horizontal;
        public bool vertical;
        
        private Camera _cam;

        private void Start() {
            _cam = GetComponent<Camera>();
            OnAppParamChanged();
            Prefs.App.OnChanged += OnAppParamChanged;
        }
        
        private void OnDestroy() {
            if (Prefs.App != null)
                Prefs.App.OnChanged -= OnAppParamChanged;
        }

        private void OnAppParamChanged() {
            horizontal = Prefs.App.FlipHorizontalSandbox;
            vertical = Prefs.App.FlipVerticalSandbox;
        }

        private void OnPreCull() {
            _cam.ResetWorldToCameraMatrix();
            _cam.ResetProjectionMatrix();
            if (horizontal || vertical) {
                _cam.projectionMatrix *= Matrix4x4.Scale(new Vector3(
                    horizontal ? -1f : 1f,
                    vertical ? -1f : 1f,
                    1f
                ));
            }
        }
        
        private void OnPreRender() {
            GL.invertCulling = (horizontal || vertical) && !(horizontal && vertical);
        }
        
        private void OnPostRender() {
            GL.invertCulling = false;
        }
    }
}