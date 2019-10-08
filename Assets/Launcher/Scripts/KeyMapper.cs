﻿using System;
using System.Collections.Generic;
using UnityEngine;

namespace Launcher {
    public class KeyMapper : MonoBehaviour {
        public static event Action OnFlipDisplay;
        public static event Action OnSceneProjectorParams;
        public static event Action OnSceneCalibration;
        public static event Action OnResetSettings;
        public static event Action OnSaveSettings;

        private static KeyMapper _instance;

        public class KeyBind {
            public KeyCode key;
            public Action action;

            public KeyBind(KeyCode key, Action action) {
                this.key = key;
                this.action = action;
            }
        }

        private readonly List<KeyBind> _bindsDown = new List<KeyBind>();
        private readonly List<KeyBind> _binds = new List<KeyBind>();

        private void Awake() {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            InitMapper();
        }

        private void InitMapper() {
            _bindsDown.Add(new KeyBind(KeyCode.F2, () => OnSaveSettings?.Invoke()));
            _bindsDown.Add(new KeyBind(KeyCode.F5, () => OnResetSettings?.Invoke()));
            _bindsDown.Add(new KeyBind(KeyCode.F10, () => OnSceneCalibration?.Invoke()));
            _bindsDown.Add(new KeyBind(KeyCode.F11, () => OnSceneProjectorParams?.Invoke()));
            _bindsDown.Add(new KeyBind(KeyCode.F12, () => OnFlipDisplay?.Invoke()));
        }

        private void Update() {
            if (Input.anyKey) {
                ProcessInput(_binds, Input.GetKey);
            }
            if (Input.anyKeyDown) {
                ProcessInput(_bindsDown, Input.GetKeyDown);
            }
        }

        private static void ProcessInput(IEnumerable<KeyBind> binds, Func<KeyCode, bool> checkKey) {
            foreach (var bind in binds) {
                if (checkKey(bind.key))
                    bind.action.Invoke();
            }
        }
    }

    internal class OnSaveSettings { }
}