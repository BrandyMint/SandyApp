using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace AsyncGPUReadbackPluginNs {
    public class AsyncGPURequestLifeTimeManager : MonoBehaviour {
        private static AsyncGPURequestLifeTimeManager _instance;
        
        private int _mainThreadId = -1;

        public static AsyncGPURequestLifeTimeManager Instance {
            get {
                if (_instance == null) {
                    var obj = new GameObject(nameof(AsyncGPUReadbackRequest));
                    DontDestroyOnLoad(obj);
                    _instance = obj.AddComponent<AsyncGPURequestLifeTimeManager>();
                }

                return _instance;
            }
        }

        private AsyncGPURequestLifeTimeManager() { }
        
        private void Awake() {
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        private class RequestState {
            public AsyncGPUReadbackRequest request;
            public bool needDispose;
            public Action<AsyncGPUReadbackRequest> onDone;
        }

        private readonly List<RequestState> _requests = new List<RequestState>();

        private void OnDestroy() {
            _instance = null;
            foreach (var s in _requests) {
                if (!s.request.manualDispose)
                    s.request.Dispose();
            }
        }

        public void Add(AsyncGPUReadbackRequest request, Action<AsyncGPUReadbackRequest> onDone) {
            var s = new RequestState {
                request = request,
                onDone = onDone
            };
            if (Thread.CurrentThread.ManagedThreadId == _mainThreadId) {
                StartCoroutine(FirstUpdateImmediatelyThanAdd(s));
            } else {
                lock (_requests) {
                    _requests.Add(s);
                }
            }
        }

        private IEnumerator FirstUpdateImmediatelyThanAdd(RequestState s) {
            UpdateRequest(s);
            yield return new WaitForEndOfFrame();
            lock (_requests) {
                _requests.Add(s);
            }
        }

        private void Update() {
            lock (_requests) {
                _requests.RemoveAll(s => {
                    if (s.needDispose && !s.request.manualDispose)
                        s.request.Dispose();
                    return s.needDispose;
                });
                
                foreach (var s in _requests) {
                    UpdateRequest(s);
                }
            }
        }

        private static void UpdateRequest(RequestState s) {
            s.request.Update();
            var done = s.request.done;
            s.needDispose = done;
            if (done) {
                s.onDone?.Invoke(s.request);
            }
        }
    }
}