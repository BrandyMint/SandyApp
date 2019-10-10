using System.Collections.Generic;
using DepthSensorCalibration;
using DepthSensorSandbox;
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
        private CalibrationStep[] _calibrationSteps;

        private class CalibrationStep {
            public string scenePath;
            private SerializableParams _params;
            
            public bool CheckIsCompleted() {
                return _params.Load(false);
            }

            public CalibrationStep(SerializableParams p, string scenePath) {
                this.scenePath = scenePath;
                _params = p;
            }
        }

        private void Awake() {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            _calibrationSteps = new[] {
                new CalibrationStep(new ProjectorParams(), _sceneProjectorParamsPath),
                new CalibrationStep(new CalibrationParams(), _sceneCalibrationPath)
            };
            
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

        private static bool GoCalibrationBefore(string scenePath) {
            if (_instance == null)
                return false;
            foreach (var step in _instance._calibrationSteps) {
                if (!step.CheckIsCompleted()) {
                    if (scenePath != step.scenePath) {
                        Debug.Log($"Before {scenePath} GoTo calibration: {step.scenePath}");
                        SceneManager.LoadScene(step.scenePath);
                        return true;
                    }
                    return false;
                }
            }
            return false;
        }

        public static void GoToWithCheckCalibration(int sceneId) {
            if (!GoCalibrationBefore(SceneManager.GetSceneAt(sceneId).path))
                SceneManager.LoadScene(sceneId);
        }
        
        public static void GoToWithCheckCalibration(string scenePath) {
            if (!GoCalibrationBefore(scenePath))
                SceneManager.LoadScene(scenePath);
        }

        private void GoBackInternal() {
            if (_scenes.Count < 2)
                return;
            _scenes.Pop();
            var sceneId = _scenes.Peek();
            GoToWithCheckCalibration(sceneId);
        }

        private void OpenProjectorParams() {
            GoToWithCheckCalibration(_sceneProjectorParamsPath);
        }
        
        private void OpenCalibration() {
            GoToWithCheckCalibration(_sceneCalibrationPath);
        }
    }
}