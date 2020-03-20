using System;
using System.Collections;
using System.Linq;
using BgConveyer;
using DepthSensor;
using DepthSensor.Buffer;
using DepthSensor.Device;
using DepthSensor.Sensor;
using DepthSensorSandbox.Processing;
using DepthSensorSandbox.Visualisation;
using UnityEngine;
using Utilities;

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

        public static event Action<Rect> OnCroppingChanged; 

        public static DepthSensorSandboxProcessor Instance { get; private set; }

        private InitProcessing _initProcessing = new InitProcessing();
        public readonly FixHolesProcessing FixHoles = new FixHolesProcessing();
        public readonly NoiseFilterProcessing NoiseFilter = new NoiseFilterProcessing();
        public readonly HandsProcessing Hands = new HandsProcessing();
        public readonly DepthToColorProcessing DepthToColor = new DepthToColorProcessing();

        private static event Action<DepthBuffer, MapDepthToCameraBuffer> _onDepthDataBackground;
        private static event Action<ColorBuffer> _onColor;
        private static event Action<DepthToColorBuffer> _onDepthToColor;
        private static event Action<DepthBuffer, MapDepthToCameraBuffer> _onNewFrame;
        private static volatile bool _needActivateSensors;

        private DepthSensorConveyer _conveyer;
        private ProcessingBase[] _processings;
        private readonly ProcessingBase[] _standartProcessings;
        private readonly ProcessingBase[] _allProcessings;
        private readonly ProcessingBase[] _initProcessings;

        private ColorBuffer _bufColor;
        private SensorDepth _bufDepth;
        private SensorDepth.Internal _bufDepthInternal;
        private MapDepthToCameraBuffer _bufMapToCamera;

        private int _coveyerId = -1;
        private static bool _processColor;
        private static bool _processDepth;
        private static bool _processMap;
        private SandboxCamera _needUpdateCroppingCamera;
        private Rect _cropping;

