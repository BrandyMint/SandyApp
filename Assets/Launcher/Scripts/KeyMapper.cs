using System;
using System.Collections.Generic;
using UnityEngine;

namespace Launcher {
    public class KeyMapper : MonoBehaviour {
        public static event Action OnFlipDisplay;
        public static event Action OnSceneProjectorParams;
        public static event Action OnSceneCalibration;
        public static event Action OnResetSettings;
        public static event Action OnSaveSettings;
        public static event Action OnGoBack;
        public static event Action OnSwitchMode;
        public static event Action OnSwitchUI;

        public static event Action OnUp;
        public static event Action OnDown;
        public static event Action OnLeft;
        public static event Action OnRight;
        public static event Action OnZoomIn;
        public static event Action OnZoomOut;

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
            _bindsDown.Add(new KeyBind(KeyCode.Tab, () => OnSwitchMode?.Invoke()));
            _bindsDown.Add(new KeyBind(KeyCode.Escape, () => OnGoBack?.Invoke()));
            
            _bindsDown.Add(new KeyBind(KeyCode.F2, () => OnSaveSettings?.Invoke()));
            _bindsDown.Add(new KeyBind(KeyCode.F3, () => OnSwitchUI?.Invoke()));
            _bindsDown.Add(new KeyBind(KeyCode.F5, () => OnResetSettings?.Invoke()));
            _bindsDown.Add(new KeyBind(KeyCode.F10, () => OnSceneCalibration?.Invoke()));
            _bindsDown.Add(new KeyBind(KeyCode.F11, () => OnSceneProjectorParams?.Invoke()));
            _bindsDown.Add(new KeyBind(KeyCode.F12, () => OnFlipDisplay?.Invoke()));
            
            _binds.Add(new KeyBind(KeyCode.UpArrow, () => OnUp?.Invoke()));
            _binds.Add(new KeyBind(KeyCode.DownArrow, () => OnDown?.Invoke()));
            _binds.Add(new KeyBind(KeyCode.LeftArrow, () => OnLeft?.Invoke()));
            _binds.Add(new KeyBind(KeyCode.RightArrow, () => OnRight?.Invoke()));
            
            _binds.Add(new KeyBind(KeyCode.Plus, () => OnZoomIn?.Invoke()));
            _binds.Add(new KeyBind(KeyCode.Equals, () => OnZoomIn?.Invoke()));
            _binds.Add(new KeyBind(KeyCode.KeypadPlus, () => OnZoomIn?.Invoke()));
            
            _binds.Add(new KeyBind(KeyCode.Minus, () => OnZoomOut?.Invoke()));
            _binds.Add(new KeyBind(KeyCode.KeypadMinus, () => OnZoomOut?.Invoke()));
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
}