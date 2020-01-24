using System;
using System.IO;
using DepthSensor;
using DepthSensor.Recorder;
using Launcher.KeyMapping;
using UINotify;
using UnityEngine;

namespace Launcher.Scripts {
    public class RecordController : MonoBehaviour {
        public bool Recording => _recorder.Recording;

        private readonly DepthSensorRecorder _recorder = new DepthSensorRecorder();
        private string _path;
        private string _currRecordingName;
        private Notify.Control _notifyRecord;

        private void Awake() {
            _path = Path.Combine(Application.persistentDataPath, "Records");
            if (!Directory.Exists(_path)) {
                Directory.CreateDirectory(_path);
            }
        }

        private void Start() {
            KeyMapper.AddListener(KeyEvent.RECORD, OnBtnStartStopRecord);
            KeyMapper.AddListener(KeyEvent.PLAY_RECORD, OnBtnStartStopPlay);
        }

        private void OnDestroy() {
            KeyMapper.RemoveListener(KeyEvent.RECORD, OnBtnStartStopRecord);
            KeyMapper.RemoveListener(KeyEvent.PLAY_RECORD, OnBtnStartStopPlay);
            _recorder?.Dispose();
        }

        private void OnBtnStartStopRecord() {
            if (Recording)
                StopRecord();
            else
                StartRecord();
        }

        private void OnBtnStartStopPlay() {
            
        }

        public bool StartRecord() {
            if (!DepthSensorManager.IsInitialized()) {
                return false;
            }

            ClearRecordInfo();
            _currRecordingName = DateTime.UtcNow.ToLocalTime().ToString("yyyy.MM.dd_HH.mm.ss");
            var started = _recorder.StartRecord(
                DepthSensorManager.Instance.Device,
                Path.Combine(_path, _currRecordingName),
                true
            );
            if (started) {
                _notifyRecord = Notify.Show(Style.INFO, LifeTime.INFINITY, "Запись...");
            } else {
                _notifyRecord = Notify.Show(Style.FAIL, LifeTime.SHORT, "Запись недоступна!");
            }
            return started;
        }

        public void StopRecord() {
            ClearRecordInfo();
            if (_recorder != null && _recorder.Recording) {
                _recorder.StopRecord();
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