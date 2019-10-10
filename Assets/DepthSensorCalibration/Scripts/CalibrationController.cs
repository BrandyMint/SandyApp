using DepthSensorCalibration.HalfVisualization;
using DepthSensorSandbox.Visualisation;
using Launcher;
using Launcher.KeyMapping;
using Launcher.MultiMonitorSupport;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Utilities;

namespace DepthSensorCalibration {
    public class CalibrationController : MonoBehaviour {
        private const float COUNT_INC_DEC_STEPS = 200.0f;

        public WallController Wall;
        public Camera SandboxCam;

        [SerializeField] private Camera _camMonitor;
        [SerializeField] private Camera _camWall;
        
        [Header("UI")] 
        [SerializeField] private HalfBase _ui;
        [SerializeField] private GameObject _calibrationImg;
        [SerializeField] private GameObject _settingsAndBtns;
        [SerializeField] private Transform _pnlCalibrationSettings;
        [SerializeField] private Button _btnSave;
        [SerializeField] private Button _btnCancel;
        [SerializeField] private Button _btnReset;
        [SerializeField] private Toggle _tglTest;
        [SerializeField] private Button _btnAutomatic;

        [Header("Auto calibration")]
        [SerializeField] private SandboxMesh _sandboxMesh;
        [SerializeField] private AutomaticCalibration _automatic;

        [Header("Test mode")]
        [SerializeField] private GameObject _imgColor;

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
        private readonly CalibrationFields _calibrationFields = new CalibrationFields();
        private readonly ProjectorParams _projector = new ProjectorParams();
        private bool _setUIOnChange = true;
        private bool _updatePrefFromUI = true;

        private void Start() {
            InitUI();
            SwitchMode(CalibrationMode.MANUAL);
            SubscribeKeys();
        }

        private void OnDestroy() {
            UnSubscribeKeys();
            _projector.OnChanged -= OnProjectorChanged;
            Prefs.Calibration.OnChanged -= OnCalibrationChanged;
            Prefs.Calibration.Load();
        }

#region Buttons
        private void OnBtnSave() {
            Prefs.NotifySaved(Prefs.Calibration.Save());
            Scenes.GoBack();
        }

        private void OnBtnReset() {
            _projector.Load();
            Prefs.Calibration.Reset();
        }

        private void OnBtnCancel() {
            _projector.Load();
            Prefs.Calibration.Load();
            Scenes.GoBack();
        }
        
        private void SubscribeKeys() {
            KeyMapper.AddListener(KeyEvent.SAVE, OnBtnSave);
            KeyMapper.AddListener(KeyEvent.RESET, OnBtnReset);
            KeyMapper.AddListener(KeyEvent.SHOW_UI, SwithcUI);
            KeyMapper.AddListener(KeyEvent.LEFT, _calibrationFields.PosX.btnDec.onClick.Invoke);
            KeyMapper.AddListener(KeyEvent.RIGHT, _calibrationFields.PosX.btnInc.onClick.Invoke);
            KeyMapper.AddListener(KeyEvent.DOWN, _calibrationFields.PosY.btnDec.onClick.Invoke);
            KeyMapper.AddListener(KeyEvent.UP, _calibrationFields.PosY.btnInc.onClick.Invoke);
            KeyMapper.AddListener(KeyEvent.ZOOM_IN, _calibrationFields.PosZ.btnInc.onClick.Invoke);
            KeyMapper.AddListener(KeyEvent.ZOOM_OUT, _calibrationFields.PosZ.btnDec.onClick.Invoke);
            /*if (Input.GetKey(KeyCode.LeftBracket) || Input.GetKey(KeyCode.LeftCurlyBracket)) {
                _calibrationFields.ZeroDepth.btnDec.onClick.Invoke();
            }
            if (Input.GetKey(KeyCode.RightBracket) || Input.GetKey(KeyCode.RightBracket)) {
                _calibrationFields.ZeroDepth.btnInc.onClick.Invoke();
            }*/
        }

