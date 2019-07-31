using System;
using System.Collections;
using AsyncGPUReadbackPluginNs;
using DepthSensorSandbox;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;
using Utilities;

namespace DepthSensorCalibration {
    [RequireComponent(typeof(CalibrationController), typeof(ImageTracker))]
    public class AutomaticCalibration : MonoBehaviour {
        private const string _AUTO_CONTRAST = "AUTO_CONTRAST";
        private static readonly int _CONTRAST_WIDTH = Shader.PropertyToID("_ContrastWidth");
        private static readonly int _CONTRAST_CENTER = Shader.PropertyToID("_ContrastCenter");
        private static readonly int _ADJUST_WIDTH = Shader.PropertyToID("_AdjustWidth");
        
        [SerializeField] private Material _matGrayScale;
        [SerializeField] private Material _matDepth;
        [SerializeField] private bool _instantiateMaterials = true;
        [SerializeField] private Color _colorDetected = Color.blue;
        [SerializeField] private RawImage _imgVisualize;
        [SerializeField] private GameObject _pnlAutomatic;
        [SerializeField] private Button _btnCancelAutomatic;
        [SerializeField] private SandboxMesh _sandboxMesh;
        [SerializeField] private int _detectIterations = 20;
        [SerializeField] private int _adjustIterations = 8;
        [SerializeField] private float _adjustTargetMaxStep = 0.2f;
        [SerializeField] private float _adjustTargetMinDist = 0.05f;
        [SerializeField] private float _adjustTargetStepWait = 2f;
        private CalibrationController _ctrl;
        private ImageTracker _imageTracker;
        private RenderTexture _texTargetGrayscale;
        private CameraRenderToTexture _renderColor;
        private CameraRenderToTexture _renderDepth;
        private bool _maySetFrame;
        private bool _frameReady;
        private readonly Vector2[] _target2D = new Vector2[4];
        private readonly Vector3[] _target3D = new Vector3[4];
        private NativeArray<ushort> _depth;
        private bool _savedSandboxGPU;
        private float _savedZeroDepth;

        private void Start() {
            if (_instantiateMaterials) {
                _matDepth = new Material(_matDepth);
                _matGrayScale = new Material(_matGrayScale);
            }
            _imageTracker = GetComponent<ImageTracker>();
            _ctrl = GetComponent<CalibrationController>();
            _renderColor = _ctrl.SandboxCam.gameObject.AddComponent<CameraRenderToTexture>();
            _renderDepth = _ctrl.SandboxCam.gameObject.AddComponent<CameraRenderToTexture>();
            InitTarget();
            
            _btnCancelAutomatic.onClick.AddListener(OnBtnCancel);
        }

        private void InitTarget() {
            var target = _ctrl.Wall.GetTargetImage().mainTexture;
            _texTargetGrayscale = new RenderTexture(target.width, target.height, 0, RenderTextureFormat.R8);
            _matGrayScale.DisableKeyword(_AUTO_CONTRAST);
            _matGrayScale.SetFloat(_CONTRAST_CENTER, 0.5f);
            _matGrayScale.SetFloat(_CONTRAST_WIDTH, 1f);
            Graphics.Blit(target, _texTargetGrayscale, _matGrayScale);
            _imageTracker.SetTarget(_texTargetGrayscale);
        }

        private void OnDestroy() {
            if (_imageTracker != null)
                _imageTracker.OnFramePrepared -= OnFramePrepared;
            if (_renderColor != null)
                _renderColor.Disable();
            if (_renderDepth != null)
                _renderDepth.Disable();
            if (_depth.IsCreated)
                _depth.Dispose();
        }

        private void RestorePrefs() {
            Prefs.Calibration.ZeroDepth = _savedZeroDepth;
            _ctrl.SandboxCam.GetComponent<SandboxCamera>().ResetToCalibration();
        }

        private void OnBtnCancel() {
            RestorePrefs();
            StopCalibration();
        }

        public void StartCalibration() {
            _savedSandboxGPU = _sandboxMesh.UpdateMeshOnGpu;
            _savedZeroDepth = Prefs.Calibration.ZeroDepth;
            
            var b = _sandboxMesh.GetBounds();
            var d = MathHelper.IsoscelesTriangleHeight(b.size.y / 2f, _ctrl.SandboxCam.fieldOfView);
            
            Prefs.Calibration.ZeroDepth = _ctrl.SandboxCam.farClipPlane;
            _ctrl.SandboxCam.transform.localPosition = b.center + Vector3.back * d;
            _ctrl.SandboxCam.transform.localRotation = Quaternion.identity;
            _ctrl.SandboxCam.farClipPlane = d + b.size.z;
            
            _pnlAutomatic.SetActive(true);
            _imgVisualize.gameObject.SetActive(true);
            _maySetFrame = true;
            _frameReady = false;
            
            _imageTracker.OnFramePrepared += OnFramePrepared;
            _renderColor.Enable(_matGrayScale, RenderTextureFormat.R8, OnNewFrame);
            StartCoroutine(Calibrating());
        }

        private void StopCalibration() {
            StopAllCoroutines();
            _imageTracker.OnFramePrepared -= OnFramePrepared;
            _renderColor.Disable();
            _renderDepth.Disable();
            _imgVisualize.gameObject.SetActive(false);
            _pnlAutomatic.SetActive(false);
            _sandboxMesh.UpdateMeshOnGpu = _savedSandboxGPU;
            _ctrl.SwitchMode(CalibrationMode.MANUAL);
        }

