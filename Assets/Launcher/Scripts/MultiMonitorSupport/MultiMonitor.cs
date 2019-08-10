#if !UNITY_EDITOR && UNITY_STANDALONE
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
#endif
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utilities;

namespace Launcher.MultiMonitorSupport {
    public class MultiMonitor : MonoBehaviour {
        [SerializeField] private bool _useMultiMonitorFix = true;
        [SerializeField, Range(1, 8)] private int _useMonitors = 2;
        
        public static MultiMonitor Instance { get; private set; }

        private MultiMonitor() {
            Instance = this;
        }

        private void Start() {
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
                    Debug.Log("Setting multi-display window rect: " + multiRect);
                    Screen.SetResolution((int) multiRect.width, (int) multiRect.height, false);
                    Display.displays[0].Activate(0,0,0);
                    Display.displays[0].SetParams((int) multiRect.width, (int) multiRect.height, (int) multiRect.x, (int) multiRect.y);
                    //var window = GetForegroundWindow();
                    //MoveWindow(window, (int) rect.x, (int) rect.y, (int) rect.width, (int) rect.height, 1);
                }
            } else if (Display.displays.Length >= _useMonitors) {
                for (int i = 0; i < Math.Min(Display.displays.Length, _useMonitors); ++i) {
                    var disp = Display.displays[i];
                    if (!disp.active) disp.Activate();
                    disp.SetRenderingResolution(disp.systemWidth, disp.systemHeight);
                }
            } else {
                Debug.LogError($"Cant find {_useMonitors} displays!");
            }
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
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
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
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
            if (UseMultiMonitorFix() && GetWinApiMonitorRects(out var monitors) && GetMultiMonitorRect(out var multiRect)) {
                var rect = monitors[dispNum];
                rect.position -= multiRect.position;
                rect.position = new Vector2(
                    rect.position.x,
                    multiRect.height - rect.position.y - rect.height
                );
                
                return rect;
            }
#endif
            var disp = Display.displays[dispNum];
            return new Rect(0, 0, disp.systemWidth, disp.systemHeight);
        }
        
        public static bool GetMultiMonitorRect(out Rect rect) {
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
            if (GetWinApiMonitorRects(out var monitors)) {
                if (monitors.Count < Instance._useMonitors) {
                    Debug.LogError($"Cant find {Instance._useMonitors} displays!");
                    rect = Rect.zero;
                    return false;
                }
                
                rect = Rect.MinMaxRect(float.MaxValue, float.MaxValue, float.MinValue, float.MinValue);
                for (int i = 0; i < Math.Min(monitors.Count, Instance._useMonitors); ++i) {
                    var monitor = monitors[i];
                    if (monitor.xMin < rect.xMin) rect.xMin = monitor.xMin;
                    if (monitor.yMin < rect.yMin) rect.yMin = monitor.yMin;
                    if (monitor.xMax > rect.xMax) rect.xMax = monitor.xMax;
                    if (monitor.yMax > rect.yMax) rect.yMax = monitor.yMax;
                }
                return true;
            }
#endif
            rect = Rect.zero;
            return false;
        }
        
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
        public static bool GetWinApiMonitorRects(out List<Rect> rects) {
            var first = Rect.zero;
            var rawRects = new List<Rect>();
            var success = EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero,
                (IntPtr monitor, IntPtr hdc, ref RectApi rect, int data) => {
                    var r = Rect.MinMaxRect(rect.left, rect.top, rect.right, rect.bottom);
                    if (rect.left == 0 && rect.top == 0)
                        first = r;
                    rawRects.Add(r);
                    return true;
                }, 0);
            if (success) {
                rects = rawRects.OrderBy(r => Vector3.Distance(r.center, first.center)).ToList();
                return true;
            } else {
                Debug.Log("Fail on Win Api EnumDisplayMonitors");
                rects = null;
                return false;
            }
        }
        
        [StructLayout(LayoutKind.Sequential)]
        struct RectApi {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }
        
        delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdc, ref RectApi pRect, int dwData);

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", EntryPoint = "MoveWindow")]
        static extern int MoveWindow(IntPtr hWnd, int x, int y, int nWidth, int nHeight, int bRepaint);
        
        [DllImport("user32.dll")]
        static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lpRect, MonitorEnumProc callback, int dwData);
#endif
    }

}