using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utilities;

namespace Launcher.MultiMonitorSupport {
    public class MultiMonitor : MonoBehaviour {
        public static event Action OnNotEnoughMonitors;
        
        [SerializeField] private bool _useMultiMonitorFix = true;
        [SerializeField] private bool _revertDefaultScreenSettingsOnExit = true;
        [SerializeField, Range(1, 8)] private int _useMonitors = 2;
#if UNITY_EDITOR
        [SerializeField, Range(1, 8)] private int _testMonitorsInEditor = 1;
#endif
        
        public static MultiMonitor Instance { get; private set; }

        public static int MonitorsCount { get; private set; }

        private MultiMonitorSystemApiBase _systemApi;

        private void Awake() {
            if (!MultiMonitorWindowsApi.GetIfAvailable(out _systemApi)
             && !MultiMonitorXLibApi.GetIfAvailable(out _systemApi)) {
                _systemApi = new MultiMonitorSystemApiBase();
                Debug.Log("Unsupported system for MultiMonitor, turn off MultiMonitorFix");
                _useMultiMonitorFix = false;
            }

            
#if UNITY_EDITOR
            var avalaible = _testMonitorsInEditor;
#else
            var avalaible = Display.displays.Length;
#endif
            MonitorsCount = Math.Min(avalaible, _useMonitors);
            _systemApi.UseMonitors = MonitorsCount;
            Instance = this;
        }

        private void OnDestroy() {
            if (_revertDefaultScreenSettingsOnExit) {
                PlayerPrefs.DeleteKey("Screenmanager Fullscreen mode");
                PlayerPrefs.DeleteKey("Screenmanager Resolution Width");
                PlayerPrefs.DeleteKey("Screenmanager Resolution Height");
            }
            _systemApi?.Dispose();
            SceneManager.sceneLoaded -= OnSceneLoaded;
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
            } else if (Display.displays.Length >= MonitorsCount) {
                var fullscreen = Application.platform == RuntimePlatform.LinuxPlayer || MonitorsCount == 1;
                for (int i = 0; i < MonitorsCount; ++i) {
                    var disp = Display.displays[i];
                    if (!disp.active)
                        disp.Activate();
                    else
                        Screen.SetResolution(disp.systemWidth, disp.systemHeight, fullscreen);
                    disp.SetRenderingResolution(disp.systemWidth, disp.systemHeight);
                    Debug.Log($"Activated display {i} {disp.systemWidth}x{disp.systemHeight}");
                }
            } else {
                NotEnoughMonitors(MonitorsCount);
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
            OnNotEnoughMonitors?.Invoke();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
            FixCamerasIn(scene);
        }

        private static void FixCamerasIn(Scene scene) {
            foreach (var rootObj in scene.GetRootGameObjects()) {
                FixCamerasIn(rootObj);
            }
        }

        public static void FixCamerasIn(GameObject obj) {
            foreach (var cam in obj.GetComponentsInChildren<Camera>(true)) {
                if (cam.targetTexture == null)
                    SetTargetDisplay(cam, cam.targetDisplay);
            }
            if (!UseMultiMonitorFix())
                return;
            foreach (var canvas in obj.GetComponentsInChildren<Canvas>(true)) {
                canvas.gameObject.AddComponent<MultiMonitorCanvasFix>();
            }
        }

        public static bool UseMultiMonitorFix() {
#if !UNITY_EDITOR && UNITY_STANDALONE
            return Instance._useMultiMonitorFix;
#endif
            return false;
        }

        public static void SetTargetDisplay(Camera cam, int dispNum, bool updateClearAndDepth = false) {
            var realNum = Mathf.Min(dispNum, MonitorsCount - 1);
            var isNew = CameraVirtualTargetDisplayStore.CreateOrGet(cam, out var store);
            if (isNew || updateClearAndDepth) {
                store.clearFlags = cam.clearFlags;
                store.depth = cam.depth;
            }
            var needModify = Instance._useMonitors > MonitorsCount && dispNum < MonitorsCount;
            cam.clearFlags = needModify ? CameraClearFlags.Nothing : store.clearFlags;
            cam.depth = needModify ? store.depth + MonitorsCount - realNum : store.depth;
            store.targetDisplay = dispNum;
            
            if (UseMultiMonitorFix()) {
                cam.targetDisplay = 0;
                var rect = GetDisplayRect(realNum);
                Debug.Log($"Set cam target {dispNum}, {rect}");
                if (GetMultiMonitorRect(out var multiRect)) {
                    var multiRectSize = multiRect.size;
                    rect.position = MathHelper.Div(rect.position, multiRectSize);
                    rect.size = MathHelper.Div(rect.size, multiRectSize);
                    cam.rect = rect;
                }
            } else {
                cam.targetDisplay = realNum;
            }
        }

        public static int GetTargetDisplay(Camera cam) {
            return CameraVirtualTargetDisplayStore.GetTargetDisplay(cam);
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
                if (Display.displays.Length < dispNum) {
                    var disp = Display.displays[dispNum];
                    return new Rect(0, 0, disp.systemWidth, disp.systemHeight);
                } else {
                    NotEnoughMonitors(dispNum);
                    return new Rect();
                }
            }
        }
        
        public static bool GetMultiMonitorRect(out Rect rect) {
            return Instance._systemApi.GetMultiMonitorRect(out rect);
        }
    }

}