using DepthSensor.Device;
using UnityEngine;

namespace DepthSensorSandbox.Processing {
    public class ZeroInBordersProcessing : ProcessingBase {
        private const ushort _BAD_HOLE_FIND_MAX_RADIUS = 100;
        private Sampler _samplerMax = Sampler.Create();

        protected override void InitInMainThreadInternal(DepthSensorDevice device) {
            base.InitInMainThreadInternal(device);
            _samplerMax.SetDimens(_s.width, _s.height);
        }

        protected override void ProcessInternal() {
            //_zeroDepth = Sampler.INVALID_DEPTH;//(ushort) ((Prefs.Sandbox.ZeroDepth + Prefs.Calibration.Position.z) * 1000f);
            _samplerMax.EachParallelHorizontal(ZeroBody);
        }

        public void SetMaxCropping01(Rect rect) {
            _samplerMax.SetCropping01(rect);
        }

        //private ushort _zeroDepth;
        private void ZeroBody(int i) {
            var p = _s.GetXYiFrom(i);
            var r = _s.Rect;
            if (r.Contains(p)) {
                if (OnlyRawBufferIsInput)
                    _out.data[i] = _rawBuffer.data[i];
            } else {
                p = new Vector2Int(
                    Mathf.Clamp(p.x, r.xMin, r.xMax), 
                    Mathf.Clamp(p.y, r.yMin, r.yMax)
                );
                var d = Sampler.INVALID_DEPTH;
                var k = _s.GetDirToCenter4(p);
                var j = _s.GetIndexOfNeighbor(_s.GetIFrom(p.x, p.y), k);
                for (int n = 0; n < _BAD_HOLE_FIND_MAX_RADIUS; n++) {
                    var dd = _out.data[j];
                    if (dd != Sampler.INVALID_DEPTH) {
                        d = dd;
                        break;
                    }
                    j = _s.GetIndexOfNeighbor(j, k);
                }
                _out.data[i] = d;
                //_out.data[i] = _zeroDepth;
            }
        }
    }
}