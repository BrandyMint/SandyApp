﻿using UnityEngine;

namespace Launcher {
    public class GameDescription : ScriptableObject {
        [HideInInspector]
        [SerializeField] private string _scenePath;
        [TextArea]
        [SerializeField] private string _sceneName;
        [SerializeField] private Sprite _icon;

        public string ScenePath {
            get => _scenePath;
            set => _scenePath = value;
        }
        
        public string SceneName => _sceneName;

        public Sprite Icon => _icon;
    }
}