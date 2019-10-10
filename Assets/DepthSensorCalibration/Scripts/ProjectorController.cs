using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Launcher;
using Launcher.KeyMapping;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Utilities;

namespace DepthSensorCalibration {
    public class ProjectorController : MonoBehaviour {
        [Header("UI")]
        [SerializeField] private Transform _pnlProjectorParams;
        [SerializeField] private Color _colorError = Color.red;
        [SerializeField] private Button _btnSave;
        [SerializeField] private Button _btnCancel;
        [SerializeField] private Button _btnReset;

        private class ProjectorFields {
            public InputField Dist { get; set; }
            public InputField Diag { get; set; }
            public InputField Width { get; set; }
            public InputField Height { get; set; }
        }

        private readonly ProjectorFields _projectorFields = new ProjectorFields();
        private readonly ProjectorParams _projector = new ProjectorParams();
        private readonly ISet<InputField> _errors = new HashSet<InputField>();
        private bool _setUIOnChange = true;
        private bool _updatePrefFromUI = true;

        private void Start() {
            InitUI();
        }

        private void InitUI() {
            BtnKeyBind.ShortCut(_btnSave, KeyEvent.SAVE);
            BtnKeyBind.ShortCut(_btnCancel, KeyEvent.BACK);
            BtnKeyBind.ShortCut(_btnReset, KeyEvent.RESET);
            KeyMapper.AddListener(KeyEvent.SAVE, OnBtnSave);
            KeyMapper.AddListener(KeyEvent.RESET, OnBtnReset);
            
            UnityHelper.SetPropsByGameObjects(_projectorFields, _pnlProjectorParams);
            InitField(_projectorFields.Dist, val => _projector.Distance = val);
            InitField(_projectorFields.Diag, val => _projector.Diagonal = val);
            InitField(_projectorFields.Width, val => _projector.Width = val);
            InitField(_projectorFields.Height, val => _projector.Height = val);
            _projector.OnChanged += OnProjectorChanged;
            OnProjectorChanged();
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
        }

        private void OnDestroy() {
            KeyMapper.RemoveListener(KeyEvent.SAVE, OnBtnSave);
            KeyMapper.RemoveListener(KeyEvent.RESET, OnBtnReset);
            _projector.OnChanged -= OnProjectorChanged;
        }

        private void OnBtnSave() {
            if (_errors.Any()) {
                Prefs.NotifyIncorrectData();
            } else {
                Prefs.NotifySaved(_projector.Save());
                Scenes.GoBack();
            }
        }

        private void OnBtnReset() {
            _projector.Reset();
        }

        private void InitField(InputField fld, UnityAction<float> act) {
            fld.onValueChanged.AddListener(strVal => {
                fld.image.color = Color.white;
                if (float.TryParse(strVal, NumberStyles.Float, CultureInfo.InvariantCulture, out var val)
                    && val > 0f) 
                {
                    UpdatePrefFromUI(val, act);
                    _errors.Remove(fld);
                } else {
                    fld.image.color = _colorError;
                    _errors.Add(fld);
                }

                _btnSave.interactable = !_errors.Any();
            });
        }

        private void UpdatePrefFromUI(float val, UnityAction<float> act) {
            if (_updatePrefFromUI) {
                _setUIOnChange = false;
                act.Invoke(val);
                _setUIOnChange = true;
            }
        }
    }
}