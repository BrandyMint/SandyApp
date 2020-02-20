using DepthSensor;
using DepthSensor.Buffer;
using DepthSensor.Device;
using DepthSensorSandbox;
using DepthSensorSandbox.Processing;
using UnityEngine;

namespace Games.Common {
    public class HandsRaycaster {
        public delegate void FireListener(Ray viewPos);

        public event FireListener HandFire;
        public Transform SandboxTransform;
        public ushort MaxHandDepth = 30;

        public virtual void SetEnable(bool enable) {
            if (enable) {
                DepthSensorSandboxProcessor.OnNewFrame += OnNewFrame;
            } else {
                DepthSensorSandboxProcessor.OnNewFrame -= OnNewFrame;
            }
        }

        private HandsProcessing _handsProcessor;
        private DepthBuffer _hands;
        private DepthSensorDevice _device;

        private void OnNewFrame(DepthBuffer depth, MapDepthToCameraBuffer map) {
            _handsProcessor = DepthSensorSandboxProcessor.Instance.Hands;
            _hands = _handsProcessor.HandsDepth.GetNewest();
            _device = DepthSensorManager.Instance.Device;
            var s = _handsProcessor.GetSamplerHandsDecreased();
            s.Each(DoRaycast);
        }

        private void DoRaycast(int i) {
            var handsDepth = _hands.data[i];
            if (handsDepth == Sampler.INVALID_DEPTH || handsDepth > MaxHandDepth)
                return;
            
            var depthPoint = _handsProcessor.HandsToMaskXY(i);
            var dir = _device.DepthMapPosToCameraPos(depthPoint, 1000);
            var origin = SandboxTransform.TransformPoint(Vector3.zero);
            dir = SandboxTransform.TransformDirection(dir);
            HandFire?.Invoke(new Ray(origin, dir));
        }

        public void RaycastFromMouse(Camera cam) {
            var pos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 1f);
            HandFire?.Invoke(cam.ScreenPointToRay(pos));
        }
    }
}