using System;
using System.Collections;
using DepthSensorSandbox;
using UnityEngine;
using UnityEngine.UI;

namespace DepthSensorCalibration {
    [RequireComponent(typeof(CalibrationController), typeof(ImageTracker))]
    public class AutomaticCalibration : MonoBehaviour {
        private const string _AUTO_CONTRAST = "AUTO_CONTRAST";
        private static readonly int _CONTRAST_WIDTH = Shader.PropertyToID("_ContrastWidth");
        private static readonly int _CONTRAST_CENTER = Shader.PropertyToID("_ContrastCenter");
        private static readonly int _ADJUST_WIDTH = Shader.PropertyToID("_AdjustWidth");
        
        [SerializeField] private Material _grayScale;
        [SerializeField] private GameObject _pnlAutomatic;
        [SerializeField] private Button _btnCancelAutomatic;
        [SerializeField] private int _adjustIterations = 8;
        [SerializeField] private float _adjustTargetMaxStep = 0.2f;
        [SerializeField] private float _adjustTargetMinDist = 0.05f;
        [SerializeField] private float _adjustTargetStepWait = 2f;

        private CalibrationController _ctrl;
        private ImageTracker _imageTracker;
        private RenderTexture _texTargetGrayscale;
        private CameraRenderToTexture _renderToTexture;
        private bool _maySetFrame;
        private bool _frameReady;

        private void Start() {
            _imageTracker = GetComponent<ImageTracker>();
            _ctrl = GetComponent<CalibrationController>();
            _renderToTexture = _ctrl.SandboxCam.GetComponent<CameraRenderToTexture>();
            InitTarget();
            
            _btnCancelAutomatic.onClick.AddListener(OnBtnCancel);
        }

        private void InitTarget() {
            var target = _ctrl.Wall.GetTargetImage().mainTexture;
            _texTargetGrayscale = new RenderTexture(target.width, target.height, 0, RenderTextureFormat.R8);
            _grayScale.DisableKeyword(_AUTO_CONTRAST);
            _grayScale.SetFloat(_CONTRAST_CENTER, 0.5f);
            _grayScale.SetFloat(_CONTRAST_WIDTH, 1f);
            Graphics.Blit(target, _texTargetGrayscale, _grayScale);
            _imageTracker.SetTarget(_texTargetGrayscale);
        }

        private void OnDestroy() {
            if (_imageTracker != null)
                _imageTracker.OnFramePrepared -= OnFramePrepared;
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
            _maySetFrame = true;
            _frameReady = false;
            
            _imageTracker.OnFramePrepared += OnFramePrepared;
            _renderToTexture.Enable(_grayScale, RenderTextureFormat.R8, OnNewFrame);
            StartCoroutine(Calibrating());
        }

        private void StopCalibration() {
            StopAllCoroutines();
            _imageTracker.OnFramePrepared -= OnFramePrepared;
            _renderToTexture.Disable();
            _pnlAutomatic.SetActive(false);
            _ctrl.SwitchMode(CalibrationMode.MANUAL);
        }

        private void OnNewFrame(RenderTexture frame) {
            if (_maySetFrame) {
                _maySetFrame = false;
                _imageTracker.SetFrame(frame);
            }
        }

        private void OnFramePrepared() {
            _frameReady = true;
        }

        private IEnumerator Calibrating() {
            yield return AdjustTargetContrast();
            while (true) {
                yield return WaitFrame(() => {
                    _imageTracker.Detect();
                });
            }
        }

        private IEnumerator AdjustTargetContrast() {
            var targetImage = _ctrl.Wall.GetTargetImage();
            float GetRank() {
                _imageTracker.Detect();
                return _imageTracker.DetectRank();
            }
            
            _grayScale.EnableKeyword(_AUTO_CONTRAST);
            _grayScale.SetFloat(_CONTRAST_CENTER, 0.5f);
            _grayScale.SetFloat(_CONTRAST_WIDTH, 2f);
            yield return SearchMaximum(0.2f, 1f,
                val => { targetImage.color = new Color(val, val, val); },
                GetRank
            );
            
            yield return SearchMaximum(0.01f, 0.05f,
                val => { _grayScale.SetFloat(_ADJUST_WIDTH, val); },
                GetRank
            );
        }

        private IEnumerator SearchMaximum(float a, float b, Action<float> set, Func<float> get) {
            const float _E = 0.00001f;
            const float _E_STEP = 1f / 8f;
            const float _STEP = 1f / 4f;
            var maxStep = Mathf.Abs(a - b) * _adjustTargetMaxStep;
            var minDist = Mathf.Abs(a - b) * _adjustTargetMinDist;
            float valA = 0f, valB = 0f;
            void GetA(float v) {valA = v;}
            void GetB(float v) {valB = v;}
            float Step(float from, float to, float step) {
                var d = Mathf.Abs(from - to);
                if (step / d > maxStep)
                    step = maxStep / d; 
                return Mathf.Lerp(from, to, step);
            }
            IEnumerator Check(float setVal, Action<float> getAct) {
                set(setVal);
                yield return new WaitForSeconds(_adjustTargetStepWait);
                var sum = 0f;
                for (int i = 0; i < _adjustIterations; ++i) {
                    yield return WaitFrame(() => sum += get());
                }
                getAct(sum / _adjustIterations);
            }

            yield return Check(a, GetA);
            while (Mathf.Abs(a - b ) > minDist) {
                yield return Check(b, GetB);
                if (Math.Abs(valA) < _E && Math.Abs(valB) < _E) {
                    var prevB = b;
                    b = Step(b, a, _E_STEP);
                    a = Step(a, prevB, _E_STEP);
                    yield return Check(a, GetA);
                } else {
                    if (valB > valA) {
                        var newB = Step(a, b, _STEP);
                        a = b;
                        valA = valB;
                        b = newB;
                    } else {
                        b = Step(b, a, _STEP);
                    }
                }
            }
            set((a + b) / 2f);
        }

        private IEnumerator WaitFrame(Action act) {
            yield return new WaitUntil(() => _frameReady);
            _frameReady = false;
            act?.Invoke();
            _maySetFrame = true;
        }
    }
}