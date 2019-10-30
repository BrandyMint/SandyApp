using System;
using System.Collections;
using BgConveyer;
using DepthSensor;
using DepthSensor.Buffer;
using DepthSensor.Device;
using DepthSensor.Sensor;
using DepthSensorSandbox.Processing;
using UnityEngine;

namespace DepthSensorSandbox {
    public class DepthSensorSandboxProcessor : MonoBehaviour {
        [SerializeField] private int  _buffersCount = 3;

        public int BuffersCount {
            get => _buffersCount;
            set {if (_buffersCount != value) {
                UpdateBuffersCount(value);
                _buffersCount = value;
            }}
        }

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

        public readonly FixHolesProcessing FixHoles = new FixHolesProcessing();
        public readonly NoiseFilterProcessing NoiseFilter = new NoiseFilterProcessing();

        private static event Action<DepthBuffer, MapDepthToCameraBuffer> _onDepthDataBackground;
        private static event Action<ColorBuffer> _onColor;
        private static event Action<DepthToColorBuffer> _onDepthToColor;
        private static event Action<DepthBuffer, MapDepthToCameraBuffer> _onNewFrame;
        private static volatile bool _needActivateSensors;

        private DepthSensorConveyer _conveyer;
        private DepthSensorManager _dsm;
        private readonly ProcessingBase[] _processings;

        private ColorBuffer _bufColor;
        private SensorDepth _bufDepth;
        private SensorDepth.Internal _bufDepthInternal;
        private MapDepthToCameraBuffer _bufMapToCamera;
        private DepthToColorBuffer _bufDepthToColor;

        //private AutoResetEvent _evUnlock = new AutoResetEvent(false);
        private int _coveyerId = -1;

#region Initializing

        private DepthSensorSandboxProcessor() {
            _processings = new ProcessingBase[] {
                NoiseFilter,
                FixHoles
            };
        }

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
            DisposeBuffer(ref _bufDepthToColor);
            DisposeSensor(ref _bufDepth);
        }

        private static void DisposeBuffer<T>(ref T stream) where T: AbstractBuffer {
            if (stream != null) {
                stream.Dispose();
                stream = null;
            }
        }
        
        private static void DisposeSensor<T>(ref T sensor) where T: AbstractSensor {
            if (sensor != null) {
                sensor.Dispose();
                sensor = null;
            }
        }

        private void OnDepthSensorAvailable() {
            var dev = GetDeviceIfAvailable();
            if (dev != null)
                dev.MapDepthToCamera.OnNewFrame += OnUpdateMapDepthToCamera;
            
            UpdateBuffersCount(BuffersCount);
            SetupConveyer(ConveyerUpdateBG(), ConveyerUpdateMain()/*, ConveyerBGUnlock()*/);
        }
        
