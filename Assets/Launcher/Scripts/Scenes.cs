using System.Collections.Generic;
using System.Linq;
using Launcher.KeyMapping;
using Launcher.MultiMonitorSupport;
using UINotify;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Launcher {
    public class Scenes : MonoBehaviour {
        private const string _TXT_NO_MONITORS_TITTLE = "Проектор подключен?";
        private const string _TXT_NO_MONITORS = "Убедитесь, что проектор подключен как расширенный экран, монитор компьютера активен и является главным. Перезапустите приложение.";

        [SerializeField] private string _sceneMainPath;
        //[SerializeField] private string _sceneProjectorParamsPath;
        [SerializeField] private string _sceneCalibrationPath;
        [SerializeField] private string _sceneSandboxCalibrationPath;
        [SerializeField] private string _sceneCalibrationViewPath;
        
        private static Scenes _instance;
        
        //private readonly Stack<int> _scenes = new Stack<int>();
        private Notify.Control _notifyNoMonitors;
        private int _currentGameSceneId;
        private readonly List<Scene> _toUnload = new List<Scene>();
        /*private CalibrationStep[] _calibrationSteps;

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
        }*/

        private void Awake() {
            Debug.Log("version: " + Application.version);
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            /*_calibrationSteps = new[] {
                new CalibrationStep(new ProjectorParams(), _sceneProjectorParamsPath),
                new CalibrationStep(new CalibrationParams(), _sceneCalibrationPath)
            };*/
            
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;

            MultiMonitor.OnNotEnoughMonitors += OnNotEnoughMonitors;
            
            KeyMapper.AddListener(KeyEvent.OPEN_VIEWER, OpenViewer, EventLayer.GLOBAL);
            KeyMapper.AddListener(KeyEvent.OPEN_CALIBRATION, OpenCalibration, EventLayer.GLOBAL);
            KeyMapper.AddListener(KeyEvent.OPEN_SANDBOX_CALIBRATION, OpenSandboxCalibration, EventLayer.GLOBAL);
            KeyMapper.AddListener(KeyEvent.OPEN_NEXT_GAME, OpenNextGame, EventLayer.GLOBAL);
            KeyMapper.AddListener(KeyEvent.OPEN_PREV_GAME, OpenPrevGame, EventLayer.GLOBAL);
            KeyMapper.AddListener(KeyEvent.BACK, GoBack, EventLayer.GLOBAL);
            KeyMapper.AddListener(KeyEvent.EXIT, Application.Quit, EventLayer.GLOBAL);
            //GoCalibrationBefore(SceneManager.GetActiveScene().path);
        }

        private void OnDestroy() {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            MultiMonitor.OnNotEnoughMonitors -= OnNotEnoughMonitors;
            
            KeyMapper.RemoveListener(KeyEvent.EXIT, Application.Quit, EventLayer.GLOBAL);
            KeyMapper.RemoveListener(KeyEvent.BACK, GoBack, EventLayer.GLOBAL);
            KeyMapper.RemoveListener(KeyEvent.OPEN_PREV_GAME, OpenPrevGame, EventLayer.GLOBAL);
            KeyMapper.RemoveListener(KeyEvent.OPEN_NEXT_GAME, OpenNextGame, EventLayer.GLOBAL);
            KeyMapper.RemoveListener(KeyEvent.OPEN_CALIBRATION, OpenCalibration, EventLayer.GLOBAL);
            KeyMapper.RemoveListener(KeyEvent.OPEN_VIEWER, OpenViewer, EventLayer.GLOBAL);
            KeyMapper.RemoveListener(KeyEvent.OPEN_SANDBOX_CALIBRATION, OpenSandboxCalibration, EventLayer.GLOBAL);
            
            _instance = null;
        }
        
        public static string CurrentGamePath => GamesList.GetDescription(_instance._currentGameSceneId).ScenePath;

        private void OnNotEnoughMonitors() {
            if (_notifyNoMonitors == null)
                _notifyNoMonitors = Notify.Show(
                    Style.FAIL, LifeTime.INFINITY, 
                    _TXT_NO_MONITORS_TITTLE, _TXT_NO_MONITORS
                );
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
            if (IsGameScene(scene))
                _currentGameSceneId = GamesList.GetId(scene);
            foreach (var unloadScene in _toUnload) {
                SceneManager.UnloadSceneAsync(unloadScene);
            }
            _toUnload.Clear();
        }
        
        private void OnSceneUnloaded(Scene scene) {
            if (!_toUnload.Any())
                Resources.UnloadUnusedAssets();
        }

        public static void GoBack() {
            if (_instance != null)
                _instance.GoBackInternal();
        }

        /*private static bool GoCalibrationBefore(string scenePath) {
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
        }*/

        public static void GoToWithChecking(string scenePath) {
            //if (!GoCalibrationBefore(scenePath))
            var currentScene = SceneManager.GetActiveScene();
            if (currentScene.path != scenePath) {
                SceneManager.LoadScene(scenePath, LoadSceneMode.Additive);
                if (_instance != null)
                    _instance._toUnload.Add(currentScene);
            }
        }

        private void GoBackInternal() {
            /*if (_scenes.Count < 2)
                return;
            _scenes.Pop();
            var sceneId = _scenes.Peek();*/
            
            //GoToWithCheckCalibration(sceneId);
            //for now, just go to main scene
            if (!IsGameScene(SceneManager.GetActiveScene()))
                GoToWithChecking(GamesList.GetDescription(_currentGameSceneId).ScenePath);
            else
                GoToWithChecking(_sceneMainPath);
        }
        
        private bool IsCalibrationScene(Scene scene) {
            return IsCalibrationScene(scene.path);
        }

        private bool IsCalibrationScene(string scenePath) {
            return scenePath == _sceneSandboxCalibrationPath
                   || scenePath == _sceneCalibrationPath
                   || scenePath == _sceneCalibrationViewPath;
            //|| scenePath == _sceneProjectorParamsPath;
        }
        
        private bool IsGameScene(Scene scene) {
            return IsGameScene(scene.path);
        }
        
        private bool IsGameScene(string scenePath) {
            return GamesList.IsGame(scenePath);
        }

        private void OpenViewer() {
            GoToWithChecking(_sceneCalibrationViewPath);
        }
        
        private void OpenCalibration() {
            GoToWithChecking(_sceneCalibrationPath);
        }
        
        private void OpenSandboxCalibration() {
            GoToWithChecking(_sceneSandboxCalibrationPath);
        }

        private void OpenNextGame() {
            _currentGameSceneId = (_currentGameSceneId + 1) % GamesList.Count;
            GoToWithChecking(GamesList.GetDescription(_currentGameSceneId).ScenePath);
        }
        
        private void OpenPrevGame() {
            _currentGameSceneId = (GamesList.Count + _currentGameSceneId - 1) % GamesList.Count;
            GoToWithChecking(GamesList.GetDescription(_currentGameSceneId).ScenePath);
        }
    }
}