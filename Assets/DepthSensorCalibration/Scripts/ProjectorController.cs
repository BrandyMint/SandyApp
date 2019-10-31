using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Games.Landscape;
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

        private class Field {
            public InputField fld { get; set; }
            public ConvertValueUI convert { get; set; }
        }

        private class ProjectorFields {
            public Field Dist { get; set; }
            public Field Diag { get; set; }
            public Field Width { get; set; }
            public Field Height { get; set; }
        }

        private readonly ProjectorFields _projectorFields = new ProjectorFields();
        private readonly ProjectorParams _projector = new ProjectorParams();
        private readonly ISet<Field> _errors = new HashSet<Field>();
        private bool _setUIOnChange = true;
        private bool _updatePrefFromUI = true;

        private void Start() {
            InitUI();
        }

        private void InitUI() {
            BtnKeyBind.ShortCut(_btnCancel, KeyEvent.BACK);
            BtnKeyBind.ShortCut(_btnReset, KeyEvent.RESET);
            KeyMapper.AddListener(KeyEvent.RESET, OnBtnReset);
            
            UnityHelper.SetPropsByGameObjects(_projectorFields, _pnlProjectorParams);
            //InitField(_projectorFields.Dist, val => _projector.Distance = val);
            //InitField(_projectorFields.Diag, val => _projector.Diagonal = val);
            InitField(_projectorFields.Width, val => _projector.Width = val);
            InitField(_projectorFields.Height, val => _projector.Height = val);
            _projector.OnChanged += OnProjectorChanged;
            OnProjectorChanged();
        }
        
        private void OnProjectorChanged() {
            if (_setUIOnChange) {
                _updatePrefFromUI = false;
                //SetUIFromPrefs(_projectorFields.Dist, _projector.Distance);
                //SetUIFromPrefs(_projectorFields.Diag, _projector.Diagonal);
                SetUIFromPrefs(_projectorFields.Width, _projector.Width);
                SetUIFromPrefs(_projectorFields.Height, _projector.Height);
                _updatePrefFromUI = true;
            }
        }

        private void SetUIFromPrefs(Field field, float val) {
            field.fld.text = field.convert.Set(val).ToString(CultureInfo.InvariantCulture);
        }

        private void OnDestroy() {
            KeyMapper.RemoveListener(KeyEvent.RESET, OnBtnReset);
            _projector.OnChanged -= OnProjectorChanged;
            Save();
        }

        private void Save() {
            if (_errors.Any()) {
                Prefs.NotifyIncorrectData();
            } else if (IsSaveAllowed()) {
                Prefs.NotifySaved(_projector.Save());
                //Scenes.GoBack();
            }
        }

        private void FixedUpdate() {
            _btnSave.interactable = IsSaveAllowed();
        }

        private bool IsSaveAllowed() {
            return !_errors.Any() && (_projector.HasChanges || !_projector.HasFile);
        }

        private void OnBtnReset() {
            _projector.Reset();
        }

        private void InitField(Field fld, UnityAction<float> set) {
            fld.fld.onValueChanged.AddListener(strVal => {
                fld.fld.image.color = Color.white;
                if (float.TryParse(strVal, NumberStyles.Float, CultureInfo.InvariantCulture, out var val)
                    && val > 0f) 
                {
                    UpdatePrefFromUI(fld.convert.Get(val), set);
                    _errors.Remove(fld);
                } else {
                    fld.fld.image.color = _colorError;
                    _errors.Add(fld);
                }
            });
        }

        private void UpdatePrefFromUI(float val, UnityAction<float> set) {
            if (_updatePrefFromUI) {
                _setUIOnChange = false;
                set.Invoke(val);
                _setUIOnChange = true;
            }
        }
    }
}