        private void OnNewFrame(RenderTexture frame) {
            Texture2D visTex = null;
            if (_imgVisualize.texture != null)
                visTex = (Texture2D) _imgVisualize.texture; 
            if (TexturesHelper.ReCreateIfNeed(ref visTex, frame.width, frame.height, TextureFormat.RGB24)) {
                _imgVisualize.texture = visTex;
            }
            
            if (_maySetFrame) {
                _maySetFrame = false;
                _imageTracker.SetFrame(frame);
            }
        }

        private void OnFramePrepared() {
            _frameReady = true;
            _imageTracker.VisualizeDetection((Texture2D)_imgVisualize.texture, _colorDetected);
        }

        private IEnumerator Calibrating() {
            yield return AdjustTargetContrast();
            _sandboxMesh.UpdateMeshOnGpu = false;
            yield return PrepareDepthMap();
            
            Vector3 sumPos = Vector3.zero, sumForward = Vector3.zero, sumUp = Vector3.zero;
            float sumZeroDepth = 0f;
            int count = 0;
            
            for (int i = 0; i < _detectIterations; ++i) {
                yield return WaitFrame(() => {
                    if (CalcCameraPos(out var pos, out var forward, out var up, out var zeroDepth)) {
                        sumPos += pos; sumForward += forward; sumUp += up;
                        sumZeroDepth += zeroDepth;
                        ++count;
                    }
                });
            }

            if (count > 0) {
                var t = _ctrl.SandboxCam.transform;
                var pos = t.TransformPointTo(t.parent, sumPos / count);
                var forward = t.TransformVectorTo(t.parent, sumForward / count);
                var up = t.TransformVectorTo(t.parent, sumUp / count);
                var zeroDepth = sumZeroDepth / count;
                Prefs.Calibration.Position = pos;
                Prefs.Calibration.Rotation = Quaternion.LookRotation(forward, up);
                Prefs.Calibration.ZeroDepth = zeroDepth;
            } else {
                RestorePrefs();
            }
            StopCalibration();
        }

        private bool CalcCameraPos(out Vector3 pos, out Vector3 forward, out Vector3 up, out float zeroDepth) {
            pos = forward = up = Vector3.zero;
            zeroDepth = 0f;
            if (!_imageTracker.GetDetectTargetCorners(_target2D)) return false;
            
            var center2D = (_target2D[0] + _target2D[2]) / 2f;
            if (!GetDepth(center2D, out var center3D)) return false;
            for (int i = 0; i < _target2D.Length; ++i) {
                var p2 = center2D + (_target2D[i] - center2D) / 2f;
                if (!GetDepth(p2, out var p3)) return false;
                _target3D[i] = center3D + (p3 - center3D) * 2f;
            }

            forward = Vector3.Cross(_target3D[3] - _target3D[0], _target3D[1] - _target3D[0]).normalized;
            up = (_target3D[1] + _target3D[2]) / 2f - (_target3D[0] + _target3D[3]) / 2f;
            var d = MathHelper.IsoscelesTriangleHeight(up.magnitude, _ctrl.SandboxCam.fieldOfView);
            pos = center3D - forward * d;
            zeroDepth = Vector3.Distance(pos, center3D);
            return true;
        }

        private bool GetDepth(Vector2 p, out Vector3 depth) {
            depth = Vector3.zero;
            int i = (int)p.y * _ctrl.SandboxCam.pixelWidth + (int)p.x;
            if (i >= _depth.Length)
                return false;
            var d = (float)_depth[i] / 1000f;
            
            var dir = _ctrl.SandboxCam.ScreenPointToRay(p).direction;
            dir = _ctrl.SandboxCam.transform.InverseTransformVector(dir);
            var proj = Vector3.ProjectOnPlane(dir, Vector3.forward);
            var forward = dir - proj;
            var scale = d / forward.magnitude;
            depth = dir * scale;
            return true;
        }

        private IEnumerator PrepareDepthMap() {
            var depthValid = false;
            _renderDepth.Enable(_matDepth, RenderTextureFormat.R16, t => {
                TexturesHelper.ReCreateIfNeed(ref _depth, t.GetPixelsCount());
                AsyncGPUReadback.RequestIntoNativeArray(ref _depth, t, 0, r => {
                    if (!r.hasError) depthValid = true;
                });
            });
            yield return new WaitUntil(() => { return depthValid; });
        }

        private IEnumerator AdjustTargetContrast() {
            var targetImage = _ctrl.Wall.GetTargetImage();
            
            _matGrayScale.EnableKeyword(_AUTO_CONTRAST);
            _matGrayScale.SetFloat(_CONTRAST_CENTER, 0.5f);
            _matGrayScale.SetFloat(_CONTRAST_WIDTH, 2f);
            yield return SearchMaximum(0.2f, 1f,
                val => { targetImage.color = new Color(val, val, val); },
                _imageTracker.FrameFeaturesRank, _adjustTargetStepWait
            );
            
            yield return SearchMaximum(0.01f, 0.05f,
                val => { _matGrayScale.SetFloat(_ADJUST_WIDTH, val); },
                _imageTracker.FrameFeaturesRank
            );
        }

        private IEnumerator SearchMaximum(float a, float b, Action<float> set, Func<float> get, float stepWait = -1f) {
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
                if (stepWait > 0f)
                    yield return new WaitForSeconds(stepWait);
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