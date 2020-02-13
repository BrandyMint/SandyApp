using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
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

        public class Layer {
            public int id;
            public readonly Dictionary<KeyEvent, UnityEvent> actions = new Dictionary<KeyEvent, UnityEvent>();

            public Layer(int id) {
                this.id = id;
            }
        }

        private readonly List<KeyBind> _bindsDown = new List<KeyBind>();
        private readonly List<KeyBind> _binds = new List<KeyBind>();
        
        private static readonly List<Layer> _layers = new List<Layer> {
            new Layer(EventLayer.GLOBAL),
            new Layer(EventLayer.LOCAL)
        };

        private void Awake() {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            InitMapper();
        }

        private void OnDestroy() {
            _layers.Clear();
        }

        private void InitMapper() {
            _bindsDown.Add(new KeyBind(KeyCode.Tab, KeyEvent.SWITCH_MODE));
            _bindsDown.Add(new KeyBind(KeyCode.Escape, KeyEvent.BACK, "ESC"));
            
            //scenes
            _bindsDown.Add(new KeyBind(KeyCode.LeftShift, KeyCode.F2, KeyEvent.OPEN_PREV_GAME, "SHIFT-F2"));
            _bindsDown.Add(new KeyBind(KeyCode.RightShift, KeyCode.F2, KeyEvent.OPEN_PREV_GAME));
            _bindsDown.Add(new KeyBind(KeyCode.F2, KeyEvent.OPEN_NEXT_GAME));
            _bindsDown.Add(new KeyBind(KeyCode.F3, KeyEvent.SHOW_UI));
            _bindsDown.Add(new KeyBind(KeyCode.F5, KeyEvent.RESET));
            _bindsDown.Add(new KeyBind(KeyCode.F9, KeyEvent.OPEN_SANDBOX_CALIBRATION));
            _bindsDown.Add(new KeyBind(KeyCode.F10, KeyEvent.OPEN_CALIBRATION));
            _bindsDown.Add(new KeyBind(KeyCode.F11, KeyEvent.OPEN_VIEWER));
            _bindsDown.Add(new KeyBind(KeyCode.LeftAlt,KeyCode.F12, KeyEvent.FLIP_SANDBOX, "ALT-F12"));
            _bindsDown.Add(new KeyBind(KeyCode.RightAlt,KeyCode.F12, KeyEvent.FLIP_SANDBOX));
            _bindsDown.Add(new KeyBind(KeyCode.F12, KeyEvent.FLIP_DISPLAY));
            
            //records
            _bindsDown.Add(new KeyBind(KeyCode.LeftAlt,KeyCode.R, KeyEvent.RECORD, "ALT-R"));
            _bindsDown.Add(new KeyBind(KeyCode.RightAlt,KeyCode.R, KeyEvent.RECORD));
            _bindsDown.Add(new KeyBind(KeyCode.LeftAlt,KeyCode.P, KeyEvent.PLAY_RECORD, "ALT-P"));
            _bindsDown.Add(new KeyBind(KeyCode.RightAlt,KeyCode.P, KeyEvent.PLAY_RECORD));
            _bindsDown.Add(new KeyBind(KeyCode.T, KeyEvent.SWITCH_TARGET));
            _bindsDown.Add(new KeyBind(KeyCode.LeftAlt,KeyCode.O, KeyEvent.OPEN, "ALT-O"));
            _bindsDown.Add(new KeyBind(KeyCode.RightAlt,KeyCode.O, KeyEvent.OPEN));
            
            //calibrating
            _bindsDown.Add(new KeyBind(KeyCode.H, KeyEvent.SET_DEPTH));
            _bindsDown.Add(new KeyBind(KeyCode.P, KeyEvent.CHANGE_PROJECTOR_SIZE));
            _bindsDown.Add(new KeyBind(KeyCode.A, KeyEvent.SWITCH_OBLIQUE));
            _bindsDown.Add(new KeyBind(KeyCode.Return,KeyEvent.ENTER, "ENTER"));
            _bindsDown.Add(new KeyBind(KeyCode.A, KeyEvent.SWITCH_ALPHA_FEATURES));

            //exit
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
        }
        
        public static void AddListener(KeyEvent ev, UnityAction act, int layerId = EventLayer.LOCAL) {
            var layer = FindLayer(layerId);
            AddListener(layer, ev, act);
        }

        public static void RemoveListener(KeyEvent ev, UnityAction act, int layerId = EventLayer.LOCAL) {
            var layer = FindLayer(layerId, false);
            if (layer != null)
                RemoveListener(layer, ev, act);
        }

        public static IEnumerable<KeyEvent> GetListenedEvents(int layerId) {
            var layer = FindLayer(layerId);
            return GetListenedEvents(layer);
        }

        private static Layer FindLayer(int layerId, bool checkNull = true) {
            var layer = _layers.FirstOrDefault(l => l.id == layerId);
            if (checkNull)
                Assert.IsNotNull(layer, $"KayMapper: layer {layerId} is not exist");
            return layer;
        }

        private static void AddListener(Layer layer, KeyEvent ev, UnityAction act) {
            if (layer.actions.TryGetValue(ev, out var unityEvent)) {
                unityEvent.AddListener(act);
            } else {
                unityEvent = new UnityEvent();
                unityEvent.AddListener(act);
                layer.actions[ev] = unityEvent;
            }
        }

        private static void RemoveListener(Layer layer, KeyEvent ev, UnityAction act) {
            if (layer.actions.TryGetValue(ev, out var unityEvent)) {
                unityEvent.RemoveListener(act);
            }
        }

        private static IEnumerable<KeyEvent> GetListenedEvents(Layer layer) {
            foreach (var act in layer.actions) {
                if (act.Value != null) {
                    yield return act.Key;
                }
            }
        }

        public static int PushOverrideLayer() {
            int id = 1;
            while (_layers.Any(l => l.id == id)) {
                ++id;
            }
            _layers.Add(new Layer(id));
            return id;
        }

        public static int PopOverrideLayer() {
            Assert.IsTrue(_layers.Count > 1, $"KayMapper: there is no override layers");
            var i = _layers.Count - 1;
            var layer = _layers[i];
            _layers.RemoveAt(i);
            return layer.id;
        }

        public static KeyBind FindFirstKey(KeyEvent ev) {
            if (_instance == null)
                return null;
            return _instance._bindsDown.FirstOrDefault(b => b.ev == ev) ??
                   _instance._binds.FirstOrDefault(b => b.ev == ev);
        }

        private static IEnumerable<Layer> EnumerateLayersByPriority() {
            return Enumerable.Reverse(_layers);
        } 

        public static bool FireEvent(KeyEvent ev) {
            foreach (var layer in EnumerateLayersByPriority()) {
                if (layer.actions.TryGetValue(ev, out var action)) {
                    if (action != null) {
                        action.Invoke();
                        return true;
                    }
                }
            }
            
            return false;
        }

        private void Update() {
            if (Input.anyKey) {
                ProcessInput(_binds, Input.GetKey, false);
            }
            if (Input.anyKeyDown) {
                ProcessInput(_bindsDown, Input.GetKeyDown, true);
            }
        }

        private static void ProcessInput(IEnumerable<KeyBind> binds, Func<KeyCode, bool> checkKey, bool onlyOne) {
            foreach (var bind in binds) {
                if (checkKey(bind.key) && bind.IsAddKeysPressed) {
                    if (!FireEvent(bind.ev))
                        continue;
                    if (onlyOne)
                        break;
                }
            }
        }
    }
}