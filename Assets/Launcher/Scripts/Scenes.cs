using System.Collections.Generic;
using Launcher.KeyMapping;
using Launcher.MultiMonitorSupport;
using UINotify;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Launcher {
    public class Scenes : MonoBehaviour {
        private const string _TXT_NO_MONITORS_TITTLE = "Проектор подключен?";
        private const string _TXT_NO_MONITORS = "Убедитесь, что проектор подключен как расширенный экран, монитор компьютера активен и является главным. Перезапустите приложение.";

        [SerializeField] private string _sceneProjectorParamsPath;
        [SerializeField] private string _sceneCalibrationPath;
        
        private static Scenes _instance;
        
        private readonly Stack<int> _scenes = new Stack<int>();
        private Notify.Control _notifyNoMonitors;

        private void Awake() {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            _scenes.Push(SceneManager.GetActiveScene().buildIndex);
            SceneManager.sceneLoaded += OnSceneLoaded;

            MultiMonitor.OnNotEnoughMonitors += OnNotEnoughMonitors;
            
            KeyMapper.AddListener(KeyEvent.OPEN_PROJECTOR_PARAMS, OpenProjectorParams);
            KeyMapper.AddListener(KeyEvent.OPEN_CALIBRATION, OpenCalibration);
            KeyMapper.AddListener(KeyEvent.BACK, GoBack);
        }
        
        private void OnDestroy() {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            MultiMonitor.OnNotEnoughMonitors -= OnNotEnoughMonitors;
            
            KeyMapper.RemoveListener(KeyEvent.BACK, GoBack);
            KeyMapper.RemoveListener(KeyEvent.OPEN_CALIBRATION, OpenCalibration);
            KeyMapper.RemoveListener(KeyEvent.OPEN_PROJECTOR_PARAMS, OpenProjectorParams);
        }

        private void OnNotEnoughMonitors() {
            if (_notifyNoMonitors == null)
                _notifyNoMonitors = Notify.Show(
                    Style.FAIL, LifeTime.INFINITY, 
                    _TXT_NO_MONITORS_TITTLE, _TXT_NO_MONITORS
                );
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
            if (!_scenes.Contains(scene.buildIndex))
                _scenes.Push(scene.buildIndex);
        }

        public static void GoBack() {
            if (_instance != null)
                _instance.GoBackInternal();
        }

        private void GoBackInternal() {
            if (_scenes.Count < 2)
                return;
            _scenes.Pop();
            var scene = _scenes.Peek();
            SceneManager.LoadScene(scene);
        }

        private void OpenProjectorParams() {
            SceneManager.LoadScene(_sceneProjectorParamsPath);
        }
        
        private void OpenCalibration() {
            SceneManager.LoadScene(_sceneCalibrationPath);
        }
    }
}