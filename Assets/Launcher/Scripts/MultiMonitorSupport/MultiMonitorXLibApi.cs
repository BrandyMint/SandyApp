using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using UnityEngine;
using Debug = UnityEngine.Debug;
#if UNITY_STANDALONE_LINUX
    using X11;
#endif

namespace Launcher.MultiMonitorSupport {
    public class MultiMonitorXLibApi : MultiMonitorSystemApiBase {
        public static bool GetIfAvailable(out MultiMonitorSystemApiBase api) {
#if UNITY_STANDALONE_LINUX && !UNITY_EDITOR
            var d = Xlib.XOpenDisplay(null);
            if (d != IntPtr.Zero) {
                api = new MultiMonitorXLibApi(d);
                Debug.Log("Using " + nameof(MultiMonitorXLibApi));
                return true;
            }
#endif
            api = null;
            return false;
        }

#if UNITY_STANDALONE_LINUX
        private IntPtr _display;
        private Window _window = Window.None;
        private ulong _atomPID;
        private ulong _pid;

        private MultiMonitorXLibApi(IntPtr display) {
            _display = display;
            _atomPID = Xlib.XInternAtom(_display, "_NET_WM_PID", true);
            if(_atomPID == 0L) {
                Debug.LogError("Xlib: No such atom");
            }
            _pid = (ulong) Process.GetCurrentProcess().Id;
        }

        public override void Dispose() {
            if (_display != IntPtr.Zero)
                Xlib.XCloseDisplay(_display);
        }
        
        public override void MoveMainWindow(Rect rect) {
            var window = GetWindow();
            TurnOffDecorate(window);
            Xlib.XMoveResizeWindow(_display, window, (int) rect.x, (int) rect.y, (uint) rect.width, (uint) rect.height);
        }
        
        private struct Hints {
            public ulong flags;
            public ulong functions;
            public ulong decorations;
            public long inputMode;
            public ulong status;
        } 

        private void TurnOffDecorate(Window wnd) {
            var hints = new Hints {flags = 2, decorations = 0};
            ulong property = Xlib.XInternAtom(_display, "_MOTIF_WM_HINTS", true);
            var handle = GCHandle.Alloc(hints, GCHandleType.Pinned);
            Xlib.XChangeProperty(_display, wnd, property, property,32, PropMode.Replace, handle.AddrOfPinnedObject(),5);
            Xlib.XMapWindow(_display, wnd);
            handle.Free();
        }

        protected override bool GetMonitorRectsInternal(out List<Rect> rects) {
            rects = new List<Rect>();
            var p = new Process {
                StartInfo = {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    FileName = "xrandr",
                    Arguments = "--listmonitors"
                }
            };
            if (p.Start()) {
                var output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                if (p.ExitCode == 0) {
                    foreach (Match match in Regex.Matches(output, @"(\d+)\/\d+x(\d+)\/\d+\+(\d+)\+(\d+)")) {
                        rects.Add(new Rect {
                            width = int.Parse(match.Groups[1].Value),
                            height = int.Parse(match.Groups[2].Value),
                            x = int.Parse(match.Groups[3].Value),
                            y = int.Parse(match.Groups[4].Value)
                        });
                    }

                    if (rects.Any()) {
                        var first = rects.First();
                        rects = rects.OrderBy(r => Vector3.Distance(r.center, first.center)).ToList();
                        return true;
                    }
                }
            }

            return false;
        }

        private Window GetWindow() {
            if (_window == Window.None) {
                var stack = new Stack<Window>();
                stack.Push(Xlib.XDefaultRootWindow(_display));
                while (stack.Count > 0) {
                    Window root = Window.None, parent = Window.None;
                    var wnd = stack.Pop();
                    if (Xlib.XGetWindowProperty(_display, wnd, _atomPID, 0, 1, false, Atom.XA_CARDINAL,
                           out _, out _, out _, out _, out var propPID) == 0) {
                        if (propPID != IntPtr.Zero) {
                            var pid = Marshal.PtrToStructure<ulong>(propPID);
                            Xlib.XFree(propPID);
                            if (_pid == pid) {
                                _window = wnd;
                                break;
                            }
                        }
                    }
                    
                    Xlib.XQueryTree(_display, wnd, ref root, ref parent, out var childs);
                    foreach (var child in childs) {
                        stack.Push(child);
                    }
                }
            }

            return _window;
        }
#endif
    }
}