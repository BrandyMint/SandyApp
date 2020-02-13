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
        [SerializeField] private GameObject _manualCalibrateImg;
        [SerializeField] private GameObject _autoCalibrateImg;
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

            DepthSensorManager.OnInitialized += OnDepthSensorAvailable;
            if (DepthSensorManager.IsInitialized())
                OnDepthSensorAvailable();

            KeyMapper.AddListener(KeyEvent.SWITCH_MODE, SwitchMode);
            KeyMapper.AddListener(KeyEvent.SHOW_UI, SwhitchUI);
            KeyMapper.AddListener(KeyEvent.SWITCH_TARGET, SwitchTarget);

            SwhitchUI();
        }

        private void OnDestroy() {
            KeyMapper.RemoveListener(KeyEvent.SHOW_UI, SwhitchUI);
            KeyMapper.RemoveListener(KeyEvent.SWITCH_MODE, SwitchMode);
            KeyMapper.RemoveListener(KeyEvent.SWITCH_TARGET, SwitchTarget);
            
            DepthSensorManager.OnInitialized -= OnDepthSensorAvailable;
            if (DepthSensorManager.IsInitialized()) {
                UnSubscribeDevice(DepthSensorManager.Instance.Device);
            }
            
            ActivateMode(null);
        }

        private static DepthSensorDevice GetDeviceIfAvailable() {
            if (DepthSensorManager.IsInitialized()) {
                return DepthSensorManager.Instance.Device;
            }
            return null;
        }

        private void OnDepthSensorAvailable() {
            DepthSensorManager.Instance.Device.OnClose += UnSubscribeDevice;
            SwitchMode();
        }

        private void UnSubscribeDevice(DepthSensorDevice device) {
            device.OnClose -= UnSubscribeDevice;
            ActivateMode(null);
        }

        private void SwitchMode() {
            _currentModeId = (_currentModeId + 1) % _modes.Length;
            ActivateMode(_modes[_currentModeId]);
        }

        private void SwitchTarget() {
            var manual = _manualCalibrateImg.activeSelf;
            var auto = _autoCalibrateImg.activeSelf;
            _manualCalibrateImg.SetActive(!manual && !auto);
            _autoCalibrateImg.SetActive(manual);
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
                        if (!sensor.AnySubscribedToNewFrames)
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

        private void SwhitchUI() {
            _ui.SetActive(!_ui.activeSelf);
        }
    }
}