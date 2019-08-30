using System.Threading.Tasks;
using DepthSensor.Buffer;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace DepthSensorSandbox.Processing {
    public class FixHolesProcessing : ProcessingBase {
        private const ushort _BAD_HOLE_FIX = 5000;

        private int4[] _holesSize;
        
        protected override void ProcessInternal(DepthBuffer[] rawBuffers, DepthBuffer inOut) {
            ReCreateIfNeed(ref _holesSize, inOut.data.Length);
            var inBuffer = OnlyRawBuffersIsInput ? rawBuffers[0] : inOut;
            FindHoles(inBuffer);
            FixDepthHoles(inBuffer, inOut);
        }

        private void FindHoles(DepthBuffer depth) {
            var darr = depth.data;

            var maxDem = Mathf.Max(depth.width, depth.height);
            Parallel.For(0, maxDem, x => {
                int hUp = 0, hDown = 0, hLeft = 0, hRight = 0;
                for (int y = 0; y < maxDem; ++y) {
                    if (x < depth.width && y < depth.height) {
                        hUp = CheckHole(depth, darr, x, y, 1, hUp);
                        hDown = CheckHole(depth, darr, x, depth.height - y - 1, 3, hDown);
                    }
                    if (x < depth.height && y < depth.width) {
                        hLeft = CheckHole(depth, darr, y, x, 0, hLeft);
                        hRight = CheckHole(depth, darr, depth.width - y - 1,  x, 2, hRight);
                    }
                }
            });
        }

        //      w.3
        //x.0->     <-z.2
        //      y.1
        private int CheckHole(DepthBuffer depth, NativeArray<ushort> darr, int x, int y, int dir, int h)  {
            var i = depth.GetIFrom(x, y);
            var d = darr[i];
            if (d == INVALID_DEPTH)
                ++h;
            else {
                h = 0;
            }
            _holesSize[i][dir] = h;
            return h;
        }

        private void FixDepthHoles(DepthBuffer inBuffer, DepthBuffer outBuffer) {
            Parallel.For(0, inBuffer.height, y => {
                for (int x = 0; x < inBuffer.width; ++x) {
                    var i = inBuffer.GetIFrom(x, y);
                    var d = inBuffer.data[i];
                    var h = _holesSize[i];
                    if (d == INVALID_DEPTH) {
                        var up = SafeGet(inBuffer, x, y + h.w);
                        var down = SafeGet(inBuffer, x, y - h.y);
                        var left = SafeGet(inBuffer, x - h.x, y);
                        var right = SafeGet(inBuffer, x + h.z, y);
                        up = SetPriorityToIfInvalid(up, down, left, right);
                        down = SetPriorityToIfInvalid(down, up, left, right);
                        left = SetPriorityToIfInvalid(left, right, up, down);
                        right = SetPriorityToIfInvalid(right, left, up, down);
                        var dd = FixDepthHole(up, down, h.w, h.y) + FixDepthHole(left, right, h.x, h.z);
                        outBuffer.data[i] = (ushort) (dd / 2);
                    } else if (OnlyRawBuffersIsInput) {
                        outBuffer.data[i] = d;
                    }
                }
            });
        }

        private ushort FixDepthHole(ushort v1, ushort v2, int s1, int s2) {
            var k = (float) s1 / (s1 + s2);
            return (ushort) Mathf.Lerp(v1, v2, k);
        }
        
        private static ushort SetPriorityToIfInvalid(ushort val, ushort v1, ushort v2, ushort v3) {
            if (val != INVALID_DEPTH)
                return val;
            if (v1 != INVALID_DEPTH)
                return v1;
            if (v2 != INVALID_DEPTH)
                return v2;
            if (v3 != INVALID_DEPTH)
                return v3;
            return _BAD_HOLE_FIX;
        }
    }
}