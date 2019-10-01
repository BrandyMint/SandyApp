using System.Collections.Generic;
using Launcher.MultiMonitorSupport;
using UINotify;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Launcher {
    public class Scenes : MonoBehaviour {
        private const string _TXT_NO_MONITORS_TITTLE = "Проектор подключен?";
        private const string _TXT_NO_MONITORS = "Убедитесь, что проектор подключен как расширенный экран, монитор компьютера активен и является главным. Перезапустите приложение.";
        
        private static Scenes _instance;
        
        private readonly Stack<int> _scenes = new Stack<int>();
        private Notify.Control _notifyNoMonitors;

        private void Awake() {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            _scenes.Push(SceneManager.GetActiveScene().buildIndex);
            SceneManager.sceneLoaded += OnSceneLoaded;

            MultiMonitor.OnNotEnoughMonitors += OnNotEnoughMonitors;
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
    }
}