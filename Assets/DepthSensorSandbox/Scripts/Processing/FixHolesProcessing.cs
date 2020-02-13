using System.Threading.Tasks;
using DepthSensor.Buffer;
using Unity.Mathematics;
using UnityEngine;

namespace DepthSensorSandbox.Processing {
    public class FixHolesProcessing : ProcessingBase {
        private const ushort _BAD_HOLE_FIX = 5000;

        private int4[] _holesSize;
        private int _maxDem;
        
        protected override void ProcessInternal() {
            ReCreateIfNeed(ref _holesSize, _out.length);
            _maxDem = Mathf.Max(_inDepth.width, _inDepth.height);
            Parallel.For(0, _maxDem, FindHolesBody);
            Parallel.For(0, _inDepth.height, FixDepthHolesBody);
        }

        private void FindHolesBody(int x) {
            int hUp = 0, hDown = 0, hLeft = 0, hRight = 0;
            for (int y = 0; y < _maxDem; ++y) {
                if (x < _inDepth.width && y < _inDepth.height) {
                    hUp = CheckHole(_inDepth, x, y, 1, hUp);
                    hDown = CheckHole(_inDepth, x, _inDepth.height - y - 1, 3, hDown);
                }
                if (x < _inDepth.height && y < _inDepth.width) {
                    hLeft = CheckHole(_inDepth, y, x, 0, hLeft);
                    hRight = CheckHole(_inDepth, _inDepth.width - y - 1,  x, 2, hRight);
                }
            }
        }

        //      w.3
        //x.0->     <-z.2
        //      y.1
        private int CheckHole(DepthBuffer depth, int x, int y, int dir, int h)  {
            var i = depth.GetIFrom(x, y);
            var d = depth.data[i];
            if (d == Sampler.INVALID_DEPTH)
                ++h;
            else {
                h = 0;
            }
            _holesSize[i][dir] = h;
            return h;
        }

        private void FixDepthHolesBody(int y) {
            for (int x = 0; x < _inDepth.width; ++x) {
                var i = _inDepth.GetIFrom(x, y);
                var d = _inDepth.data[i];
                var h = _holesSize[i];
                if (d == Sampler.INVALID_DEPTH) {
                    var up = _s.SafeGet(_inDepth, x, y + h.w);
                    var down = _s.SafeGet(_inDepth, x, y - h.y);
                    var left = _s.SafeGet(_inDepth, x - h.x, y);
                    var right = _s.SafeGet(_inDepth, x + h.z, y);
                    up = SetPriorityToIfInvalid(up, down, left, right);
                    down = SetPriorityToIfInvalid(down, up, left, right);
                    left = SetPriorityToIfInvalid(left, right, up, down);
                    right = SetPriorityToIfInvalid(right, left, up, down);
                    var dd = FixDepthHole(up, down, h.w, h.y) + FixDepthHole(left, right, h.x, h.z);
                    _out.data[i] = (ushort) (dd / 2);
                } else if (OnlyRawBufferIsInput) {
                    _out.data[i] = d;
                }
            }
        }

        private ushort FixDepthHole(ushort v1, ushort v2, int s1, int s2) {
            var k = (float) s1 / (s1 + s2);
            return (ushort) Mathf.Lerp(v1, v2, k);
        }
        
        private static ushort SetPriorityToIfInvalid(ushort val, ushort v1, ushort v2, ushort v3) {
            if (val != Sampler.INVALID_DEPTH)
                return val;
            if (v1 != Sampler.INVALID_DEPTH)
                return v1;
            if (v2 != Sampler.INVALID_DEPTH)
                return v2;
            if (v3 != Sampler.INVALID_DEPTH)
                return v3;
            return _BAD_HOLE_FIX;
        }
    }
}