#region Initializing

        private DepthSensorSandboxProcessor() {
            HandsProcessingSwitch(false);
            _processings = _initProcessings = new ProcessingBase[] {
                _initProcessing
            };
            _standartProcessings = new ProcessingBase[] {
                Hands,
                NoiseFilter,
                FixHoles,
                DepthToColor
            };
            _allProcessings = _initProcessings.Union(_standartProcessings).ToArray();

            _initProcessing.OnDone += OnInitProcessingsDone;
        }

        private void Awake() {
            Instance = this;
        }

        private void Start() {
            _conveyer = gameObject.AddComponent<DepthSensorConveyer>();
            _conveyer.OnNoFrame += ActivateSensorsIfNeed;
            DepthSensorManager.OnInitialized += OnDepthSensorAvailable;
            if (DepthSensorManager.IsInitialized()) {
                OnDepthSensorAvailable();
            }

            SandboxCamera.AfterCalibrationUpdated += UpdateCropping;
            SetupConveyer(ConveyerUpdateBG(), ConveyerUpdateMain());
        }

        private void OnDestroy() {
            SandboxCamera.AfterCalibrationUpdated -= UpdateCropping;
            if (_conveyer != null)
                _conveyer.OnNoFrame -= ActivateSensorsIfNeed;
            RemoveConveyers();
            DepthSensorManager.OnInitialized -= OnDepthSensorAvailable;
            if (DepthSensorManager.IsInitialized()) {
                UnSubscribeDevice(DepthSensorManager.Instance.Device);
            }
            foreach (var processing in _allProcessings) {
                processing.Dispose();
            }
        }

        public void HandsProcessingSwitch(bool enable) {
            NoiseFilter.Active = !enable;
            Hands.Active = enable;
        }
        
        private static void DisposeSensor<T>(ref T sensor) where T: AbstractSensor {
            if (sensor != null) {
                sensor.Dispose();
                sensor = null;
            }
        }

        private void OnDepthSensorAvailable() {
            var dev = GetDeviceIfAvailable();
            if (dev != null) {
                dev.MapDepthToCamera.OnNewFrame += OnUpdateMapDepthToCamera;
                dev.OnClose += UnSubscribeDevice;
            }

            UpdateBuffersCount(BuffersCount);
            FindCameraIfNotAndUpdateCropping();
        }

        private void UnSubscribeDevice(DepthSensorDevice device) {
            device.OnClose -= UnSubscribeDevice;
            device.MapDepthToCamera.OnNewFrame -= OnUpdateMapDepthToCamera;
            DisposeSensor(ref _bufDepth);
            _bufColor = null;
            _bufMapToCamera = null;
            _needActivateSensors = true;
        }

        private static void UpdateBuffersCount(int value) {
            var dev = GetDeviceIfAvailable();
            if (dev != null) {
                dev.Color.BuffersCount = value;
                dev.Depth.BuffersCount = value;
            }
        }

        private static DepthSensorDevice GetDeviceIfAvailable() {
            if (DepthSensorManager.IsInitialized()) {
                return DepthSensorManager.Instance.Device;
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
                _processColor = _onColor != null || _onDepthToColor != null;
                _processDepth = _onNewFrame != null || _onDepthDataBackground != null || _onDepthToColor != null;
                _processMap = _onNewFrame != null || _onDepthDataBackground != null;
                _needActivateSensors = false;
                ActivateSensorIfNeed(device.Color, Instance._bufColor, _processColor);
                ActivateSensorIfNeed(device.Depth, Instance._bufDepth?.GetNewest(), _processDepth);
                ActivateSensorIfNeed(device.MapDepthToCamera, Instance._bufMapToCamera, _processMap);

                Instance.DepthToColor.Active = _onDepthToColor != null;
            }
        }

        private static void ActivateSensorIfNeed<T>(ISensor sensor, T buffer, bool activate) where T: AbstractBuffer {
            var hasExternalUsings = sensor.AnySubscribedToNewFramesExcept(
                                        typeof(BgConveyer.BgConveyer), 
                                        typeof(DepthSensorSandboxProcessor)
                                    );
            var prevActive = sensor.Active;
            sensor.Active = activate || prevActive && hasExternalUsings;
            if (prevActive != activate) {
                if (activate) {
                    sensor.OnNewFrame += EmptySensorSubscribe;
                } else {
                    sensor.OnNewFrame -= EmptySensorSubscribe;
                }
            }
        }

        private static void EmptySensorSubscribe(ISensor sensor) {
            //need for sensor.AnySubscribedToNewFrames == true when sensor used by DepthSensorSandboxProcessor
        }

        private void SetupConveyer(IEnumerator bg, IEnumerator main) {
            RemoveConveyers();
            
            var taskMainName = GetType().Name;
            var taskBGName = taskMainName + "BG";
            _coveyerId = _conveyer.AddToBG(taskBGName, null, bg);
            _conveyer.AddToMainThread(taskMainName, taskBGName, main);
        }

        private void RemoveConveyers() {
            if (_coveyerId >= 0)
                _conveyer.RemoveTask(_coveyerId);
        }

        private void FindCameraIfNotAndUpdateCropping() {
            if (_needUpdateCroppingCamera == null)
                _needUpdateCroppingCamera = FindObjectOfType<SandboxCamera>();
            UpdateCropping(_needUpdateCroppingCamera);
        }

        private void UpdateCropping(SandboxCamera scam) {
            var device = GetDeviceIfAvailable();
            if (device == null) {
                _needUpdateCroppingCamera = scam;
                return;
            }
            _needUpdateCroppingCamera = null;
            
            if (scam == null) return;
            var cam = scam.GetCamera();
            var maxDist = Prefs.Sandbox.ZeroDepth + Prefs.Sandbox.OffsetMinDepth;
            var minDist = Prefs.Sandbox.ZeroDepth - 3f * Prefs.Sandbox.OffsetMaxDepth;
            minDist = Mathf.Max(Mathf.Lerp(minDist, 0.4f * Prefs.Sandbox.ZeroDepth, 0.5f), cam.nearClipPlane);
            var meshTransform = FindObjectOfType<SandboxMesh>()?.transform;
            var croppingMax = cam.GetCroppingToDepth(meshTransform, maxDist, device);
            var croppingMin = cam.GetCroppingToDepth(meshTransform, minDist, device);
            _cropping = RectUtils.Encompass(croppingMin, croppingMax);
            //Debug.Log("cropping " + cropping);
            //return;
            foreach (var processing in _allProcessings) {
                processing.SetCropping(_cropping);
            }
            OnCroppingChanged?.Invoke(_cropping);
        }

        public Rect GetCropping() {
            return _cropping;
        }
