#if USE_MAT_ASYNC_SET
    using AsyncGPUReadbackPluginNs;
#endif
using System;
using DepthSensorSandbox;
using DepthSensorSandbox.Visualisation;
using Launcher.KeyMapping;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;
using Utilities;

namespace DepthSensorCalibration {
    public class SandboxCalibrationController : MonoBehaviour {
        [Header("UI")]
        [SerializeField] private Text _txtZValue;
        [SerializeField] private GameObject[] _uiHide;
        [SerializeField] private Transform _pnlSandboxSettings;
        [SerializeField] private Button _btnSave;
        [SerializeField] private Button _btnReset;
        
        [Header("Sandbox")]
        [SerializeField] private Camera _sandboxCam;
        [SerializeField] private SandboxVisualizerBase _sandbox;
        [SerializeField] private Material _matDepth;

        private class ShortCutValue {
            public Text txtShortCut { get; set; }
            public Text txtValue { get; set; }
        }

        private class SandboxFields {
            public ShortCutValue OffsetMinDepth { get; set; }
            public ShortCutValue ZeroDepth { get; set; }
            public ShortCutValue OffsetMaxDepth { get; set; }
        }
        private readonly SandboxFields _sandboxFields = new SandboxFields();
        private event Action _updateUIValues;
        
        private CameraRenderToTexture _renderDepth;
        private bool _depthValid;
        private NativeArray<ushort> _depth;
        private Texture2D _depthTex;
        
        private readonly SandboxParams _tempSettings = new SandboxParams();
        private float _minClip = float.MinValue;
        private float _maxClip = float.MaxValue;

        private void Start() {
            InitUI();
            SubscribeKeys();
            //SwithcUI();
            _sandboxCam.depthTextureMode = DepthTextureMode.Depth;
            _renderDepth = _sandboxCam.gameObject.AddComponent<CameraRenderToTexture>();
            _renderDepth.MaxResolution = 64;
            _renderDepth.InvokesOnlyOnProcessedFrame = true;
            _renderDepth.Enable(_matDepth, RenderTextureFormat.R16, OnNewDepthFrame);

            _sandbox.OverrideParamsSource(_tempSettings);
            _sandbox.SetEnable(true);
            
            Prefs.Sandbox.OnChanged += OnSandboxSettingChanged;
            Prefs.Calibration.OnChanged += OnCalibrationChanged;
            OnCalibrationChanged();
        }

        private void OnDestroy() {
            if (_renderDepth != null) {
                _renderDepth.Disable();
            }
            if (_sandbox != null) {
                _sandbox.SetEnable(false);
            }
            Prefs.Calibration.OnChanged -= OnCalibrationChanged;
            Prefs.Sandbox.OnChanged -= OnSandboxSettingChanged;
            if (_depthTex != null) {
                Destroy(_depthTex);
            } else if (_depth.IsCreated) {
                _depth.Dispose();
            }

            UnSubscribeKeys();
            Save();
        }

#region Buttons
        private void Save() {
            if (IsSaveAllowed()) {
                Prefs.NotifySaved(Prefs.Sandbox.Save());
                //Scenes.GoBack();
            }
        }
        
        private void FixedUpdate() {
            _btnSave.interactable = IsSaveAllowed();
            if (_sandboxCam != null) {
                _minClip = _sandboxCam.nearClipPlane + 0.01f;
                _maxClip = _sandboxCam.farClipPlane - 0.01f;
            }
        }

        private bool IsSaveAllowed() {
            return Prefs.Sandbox.HasChanges || !Prefs.Sandbox.HasFile;
        }

        private void OnBtnReset() {
            Prefs.Sandbox.Reset();
        }

        private void SubscribeKeys() {
            KeyMapper.AddListener(KeyEvent.RESET, OnBtnReset);
            KeyMapper.AddListener(KeyEvent.SHOW_UI, SwithcUI);
            KeyMapper.AddListener(KeyEvent.SET_DEPTH_MAX, SetDepthMax);
            KeyMapper.AddListener(KeyEvent.SET_DEPTH_ZERO, SetDepthZero);
            KeyMapper.AddListener(KeyEvent.SET_DEPTH_MIN, SetDepthMin);
        }

