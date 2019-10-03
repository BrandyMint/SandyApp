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
        public readonly HandsProcessing Hands = new HandsProcessing();

        private static event Action<DepthBuffer, MapDepthToCameraBuffer> _onDepthDataBackground;
        private static event Action<ColorBuffer> _onColor;
        private static event Action<DepthToColorBuffer> _onDepthToColor;
        private static event Action<DepthBuffer, MapDepthToCameraBuffer> _onNewFrame;
        private static volatile bool _needActivateSensors;

        private DepthSensorConveyer _conveyer;
        private DepthSensorManager _dsm;
        private readonly ProcessingBase[] _processings;

        private ColorBuffer _bufColor;
        private DepthBuffer _bufDepth;
        private MapDepthToCameraBuffer _bufMapToCamera;
        private DepthToColorBuffer _bufDepthToColor;

        //private AutoResetEvent _evUnlock = new AutoResetEvent(false);
        private int _coveyerId = -1;

#region Initializing

        private DepthSensorSandboxProcessor() {
            _processings = new ProcessingBase[] {
                NoiseFilter,
                FixHoles,
                Hands
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
            DisposeBuffer(ref _bufDepth);
            foreach (var processing in _processings) {
                processing.Dispose();
            }
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
                ActivateSensorIfNeed(device.Color, ref Instance._bufColor, activeColor);
                ActivateSensorIfNeed(device.Depth, ref Instance._bufDepth, activeDepth);
                ActivateSensorIfNeed(device.MapDepthToCamera, ref Instance._bufMapToCamera, activeMap);
            }
        }

        private static void ActivateSensorIfNeed<T>(AbstractSensor sensor, ref T buffer, bool activate) where T: AbstractBuffer {
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
            if (_bufDepthToColor == null || _bufDepthToColor.length != depth.length) {
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
                    var depthBuffers = sDepth.Active ? sDepth.GetFreeBuffersAndLock(200) : null;
                    if (depthBuffers != null) {
                        var bufferChanged = false;
                        foreach (var p in _processings) {
                            p.OnlyRawBuffersIsInput = !bufferChanged;
                            p.Process(depthBuffers, _bufDepth);
                            bufferChanged |= p.Active;
                        }

                        foreach (var buffer in depthBuffers) {
                            buffer.Unlock();
                        }

                        _onDepthDataBackground?.Invoke(_bufDepth, _bufMapToCamera);
                        if (_onDepthToColor != null)
                            UpdateDepthToColor(_bufDepth);
                    }
                }

                yield return null;
            }
        }

        private IEnumerator ConveyerUpdateMain() {
            var sDepth = _dsm.Device.Depth;
            while (true) {
                if (_bufDepth == null) {
                    _bufDepth = sDepth.GetNewest().CreateSome<DepthBuffer>();
                } else {
                    FlushTextureBuffer(_bufColor, _onColor, true);

                    if (CreateDepthToColorIfNeed(_bufDepth))
                        FlushTextureBuffer(_bufDepthToColor, _onDepthToColor);

                    FlushTextureBuffer(_bufDepth, InvokeOnNewFrame);
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
        
        private static void OnUpdateMapDepthToCamera(AbstractSensor abstractSensor) {
            var sensor = (SensorMapDepthToCamera) abstractSensor;
            var buffer = sensor.GetNewest();
            FlushTextureBuffer(buffer, null);
        }

        private void InvokeOnNewFrame(DepthBuffer buff) {
            _onNewFrame?.Invoke(buff, _bufMapToCamera);
        }

        private void UpdateDepthToColor(DepthBuffer depth) {
            if (_bufDepthToColor == null || _bufDepthToColor.length != depth.length)
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

        private static void UnlockBuffer<T>(T buffer) where T : IBuffer {
            if (buffer != null) {
                buffer.Unlock();
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