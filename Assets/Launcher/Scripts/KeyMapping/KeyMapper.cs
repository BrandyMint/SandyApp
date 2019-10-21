using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Launcher.KeyMapping {
    public class KeyMapper : MonoBehaviour {
        private static KeyMapper _instance;

        public class KeyBind {
            public KeyCode key;
            public KeyCode[] addKeys = new KeyCode[0];
            public KeyEvent ev;
            public string ShortCut => _shortCut 
                ?? string.Join("+", addKeys.Append(key).Select(k => k.ToString().ToUpper()));
            
            private string _shortCut;

            public bool IsAddKeysPressed => addKeys.All(Input.GetKey);

            public KeyBind(KeyCode key, KeyEvent ev, string shortCut = null) {
                this.key = key;
                this.ev = ev;
                this._shortCut = shortCut;
            }
            
            public KeyBind(KeyCode addKey, KeyCode key, KeyEvent ev, string shortCut = null)
                : this(key, ev, shortCut) {
                addKeys = new[] {addKey};
            }
        }

        private readonly List<KeyBind> _bindsDown = new List<KeyBind>();
        private readonly List<KeyBind> _binds = new List<KeyBind>();
        
        private static readonly Dictionary<KeyEvent, UnityEvent> _actions = new Dictionary<KeyEvent, UnityEvent>();

        private void Awake() {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            InitMapper();
        }

        private void OnDestroy() {
            _actions.Clear();
        }

        private void InitMapper() {
            _bindsDown.Add(new KeyBind(KeyCode.Tab, KeyEvent.SWITCH_MODE));
            _bindsDown.Add(new KeyBind(KeyCode.Escape, KeyEvent.BACK, "ESC"));
            
            _bindsDown.Add(new KeyBind(KeyCode.LeftShift, KeyCode.F2, KeyEvent.OPEN_PREV_GAME, "SHIFT-F2"));
            _bindsDown.Add(new KeyBind(KeyCode.RightShift, KeyCode.F2, KeyEvent.OPEN_PREV_GAME));
            _bindsDown.Add(new KeyBind(KeyCode.F2, KeyEvent.OPEN_NEXT_GAME));
            _bindsDown.Add(new KeyBind(KeyCode.F3, KeyEvent.SHOW_UI));
            _bindsDown.Add(new KeyBind(KeyCode.F5, KeyEvent.RESET));
            _bindsDown.Add(new KeyBind(KeyCode.F9, KeyEvent.OPEN_SANDBOX_CALIBRATION));
            _bindsDown.Add(new KeyBind(KeyCode.F10, KeyEvent.OPEN_CALIBRATION));
            _bindsDown.Add(new KeyBind(KeyCode.F11, KeyEvent.OPEN_PROJECTOR_PARAMS));
            _bindsDown.Add(new KeyBind(KeyCode.LeftAlt,KeyCode.F12, KeyEvent.FLIP_SANDBOX, "ALT-F12"));
            _bindsDown.Add(new KeyBind(KeyCode.RightAlt,KeyCode.F12, KeyEvent.FLIP_SANDBOX));
            _bindsDown.Add(new KeyBind(KeyCode.F12, KeyEvent.FLIP_DISPLAY));
            
            _bindsDown.Add(new KeyBind(KeyCode.Y, KeyEvent.SET_DEPTH_MAX));
            _bindsDown.Add(new KeyBind(KeyCode.H, KeyEvent.SET_DEPTH_ZERO));
            _bindsDown.Add(new KeyBind(KeyCode.N, KeyEvent.SET_DEPTH_MIN));
            
            _bindsDown.Add(new KeyBind(KeyCode.LeftControl,KeyCode.Q, KeyEvent.EXIT, "CTRL-Q"));
            _bindsDown.Add(new KeyBind(KeyCode.RightControl,KeyCode.Q, KeyEvent.EXIT));
            _bindsDown.Add(new KeyBind(KeyCode.LeftControl,KeyCode.C, KeyEvent.EXIT));
            _bindsDown.Add(new KeyBind(KeyCode.RightControl,KeyCode.C, KeyEvent.EXIT));
            
            _binds.Add(new KeyBind(KeyCode.UpArrow, KeyEvent.UP, "↑"));
            _binds.Add(new KeyBind(KeyCode.DownArrow, KeyEvent.DOWN, "↓"));
            _binds.Add(new KeyBind(KeyCode.LeftArrow, KeyEvent.LEFT, "←"));
            _binds.Add(new KeyBind(KeyCode.RightArrow, KeyEvent.RIGHT, "→"));
            
            _binds.Add(new KeyBind(KeyCode.Plus, KeyEvent.ZOOM_IN, "+"));
            _binds.Add(new KeyBind(KeyCode.Equals, KeyEvent.ZOOM_IN, "="));
            _binds.Add(new KeyBind(KeyCode.KeypadPlus, KeyEvent.ZOOM_IN, "+"));
            
            _binds.Add(new KeyBind(KeyCode.Minus, KeyEvent.ZOOM_OUT, "-"));
            _binds.Add(new KeyBind(KeyCode.KeypadMinus, KeyEvent.ZOOM_OUT, "-"));
            
            _binds.Add(new KeyBind(KeyCode.Backslash, KeyEvent.ANGLE_INC, "\\"));
            _binds.Add(new KeyBind(KeyCode.Slash, KeyEvent.ANGLE_DEC, "/"));
            
            _binds.Add(new KeyBind(KeyCode.LeftBracket, KeyEvent.WIDE_MINUS, "["));
            _binds.Add(new KeyBind(KeyCode.RightBracket, KeyEvent.WIDE_PLUS, "]"));
        }
        
        public static void AddListener(KeyEvent ev, UnityAction act) {
            if (_actions.TryGetValue(ev, out var unityEvent)) {
                unityEvent.AddListener(act);
            } else {
                unityEvent = new UnityEvent();
                unityEvent.AddListener(act);
                _actions[ev] = unityEvent;
            }
        }

        public static void RemoveListener(KeyEvent ev, UnityAction act) {
            if (_actions.TryGetValue(ev, out var unityEvent)) {
                unityEvent.RemoveListener(act);
            }
        }

        public static KeyBind FindFirstKey(KeyEvent ev) {
            if (_instance == null)
                return null;
            return _instance._bindsDown.FirstOrDefault(b => b.ev == ev) ??
                   _instance._binds.FirstOrDefault(b => b.ev == ev);
        }

        public static void FireEvent(KeyEvent ev) {
            if (_actions.TryGetValue(ev, out var action))
                action?.Invoke();
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
                if (checkKey(bind.key) && bind.IsAddKeysPressed) {
                    FireEvent(bind.ev);
                    break;
                }
            }
        }
    }
}