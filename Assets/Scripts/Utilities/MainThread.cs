using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Utilities {
    public class MainThread : MonoBehaviour {
        private readonly Queue<object> _mainThreadActions = new Queue<object>();
        private readonly object _actionsLock = new object();
        private bool _enabled;
        private int _mainThreadId = -1;

        private static MainThread _instance;
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize() {
            if (_instance == null) {
                _instance = new GameObject("MainThreadExecutor").AddComponent<MainThread>();
                DontDestroyOnLoad(_instance.gameObject);
            }
        }

        private void Awake() {
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        private void OnDestroy() {
            _instance = null;
            lock (_actionsLock) {
                _mainThreadActions.Clear();
            }
        }

        public static bool IsMainThread => _instance != null && Thread.CurrentThread.ManagedThreadId == _instance._mainThreadId;

        public static bool ExecuteOrPush(Action callback) {
            if (IsMainThread) {
                callback?.Invoke();
                return true;
            } else {
                PushInternal(callback);
                return false;
            }
        }

        public static void Push(Action callback) {
            PushInternal(callback);
        }

        public static void Push(IEnumerator callback) {
            PushInternal(callback);
        }

        private static void PushInternal(object callback) {
            if (_instance == null) {
                Debug.LogError("Utilities.MainThread not initialized");
            } else {
                lock (_instance._actionsLock) {
                    _instance._mainThreadActions.Enqueue(callback);
                    _instance.EnableMainThreadUpdate();
                }
            }
        }

        private void Update() {
            if (!_enabled) return;
            
            lock (_actionsLock) {
                if (_mainThreadActions.Count == 0)
                    DisableMainThreadUpdate();
                else {
                    var count = _mainThreadActions.Count;
                    for(int i = 0; i < count; ++i) {
                        var obj = _mainThreadActions.Peek();
                        if (obj is Action action) {
                            action();
                        } else if (obj is IEnumerator enumerator) {
                            if (enumerator.MoveNext()) {
                                var res = enumerator.Current;
                                continue;
                            }
                        }

                        _mainThreadActions.Dequeue();
                    }
                }
            }
        }

        private void EnableMainThreadUpdate() {
            _enabled = true;
        }

        private void DisableMainThreadUpdate() {
            _enabled = false;
        }
    }
}