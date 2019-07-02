﻿using System;
using System.Globalization;
using Launcher.Scripts;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Utilities;

namespace DepthSensorCalibration {
    public class CalibrationController : MonoBehaviour {
        private const float COUNT_INC_DEC_STEPS = 200.0f;
        
        [SerializeField] private Transform _pnlCalibrationSettings;
        [SerializeField] private Transform _pnlProjectorParams;
        [SerializeField] private Color _colorError = Color.red;
        [SerializeField] private Button _btnSave;
        [SerializeField] private Button _btnCancel;
        [SerializeField] private Button _btnReset;

        private class SliderField {
            public Slider sl { get; set; }
            public Text txtVal { get; set; }
            public Button btnInc { get; set; }
            public Button btnDec { get; set; }
        }

        private class CalibrationFields {
            public SliderField PosX { get; set; }
            public SliderField PosY { get; set; }
            public SliderField PosZ { get; set; }
            public SliderField ZeroDepth { get; set; }
        }

        private class ProjectorFields {
            public InputField Dist { get; set; }
            public InputField Diag { get; set; }
            public InputField Width { get; set; }
            public InputField Height { get; set; }
        }

        private readonly CalibrationFields _calibrationFields = new CalibrationFields();
        private readonly ProjectorFields _projectorFields = new ProjectorFields();
        private readonly ProjectorParams _projector = new ProjectorParams();
        private bool _setUIOnChange = true;
        private bool _updatePrefFromUI = true;

        private void Start() {
            _btnSave.onClick.AddListener(OnBtnSave);
            _btnCancel.onClick.AddListener(OnBtnCancel);
            _btnReset.onClick.AddListener(OnBtnReset);
            
            UnityHelper.SetPropsByGameObjects(_projectorFields, _pnlProjectorParams);
            InitField(_projectorFields.Dist, val => _projector.Distance = val);
            InitField(_projectorFields.Diag, val => _projector.Diagonal = val);
            InitField(_projectorFields.Width, val => _projector.Width = val);
            InitField(_projectorFields.Height, val => _projector.Height = val);
            _projector.OnChanged += OnProjectorChanged;
            OnProjectorChanged();
            
            UnityHelper.SetPropsByGameObjects(_calibrationFields, _pnlCalibrationSettings);
            InitSlider(_calibrationFields.PosX, val => UpdatePosFromUI());
            InitSlider(_calibrationFields.PosY, val => UpdatePosFromUI());
            InitSlider(_calibrationFields.PosZ, val => UpdatePosFromUI());
            InitSlider(_calibrationFields.ZeroDepth, val => Prefs.Calibration.ZeroDepth = val / 1000f);
            Prefs.Calibration.OnChanged += OnCalibrationChanged;
            OnCalibrationChanged();
        }
        
        private void OnProjectorChanged() {
            if (_setUIOnChange) {
                _updatePrefFromUI = false;
                _projectorFields.Dist.text = _projector.Distance.ToString(CultureInfo.InvariantCulture);
                _projectorFields.Diag.text = _projector.Diagonal.ToString(CultureInfo.InvariantCulture);
                _projectorFields.Width.text = _projector.Width.ToString(CultureInfo.InvariantCulture);
                _projectorFields.Height.text = _projector.Height.ToString(CultureInfo.InvariantCulture);
                _updatePrefFromUI = true;
            }
            UpdateCalibrationFov();
        }

        private void OnCalibrationChanged() {
            if (_setUIOnChange) {
                _updatePrefFromUI = false;
                _calibrationFields.PosX.sl.value = Prefs.Calibration.Position.x * 1000f;
                _calibrationFields.PosY.sl.value = Prefs.Calibration.Position.y * 1000f;
                _calibrationFields.PosZ.sl.value = Prefs.Calibration.Position.z * 1000f;
                _calibrationFields.ZeroDepth.sl.value = Prefs.Calibration.ZeroDepth * 1000f;
                _updatePrefFromUI = true;
            }
        }

        private void OnDestroy() {
            _projector.OnChanged -= OnProjectorChanged;
            Prefs.Calibration.OnChanged -= OnCalibrationChanged; 
        }

        private void OnBtnSave() {
            _projector.Save();
            Prefs.Calibration.Save();
        }

        private void OnBtnReset() {
            Prefs.Calibration.Reset();
            _projector.Reset();
        }

        private void OnBtnCancel() {
            Prefs.Calibration.Load();
            _projector.Load();
        }

        private void InitSlider(SliderField fld, UnityAction<float> act) {
            UnityAction<float> changeTxt = val => {
                fld.txtVal.text = (fld.sl.wholeNumbers)
                    ? val.ToString()
                    : val.ToString("0.000");
            };
            fld.sl.onValueChanged.AddListener(changeTxt);
            fld.sl.onValueChanged.AddListener(val => { UpdatePrefFromUI(val, act); });
            fld.btnInc.onClick.AddListener(CreateOnBtnIncDec(fld.sl, 1.0f));
            fld.btnDec.onClick.AddListener(CreateOnBtnIncDec(fld.sl, -1.0f));
        }

        private void InitField(InputField fld, UnityAction<float> act) {
            fld.onValueChanged.AddListener(strVal => {
                fld.image.color = Color.white;
                if (float.TryParse(strVal, NumberStyles.Float, CultureInfo.InvariantCulture, out var val)
                    && val > 0f) 
                {
                    UpdatePrefFromUI(val, act);
                } else {
                    fld.image.color = _colorError;
                }
            });
        }

        private static UnityAction CreateOnBtnIncDec(Slider sl, float mult) {
            return () => {
                var val = (sl.wholeNumbers) ?
                    1 :
                    (sl.maxValue - sl.minValue) / COUNT_INC_DEC_STEPS;
                sl.value += val * mult;
            };
        }

        private void UpdateCalibrationFov() {        
            var aspect = _projector.Width / _projector.Height;
            var s = _projector.Diagonal;
            var d = _projector.Distance;
            var h = s / Mathf.Sqrt((aspect * aspect + 1f));
            var fov = 180f - 2 * Mathf.Acos(h / 2f / d) * Mathf.Rad2Deg;
            Prefs.Calibration.Fov = fov;
        }

        private void UpdatePrefFromUI(float val, UnityAction<float> act) {
            if (_updatePrefFromUI) {
                _setUIOnChange = false;
                act.Invoke(val);
                _setUIOnChange = true;
            }
        }


        private void UpdatePosFromUI() {
            Prefs.Calibration.Position = new Vector3(
             _calibrationFields.PosX.sl.value, 
             _calibrationFields.PosY.sl.value,
             _calibrationFields.PosZ.sl.value
            ) / 1000f;
        }
    }
}