#endregion

#region Conveyer
        private IEnumerator ConveyerUpdateBG() {
            while (true) {
                var device = DepthSensorManager.Instance.Device;
                if (device != null) {
                    var sDepth = device.Depth;
                    var sColor = device.Color;
                    var sMap = device.MapDepthToCamera;

                    _bufColor = _processColor && sColor.Active ? sColor.GetNewest() : null;
                    _bufMapToCamera = _processMap && sMap.Active ? sMap.GetNewest() : null;

                    if (_bufDepth != null) {
                        var depthRaw = _processDepth && sDepth.Active
                            ? sDepth.GetNewest()
                            : null;
                        if (depthRaw != null) {
                            Hands.SetMapDepthToCamera(_bufMapToCamera);
                            var bufferChanged = false;
                            var depth = _bufDepth.GetOldest();
                            var depthPrev = _bufDepth.GetNewest();
                            foreach (var p in _processings) {
                                p.OnlyRawBufferIsInput = !bufferChanged;
                                p.Process(depthRaw, depth, depthPrev);
                                bufferChanged |= p.Active;
                            }

                            _bufDepthInternal.OnNewFrameBackground();
                            _onDepthDataBackground?.Invoke(depth, _bufMapToCamera);
                        }
                    }
                }

                yield return null;
            }
        }

        private IEnumerator ConveyerUpdateMain() {
            while (true) {
                var device = DepthSensorManager.Instance.Device;
                if (device != null && !ReCreateBuffersIfNeed(device)) {
                    var depth = _bufDepth.GetNewest();
                    if (_processColor)
                        FlushTextureBuffer(_bufColor, _onColor, true);

                    if (DepthToColor.Active)
                        FlushTextureBuffer(DepthToColor.Map, _onDepthToColor);

                    if (_processDepth)
                        FlushTextureBuffer(depth, InvokeOnNewFrame);
                    //_initProcessing.DebugShowErrorsMap(depth, device);
                }
                yield return null;
                ActivateSensorsIfNeed();
            }
        }

        private bool ReCreateBuffersIfNeed(DepthSensorDevice device) {
            var bufDevice = device.Depth.GetNewest();
            var bufProcessing = _bufDepth?.GetNewest();
            if (bufProcessing == null || bufProcessing.width != bufDevice.width || bufProcessing.height != bufDevice.height) {
                foreach (var processing in _allProcessings) {
                    processing.InitInMainThread(device);
                }
                DisposeSensor(ref _bufDepth);
                _bufDepth = new SensorDepth(bufDevice.CreateSome<DepthBuffer>()) {
                    BuffersCount = _buffersCount
                };
                _bufDepthInternal = new Sensor<DepthBuffer>.Internal(_bufDepth);

                _processings = _initProcessings;
                _initProcessing.StartInit(_bufDepth, new ProcessingBase[]{FixHoles});
                return true;
            }

            return false;
        }

        private void OnInitProcessingsDone() {
            foreach (var processing in _allProcessings) {
                processing.SetErrorsMap(_initProcessing.ErrorsMap);
            }
            _processings = _standartProcessings;
        }
        
        private static void OnUpdateMapDepthToCamera(ISensor abstractSensor) {
            var sensor = (SensorMapDepthToCamera) abstractSensor;
            var buffer = sensor.GetNewest();
            FlushTextureBuffer(buffer, null);
        }

        private void InvokeOnNewFrame(DepthBuffer buff) {
            if (_bufMapToCamera != null)
                _onNewFrame?.Invoke(buff, _bufMapToCamera);
        }

        private static void FlushTextureBuffer<T>(T buffer, Action<T> action, bool dolock = false) where  T : ITextureBuffer {
            if (buffer != null) {
                /*if (!dolock || buffer.Lock(200)) {*/
                    buffer.UpdateTexture();
                    action?.Invoke(buffer);
                    /*if (dolock)
                        buffer.Unlock();
                }*/
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