        private static void UpdateBuffersCount(int value) {
            var dev = GetDeviceIfAvailable();
            if (dev != null) {
                dev.Color.BuffersCount = value;
                dev.Depth.BuffersCount = value;
            }
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
                ActivateSensorIfNeed(device.Color, Instance._bufColor, activeColor);
                ActivateSensorIfNeed(device.Depth, Instance._bufDepth?.GetNewest(), activeDepth);
                ActivateSensorIfNeed(device.MapDepthToCamera, Instance._bufMapToCamera, activeMap);
            }
        }

        private static void ActivateSensorIfNeed<T>(AbstractSensor sensor, T buffer, bool activate) where T: AbstractBuffer {
            if (!activate && buffer != null) {
                buffer.SafeUnlock();
                //buffer = null;
            }
            sensor.Active = activate;
        }

        private void SetupConveyer(IEnumerator bg, IEnumerator main/*, IEnumerator bgUnlock*/) {
            RemoveConveyers();
            
            var taskMainName = GetType().Name;
            var taskBGName = taskMainName + "BG";
            //var taskBGUnlock = taskBGName + "Unlock";
            _coveyerId = _conveyer.AddToBG(taskBGName, null, bg);
            _conveyer.AddToMainThread(taskMainName, taskBGName, main);
            //_conveyer.AddToBG(taskBGUnlock, taskMainName, bgUnlock);
        }

        private void RemoveConveyers() {
            if (_coveyerId >= 0)
                _conveyer.RemoveTask(_coveyerId);
        }
        
        private bool CreateDepthToColorIfNeed(DepthBuffer depth) {
            if (_onDepthToColor == null) {
                DisposeBuffer(ref _bufDepthToColor);
                return false;
            }
            if (_bufDepthToColor == null || _bufDepthToColor.data.Length != depth.data.Length) {
                DisposeBuffer(ref _bufDepthToColor);
                _bufDepthToColor = new DepthToColorBuffer(depth.width, depth.height);
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
                _bufColor = sColor.Active ? sColor.GetNewest() : null;
                _bufMapToCamera =  sMap.Active ? sMap.GetNewest() : null;

                if (_bufDepth != null) {
                    var depthRaw = sDepth.Active ? sDepth.GetNewestAndLock(200) : null;
                    if (depthRaw != null) {
                        var bufferChanged = false;
                        var depth = _bufDepth.GetOldest();
                        var depthPrev = _bufDepth.GetNewest();
                        foreach (var p in _processings) {
                            p.OnlyRawBufferIsInput = !bufferChanged;
                            p.Process(depthRaw, depth, depthPrev);
                            bufferChanged |= p.Active;
                        }
                        depthRaw.Unlock();

                        _bufDepthInternal.OnNewFrameBackground();
                        _onDepthDataBackground?.Invoke(depth, _bufMapToCamera);
                        if (_onDepthToColor != null)
                            UpdateDepthToColor(depth);
                    }
                }

                yield return null;
            }
        }

        private IEnumerator ConveyerUpdateMain() {
            var sDepth = _dsm.Device.Depth;
            while (true) {
                if (_bufDepth == null) {
                    _bufDepth = new SensorDepth(sDepth.GetNewest().CreateSome<DepthBuffer>()) {
                        BuffersCount = _buffersCount
                    };
                    _bufDepthInternal = new Sensor<DepthBuffer>.Internal(_bufDepth);
                } else {
                    var depth = _bufDepth.GetNewest();
                    FlushTextureBuffer(_bufColor, _onColor, true);

                    if (CreateDepthToColorIfNeed(depth))
                        FlushTextureBuffer(_bufDepthToColor, _onDepthToColor);

                    FlushTextureBuffer(depth, InvokeOnNewFrame);
                }

                //_evUnlock.Set();
                ActivateSensorsIfNeed();
                yield return null;
            }
        }

        /*private IEnumerator ConveyerBGUnlock() {
            while (true) {
                _evUnlock.WaitOne(200);
                UnlockBuffer(_bufColor);
                //UnlockBuffer(_bufDepth);
                ActivateSensorsIfNeed();
                yield return null;
            }
        }*/
        
        private static void OnUpdateMapDepthToCamera(ISensor abstractSensor) {
            var sensor = (SensorMapDepthToCamera) abstractSensor;
            var buffer = sensor.GetNewest();
            FlushTextureBuffer(buffer, null);
        }

        private void InvokeOnNewFrame(DepthBuffer buff) {
            _onNewFrame?.Invoke(buff, _bufMapToCamera);
        }

        private void UpdateDepthToColor(DepthBuffer depth) {
            if (_bufDepthToColor == null || _bufDepthToColor.data.Length != depth.data.Length)
                return;
            _dsm.Device.DepthMapToColorMap(depth.data, _bufDepthToColor.data);
        }

        private static void FlushTextureBuffer<T>(T buffer, Action<T> action, bool dolock = false) where  T : ITextureBuffer {
            if (buffer != null) {
                if (!dolock || buffer.Lock(200)) {
                    buffer.UpdateTexture();
                    action?.Invoke(buffer);
                    if (dolock)
                        buffer.Unlock();
                }
            }
        }
#endregion

#if UNITY_EDITOR

        private void OnValidate() {
            var dev = GetDeviceIfAvailable();
            if (dev != null && dev.Depth.BuffersCount != BuffersCount) {
                UpdateBuffersCount(BuffersCount);
            }
        }
#endif
    }
}