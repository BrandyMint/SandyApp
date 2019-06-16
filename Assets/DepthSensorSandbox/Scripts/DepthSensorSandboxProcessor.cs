using System;
using System.Collections;
using BgConveyer;
using DepthSensor;
using UnityEngine;

namespace DepthSensorSandbox {
    public class DepthSensorSandboxProcessor : MonoBehaviour {
        public static Action<int, int, ushort[], Vector2[]> OnDepthDataBackground;
        public static Action OnNewFrame;
        public static DepthSensorSandboxProcessor Instance { get; private set; }
        
        private DepthSensorConveyer _kinConv;
        private DepthSensorManager _dsm;
        
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
            _dsm.OnInitialized -= OnDepthSensorAvailable;
        }

        private void OnDepthSensorAvailable() {
            _dsm.Device.Depth.Active = true;
            _dsm.Device.MapDepthToCamera.Active = true;
            var taskMainName = GetType().Name;
            var taskBGName = taskMainName + "BG";
            _kinConv.AddToBG(taskBGName, null, ConveyerUpdateBG());
            _kinConv.AddToMainThread(taskMainName, taskBGName, ConveyerUpdateMain());
        }
        
        private IEnumerator ConveyerUpdateBG() {
            var sDepth = _dsm.Device.Depth;
            var sMap = _dsm.Device.MapDepthToCamera;
            while (true) {
                OnDepthDataBackground?.Invoke(sDepth.width, sDepth.height, sDepth.data, sMap.data);
                yield return null;
            }
        }
        
        private IEnumerator ConveyerUpdateMain() {
            while (true) {
                OnNewFrame?.Invoke();
                yield return null;
            }
        }
    }
}