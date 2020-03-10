using DepthSensor;
using DepthSensor.Buffer;
using DepthSensor.Device;
using DepthSensorSandbox.Processing;
using UnityEngine;
using Utilities;

namespace DepthSensorSandbox.Projecting {
    [RequireComponent(typeof(Camera))]
    public class SensorProjectorByDevice : SensorProjector {
        private Camera _cam;
        private DepthBuffer _depth;
        private RenderTexture _tex;
        private DepthSensorDevice _device;

        protected override void Awake() {
            _cam = GetComponent<Camera>();
            base.Awake();
            DepthSensorSandboxProcessor.OnNewFrame += OnFrame;
        }

        private void OnDestroy() {
            DepthSensorSandboxProcessor.OnNewFrame -= OnFrame;
            if (_tex != null) {
                _cam.targetTexture = null;
                Destroy(_tex);
            }
        }

        private void OnFrame(DepthBuffer depth, MapDepthToCameraBuffer map) {
            _depth = depth;
            if (TexturesHelper.ReCreateIfNeed(ref _tex, depth.width, depth.height, 0, RenderTextureFormat.R8)) {
                _cam.targetTexture = _tex;
            }
            
            _device = DepthSensorManager.Instance.Device;
            if (_device == null)
                return;
            var fov = _device.Depth.FOV;
            _cam.aspect = fov.x / fov.y;
            _cam.fieldOfView = fov.y;
        }

        public override Vector3 PlaceOnSandbox(Vector3 worldPos) {
            if (_depth == null || _device == null || !_device.IsAvailable())
                return worldPos;
            
            var p = _cam.WorldToScreenPoint(worldPos);
            p.y = _depth.height - p.y - 1f;
            if (_depth.IsValidXY(p)) {
                var d = _depth.data[_depth.GetIFrom((int) p.x, (int) p.y)];
                if (d != Sampler.INVALID_DEPTH) {
                    var pd = _device.DepthMapPosToCameraPos(p, d);
                    var pos = transform.InverseTransformPoint(worldPos);
                    pos.z = pd.z;
                    return transform.TransformPoint(pos);
                }
            }

            return worldPos;
        }
    }
}