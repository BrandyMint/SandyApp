using System;
using DepthSensor;
using DepthSensor.Buffer;
using DepthSensor.Device;
using DepthSensor.Sensor;
using Launcher.KeyMapping;
using UnityEngine;
using UnityEngine.UI;

namespace DepthSensorCalibration {
    public class ViewerController : MonoBehaviour {
        [SerializeField] private RawImage _view;
        [SerializeField] private GameObject _ui;
        [SerializeField] private Mode[] _modes;
        [SerializeField] private int _minBuffersCount = 3;

        public enum Source {
            COLOR,
            DEPTH,
            INFRARED
        }

        [Serializable]
        public class Mode {
            public Source source;
            public Material material;
        }

        private int _currentModeId = -1;
        private Mode _currentMode;

        private void Start() {
            _view.gameObject.SetActive(false);

            if (GetDeviceIfAvailable() == null)
                DepthSensorManager.Instance.OnInitialized += OnDepthSensorAvailable;
            else
                OnDepthSensorAvailable();

            KeyMapper.AddListener(KeyEvent.SWITCH_MODE, SwitchMode);
            KeyMapper.AddListener(KeyEvent.SHOW_UI, SwhitchUI);

            SwhitchUI();
        }

        private void OnDestroy() {
            KeyMapper.RemoveListener(KeyEvent.SHOW_UI, SwhitchUI);
            KeyMapper.RemoveListener(KeyEvent.SWITCH_MODE, SwitchMode);
            
            if (DepthSensorManager.Instance != null)
                DepthSensorManager.Instance.OnInitialized -= OnDepthSensorAvailable;
            
            ActivateMode(null);
        }

        private static DepthSensorDevice GetDeviceIfAvailable() {
            var dsm = DepthSensorManager.Instance;
            if (dsm != null && dsm.Device != null && dsm.Device.IsAvailable()) {
                return dsm.Device;
            }
            return null;
        }

        private void OnDepthSensorAvailable() {
            SwitchMode();
        }

        private void SwitchMode() {
            _currentModeId = (_currentModeId + 1) % _modes.Length;
            ActivateMode(_modes[_currentModeId]);
        }

        private void ActivateMode(Mode mode) {
            _currentMode = mode;
            foreach (Source source in Enum.GetValues(typeof(Source))) {
                var sensor = GetSensor(source);
                if (sensor != null) {
                    if (mode != null && mode.source == source) {
                        _view.gameObject.SetActive(false);
                        if (sensor.BuffersCount < _minBuffersCount)
                            sensor.BuffersCount = _minBuffersCount;
                        sensor.OnNewFrame += OnNewFrame;
                        sensor.Active = true;
                        _view.material = mode.material;
                    } else {
                        sensor.OnNewFrame -= OnNewFrame;
                        sensor.Active = false;
                    }
                }
            }
        }

        private void OnNewFrame(ISensor iSensor) {
            var sensor = (ISensor<ITextureBuffer>) iSensor;
            var buffer = sensor.GetNewest();
            buffer.UpdateTexture();
            _view.texture = buffer.GetTexture();
            _view.gameObject.SetActive(true);
        }

        private static AbstractSensor GetSensor(Source source) {
            var device = GetDeviceIfAvailable();
            if (device != null) {
                switch (source) {
                    case Source.COLOR:
                        return device.Color;
                    case Source.DEPTH:
                        return device.Depth;
                    case Source.INFRARED:
                        return device.Infrared;
                }
            }

            return null;
        }

        private void FixedUpdate() {
            if (_currentMode != null) {
                var sensor = GetSensor(_currentMode.source);
                if (sensor != null) {
                    sensor.Active = true;
                }
            }
        }

        private void SwhitchUI() {
            _ui.SetActive(!_ui.activeSelf);
        }
    }
}