        private void UnSubscribeKeys() {
            KeyMapper.RemoveListener(KeyEvent.RESET, OnBtnReset);
            KeyMapper.RemoveListener(KeyEvent.SHOW_UI, SwithcUI);
            KeyMapper.RemoveListener(KeyEvent.SET_DEPTH_MAX, SetDepthMax);
            KeyMapper.RemoveListener(KeyEvent.SET_DEPTH_ZERO, SetDepthZero);
            KeyMapper.RemoveListener(KeyEvent.SET_DEPTH_MIN, SetDepthMin);
        }
        
        private void SwithcUI() {
            foreach (var ui in _uiHide) {
                ui.gameObject.SetActive(!ui.gameObject.activeSelf);
            }
        }
#endregion

#region Calculate Depth
        private void OnNewDepthFrame(RenderTexture t) {
#if USE_MAT_ASYNC_SET
            TexturesHelper.ReCreateIfNeed(ref _depth, t.GetPixelsCount());
            AsyncGPUReadback.RequestIntoNativeArray(ref _depth, _renderDepth.GetTempCopy(), 0, r => {
                if (!r.hasError) {
                    ProcessDepthFrame(_depth);
                    _depthValid = true;
                }
            });
#else
            TexturesHelper.ReCreateIfNeedCompatible(ref _depthTex, t);
            TexturesHelper.Copy(t, _depthTex);
            _depth = _depthTex.GetRawTextureData<ushort>();
            ProcessDepthFrame(_depth);
            _depthValid = true;
#endif
        }

        private void ProcessDepthFrame(NativeArray<ushort> depth) {
            var min = float.MaxValue;
            var max = float.MinValue;
            var mid = 0f;
            var count = 0;
            foreach (var ushortDepth in depth) {
                var d = (float)ushortDepth / 1000f;
                if (d < _minClip) continue;
                
                Mathf.Clamp(d, _minClip, _maxClip);
                mid += d;
                if (d < min)
                    min = d;
                if (d > max)
                    max = d;
                ++count;
            }
            mid /= count;
            _tempSettings.OffsetMaxDepth = mid - min;
            _tempSettings.ZeroDepth = mid;
            _tempSettings.OffsetMinDepth = max - mid;
        }

        private void SetDepthMax() {
            if (_depthValid) 
                Prefs.Sandbox.OffsetMaxDepth = _tempSettings.OffsetMaxDepth;
        }

        private void SetDepthMin() {
            if (_depthValid) 
                Prefs.Sandbox.OffsetMinDepth = _tempSettings.OffsetMinDepth;
        }

        private void SetDepthZero() {
            if (_depthValid) 
                Prefs.Sandbox.ZeroDepth = _tempSettings.ZeroDepth;
        }
#endregion

#region UI
        private void InitUI() {
            BtnKeyBind.ShortCut(_btnReset, KeyEvent.RESET);
            
            UnityHelper.SetPropsByGameObjects(_sandboxFields, _pnlSandboxSettings);
            InitShortCutValue(_sandboxFields.OffsetMaxDepth, KeyEvent.SET_DEPTH_MAX, () => Prefs.Sandbox.OffsetMaxDepth);
            InitShortCutValue(_sandboxFields.ZeroDepth, KeyEvent.SET_DEPTH_ZERO, () => Prefs.Sandbox.ZeroDepth + Prefs.Calibration.Position.z);
            InitShortCutValue(_sandboxFields.OffsetMinDepth, KeyEvent.SET_DEPTH_MIN, () => Prefs.Sandbox.OffsetMinDepth);
            OnSandboxSettingChanged();
        }

        private void InitShortCutValue(ShortCutValue scv, KeyEvent ev, Func<float> get) {
            var key = KeyMapper.FindFirstKey(ev);
            if (key != null) {
                scv.txtShortCut.text = key.ShortCut;
            }

            _updateUIValues += () => SetTextDistValue(scv.txtValue, get());
        }

        private void OnSandboxSettingChanged() {
            _updateUIValues?.Invoke();
        }

        private void OnCalibrationChanged() {
            SetTextDistValue(_txtZValue, Prefs.Calibration.Position.z);
        }

        private void SetTextDistValue(Text t, float val) {
            t.text = (val * 1000f).ToString("F0");
        }
#endregion
    }
}