        private void UnSubscribeKeys() {
            KeyMapper.RemoveListener(KeyEvent.SAVE, OnBtnSave);
            KeyMapper.RemoveListener(KeyEvent.RESET, OnBtnReset);
            KeyMapper.RemoveListener(KeyEvent.SHOW_UI, SwithcUI);
            KeyMapper.RemoveListener(KeyEvent.LEFT, _calibrationFields.PosX.btnDec.onClick.Invoke);
            KeyMapper.RemoveListener(KeyEvent.RIGHT, _calibrationFields.PosX.btnInc.onClick.Invoke);
            KeyMapper.RemoveListener(KeyEvent.DOWN, _calibrationFields.PosY.btnDec.onClick.Invoke);
            KeyMapper.RemoveListener(KeyEvent.UP, _calibrationFields.PosY.btnInc.onClick.Invoke);
            KeyMapper.RemoveListener(KeyEvent.ZOOM_IN, _calibrationFields.PosZ.btnInc.onClick.Invoke);
            KeyMapper.RemoveListener(KeyEvent.ZOOM_OUT, _calibrationFields.PosZ.btnDec.onClick.Invoke);
        }

        private void SwithcUI() {
            _ui.Hide = !_ui.Hide;
        }
#endregion

#region Calibration Modes
        public void SwitchMode(CalibrationMode mode) {
            if (mode != CalibrationMode.TEST && _tglTest.isOn)
                _tglTest.SetIsOnWithoutNotify(false);
            
            if (mode == CalibrationMode.AUTOMATIC)
                _automatic.StartCalibration();
            
            _calibrationImg.SetActive(mode != CalibrationMode.TEST);
            _imgColor.gameObject.SetActive(mode == CalibrationMode.TEST);
            _settingsAndBtns.SetActive(mode != CalibrationMode.AUTOMATIC);
            MultiMonitor.SetTargetDisplay(SandboxCam, mode == CalibrationMode.TEST ? 1 : 0);
            _camMonitor.gameObject.SetActive(mode != CalibrationMode.AUTOMATIC || MultiMonitor.MonitorsCount > 1);
            
            _sandboxMesh.GetComponent<SandboxVisualizerBase>().SetEnable(mode == CalibrationMode.TEST);
            _sandboxMesh.GetComponent<SandboxVisualizerColor>().SetEnable(mode != CalibrationMode.TEST);

            Wall.SwitchMode(mode);
        }

        private void OnBtnStartAutomatic() {
            SwitchMode(CalibrationMode.AUTOMATIC);
        }
        
        private void OnTglTest(bool on) {
            if (on)
                SwitchMode(CalibrationMode.TEST);
            else
                SwitchMode(CalibrationMode.MANUAL);
        }
#endregion

#region UI
        private void InitUI() {
            _btnSave.onClick.AddListener(OnBtnSave);
            _btnCancel.onClick.AddListener(OnBtnCancel);
            _btnReset.onClick.AddListener(OnBtnReset);
            _btnAutomatic.onClick.AddListener(OnBtnStartAutomatic);
            _tglTest.onValueChanged.AddListener(OnTglTest);
            _tglTest.gameObject.SetActive(MultiMonitor.MonitorsCount > 1);
            
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

        private void InitSlider(SliderField fld, UnityAction<float> act) {
            void ChangeTxt(float val) {
                fld.txtVal.text = (fld.sl.wholeNumbers)
                    ? val.ToString()
                    : val.ToString("0.000");
            }
            ChangeTxt(fld.sl.value);
            fld.sl.onValueChanged.AddListener(ChangeTxt);
            fld.sl.onValueChanged.AddListener(val => { UpdatePrefFromUI(val, act); });
            fld.btnInc.onClick.AddListener(CreateOnBtnIncDec(fld.sl, 1.0f));
            fld.btnDec.onClick.AddListener(CreateOnBtnIncDec(fld.sl, -1.0f));
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
            var fov = MathHelper.IsoscelesTriangleAngle(h, d);
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
        
        private void OnProjectorChanged() {
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
#endregion
    }
}