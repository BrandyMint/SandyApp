using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utilities;

namespace Launcher.MultiMonitorSupport {
    public class MultiMonitor : MonoBehaviour {
        [SerializeField] private bool _useMultiMonitorFix = true;
        [SerializeField, Range(1, 8)] private int _useMonitors = 2;
        
        public static MultiMonitor Instance { get; private set; }

        private MultiMonitorSystemApiBase _systemApi;

        private void Awake() {
            if (!MultiMonitorWindowsApi.GetIfAvailable(out _systemApi)
             && !MultiMonitorXLibApi.GetIfAvailable(out _systemApi)) {
                _systemApi = new MultiMonitorSystemApiBase();
                Debug.Log("Unsupported system for MultiMonitor, turn off MultiMonitorFix");
                _useMultiMonitorFix = false;
            }
            _systemApi.UseMonitors = _useMonitors;
            
            Instance = this;
        }

        private void OnDestroy() {
            _systemApi?.Dispose();
        }

        private void Start() {
            _systemApi.OnNotEnoughMonitors += NotEnoughMonitors;
            ActivateDisplays();
            FixCamerasIn(SceneManager.GetActiveScene());
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void ActivateDisplays() {
#if UNITY_EDITOR || !UNITY_STANDALONE
            return;
#endif            
            if (UseMultiMonitorFix()) {
                if (GetMultiMonitorRect(out var multiRect)) {
                    StartCoroutine(ActivatingDisplays(multiRect));
                }
            } else if (Display.displays.Length >= _useMonitors) {
                for (int i = 0; i < Math.Min(Display.displays.Length, _useMonitors); ++i) {
                    var disp = Display.displays[i];
                    if (!disp.active) disp.Activate();
                    disp.SetRenderingResolution(disp.systemWidth, disp.systemHeight);
                    Debug.Log($"Activated display {i} {disp.systemWidth}x{disp.systemHeight}");
                }
            } else {
                NotEnoughMonitors(_useMonitors);
            }
        }

        private IEnumerator ActivatingDisplays(Rect multiRect) {
            Debug.Log("Setting multi-display window rect: " + multiRect);
            Screen.SetResolution((int) multiRect.width, (int) multiRect.height, false);
#if !UNITY_STANDALONE_WIN
            yield return null;
#endif
            _systemApi.MoveMainWindow(multiRect);
            yield break;
        }

        private static void NotEnoughMonitors(int useMonitors) {
            Debug.LogError($"Cant find {useMonitors} displays!");
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
            FixCamerasIn(scene);
        }

        private static void FixCamerasIn(Scene scene) {
            if (!UseMultiMonitorFix())
                return;
            foreach (var rootObjs in scene.GetRootGameObjects()) {
                foreach (var canvas in rootObjs.GetComponentsInChildren<Canvas>(true)) {
                    canvas.gameObject.AddComponent<MultiMonitorCanvasFix>();
                }
                foreach (var cam in rootObjs.GetComponentsInChildren<Camera>(true)) {
                    if (cam.targetTexture == null)
                        SetTargetDisplay(cam, cam.targetDisplay);
                }
            }
        }

        public static bool UseMultiMonitorFix() {
#if !UNITY_EDITOR && UNITY_STANDALONE
            return Instance._useMultiMonitorFix;
#endif
            return false;
        }

        public static void SetTargetDisplay(Camera cam, int dispNum) {
            if (UseMultiMonitorFix()) {
                CameraVirtualTargetDisplayStore.Store(cam, dispNum);
                cam.targetDisplay = 0;
                var rect = GetDisplayRect(dispNum);
                Debug.Log($"Set cam target {dispNum}, {rect}");
                if (GetMultiMonitorRect(out var multiRect)) {
                    var multiRectSize = multiRect.size;
                    rect.position = MathHelper.Div(rect.position, multiRectSize);
                    rect.size = MathHelper.Div(rect.size, multiRectSize);
                    cam.rect = rect;
                }
            } else {
                cam.targetDisplay = dispNum;
            }
        }

        public static int GetTargetDisplay(Camera cam) {
            if (UseMultiMonitorFix())
                return CameraVirtualTargetDisplayStore.Get(cam);
            else
                return cam.targetDisplay;
        }

        public static Rect GetDisplayRect(int dispNum) {
            if (UseMultiMonitorFix() && Instance._systemApi.GetMonitorRects(out var monitors) && GetMultiMonitorRect(out var multiRect)) {
                var rect = monitors[dispNum];
                rect.position -= multiRect.position;
                rect.position = new Vector2(
                    rect.position.x,
                    multiRect.height - rect.position.y - rect.height
                );
                
                return rect;
            } else {
                var disp = Display.displays[dispNum];
                return new Rect(0, 0, disp.systemWidth, disp.systemHeight);
            }
        }
        
        public static bool GetMultiMonitorRect(out Rect rect) {
            return Instance._systemApi.GetMultiMonitorRect(out rect);
        }
    }

}