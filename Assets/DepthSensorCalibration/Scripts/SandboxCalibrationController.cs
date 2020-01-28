using System;
using DepthSensorSandbox;
using DepthSensorSandbox.Visualisation;
using Launcher.KeyMapping;
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
            public ShortCutValue OffsetDepthHelp { get; set; }
        }
        private readonly SandboxFields _sandboxFields = new SandboxFields();
        private event Action _updateUIValues;
        
        private CameraRenderToTexture _renderDepth;
        private bool _depthValid;
        private readonly DelayedDisposeNativeArray<ushort> _depth = new DelayedDisposeNativeArray<ushort>();
        
        private readonly SandboxParams _tempSettings = new SandboxParams();
        private float _minClip = float.MinValue;
        private float _maxClip = float.MaxValue;
        private bool _onDestroyed;

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
            _onDestroyed = true;
            if (_renderDepth != null) {
                _renderDepth.Disable();
            }
            if (_sandbox != null) {
                _sandbox.SetEnable(false);
            }
            Prefs.Calibration.OnChanged -= OnCalibrationChanged;
            Prefs.Sandbox.OnChanged -= OnSandboxSettingChanged;
            _depth.Dispose();

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
            KeyMapper.AddListener(KeyEvent.SET_DEPTH, SetDepth);
        }

        private void UnSubscribeKeys() {
            KeyMapper.RemoveListener(KeyEvent.RESET, OnBtnReset);
            KeyMapper.RemoveListener(KeyEvent.SHOW_UI, SwithcUI);
            KeyMapper.RemoveListener(KeyEvent.SET_DEPTH, SetDepth);
        }
        
        private void SwithcUI() {
            foreach (var ui in _uiHide) {
                ui.gameObject.SetActive(!ui.gameObject.activeSelf);
            }
        }
#endregion

#region Calculate Depth
        private void OnNewDepthFrame(RenderTexture t) {
            _renderDepth.RequestData(_depth, ProcessDepthFrame);
        }

        private void ProcessDepthFrame() {
            var min = float.MaxValue;
            var max = float.MinValue;
            var mid = 0f;
            var count = 0;
            foreach (var ushortDepth in _depth.o) {
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
            _depthValid = true;
        }

        private void SetDepth() {
            if (_depthValid) {
                Prefs.Sandbox.OffsetMaxDepth = _tempSettings.OffsetMaxDepth;
                Prefs.Sandbox.OffsetMinDepth = _tempSettings.OffsetMinDepth;
                Prefs.Sandbox.ZeroDepth = _tempSettings.ZeroDepth;
            }
        }
#endregion

#region UI
        private void InitUI() {
            BtnKeyBind.ShortCut(_btnReset, KeyEvent.RESET);
            
            UnityHelper.SetPropsByGameObjects(_sandboxFields, _pnlSandboxSettings);
            InitUIValue(_sandboxFields.OffsetMaxDepth, () => Prefs.Sandbox.OffsetMaxDepth);
            InitUIValue(_sandboxFields.ZeroDepth, () => Prefs.Sandbox.ZeroDepth + Prefs.Calibration.Position.z);
            InitUIValue(_sandboxFields.OffsetMinDepth, () => Prefs.Sandbox.OffsetMinDepth);
            InitShortCut(_sandboxFields.OffsetDepthHelp, KeyEvent.SET_DEPTH);
            OnSandboxSettingChanged();
        }

        private void InitUIValue(ShortCutValue scv, Func<float> get) {
            _updateUIValues += () => SetTextDistValue(scv.txtValue, get());
        }

        private static void InitShortCut(ShortCutValue field, KeyEvent ev) {
            var key = KeyMapper.FindFirstKey(ev);
            if (key != null) {
                field.txtShortCut.text = key.ShortCut;
            }
        }

        private void OnSandboxSettingChanged() {
            _updateUIValues?.Invoke();
        }

        private void OnCalibrationChanged() {
            var sensorDist = Prefs.Projector.DistanceToSensor > 0f ? Prefs.Projector.DistanceToSensor : 0f; 
            SetTextDistValue(_txtZValue, sensorDist - Prefs.Calibration.Position.z);
        }

        private void SetTextDistValue(Text t, float val) {
            t.text = (val * 1000f).ToString("F0");
        }
#endregion
    }
}