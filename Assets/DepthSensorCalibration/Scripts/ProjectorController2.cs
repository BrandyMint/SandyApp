using Launcher.KeyMapping;
using UnityEngine;
using UnityEngine.UI;
using Utilities;

namespace DepthSensorCalibration {
    public class ProjectorController2 : MonoBehaviour {
        private const float _INC_DEC_STEPS = 0.5f;
        
        [Header("UI")]
        [SerializeField] private Transform _pnlProjectorParams;

        private class Field {
            public Text txtValue { get; set; }
            public Text txtShortCut { get; set; }
        }

        private class ProjectorFields {
            public Field Angel { get; set; }
            public Field Zoom { get; set; }
            public Field Wide { get; set; }
            public Field Position { get; set; }
        }

        private readonly ProjectorFields _projectorFields = new ProjectorFields();

        private void Start() {
            InitUI();
        }

        private void InitUI() {
            UnityHelper.SetPropsByGameObjects(_projectorFields, _pnlProjectorParams);
            InitField(_projectorFields.Angel, KeyEvent.ANGLE_DEC, KeyEvent.ANGLE_INC);
            InitField(_projectorFields.Zoom, KeyEvent.ZOOM_OUT, KeyEvent.ZOOM_IN);
            InitField(_projectorFields.Wide, KeyEvent.WIDE_MINUS, KeyEvent.WIDE_PLUS);
            InitField(_projectorFields.Position, KeyEvent.LEFT, KeyEvent.RIGHT, KeyEvent.UP, KeyEvent.DOWN);
            
            KeyMapper.AddListener(KeyEvent.RESET, OnBtnReset);
            KeyMapper.AddListener(KeyEvent.ANGLE_INC, OnAngleIncrease);
            KeyMapper.AddListener(KeyEvent.ANGLE_DEC, OnAngleDecrease);
            Prefs.Calibration.OnChanged += OnCalibrationChanged;
            OnCalibrationChanged();
        }

        private void OnDestroy() {
            KeyMapper.RemoveListener(KeyEvent.RESET, OnBtnReset);
            KeyMapper.RemoveListener(KeyEvent.ANGLE_INC, OnAngleIncrease);
            KeyMapper.RemoveListener(KeyEvent.ANGLE_DEC, OnAngleDecrease);
            Prefs.Calibration.OnChanged -= OnCalibrationChanged;
            Save();
        }
        
        private void ModifyAngle(float k) {
            var angle = Prefs.Calibration.Fov;
            var speed = Mathf.Lerp(_INC_DEC_STEPS, _INC_DEC_STEPS * 10, Mathf.InverseLerp(25, 70, angle));
            angle += k * speed * Time.deltaTime;
            angle = Mathf.Clamp(angle, 5, 170);
            Prefs.Calibration.Fov = angle;
        }

        private void OnAngleIncrease() {
            ModifyAngle(1f);
        }
        
        private void OnAngleDecrease() {
            ModifyAngle(-1f);
        }

        private void Save() {
            if (IsSaveAllowed()) {
                Prefs.NotifySaved(Prefs.Sandbox.Save());
            }
        }

        private bool IsSaveAllowed() {
            return Prefs.Sandbox.HasChanges || !Prefs.Sandbox.HasFile;
        }

        private void OnBtnReset() {
            Prefs.Sandbox.Reset();
        }

        private void InitField(Field fld, params KeyEvent[] keys) {
            var shortCut = "";
            foreach (var keyEv in keys) {
                var key = KeyMapper.FindFirstKey(keyEv);
                if (key != null) {
                    shortCut += key.ShortCut;
                }
            }
            fld.txtShortCut.text = $"({shortCut})";
        }

        private void OnCalibrationChanged() {
            _projectorFields.Angel.txtValue.text = Prefs.Calibration.Fov.ToString("F2");
            _projectorFields.Wide.txtValue.text = Prefs.Calibration.WideMultiply.ToString("F4");
            _projectorFields.Zoom.txtValue.text = (1000f * Prefs.Calibration.Position.z).ToString("F0");
            _projectorFields.Position.txtValue.text = 
                $"x={(1000f * Prefs.Calibration.Position.x):F0} y={(1000f * Prefs.Calibration.Position.y):F0}";
        }
    }
}