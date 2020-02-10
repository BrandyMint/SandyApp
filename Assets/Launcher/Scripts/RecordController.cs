using System;
using System.Collections.Generic;
using System.IO;
using DepthSensor;
using DepthSensor.Device;
using DepthSensor.Recorder;
using Launcher.KeyMapping;
using UINotify;
using UnityEngine;

namespace Launcher.Scripts {
    public class RecordController : MonoBehaviour {
        private ulong _BYTES_IN_GB = 1024 * 1024 * 1024;
        
        public event Action<string, string> OnRecordFinished;
        public bool Recording => _recorder.Recording;
        public object RecordsPath => _path;

        private readonly DepthSensorRecorder _recorder = new DepthSensorRecorder();
        private string _path;
        private string _currRecordingName;
        private Notify.Control _notifyRecord;
        private Notify.Params _notifyRecordParams;

        private void Awake() {
            _path = Path.Combine(Application.persistentDataPath, "..", "SandboxRecords");
            _notifyRecordParams = new Notify.Params {
                style = Style.INFO,
                title = "Запись...",
                time = LifeTime.INFINITY
            };
        }
        
        private IEnumerable<SerializableParams> AlsoSaveConfigs() {
            yield return Prefs.App;
            yield return Prefs.Projector;
            yield return Prefs.Calibration;
            yield return Prefs.Sandbox;
            yield return Prefs.Landscape;
        }

        private void Start() {
            _recorder.OnFail += OnRecordFail;
            DepthSensorManager.OnInitialized += OnDepthSensorAvailable;
            if (DepthSensorManager.IsInitialized())
                OnDepthSensorAvailable();
            KeyMapper.AddListener(KeyEvent.RECORD, OnBtnStartStopRecord);
            KeyMapper.AddListener(KeyEvent.PLAY_RECORD, OnBtnStartStopPlay);
        }

        private void OnDestroy() {
            KeyMapper.RemoveListener(KeyEvent.RECORD, OnBtnStartStopRecord);
            KeyMapper.RemoveListener(KeyEvent.PLAY_RECORD, OnBtnStartStopPlay);
            DepthSensorManager.OnInitialized -= OnDepthSensorAvailable;
            if (DepthSensorManager.IsInitialized()) {
                UnSubscribeDevice(DepthSensorManager.Instance.Device);
            }
            if (_recorder != null) {
                _recorder.Dispose();
                _recorder.OnFail -= OnRecordFail;
            }
        }

        private void OnDepthSensorAvailable() {
            DepthSensorManager.Instance.Device.OnClose += UnSubscribeDevice;
        }

        private void UnSubscribeDevice(DepthSensorDevice device) {
            device.OnClose -= UnSubscribeDevice;
            StopRecord();
        }

        private void OnBtnStartStopRecord() {
            if (Recording)
                StopRecord();
            else
                StartRecord();
        }

        private void OnBtnStartStopPlay() {
            //TODO: start play
        }

        public void StartRecord() {
            if (!DepthSensorManager.IsInitialized()) {
                return;
            }

            ClearRecordInfo();
            _currRecordingName = DateTime.UtcNow.ToLocalTime().ToString("yyyy.MM.dd_HH.mm.ss");
            _recorder.StartRecord(
                DepthSensorManager.Instance.Device,
                Path.Combine(_path, _currRecordingName),
                true
            );
            if (_recorder.Recording) {
                _notifyRecordParams.text = null;
                _notifyRecord = Notify.Show(_notifyRecordParams);
            }
        }

        public void StopRecord() {
            ClearRecordInfo();
            if (_recorder != null && _recorder.Recording) {
                _recorder.StopRecord();
                var configPath = Path.Combine(_path, _currRecordingName, "Configs");
                if (!Directory.Exists(configPath))
                    Directory.CreateDirectory(configPath);
                foreach (var config in AlsoSaveConfigs()) {
                    config.SaveCopyTo(configPath);
                }
                OnRecordFinished?.Invoke(_path, _currRecordingName);
            }
        }

        private void OnRecordFail() {
            ClearRecordInfo();
            Notify.Show(Style.FAIL, LifeTime.SHORT, "Запись недоступна!");
        }

        private void Update() {
            if (_recorder.Recording && _notifyRecord != null) {
                _notifyRecordParams.text = GetRecordInfo();
                _notifyRecord.Set(_notifyRecordParams);
            }
        }
        
        private string GetRecordInfo() {
            var bytes = _recorder.StoredBytes;
            if (bytes < 1) {
                return "Подготовка...";
            } else {
                return $"{(float)bytes / _BYTES_IN_GB:F2} GB";
            }
        }

        private void ClearRecordInfo() {
            if (_notifyRecord != null) {
                _notifyRecord.Hide();
                _notifyRecord = null;
            }
        }
    }
}