using Games.Common;
using Launcher.KeyMapping;
using UnityEngine;
using UnityEngine.UI;
using Utilities;

namespace DepthSensorCalibration {
    public class ProjectorController2 : MonoBehaviour {
        [SerializeField] private CalibrationController2 _calibration;
        [Header("UI")]
        [SerializeField] private Transform _pnlProjectorParams;
        [SerializeField] private UIScwitcher _ui;
        [Header("Sampler")]
        [SerializeField] private SensorDistSampler _sampler;
        [SerializeField] private float _lineWidth = 0.05f;
        [SerializeField] private LineRenderer _lineArea;

        private class Field {
            public Text txtValue { get; set; }
            public Text txtShortCut { get; set; }
        }

        private class ProjectorFields {
            public Field Angel { get; set; }
            public Field SensorDist { get; set; }
            public Field Wide { get; set; }
            public Field Z { get; set; }
            public Field X { get; set; }
            public Field Y { get; set; }
        }

        private readonly ProjectorFields _projectorFields = new ProjectorFields();

        private void Start() {
            InitUI();

            _lineArea.gameObject.SetActive(false);
            _sampler.OnDistReceive += OnDistReceive;
            _sampler.OnSampleAreaPoints += ShowSampleArea;
            KeyMapper.AddListener(KeyEvent.SET_DEPTH_ZERO, SampleSensorDist);
        }

        private void InitUI() {
            UnityHelper.SetPropsByGameObjects(_projectorFields, _pnlProjectorParams);
            InitField(_projectorFields.Angel);
            InitField(_projectorFields.SensorDist, KeyEvent.SET_DEPTH_ZERO);
            InitField(_projectorFields.Wide, KeyEvent.WIDE_MINUS, KeyEvent.WIDE_PLUS);
            InitField(_projectorFields.Z, KeyEvent.ZOOM_OUT, KeyEvent.ZOOM_IN);
            InitField(_projectorFields.X, KeyEvent.LEFT, KeyEvent.RIGHT);
            InitField(_projectorFields.Y, KeyEvent.UP, KeyEvent.DOWN);
            
            KeyMapper.AddListener(KeyEvent.RESET, OnBtnReset);
            Prefs.Calibration.OnChanged += OnCalibrationChanged;
            Prefs.Projector.OnChanged += OnCalibrationChanged;
            OnCalibrationChanged();
        }

        private void OnDestroy() {
            KeyMapper.RemoveListener(KeyEvent.RESET, OnBtnReset);
            KeyMapper.RemoveListener(KeyEvent.SET_DEPTH_ZERO, SampleSensorDist);
            if (_sampler != null) {
                _sampler.OnDistReceive -= OnDistReceive;
                _sampler.OnSampleAreaPoints -= ShowSampleArea;
            }
            Prefs.Calibration.OnChanged -= OnCalibrationChanged;
            Prefs.Projector.OnChanged -= OnCalibrationChanged;
            Save();
        }

        private void Save() {
            if (IsSaveAllowed()) {
                Prefs.NotifySaved(Prefs.Projector.Save());
            }
        }

        private bool IsSaveAllowed() {
            return Prefs.Projector.HasChanges || !Prefs.Projector.HasFile;
        }

        private void OnBtnReset() {
            var def = SerializableParams.Default<ProjectorParams>();
            Prefs.Projector.DistanceToSensor = def.DistanceToSensor;
            Debug.Log(Prefs.Projector.DistanceToSensor);
            Prefs.Projector.Reset();
            Debug.Log(def.DistanceToSensor);
        }

        private void InitField(Field fld, params KeyEvent[] keys) {
            var shortCut = "";
            foreach (var keyEv in keys) {
                var key = KeyMapper.FindFirstKey(keyEv);
                if (key != null) {
                    shortCut += key.ShortCut;
                }
            }
            if (string.IsNullOrEmpty(shortCut))
                fld.txtShortCut.gameObject.SetActive(false);
            else
                fld.txtShortCut.text = $"({shortCut})";
        }

        private void OnCalibrationChanged() {
            _projectorFields.Angel.txtValue.text = Prefs.Calibration.Fov.ToString("F2");
            _projectorFields.Wide.txtValue.text = Prefs.Calibration.WideMultiply.ToString("F4");
            var sensorDist = Prefs.Projector.DistanceToSensor > 0f ? Prefs.Projector.DistanceToSensor : 0f; 
            SetPositionValue(_projectorFields.SensorDist, sensorDist);
            SetPositionValue(_projectorFields.Z, sensorDist - Prefs.Calibration.Position.z);
            SetPositionValue(_projectorFields.X, Prefs.Calibration.Position.x);
            SetPositionValue(_projectorFields.Y, Prefs.Calibration.Position.y);
        }

        private void SetPositionValue(Field f, float val) {
            f.txtValue.text = (1000f * val).ToString("F0");
        }
        
        private void SampleSensorDist() {
            _sampler.StartSampling();
            _calibration.Pause();
            _lineArea.widthMultiplier = _lineWidth;
            _ui.AllowShow = false;
        }
        
        private void OnDistReceive(float dist) {
            Prefs.Projector.DistanceToSensor = dist;
            _lineArea.gameObject.SetActive(false);
            _calibration.Resume();
            _ui.AllowShow = true;
        }
        
        private void ShowSampleArea(Vector3[] corners) {
            _lineArea.gameObject.SetActive(true);
            _lineArea.SetPositions(corners);
        }
    }
}