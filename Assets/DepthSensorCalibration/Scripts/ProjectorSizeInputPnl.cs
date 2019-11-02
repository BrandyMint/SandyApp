using System.Collections.Generic;
using System.Linq;
using Launcher.KeyMapping;
using UnityEngine;
using UnityEngine.UI;
using Utilities;

namespace DepthSensorCalibration {
    public class ProjectorSizeInputPnl : MonoBehaviour {
        private class Field {
            public InputField fld { get; set; }
            public GameObject Error { get; set; }
        }
        
        private class ProjectorSize {
            public Field Width { get; set; }
            public Field Height { get; set; }
        }
        
        private ProjectorSize _fields = new ProjectorSize();
        private readonly List<Field> _fieldsArr = new List<Field>();
        private int _overrideLayerId;
        private int _step;
        private bool _fieldValid;

        private void Awake() {
            UnityHelper.SetPropsByGameObjects(_fields, transform);
            _fieldsArr.Add(_fields.Width);
            _fieldsArr.Add(_fields.Height);
            foreach (var field in _fieldsArr) {
                field.fld.onValueChanged.AddListener(str => ValidateField(field));
            }
        }

        public void OnEnable() {
            _overrideLayerId = KeyMapper.PushOverrideLayer();
            var overrideEvents = KeyMapper.GetListenedEvents(EventLayer.LOCAL);
            foreach (var ev in overrideEvents
                .Append(KeyEvent.SHOW_UI)
                .Append(KeyEvent.RESET)
            ) {
                KeyMapper.AddListener(ev, DoNothing, _overrideLayerId);
            }
            KeyMapper.AddListener(KeyEvent.BACK, OnCancel, _overrideLayerId);
            KeyMapper.AddListener(KeyEvent.ENTER, OnNextStep, _overrideLayerId);
            Step(0);
        }

        public void OnDisable() {
            KeyMapper.PopOverrideLayer();
        }

        private static void DoNothing() {}

        private void OnCancel() {
            gameObject.SetActive(false);
        }

        private void Step(int i) {
            _step = i;
            switch (i) {
                case 0:
                    ActivateField(_fields.Width, Prefs.Projector.Width);
                    break;
                case 1:
                    ActivateField(_fields.Height, Prefs.Projector.Height);
                    break;
                case 2:
                    FinishAndSave();
                    break;
            }
        }

        private void ActivateField(Field field, float val) {
            foreach (var f in _fieldsArr) {
                f.fld.transform.parent.gameObject.SetActive(f == field);
            }
            field.fld.text = (val * 1000f).ToString("F0");
            field.fld.ActivateInputField();
            field.fld.Select();
            ValidateField(field);
        }

        private void FinishAndSave() {
            Prefs.Projector.Width = float.Parse(_fields.Width.fld.text) / 1000f;
            Prefs.Projector.Height = float.Parse(_fields.Height.fld.text) / 1000f;
            gameObject.SetActive(false);
        }

        private void ValidateField(Field field) {
            if (int.TryParse(field.fld.text, out var val)) {
                if (val >= 100 && val <= 5000) {
                    field.Error.SetActive(false);
                    _fieldValid = true;
                    return;
                }
            }
            
            field.Error.SetActive(true);
            _fieldValid = false;
        }

        private void OnNextStep() {
            if (_fieldValid) {
                Step(++_step);
            }
        }
    }
}