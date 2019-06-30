using System;
using System.Collections;
using BgConveyer;
using DepthSensor;
using DepthSensor.Device;
using UnityEngine;

namespace DepthSensorSandbox {
    public class DepthSensorSandboxProcessor : MonoBehaviour {
        public static event Action<int, int, ushort[], Vector2[]> OnDepthDataBackground {
            add { ActivateDepthIfNeed(); _onDepthDataBackground += value; }
            remove { _onDepthDataBackground -= value; ActivateDepthIfNeed(); }
        }
        public static event Action<int, int, Vector2[]> OnDepthToColorBackground {
            add { ActivateColorIfNeed(); _onDepthToColorBackground += value; }
            remove { _onDepthToColorBackground -= value; ActivateColorIfNeed(); }
        }
        public static event Action<int, int, ushort[], Vector2[]> OnNewFrame;
        public static event Action<int, int, byte[], TextureFormat> OnColor {
            add { ActivateColorIfNeed(); _onColor += value; }
            remove { _onColor -= value; ActivateColorIfNeed(); }
        }
        
        public static DepthSensorSandboxProcessor Instance { get; private set; }

        public static event Action<int, int, ushort[], Vector2[]> _onDepthDataBackground;
        public static event Action<int, int, Vector2[]> _onDepthToColorBackground;
        public static event Action<int, int, byte[], TextureFormat> _onColor;

        private DepthSensorConveyer _kinConv;
        private DepthSensorManager _dsm;

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
        }

        private void OnDepthSensorAvailable() {
            ActivateDepthIfNeed();
            ActivateColorIfNeed();
            SetupConveyer(ConveyerUpdateBG(), ConveyerUpdateMain());
        }

        private static DepthSensorDevice GetDeviceIfAvailable() {
            var dsm = DepthSensorManager.Instance;
            if (dsm != null && dsm.Device.IsAvailable()) {
                return dsm.Device;
            }
            return null;
        }
        
        private static void ActivateColorIfNeed() {
            var device = GetDeviceIfAvailable();
            if (device != null) {
                device.Color.Active = _onColor != null || _onDepthToColorBackground != null;
            }
        }
        
        private static void ActivateDepthIfNeed() {
            var device = GetDeviceIfAvailable();
            if (device != null) {
                var needDepth = _onDepthDataBackground != null;
                device.Depth.Active = needDepth;
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
                _onDepthDataBackground?.Invoke(sDepth.width, sDepth.height, sDepth.data, sMap.data);
                if (_onDepthToColorBackground != null) 
                    UpdateDepthToColorMap(sDepth.width, sDepth.height, sDepth.data);
                yield return null;
            }
        }
        
        private IEnumerator ConveyerUpdateMain() {
            var sDepth = _dsm.Device.Depth;
            var sMap = _dsm.Device.MapDepthToCamera;
            var sColor = _dsm.Device.Color;
            while (true) {
                OnNewFrame?.Invoke(sDepth.width, sDepth.height, sDepth.data, sMap.data);
                _onColor?.Invoke(sColor.width, sColor.height, sColor.data, sColor.format);
                yield return null;
            }
        }
#endregion

#region DepthToColor
        private Vector2[] _depthToColorMap;

        private void UpdateDepthToColorMap(int width, int height, ushort[] depth) {
            if (_depthToColorMap == null || _depthToColorMap.Length != depth.Length) {
                _depthToColorMap = new Vector2[depth.Length];
            }
            _dsm.Device.DepthToColorMap(depth, _depthToColorMap);
            _onDepthToColorBackground?.Invoke(width, height, _depthToColorMap);
        }
#endregion
    }
}