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
        public class DepthToColorStream : TextureStream<half2> {
            public DepthToColorStream(int width, int height) : base(width, height, TextureFormat.RGHalf) { }
            public DepthToColorStream(bool available) : base(available) { }
        } 
        
        public static event Action<DepthStream, MapDepthToCameraStream> OnDepthDataBackground {
            add { _onDepthDataBackground += value; ActivateDepthIfNeed(); }
            remove { _onDepthDataBackground -= value; ActivateDepthIfNeed(); }
        }
        public static event Action<ColorStream> OnColor {
            add { _onColor += value; ActivateColorIfNeed(); }
            remove { _onColor -= value; ActivateColorIfNeed(); }
        }
        public static event Action<DepthToColorStream> OnDepthToColor {
            add { _onDepthToColor += value; ActivateColorIfNeed(); }
            remove { _onDepthToColor -= value; ActivateColorIfNeed(); }
        }
        public static event Action<DepthStream, MapDepthToCameraStream> OnNewFrame;

        public static DepthSensorSandboxProcessor Instance { get; private set; }

        public static event Action<DepthStream, MapDepthToCameraStream> _onDepthDataBackground;
        public static event Action<ColorStream> _onColor;
        private static event Action<DepthToColorStream> _onDepthToColor;

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
            ActivateDepthIfNeed();
            ActivateColorIfNeed();
            SetupConveyer(ConveyerUpdateBG(), ConveyerUpdateMain());
        }

        private static DepthSensorDevice GetDeviceIfAvailable() {
            var dsm = DepthSensorManager.Instance;
            if (dsm != null && dsm.Device != null && dsm.Device.IsAvailable()) {
                return dsm.Device;
            }
            return null;
        }
        
        private static void ActivateColorIfNeed() {
            var device = GetDeviceIfAvailable();
            if (device != null) {
                device.Color.Active = _onColor != null || _onDepthToColor != null;
            }
        }
        
        private static void ActivateDepthIfNeed() {
            var device = GetDeviceIfAvailable();
            if (device != null) {
                var needDepth = _onDepthDataBackground != null || _onColor != null;
                device.Depth.Active = needDepth || _onDepthToColor != null;
                device.MapDepthToCamera.Active = needDepth;
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
#endregion

#region Conveyer
        private IEnumerator ConveyerUpdateBG() {
            var sDepth = _dsm.Device.Depth;
            var sMap = _dsm.Device.MapDepthToCamera;
            while (true) {
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
                OnNewFrame?.Invoke(sDepth, sMap);
                _onColor?.Invoke(sColor);
                if (CreateDepthToColorIfNeed(sDepth))
                    _onDepthToColor?.Invoke(_depthToColorStream);
                yield return null;
            }
        }

#endregion

#region Processing

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