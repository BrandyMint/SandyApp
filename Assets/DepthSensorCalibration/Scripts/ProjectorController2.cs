using System;
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
        [SerializeField] private GameObject _projectorSizePnl;
        [Header("Sampler")]
        [SerializeField] private SensorDistSampler _sampler;
        [SerializeField] private float _lineWidth = 0.05f;
        [SerializeField] private LineRenderer _lineArea;

        private class Field {
            public Text txtValue { get; set; }
            public Text txtShortCut { get; set; }
        }
        
        private class ObliqueField : Field {
            public GameObject Oblique0 { get; set; }
            public GameObject Oblique_1 { get; set; }
            public GameObject Oblique1 { get; set; }
        }

        private class ProjectorFields {
            public Field Angel { get; set; }
            public ObliqueField Oblique { get; set; }
            public Field ProjectorSize { get; set; }
            public Field SensorDist { get; set; }
            public Field Z { get; set; }
            public Field X { get; set; }
            public Field Y { get; set; }
        }
        
        private class ObliqueChangeInfo {
            public float val;
            public float changeY;
        }

        private readonly ProjectorFields _projectorFields = new ProjectorFields();
        private readonly ObliqueChangeInfo[] _obliques = {
            new ObliqueChangeInfo {val = 0f, changeY = 0.5f},
            new ObliqueChangeInfo {val = -1f, changeY = 0.5f},
            new ObliqueChangeInfo {val = 1f, changeY = -1f}
        };

        private void Start() {
            InitUI();

            _lineArea.gameObject.SetActive(false);
            _sampler.OnDistReceive += OnDistReceive;
            _sampler.OnSampleAreaPoints += ShowSampleArea;
            
            KeyMapper.AddListener(KeyEvent.RESET, OnBtnReset);
            KeyMapper.AddListener(KeyEvent.SET_DEPTH, SampleSensorDist);
            KeyMapper.AddListener(KeyEvent.CHANGE_PROJECTOR_SIZE, OnChangeProjectorSize);
            KeyMapper.AddListener(KeyEvent.SWITCH_OBLIQUE, OnChangeOblique);
        }

        private void InitUI() {
            UnityHelper.SetPropsByGameObjects(_projectorFields, _pnlProjectorParams);
            InitField(_projectorFields.Angel);
            InitField(_projectorFields.Oblique, KeyEvent.SWITCH_OBLIQUE);
            InitField(_projectorFields.ProjectorSize, KeyEvent.CHANGE_PROJECTOR_SIZE);
            InitField(_projectorFields.SensorDist, KeyEvent.SET_DEPTH);
            InitField(_projectorFields.Z, KeyEvent.ZOOM_OUT, KeyEvent.ZOOM_IN);
            InitField(_projectorFields.X, KeyEvent.LEFT, KeyEvent.RIGHT);
            InitField(_projectorFields.Y, KeyEvent.UP, KeyEvent.DOWN);
            
            Prefs.Calibration.OnChanged += OnCalibrationChanged;
            Prefs.Projector.OnChanged += OnCalibrationChanged;
            OnCalibrationChanged();
        }

        private void OnDestroy() {
            KeyMapper.RemoveListener(KeyEvent.RESET, OnBtnReset);
            KeyMapper.RemoveListener(KeyEvent.SET_DEPTH, SampleSensorDist);
            KeyMapper.RemoveListener(KeyEvent.CHANGE_PROJECTOR_SIZE, OnChangeProjectorSize);
            KeyMapper.RemoveListener(KeyEvent.SWITCH_OBLIQUE, OnChangeOblique);
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
            Prefs.Projector.Reset();
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
                fld.txtShortCut.text = $"[{shortCut}]";
        }

        private void OnCalibrationChanged() {
            _projectorFields.Angel.txtValue.text = Prefs.Calibration.Fov.ToString("F2");
            _projectorFields.ProjectorSize.txtValue.text =
                ConvertValueToUI(Prefs.Projector.Width) + "x" + ConvertValueToUI(Prefs.Projector.Height); 
            var sensorDist = Prefs.Projector.DistanceToSensor > 0f ? Prefs.Projector.DistanceToSensor : 0f; 
            SetPositionValue(_projectorFields.SensorDist, sensorDist);
            SetPositionValue(_projectorFields.Z, sensorDist - Prefs.Calibration.Position.z);
            SetPositionValue(_projectorFields.X, Prefs.Calibration.Position.x);
            SetPositionValue(_projectorFields.Y, Prefs.Calibration.Position.y);
            
            _projectorFields.Oblique.txtValue.text = Prefs.Calibration.Oblique.ToString();
            _projectorFields.Oblique.Oblique0.SetActive(Mathf.Approximately(Prefs.Calibration.Oblique, 0f));
            _projectorFields.Oblique.Oblique_1.SetActive(Mathf.Approximately(Prefs.Calibration.Oblique, -1f));
            _projectorFields.Oblique.Oblique1.SetActive(Mathf.Approximately(Prefs.Calibration.Oblique, 1f));
        }

        private void SetPositionValue(Field f, float val) {
            f.txtValue.text = ConvertValueToUI(val);
        }

        private static string ConvertValueToUI(float val) {
            return (1000f * val).ToString("F0");
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

        private void OnChangeProjectorSize() {
            _projectorSizePnl.SetActive(true);
        }

        private void OnChangeOblique() {
            var i = Array.FindIndex(_obliques, 
                o => Mathf.Approximately(o.val, Prefs.Calibration.Oblique));
            var oblique = _obliques[(i + 1) % _obliques.Length];
            
            var pos = Prefs.Calibration.Position;
            pos.y += Prefs.Projector.Height * oblique.changeY;
            Prefs.Calibration.Position = pos;
            
            Prefs.Calibration.Oblique = oblique.val;
        }
    }
}