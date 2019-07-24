using System;
using Launcher;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Utilities;

namespace Games.Landscape {
    public class LandscapeSettings : MonoBehaviour {
        private const float _INC_DEC_STEP = 1.0f;

        [SerializeField] private Button _btnBack;
        [SerializeField] private Button _btnSave;
        [SerializeField] private Transform _pnlParams;
        
        private class SliderField {
            public Slider sl { get; set; }
            public Text txtVal { get; set; }
            public Button btnInc { get; set; }
            public Button btnDec { get; set; }
        }
        
        private class ParamsFields {
            public SliderField DepthSeaBottom { get; set; }
            public SliderField DepthSea { get; set; }
            public SliderField DepthGround { get; set; }
            public SliderField DepthMountains { get; set; }
        }

        private readonly ParamsFields _params = new ParamsFields();

        private void Start() {
            _btnBack.onClick.AddListener(OnBtnBack);
            _btnSave.onClick.AddListener(OnBtnSave);
            
            UnityHelper.SetPropsByGameObjects(_params, _pnlParams);
            InitSlider(_params.DepthSeaBottom, nameof(Prefs.Landscape.DepthSeaBottom));
            InitSlider(_params.DepthSea, nameof(Prefs.Landscape.DepthSea));
            InitSlider(_params.DepthGround, nameof(Prefs.Landscape.DepthGround));
            InitSlider(_params.DepthMountains, nameof(Prefs.Landscape.DepthMountains));
        }

        private static void OnBtnSave() {
            Prefs.Landscape.Save();
        }

        private static void OnBtnBack() {
            Scenes.GoBack();
        }

        private static void InitSlider(SliderField fld, string param) {
            var obj = Prefs.Landscape;
            var prop = obj.GetType().GetProperty(param);
            InitSlider(fld, 
                (float) prop.GetValue(obj), 
                (Action<float>) prop.GetSetMethod().CreateDelegate(typeof(Action<float>), obj));
        }

        private static void InitSlider(SliderField fld, float initVal, Action<float> set) {
            UnityAction<float> changeTxt = val => {
                fld.txtVal.text = (fld.sl.wholeNumbers)
                    ? val.ToString()
                    : val.ToString("0.0");
            };
            fld.sl.onValueChanged.AddListener(changeTxt);
            fld.sl.value = initVal;
            fld.sl.onValueChanged.AddListener(val => set(val));
            fld.btnInc.onClick.AddListener(CreateOnBtnIncDec(fld.sl, 1.0f));
            fld.btnDec.onClick.AddListener(CreateOnBtnIncDec(fld.sl, -1.0f));
        }
        
        private static UnityAction CreateOnBtnIncDec(Slider sl, float mult) {
            return () => {
                var val = (sl.wholeNumbers) ?
                    1 :
                    _INC_DEC_STEP;
                sl.value += val * mult;
            };
        }
    }
}