using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Launcher {
    public class Scenes : MonoBehaviour {
        private static Scenes _instance;
        
        private readonly Stack<int> _scenes = new Stack<int>();

        private void Awake() {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            _scenes.Push(SceneManager.GetActiveScene().buildIndex);
            SceneManager.sceneLoaded += OnSceneLoaded; 
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