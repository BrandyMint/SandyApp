using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using BgConveyer;
using DepthSensor;
using DepthSensor.Buffer;
using DepthSensor.Device;
using DepthSensor.Sensor;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace DepthSensorSandbox {
    public class DepthSensorSandboxProcessor : MonoBehaviour {
        public const ushort _INVALID_DEPTH = 0;
        public const ushort _BAD_HOLE_FIX = 5000;
        public const int  _BUFFERS_COUNT = 3;
        
        public class DepthToColorBuffer : TextureBuffer<Vector2> {
            public DepthToColorBuffer(int width, int height) : base(width, height, TextureFormat.RGFloat) { }
        } 
        
        public static event Action<DepthBuffer, MapDepthToCameraBuffer> OnDepthDataBackground {
            add { _onDepthDataBackground += value; ActivateSensors(); }
            remove { _onDepthDataBackground -= value; ActivateSensors(); }
        }
        public static event Action<ColorBuffer> OnColor {
            add { _onColor += value; ActivateSensors(); }
            remove { _onColor -= value; ActivateSensors(); }
        }
        public static event Action<DepthToColorBuffer> OnDepthToColor {
            add { _onDepthToColor += value; ActivateSensors(); }
            remove { _onDepthToColor -= value; ActivateSensors(); }
        }
        public static event Action<DepthBuffer, MapDepthToCameraBuffer> OnNewFrame {
            add { _onNewFrame += value; ActivateSensors(); }
            remove { _onNewFrame -= value; ActivateSensors(); }
        }

        public static DepthSensorSandboxProcessor Instance { get; private set; }

        private static event Action<DepthBuffer, MapDepthToCameraBuffer> _onDepthDataBackground;
        private static event Action<ColorBuffer> _onColor;
        private static event Action<DepthToColorBuffer> _onDepthToColor;
        private static event Action<DepthBuffer, MapDepthToCameraBuffer> _onNewFrame;
        private static volatile bool _needActivateSensors;

        private DepthSensorConveyer _conveyer;
        private DepthSensorManager _dsm;
        private DepthToColorBuffer _depthToColorBuffer;

        private ColorBuffer _bufColor;
        private DepthBuffer _bufDepth;
        private MapDepthToCameraBuffer _bufMapToCamera;

        private AutoResetEvent _evUnlock = new AutoResetEvent(false);
        private int _coveyerId = -1;

#region Initializing
        private void Awake() {
            Instance = this;
        }

        private void Start() {
            _conveyer = gameObject.AddComponent<DepthSensorConveyer>();
            _conveyer.OnNoFrame += ActivateSensorsIfNeed;
            _dsm = DepthSensorManager.Instance;
            if (_dsm != null)
                _dsm.OnInitialized += OnDepthSensorAvailable;
        }

        private void OnDestroy() {
            if (_conveyer != null)
                _conveyer.OnNoFrame -= ActivateSensorsIfNeed;
            if (_dsm != null)
                _dsm.OnInitialized -= OnDepthSensorAvailable;
            RemoveConveyers();
            DisposeBuffer(ref _depthToColorBuffer);
        }

        private static void DisposeBuffer<T>(ref T stream) where T: AbstractBuffer {
            if (stream != null) {
                stream.Dispose();
                stream = null;
            }
        }

        private void OnDepthSensorAvailable() {
            var dev = GetDeviceIfAvailable();
            if (dev != null)
                dev.MapDepthToCamera.OnNewFrame += OnUpdateMapDepthToCamera;
            
            dev.Color.BuffersCount = _BUFFERS_COUNT;
            dev.Depth.BuffersCount = _BUFFERS_COUNT;
            
            SetupConveyer(ConveyerUpdateBG(), ConveyerUpdateMain(), ConveyerBGUnlock());
        }

        private static DepthSensorDevice GetDeviceIfAvailable() {
            var dsm = DepthSensorManager.Instance;
            if (dsm != null && dsm.Device != null && dsm.Device.IsAvailable()) {
                return dsm.Device;
            }
            return null;
        }

        private static void ActivateSensors() {
            _needActivateSensors = true;
        }
        
        private static void ActivateSensorsIfNeed() {
            if (!_needActivateSensors)
                return;

            var device = GetDeviceIfAvailable();
            if (device != null) {
                var activeColor = _onColor != null || _onDepthToColor != null;
                var activeDepth = _onNewFrame != null || _onDepthDataBackground != null || _onDepthToColor != null;
                var activeMap = _onNewFrame != null || _onDepthDataBackground != null;
                _needActivateSensors = false;
                ActivateSensorsIfNeed(device.Color, ref Instance._bufColor, activeColor);
                ActivateSensorsIfNeed(device.Depth, ref Instance._bufDepth, activeDepth);
                ActivateSensorsIfNeed(device.MapDepthToCamera, ref Instance._bufMapToCamera, activeMap);
            }
        }

        private static void ActivateSensorsIfNeed<T>(AbstractSensor sensor, ref T buffer, bool activate) where T: AbstractBuffer {
            sensor.Active = activate;
            if (!activate && buffer != null) {
                buffer.SafeUnlock();
                buffer = null;
            }
        }

        private void SetupConveyer(IEnumerator bg, IEnumerator main, IEnumerator bgUnlock) {
            RemoveConveyers();
            
            var taskMainName = GetType().Name;
            var taskBGName = taskMainName + "BG";
            var taskBGUnlock = taskBGName + "Unlock";
            _coveyerId = _conveyer.AddToBG(taskBGName, null, bg);
            _conveyer.AddToMainThread(taskMainName, taskBGName, main);
            _conveyer.AddToBG(taskBGUnlock, taskMainName, bgUnlock);
        }

        private void RemoveConveyers() {
            if (_coveyerId >= 0)
                _conveyer.RemoveTask(_coveyerId);
        }
        
        private bool CreateDepthToColorIfNeed(DepthBuffer depth) {
            if (_onDepthToColor == null) {
                DisposeBuffer(ref _depthToColorBuffer);
                return false;
            }
            if (_depthToColorBuffer == null || _depthToColorBuffer.data.Length != depth.data.Length) {
                DisposeBuffer(ref _depthToColorBuffer);
                _depthToColorBuffer = new DepthToColorBuffer(depth.width, depth.height);
                return false;
            }
            return true;
        }
#endregion

#region Conveyer
        private IEnumerator ConveyerUpdateBG() {
            var sDepth = _dsm.Device.Depth;
            var sColor = _dsm.Device.Color;
            var sMap = _dsm.Device.MapDepthToCamera;
            
            while (true) {
                _bufDepth = sDepth.GetNewestAndLock();
                _bufColor = sColor.GetNewestAndLock();
                _bufMapToCamera = sMap.GetNewest();
                FixDepthHoles(_bufDepth);
                _onDepthDataBackground?.Invoke(_bufDepth, _bufMapToCamera);
                if (_onDepthToColor != null)
                    UpdateDepthToColor(_bufDepth);
                yield return null;
            }
        }

        private IEnumerator ConveyerUpdateMain() {
            while (true) {
                FlushTextureBuffer(_bufColor, _onColor);
                
                if (CreateDepthToColorIfNeed(_bufDepth))
                    FlushTextureBuffer(_depthToColorBuffer, _onDepthToColor);
                
                FlushTextureBuffer(_bufDepth, InvokeOnNewFrame);
                _evUnlock.Set();
                yield return null;
            }
        }

        private IEnumerator ConveyerBGUnlock() {
            while (true) {
                _evUnlock.WaitOne(200);
                UnlockBuffer(_bufColor);
                UnlockBuffer(_bufDepth);
                ActivateSensorsIfNeed();
                yield return null;
            }
        }
        
        private static void OnUpdateMapDepthToCamera(AbstractSensor abstractSensor) {
            var sensor = (SensorMapDepthToCamera) abstractSensor;
            var buffer = sensor.GetNewestAndLock();
            FlushTextureBuffer(buffer, null);
        }

        private void InvokeOnNewFrame(DepthBuffer buff) {
            _onNewFrame?.Invoke(buff, _bufMapToCamera);
        }

        private void UpdateDepthToColor(DepthBuffer depth) {
            if (_depthToColorBuffer == null || _depthToColorBuffer.data.Length != depth.data.Length)
                return;
            _dsm.Device.DepthMapToColorMap(depth.data, _depthToColorBuffer.data);
        }

        private static void FlushTextureBuffer<T>(T buffer, Action<T> action) where  T : ITextureBuffer {
            if (buffer != null) {
                buffer.UpdateTexture();
                action?.Invoke(buffer);
            }
        }

        private static void UnlockBuffer<T>(T buffer) where T : IBuffer {
            if (buffer != null) {
                buffer.Unlock();
            }
        }
#endregion

#region Processing
        private int4[] _holesSize;
        //      w.3
        //x.0->     <-z.2
        //      y.1
        private int CheckHole(DepthBuffer depth, NativeArray<ushort> darr, int x, int y, int dir, int h)  {
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

        private void FixDepthHoles(DepthBuffer depth) {
            var darr = depth.data; 
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

        private ushort SafeGet(DepthBuffer depth, NativeArray<ushort> darr, int x, int y) {
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
#endregion
    }
}