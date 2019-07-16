using DepthSensorSandbox;
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
            if (_renderToTexture != null)
                _renderToTexture.Disable();
        }

        private void OnBtnCancel() {
            _ctrl.SandboxCam.GetComponent<SandboxCamera>().ResetToCalibration();
            StopCalibration();
        }

        public void StartCalibration() {
            _ctrl.SandboxCam.transform.localPosition = Vector3.back;
            _ctrl.SandboxCam.transform.localRotation = Quaternion.identity;
            _pnlAutomatic.SetActive(true);
            _renderToTexture.Enable(_grayScale, RenderTextureFormat.R8, t => {
                _imageTracker.SetFrame(t);
            });
        }

        private void StopCalibration() {
            _pnlAutomatic.SetActive(false);
            _renderToTexture.Disable();
            _ctrl.SwitchMode(CalibrationMode.MANUAL);
        }
    }
}