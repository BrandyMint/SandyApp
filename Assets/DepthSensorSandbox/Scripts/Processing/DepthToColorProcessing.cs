using DepthSensor.Buffer;
using DepthSensor.Device;

namespace DepthSensorSandbox.Processing {
    public class DepthToColorProcessing : ProcessingBase {
        public DepthToColorBuffer Map => _bufDepthToColor;
        
        private DepthToColorBuffer _bufDepthToColor;
        private DepthSensorDevice _device;
        
        public override void Dispose() {
            _bufDepthToColor?.Dispose();
            base.Dispose();
        }

        protected override void InitInMainThreadInternal(DepthSensorDevice device) {
            _device = device;
            var buffer = device.Depth.GetNewest();
            AbstractBuffer2D.ReCreateIfNeed(ref _bufDepthToColor, buffer.width, buffer.height);
        }

        protected override void ProcessInternal() {
            if (!CheckValid(_bufDepthToColor))
                return;

            _s.EachParallelHorizontal(DepthToColorBody);
        }

        private void DepthToColorBody(int i) {
            var p = _s.GetXYFrom(i);
            _bufDepthToColor.data[i] = _device.DepthMapPosToColorMapPos(p, _inDepth.data[i]);
        }
    }
}