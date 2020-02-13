using System.IO;
using System.Linq;
using DepthSensor;
using DepthSensor.Device;
using Launcher.KeyMapping;
using SFB;
using UINotify;
using UnityEngine;

namespace Launcher {
    public class RecordPlayerController : MonoBehaviour {
        private string _recordsPath;
        private Notify.Params _notifyPlayParams;
        private Notify.Control _notifyPlay;
        private RecordPlayerDevice _player;

        private void Awake() {
            _recordsPath = Path.Combine(Application.persistentDataPath, "..", "SandboxRecords");
            _notifyPlayParams = new Notify.Params {
                style = Style.INFO,
                time = LifeTime.INFINITY
            };
        }

        private void Start() {
            DepthSensorManager.OnInitialized += OnDepthSensorAvailable;
            if (DepthSensorManager.IsInitialized())
                OnDepthSensorAvailable();
            KeyMapper.AddListener(KeyEvent.PLAY_RECORD, OnBtnStartPlay);
        }

        private void OnDestroy() {
            KeyMapper.RemoveListener(KeyEvent.PLAY_RECORD, OnBtnStartPlay);
            DepthSensorManager.OnInitialized -= OnDepthSensorAvailable;
            if (DepthSensorManager.IsInitialized()) {
                UnSubscribeDevice(DepthSensorManager.Instance.Device);
            }
        }

        private void OnDepthSensorAvailable() {
            var device = DepthSensorManager.Instance.Device;
            device.OnClose += UnSubscribeDevice;
            _player = DepthSensorManager.Instance.Device as RecordPlayerDevice;
            if (_player != null)
                OnStartPlay();
        }

        private void UnSubscribeDevice(DepthSensorDevice device) {
            device.OnClose -= UnSubscribeDevice;
            OnStopPlay();
            _player = null;
        }

        private void OnBtnStartPlay() {
            var path = StandaloneFileBrowser.OpenFolderPanel(
                "Открыть запись",
                _recordsPath, 
                false).First();
            DepthSensorManager.Instance.OpenRecord(path);
        }

        private void OnStartPlay() {
            ClearUI();
            _notifyPlayParams.title = $"Воспроизведение {_player.Name}";
            _notifyPlay = Notify.Show(_notifyPlayParams);
            var configsPath = Path.Combine(_player.RecordPath, "Configs");
            foreach (var config in Prefs.RecordPlayerOverrides()) {
                config.OverrideLoad(configsPath);
            }
        }

        private void OnStopPlay() {
            ClearUI();
            foreach (var param in Prefs.RecordPlayerOverrides()) {
                param.OverrideLoad(null);
            }
        }
        
        private void ClearUI() {
            if (_notifyPlay != null) {
                _notifyPlay.Hide();
                _notifyPlay = null;
            }
        }
    }
}