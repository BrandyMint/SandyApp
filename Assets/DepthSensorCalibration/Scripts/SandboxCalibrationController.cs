#if USE_MAT_ASYNC_SET
    using AsyncGPUReadbackPluginNs;
#endif
using System;
using DepthSensorSandbox;
using DepthSensorSandbox.Visualisation;
using Launcher;
using Launcher.KeyMapping;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;
using Utilities;

namespace DepthSensorCalibration {
    public class SandboxCalibrationController : MonoBehaviour {
        [Header("UI")]
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


        private void Start() {
            InitUI();
            SubscribeKeys();
            //SwithcUI();
            _renderDepth = _sandboxCam.gameObject.AddComponent<CameraRenderToTexture>();
            _renderDepth.MaxResolution = 64;
            _renderDepth.Enable(_matDepth, RenderTextureFormat.R16, OnNewDepthFrame);
            _sandbox.SetEnable(true);
        }

        private void OnDestroy() {
            if (_renderDepth != null) {
                _renderDepth.Disable();
            }
            if (_sandbox != null) {
                _sandbox.SetEnable(false);
            }
            UnSubscribeKeys();
            Prefs.Sandbox.Load();
        }

#region Buttons
        private void OnBtnSave() {
            if (IsSaveAllowed()) {
                Prefs.NotifySaved(_toSave.Save());
                Scenes.GoBack();
            }
        }
        
        private void FixedUpdate() {
            _btnSave.interactable = IsSaveAllowed();
        }

        private bool IsSaveAllowed() {
            return _toSave.HasChanges || !_toSave.HasFile;
        }

        private void OnBtnReset() {
            _toSave.Reset();
        }
        
        private void SubscribeKeys() {
            KeyMapper.AddListener(KeyEvent.SAVE, OnBtnSave);
            KeyMapper.AddListener(KeyEvent.RESET, OnBtnReset);
            KeyMapper.AddListener(KeyEvent.SHOW_UI, SwithcUI);
            KeyMapper.AddListener(KeyEvent.SET_DEPTH_MAX, SetDepthMax);
            KeyMapper.AddListener(KeyEvent.SET_DEPTH_ZERO, SetDepthZero);
            KeyMapper.AddListener(KeyEvent.SET_DEPTH_MIN, SetDepthMin);
        }

       

        private void UnSubscribeKeys() {
            KeyMapper.RemoveListener(KeyEvent.SAVE, OnBtnSave);
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
            foreach (var d in depth) {
                mid += (float)d / depth.Length;
                if (d < min)
                    min = d;
                if (d > max)
                    max = d;
            }

            Prefs.Sandbox.OffsetMaxDepth = (mid - min) / 1000f;
            Prefs.Sandbox.ZeroDepth = mid / 1000f;
            Prefs.Sandbox.OffsetMinDepth = (max - mid) / 1000f;
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
#endregion
    }
}