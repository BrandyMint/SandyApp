using System;
using DepthSensor;
using DepthSensor.Buffer;
using DepthSensor.Device;
using DepthSensorSandbox;
using DepthSensorSandbox.Processing;
using Games.Common.ColliderGenerator;
using UnityEngine;

namespace Games.Common {
    public class HandsRaycaster : IColliderGeneratorDataProvider {
        public delegate void FireListener(Ray ray, Vector2 uv);
        public delegate void CustomProcessor(DepthSensorDevice device, HandsProcessing processor, DepthBuffer hands, Sampler s);
        public event FireListener HandFire;
        public event Action OnPreProcessDepthFrame;
        public event Action OnPostProcessDepthFrame;
        public event CustomProcessor CustomProcessingFrame;
        
        public Transform SandboxTransform;
        public Camera Cam;
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
        private Sampler _sampler;
        private DepthBuffer _depth;

        private void OnNewFrame(DepthBuffer depth, MapDepthToCameraBuffer map) {
            _handsProcessor = DepthSensorSandboxProcessor.Instance.Hands;
            _hands = _handsProcessor.HandsDepthDecreased.GetNewest();
            _depth = depth;
            _device = DepthSensorManager.Instance.Device;
            
            OnPreProcessDepthFrame?.Invoke();
            _sampler = _handsProcessor.GetSamplerHandsDecreased();
            CustomProcessingFrame?.Invoke(_device, _handsProcessor, _hands, _sampler);
            if (HandFire != null)
                _sampler.Each(DoRaycast);
            OnPostProcessDepthFrame?.Invoke();
        }

        private void DoRaycast(int i) {
            var handsDepth = _hands.data[i];
            if (!IsHandInteract(handsDepth))
                return;

            var p = ProjectToWorld(i, handsDepth);
            var uv = Cam.WorldToViewportPoint(p);
            var ray = Cam.ViewportPointToRay(uv);
            HandFire.Invoke(ray, uv);
        }

        public void RaycastFromMouse() {
            var pos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 1f);
            var uv = Cam.ScreenToViewportPoint(pos);
            var ray = Cam.ScreenPointToRay(pos);
            HandFire?.Invoke(ray, uv);
        }

        private Vector3 ProjectToWorld(int i, ushort handsDepth) {
            var depthPoint = _handsProcessor.DecreasedToFullXY(i);
            var d = _depth.data[_depth.GetIFrom((int) depthPoint.x, (int) depthPoint.y)];
            var p = _device.DepthMapPosToCameraPos(depthPoint, (ushort) (d - handsDepth));
            return SandboxTransform.TransformPoint(p);
        }

        public Vector3 ProjectToWorld(Vector2 p) {
            var i = _sampler.GetIFrom((int) p.x, (int) p.y);
            var handsDepth = _hands.data[i];
            return ProjectToWorld(i, handsDepth);
        }
        
        private bool IsHandInteract(ushort d) {
            return d != Sampler.INVALID_DEPTH && d <= MaxHandDepth;
        }

        public Sampler Sampler {
            get => _sampler;
            set => _sampler = value;
        }
        
        public bool IsShapePixel(int x, int y) {
            var i = _sampler.GetIFrom(x, y);
            var handsDepth = _hands.data[i];
            return IsHandInteract(handsDepth);
        }

        public bool IsShapePixel(Vector2Int p) {
            return IsShapePixel(p.x, p.y);
        }
    }
}