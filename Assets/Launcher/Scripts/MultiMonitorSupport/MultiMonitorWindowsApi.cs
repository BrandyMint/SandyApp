using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Launcher.MultiMonitorSupport {
    public class MultiMonitorWindowsApi : MultiMonitorSystemApiBase {
        public static bool GetIfAvailable(out MultiMonitorSystemApiBase api) {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            api = new MultiMonitorWindowsApi();
            Debug.Log("Using " + nameof(MultiMonitorWindowsApi));
            return true;
#endif
            api = null;
            return false;
        }

        private MultiMonitorWindowsApi() {} 
        
        public override void MoveMainWindow(Rect rect) {
            Display.displays[0].Activate(0,0,0);
            Display.displays[0].SetParams((int) rect.width, (int) rect.height, (int) rect.x, (int) rect.y);
        }

        protected override bool GetMonitorRectsInternal(out List<Rect> rects) {
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
        private struct RectApi {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdc, ref RectApi pRect, int dwData);

        [DllImport("user32.dll")]
        private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lpRect, MonitorEnumProc callback, int dwData);
    }
}