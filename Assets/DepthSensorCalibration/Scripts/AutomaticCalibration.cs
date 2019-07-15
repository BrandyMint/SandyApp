using UnityEngine;
using UnityEngine.UI;

namespace DepthSensorCalibration {
    [RequireComponent(typeof(CalibrationController), typeof(ImageTracker))]
    public class AutomaticCalibration : MonoBehaviour {
        [SerializeField] private Material _grayScale;
        [SerializeField] private GameObject _pnlAutomatic;
        [SerializeField] private Button _btnCancelAutomatic;
        
        private CalibrationController _ctrl;
        private ImageTracker _imageTracker;
        private RenderTexture _texTargetGrayscale;
        private CameraRenderToTexture _renderToTexture;

        private void Start() {
            _imageTracker = GetComponent<ImageTracker>();
            _ctrl = GetComponent<CalibrationController>();
            _renderToTexture = _ctrl.SandboxCam.GetComponent<CameraRenderToTexture>();
            InitTarget();
            
            _btnCancelAutomatic.onClick.AddListener(OnBtnCancel);
        }

        private void InitTarget() {
            var target = _ctrl.Wall.GetTargetTexture();
            _texTargetGrayscale = new RenderTexture(target.width, target.height, 0, RenderTextureFormat.R8);
            Graphics.Blit(target, _texTargetGrayscale, _grayScale);
            _imageTracker.SetTarget(_texTargetGrayscale);
        }

        private void OnDestroy() {
            StopCalibration();
        }

        private void OnBtnCancel() {
            StopCalibration();
        }

        public void StartCalibration() {
            _pnlAutomatic.SetActive(true);
            _renderToTexture.Enable(_grayScale, RenderTextureFormat.R8, t => {
                _imageTracker.SetFrame(t);
            });
        }

        private void StopCalibration() {
            if (_pnlAutomatic != null)
                _pnlAutomatic.SetActive(false);
            if (_renderToTexture != null)
                _renderToTexture.Disable();
            if (_ctrl != null)
                _ctrl.SwitchMode(CalibrationMode.MANUAL);
        }
    }
}