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
        private const float COUNT_INC_DEC_STEPS = 0.01f;
        
        [Header("UI")]
        [SerializeField] private Text _txtZValue;
        [SerializeField] private GameObject[] _uiHide;
        [SerializeField] private Transform _pnlSandboxSettings;
        [SerializeField] private Button _btnSave;
        [SerializeField] private Button _btnCancel;
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
        
        private readonly SandboxParams _toSave = new SandboxParams();
        private readonly ProjectorParams _projector = new ProjectorParams();


        private void Start() {
            InitUI();
            SubscribeKeys();
            //SwithcUI();
            _renderDepth = _sandboxCam.gameObject.AddComponent<CameraRenderToTexture>();
            _renderDepth.MaxResolution = 64;
            _renderDepth.Enable(_matDepth, RenderTextureFormat.R16, OnNewDepthFrame);
            _sandbox.SetEnable(true);
            
            _projector.OnChanged += OnProjectorChanged;
            Prefs.Calibration.OnChanged += OnCalibrationChanged;
            OnProjectorChanged();
            OnCalibrationChanged();
        }

        private void OnDestroy() {
            if (_renderDepth != null) {
                _renderDepth.Disable();
            }
            if (_sandbox != null) {
                _sandbox.SetEnable(false);
            }
            _projector.OnChanged -= OnProjectorChanged;
            Prefs.Calibration.OnChanged -= OnCalibrationChanged;
            UnSubscribeKeys();
            Save();
        }

#region Buttons
        private void Save() {
            if (IsSaveAllowed()) {
                Prefs.NotifySaved(_toSave.Save() && Prefs.Calibration.Save());
                //Scenes.GoBack();
            }
        }
        
        private void FixedUpdate() {
            _btnSave.interactable = IsSaveAllowed();
        }

        private bool IsSaveAllowed() {
            return _toSave.HasChanges || !_toSave.HasFile || Prefs.Calibration.HasChanges || !Prefs.Calibration.HasFile;
        }

        private void OnBtnReset() {
            _toSave.Reset();
            Prefs.Calibration.Reset();
        }
        
        private void SubscribeKeys() {
            //KeyMapper.AddListener(KeyEvent.SAVE, Save);
            KeyMapper.AddListener(KeyEvent.RESET, OnBtnReset);
            KeyMapper.AddListener(KeyEvent.SHOW_UI, SwithcUI);
            KeyMapper.AddListener(KeyEvent.SET_DEPTH_MAX, SetDepthMax);
            KeyMapper.AddListener(KeyEvent.SET_DEPTH_ZERO, SetDepthZero);
            KeyMapper.AddListener(KeyEvent.SET_DEPTH_MIN, SetDepthMin);
            KeyMapper.AddListener(KeyEvent.LEFT, MoveLeft);
            KeyMapper.AddListener(KeyEvent.RIGHT, MoveRight);
            KeyMapper.AddListener(KeyEvent.DOWN, MoveDown);
            KeyMapper.AddListener(KeyEvent.UP, MoveUp);
            KeyMapper.AddListener(KeyEvent.ZOOM_IN, MoveForward);
            KeyMapper.AddListener(KeyEvent.ZOOM_OUT, MoveBackward);
        }

        private void UnSubscribeKeys() {
            //KeyMapper.RemoveListener(KeyEvent.SAVE, Save);
            KeyMapper.RemoveListener(KeyEvent.RESET, OnBtnReset);
            KeyMapper.RemoveListener(KeyEvent.SHOW_UI, SwithcUI);
            KeyMapper.RemoveListener(KeyEvent.SET_DEPTH_MAX, SetDepthMax);
            KeyMapper.RemoveListener(KeyEvent.SET_DEPTH_ZERO, SetDepthZero);
            KeyMapper.RemoveListener(KeyEvent.SET_DEPTH_MIN, SetDepthMin);
            KeyMapper.RemoveListener(KeyEvent.LEFT, MoveLeft);
            KeyMapper.RemoveListener(KeyEvent.RIGHT, MoveRight);
            KeyMapper.RemoveListener(KeyEvent.DOWN, MoveDown);
            KeyMapper.RemoveListener(KeyEvent.UP, MoveUp);
            KeyMapper.RemoveListener(KeyEvent.ZOOM_IN, MoveForward);
            KeyMapper.RemoveListener(KeyEvent.ZOOM_OUT, MoveBackward);
        }

        private void MovePosition(int direct, float k) {
            var pos = Prefs.Calibration.Position;
            pos[direct] += k * COUNT_INC_DEC_STEPS;
            Prefs.Calibration.Position = pos;
        }
        
        private void MoveLeft() {
            MovePosition(0, -1f);
        }
        
        private void MoveRight() {
            MovePosition(0, 1f);
        }
        
        private void MoveDown() {
            MovePosition(1, -1f);
        }
        
        private void MoveUp() {
            MovePosition(1, 1f);
        }
        
        private void MoveForward() {
            MovePosition(2, 1f);
        }
        
        private void MoveBackward() {
            MovePosition(2, -1f);
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
            var minClip = _sandboxCam.nearClipPlane + 0.01f;
            var maxClip = _sandboxCam.farClipPlane - 0.01f;
            var count = 0;
            foreach (var ushortDepth in depth) {
                var d = (float)ushortDepth / 1000f;
                if (d < minClip) continue;
                
                Mathf.Clamp(d, minClip, maxClip);
                mid += d;
                if (d < min)
                    min = d;
                if (d > max)
                    max = d;
                ++count;
            }
            mid /= count;
            Prefs.Sandbox.OffsetMaxDepth = mid - min;
            Prefs.Sandbox.ZeroDepth = mid;
            Prefs.Sandbox.OffsetMinDepth = max - mid;
        }
        
        private void SetDepthMax() {
            if (_depthValid) 
                _toSave.OffsetMaxDepth = Prefs.Sandbox.OffsetMaxDepth;
        }

        private void SetDepthMin() {
            if (_depthValid) 
                _toSave.OffsetMinDepth = Prefs.Sandbox.OffsetMinDepth;
        }

        private void SetDepthZero() {
            if (_depthValid) 
                _toSave.ZeroDepth = Prefs.Sandbox.ZeroDepth;
        }
