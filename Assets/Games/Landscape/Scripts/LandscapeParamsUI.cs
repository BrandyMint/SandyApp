using System;
using System.Collections.Generic;
using Launcher.KeyMapping;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Utilities;

namespace Games.Landscape {
    public class LandscapeParamsUI : MonoBehaviour {
        private const float _INC_DEC_STEP = 1.0f;
        private const float _INC_DEC_STEPS_COUNT = 15;

        [SerializeField] private Canvas _uiCanvas;
        [SerializeField] private Button _btnBack;
        [SerializeField] private Button _btnReset;
        [SerializeField] private Button _btnSave;
        [SerializeField] private Button _btnResetWater;
        [SerializeField] private Toggle _tglWater;
        [SerializeField] private LandscapeVisualizer _landscape;
        [SerializeField] private Transform _pnlParams;
        
        private class SliderField {
            public Slider sl { get; set; }
            public Text txtVal { get; set; }
            public Button btnInc { get; set; }
            public Button btnDec { get; set; }
            public ConvertValueUI convert { get; set; }
        }
        
        private class ParamsFields {
            public SliderField DepthSeaBottom { get; set; }
            public SliderField DepthSea { get; set; }
            public SliderField DepthGround { get; set; }
            public SliderField DepthMountains { get; set; }
            public SliderField DepthIce { get; set; }
            public SliderField DetailsSize { get; set; }
            
            public SliderField FluidResolution { get; set; }
            
            public SliderField FluidCellSize { get; set; }
            
            public SliderField FluidAcceleration { get; set; }
            public SliderField FluidFading { get; set; }
        }

        private readonly ParamsFields _params = new ParamsFields();
        private bool _invokeChagedUI = true;
        private readonly List<Action> _onParamsReset = new List<Action>();

        private void Start() {
            _btnResetWater.onClick.AddListener(OnBtnResetWater);
            BtnKeyBind.ShortCut(_btnBack, KeyEvent.BACK);
            BtnKeyBind.ShortCut(_btnReset, KeyEvent.RESET);
            //BtnKeyBind.ShortCut(_btnSave, KeyEvent.SAVE);
            
            KeyMapper.AddListener(KeyEvent.RESET, OnBtnReset);
            //KeyMapper.AddListener(KeyEvent.SAVE, Save);
            KeyMapper.AddListener(KeyEvent.SHOW_UI, SwitchUI);

            _tglWater.isOn = Prefs.Landscape.EnableWaterSimulation;
            _tglWater.onValueChanged.AddListener(OnTglWater);

            UnityHelper.SetPropsByGameObjects(_params, _pnlParams);
            InitSlider(_params.DepthSeaBottom, nameof(Prefs.Landscape.DepthSeaBottom));
            InitSlider(_params.DepthSea, nameof(Prefs.Landscape.DepthSea));
            InitSlider(_params.DepthGround, nameof(Prefs.Landscape.DepthGround));
            InitSlider(_params.DepthMountains, nameof(Prefs.Landscape.DepthMountains));
            InitSlider(_params.DepthIce, nameof(Prefs.Landscape.DepthIce));
            InitSlider(_params.DetailsSize, nameof(Prefs.Landscape.DetailsSize));
            InitSlider(_params.FluidResolution, nameof(Prefs.Landscape.FluidResolution));
            InitSlider(_params.FluidCellSize, nameof(Prefs.Landscape.FluidCellSize));
            InitSlider(_params.FluidAcceleration, nameof(Prefs.Landscape.FluidAcceleration));
            InitSlider(_params.FluidFading, nameof(Prefs.Landscape.FluidFading));
            
            SwitchUI();
        }

        private void OnDestroy() {
            KeyMapper.RemoveListener(KeyEvent.RESET, OnBtnReset);
            //KeyMapper.RemoveListener(KeyEvent.SAVE, Save);
            KeyMapper.RemoveListener(KeyEvent.SHOW_UI, SwitchUI);
            Save();
        }

        private void Save() {
            if (IsSaveAllowed())
                Prefs.NotifySaved(Prefs.Landscape.Save());
        }
        
        private void FixedUpdate() {
            _btnSave.interactable = IsSaveAllowed();
        }

        private bool IsSaveAllowed() {
            return Prefs.Landscape.HasChanges || !Prefs.Landscape.HasFile;
        }

        private void OnBtnReset() {
            Prefs.Landscape.Reset();
            _invokeChagedUI = false;
            foreach (var action in _onParamsReset) {
                action();
            }
            _invokeChagedUI = true;
        }
        
        private void OnBtnResetWater() {
            _landscape.ClearFluidFlows();
        }

        private void SwitchUI() {
            _uiCanvas.gameObject.SetActive(!_uiCanvas.gameObject.activeSelf);
        }

        private static void OnTglWater(bool enable) {
            Prefs.Landscape.EnableWaterSimulation = enable;
        }

        private void InitSlider(SliderField fld, string param) {
            var obj = Prefs.Landscape;
            var prop = obj.GetType().GetProperty(param);
            InitSlider(fld, 
                (Func<float>) prop.GetGetMethod().CreateDelegate(typeof(Func<float>), obj), 
                (Action<float>) prop.GetSetMethod().CreateDelegate(typeof(Action<float>), obj));
        }

        private void InitSlider(SliderField fld, Func<float> get, Action<float> set) {
            void ChangeTxt(float val) {
                fld.txtVal.text = (fld.sl.wholeNumbers)
                    ? val.ToString()
                    : val.ToString("0.0");
            }
            fld.sl.onValueChanged.AddListener(ChangeTxt);
            
            void OnParamsReset() => fld.sl.value = fld.convert.Set(get());
            _onParamsReset.Add(OnParamsReset);
            OnParamsReset();
            ChangeTxt(fld.sl.value);
            
            void SetFromUI(float val) {
                if (_invokeChagedUI) set(fld.convert.Get(val));
            }
            fld.sl.onValueChanged.AddListener(SetFromUI);
            fld.btnInc.onClick.AddListener(CreateOnBtnIncDec(fld.sl, 1.0f));
            fld.btnDec.onClick.AddListener(CreateOnBtnIncDec(fld.sl, -1.0f));
        }
        
        private static UnityAction CreateOnBtnIncDec(Slider sl, float mult) {
            var step = 1f;
            if (!sl.wholeNumbers) {
                var diff = Math.Abs(sl.maxValue - sl.minValue);
                if (diff / _INC_DEC_STEP > _INC_DEC_STEPS_COUNT) {
                    step = _INC_DEC_STEP;
                } else {
                    step = diff / _INC_DEC_STEPS_COUNT;
                }
            }
            return () => {
                sl.value += step * mult;
            };
        }
    }
}