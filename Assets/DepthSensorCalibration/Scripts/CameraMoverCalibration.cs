using Launcher.KeyMapping;
using UnityEngine;
using Utilities;

namespace DepthSensorCalibration {
    public class CameraMoverCalibration : MonoBehaviour {
        private const float _INC_DEC_STEPS = 0.01f;
        private const float _Z_MULTIPLE_STEP = 3f;

        private readonly ProjectorParams _projector = new ProjectorParams();

        private enum Direct {
            X = 0,
            Y = 1,
            Z = 2
        }

        private void Start() {
            _projector.OnChanged += OnProjectorChanged;
            Prefs.Calibration.OnChanged += OnCalibrationChanged;
            OnProjectorChanged();
            OnCalibrationChanged();
            
            SubscribeKeys();
        }

        private void OnDestroy() {
            _projector.OnChanged -= OnProjectorChanged;
            Prefs.Calibration.OnChanged -= OnCalibrationChanged;
            UnSubscribeKeys();
            Save();
        }

#region Buttons

        private void Save() {
            if (IsSaveAllowed()) {
                Prefs.NotifySaved(Prefs.Calibration.Save());
                //Scenes.GoBack();
            }
        }

        private bool IsSaveAllowed() {
            return Prefs.Calibration.HasChanges || !Prefs.Calibration.HasFile;
        }

        private void OnBtnReset() {
            Prefs.Calibration.Reset();
            OnProjectorChanged();
        }

        private void SubscribeKeys() {
            KeyMapper.AddListener(KeyEvent.RESET, OnBtnReset);
            KeyMapper.AddListener(KeyEvent.LEFT, MoveLeft);
            KeyMapper.AddListener(KeyEvent.RIGHT, MoveRight);
            KeyMapper.AddListener(KeyEvent.DOWN, MoveDown);
            KeyMapper.AddListener(KeyEvent.UP, MoveUp);
            KeyMapper.AddListener(KeyEvent.ZOOM_IN, MoveForward);
            KeyMapper.AddListener(KeyEvent.ZOOM_OUT, MoveBackward);
            KeyMapper.AddListener(KeyEvent.WIDE_PLUS, WidePlus);
            KeyMapper.AddListener(KeyEvent.WIDE_MINUS, WideMinus);
        }

        private void UnSubscribeKeys() {
            KeyMapper.RemoveListener(KeyEvent.RESET, OnBtnReset);
            KeyMapper.RemoveListener(KeyEvent.LEFT, MoveLeft);
            KeyMapper.RemoveListener(KeyEvent.RIGHT, MoveRight);
            KeyMapper.RemoveListener(KeyEvent.DOWN, MoveDown);
            KeyMapper.RemoveListener(KeyEvent.UP, MoveUp);
            KeyMapper.RemoveListener(KeyEvent.ZOOM_IN, MoveForward);
            KeyMapper.RemoveListener(KeyEvent.ZOOM_OUT, MoveBackward);
            KeyMapper.RemoveListener(KeyEvent.WIDE_PLUS, WidePlus);
            KeyMapper.RemoveListener(KeyEvent.WIDE_MINUS, WideMinus);
        }

        private void MovePosition(Direct direct, float k) {
            var pos = Prefs.Calibration.Position;
            var step = k * _INC_DEC_STEPS * Time.deltaTime;
            if (direct == Direct.Z)
                step *= _Z_MULTIPLE_STEP;
            pos[(int) direct] += step;

            Prefs.Calibration.Position = pos;
        }

        private void ModifyWide(float k) {
            Prefs.Calibration.WideMultiply += k * _INC_DEC_STEPS * Time.deltaTime;
        }

        private void MoveLeft() {
            MovePosition(Direct.X, -1f);
        }

        private void MoveRight() {
            MovePosition(Direct.X, 1f);
        }

        private void MoveDown() {
            MovePosition(Direct.Y, -1f);
        }

        private void MoveUp() {
            MovePosition(Direct.Y, 1f);
        }

        private void MoveForward() {
            MovePosition(Direct.Z, 1f);
        }

        private void MoveBackward() {
            MovePosition(Direct.Z, -1f);
        }
        
        private void WideMinus() {
            ModifyWide(-1f);
        }

        private void WidePlus() {
            ModifyWide(1f);
        }

#endregion 

        public static void UpdateCalibrationFov(ProjectorParams projector) {
            var aspect = projector.Width / projector.Height;
            var s = projector.Diagonal;
            var d = projector.Distance;
            var h = s / Mathf.Sqrt((aspect * aspect + 1f));
            var fov = MathHelper.IsoscelesTriangleAngle(h, d);
            Prefs.Calibration.Fov = fov;
        }

        private void OnProjectorChanged() {
            UpdateCalibrationFov(_projector);
        }

        private void OnCalibrationChanged() { }
    }
}