#endregion

#region UI
        private void InitUI() {
            BtnKeyBind.ShortCut(_btnCancel, KeyEvent.BACK);
            BtnKeyBind.ShortCut(_btnReset, KeyEvent.RESET);
            BtnKeyBind.ShortCut(_btnSave, KeyEvent.SAVE);
            
            UnityHelper.SetPropsByGameObjects(_sandboxFields, _pnlSandboxSettings);
            InitShortCutValue(_sandboxFields.OffsetMaxDepth, KeyEvent.SET_DEPTH_MAX, () => _toSave.OffsetMaxDepth);
            InitShortCutValue(_sandboxFields.ZeroDepth, KeyEvent.SET_DEPTH_ZERO, () => _toSave.ZeroDepth);
            InitShortCutValue(_sandboxFields.OffsetMinDepth, KeyEvent.SET_DEPTH_MIN, () => _toSave.OffsetMinDepth);
            _toSave.OnChanged += OnSandboxSettingChanged;
            OnSandboxSettingChanged();
        }

        private void InitShortCutValue(ShortCutValue scv, KeyEvent ev, Func<float> get) {
            var key = KeyMapper.FindFirstKey(ev);
            if (key != null) {
                scv.txtShortCut.text = key.ShortCut;
            }

            _updateUIValues += () => scv.txtValue.text = (get() * 100).ToString("0.0");
        }

        private void OnSandboxSettingChanged() {
            _updateUIValues?.Invoke();
        }
        
        private void OnProjectorChanged() {
            CalibrationController.UpdateCalibrationFov(_projector);
        }
        
        private void OnCalibrationChanged() {
            _txtZValue.text = (Prefs.Calibration.Position.z * 1000f).ToString("F0");
        }
#endregion
    }
}