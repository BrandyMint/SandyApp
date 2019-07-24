using System;
using System.Collections;
using System.Threading.Tasks;
using BgConveyer;
using DepthSensor;
using DepthSensor.Device;
using DepthSensor.Stream;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace DepthSensorSandbox {
    public class DepthSensorSandboxProcessor : MonoBehaviour {
        public const ushort _INVALID_DEPTH = 0;
        public const ushort _BAD_HOLE_FIX = 5000;
        
        public class DepthToColorStream : TextureStream<half2> {
            public DepthToColorStream(int width, int height) : base(width, height, TextureFormat.RGHalf) { }
            public DepthToColorStream(bool available) : base(available) { }
        } 
        
        public static event Action<DepthStream, MapDepthToCameraStream> OnDepthDataBackground {
            add { _onDepthDataBackground += value; ActivateSensorsIfNeed(); }
            remove { _onDepthDataBackground -= value; ActivateSensorsIfNeed(); }
        }
        public static event Action<ColorStream> OnColor {
            add { _onColor += value; ActivateSensorsIfNeed(); }
            remove { _onColor -= value; ActivateSensorsIfNeed(); }
        }
        public static event Action<DepthToColorStream> OnDepthToColor {
            add { _onDepthToColor += value; ActivateSensorsIfNeed(); }
            remove { _onDepthToColor -= value; ActivateSensorsIfNeed(); }
        }
        public static event Action<DepthStream, MapDepthToCameraStream> OnNewFrame {
            add { _onNewFrame += value; ActivateSensorsIfNeed(); }
            remove { _onNewFrame -= value; ActivateSensorsIfNeed(); }
        }

        public static DepthSensorSandboxProcessor Instance { get; private set; }

        private static event Action<DepthStream, MapDepthToCameraStream> _onDepthDataBackground;
        private static event Action<ColorStream> _onColor;
        private static event Action<DepthToColorStream> _onDepthToColor;
        private static event Action<DepthStream, MapDepthToCameraStream> _onNewFrame;

        private DepthSensorConveyer _kinConv;
        private DepthSensorManager _dsm;
        private DepthToColorStream _depthToColorStream;

        private int _coveyerId = -1;

#region Initializing
        private void Awake() {
            Instance = this;
        }

        private void Start() {
            _kinConv = gameObject.AddComponent<DepthSensorConveyer>();
            _dsm = DepthSensorManager.Instance;
            if (_dsm != null)
                _dsm.OnInitialized += OnDepthSensorAvailable;
        }

        private void OnDestroy() {
            if (_dsm != null)
                _dsm.OnInitialized -= OnDepthSensorAvailable;
            CloseStream(ref _depthToColorStream);
        }

        private static void CloseStream<T>(ref T stream) where T: AbstractStream {
            if (stream != null) {
                stream.Dispose();
                stream = null;
            }
        }

        private void OnDepthSensorAvailable() {
            _dsm.Device.IsManualUpdate = true;
            ActivateSensorsIfNeed();
            SetupConveyer(ConveyerUpdateBG(), ConveyerUpdateMain());
        }

        private static DepthSensorDevice GetDeviceIfAvailable() {
            var dsm = DepthSensorManager.Instance;
            if (dsm != null && dsm.Device != null && dsm.Device.IsAvailable()) {
                return dsm.Device;
            }
            return null;
        }
        
        private static void ActivateSensorsIfNeed() {
            var device = GetDeviceIfAvailable();
            if (device != null) {
                var any = false;
                any |= device.Color.Active = _onColor != null || _onDepthToColor != null;
                any |= device.Depth.Active = _onNewFrame != null || _onDepthDataBackground != null || _onDepthToColor != null;
                any |= device.MapDepthToCamera.Active = _onNewFrame != null || _onDepthDataBackground != null;
                if (any)
                    device.ManualUpdate();
            }
        }

        private void SetupConveyer(IEnumerator bg, IEnumerator main) {
            if (_coveyerId >= 0)
                _kinConv.RemoveTask(_coveyerId);
            
            var taskMainName = GetType().Name;
            var taskBGName = taskMainName + "BG";
            _kinConv.AddToBG(taskBGName, null, bg);
            _kinConv.AddToMainThread(taskMainName, taskBGName, main);
        }
        
        private bool CreateDepthToColorIfNeed(DepthStream depth) {
            if (_onDepthToColor == null) {
                CloseStream(ref _depthToColorStream);
                return false;
            }
            if (_depthToColorStream == null || _depthToColorStream.data.Length != depth.data.Length) {
                CloseStream(ref _depthToColorStream);
                _depthToColorStream = new DepthToColorStream(depth.width, depth.height);
                return false;
            }
            return true;
        }
#endregion

#region Conveyer
        private IEnumerator ConveyerUpdateBG() {
            var sDepth = _dsm.Device.Depth;
            var sMap = _dsm.Device.MapDepthToCamera;
            while (true) {
                FixDepthHoles(sDepth, sDepth.data);
                _onDepthDataBackground?.Invoke(sDepth, sMap);
                if (_onDepthToColor != null)
                    UpdateDepthToColor(sDepth.data);
                yield return null;
            }
        }

        private IEnumerator ConveyerUpdateMain() {
            var sDepth = _dsm.Device.Depth;
            var sMap = _dsm.Device.MapDepthToCamera;
            var sColor = _dsm.Device.Color;
            while (true) {
                if (_onColor != null) {
                    sColor.ManualApplyTexture();
                    _onColor.Invoke(sColor);
                }
                if (CreateDepthToColorIfNeed(sDepth) && _onDepthToColor != null) {
                    _depthToColorStream.ManualApplyTexture();
                    _onDepthToColor.Invoke(_depthToColorStream);
                }
                if (_onNewFrame != null) {
                    sDepth.ManualApplyTexture();
                    _onNewFrame.Invoke(sDepth, sMap);
                }
                _dsm.Device.ManualUpdate();
                yield return null;
            }
        }

#endregion

#region Processing
        private int4[] _holesSize;
        //      w.3
        //x.0->     <-z.2
        //      y.1
        private int CheckHole(DepthStream depth, NativeArray<ushort> darr, int x, int y, int dir, int h)  {
            var i = depth.GetIFrom(x, y);
            var d = darr[i];
            if (d == _INVALID_DEPTH)
                ++h;
            else {
                h = 0;
            }
            _holesSize[i][dir] = h;
            return h;
        }

        private void FixDepthHoles(DepthStream depth, NativeArray<ushort> darr) {
            if (_holesSize == null || _holesSize.Length != darr.Length)
                _holesSize = new int4[darr.Length];

            var maxDem = Mathf.Max(depth.width, depth.height);
            Parallel.For(0, maxDem, x => {
                int hUp = 0, hDown = 0, hLeft = 0, hRight = 0;
                for (int y = 0; y < maxDem; ++y) {
                    if (x < depth.width && y < depth.height) {
                        hUp = CheckHole(depth, darr, x, y, 1, hUp);
                        hDown = CheckHole(depth, darr, x, depth.height - y - 1, 3, hDown);
                    }
                    if (x < depth.height && y < depth.width) {
                        hLeft = CheckHole(depth, darr, y, x, 0, hLeft);
                        hRight = CheckHole(depth, darr, depth.width - y - 1,  x, 2, hRight);
                    }
                }
            });
            /*Parallel.For(0, depth.height, y => {
                int hLeft = 0, hRight = 0;
                for (int x = 0; x < depth.width; ++x) {
                    hLeft = CheckHole(depth, darr, x, y, 0, hLeft);
                    hRight = CheckHole(depth, darr, depth.width - x - 1,  y, 2, hRight);
                }
            });*/
            Parallel.For(0, depth.height, y => {
                for (int x = 0; x < depth.width; ++x) {
                    var i = depth.GetIFrom(x, y);
                    var d = darr[i];
                    var h = _holesSize[i];
                    if (d == _INVALID_DEPTH) {
                        var up = SafeGet(depth, darr, x, y + h.w);
                        var down = SafeGet(depth, darr, x, y - h.y);
                        var left = SafeGet(depth, darr, x - h.x, y);
                        var right = SafeGet(depth, darr, x + h.z, y);
                        up = SetPriorityToIfInvalid(up, down, left, right);
                        down = SetPriorityToIfInvalid(down, up, left, right);
                        left = SetPriorityToIfInvalid(left, right, up, down);
                        right = SetPriorityToIfInvalid(right, left, up, down);
                        var dd = FixDepthHole(up, down, h.w, h.y) + FixDepthHole(left, right, h.x, h.z);
                        darr[i] = (ushort) (dd / 2);
                    }
                }
            });
        }

        private ushort FixDepthHole(ushort v1, ushort v2, int s1, int s2) {
            var k = (float) s1 / (s1 + s2);
            return (ushort) Mathf.Lerp(v1, v2, k);
        }

        private ushort SafeGet(DepthStream depth, NativeArray<ushort> darr, int x, int y) {
            if (x < 0 || x >= depth.width
                      || y < 0 || y >= depth.height)
                return _INVALID_DEPTH;
            return darr[depth.GetIFrom(x, y)];
        }

        private static ushort SetPriorityToIfInvalid(ushort val, ushort v1, ushort v2, ushort v3) {
            if (val != _INVALID_DEPTH)
                return val;
            if (v1 != _INVALID_DEPTH)
                return v1;
            if (v2 != _INVALID_DEPTH)
                return v2;
            if (v3 != _INVALID_DEPTH)
                return v3;
            return _BAD_HOLE_FIX;
        }

        private void UpdateDepthToColor(NativeArray<ushort> depth) {
            if (_depthToColorStream == null || _depthToColorStream.data.Length != depth.Length)
                return;
            Parallel.For(0, depth.Length, i => {
                var p = _depthToColorStream.GetXYFrom(i);
                var d = new half2(_dsm.Device.DepthMapPosToColorMapPos(p, depth[i]));
                _depthToColorStream.data[i] = d;
            });
        }
#endregion
    }
}