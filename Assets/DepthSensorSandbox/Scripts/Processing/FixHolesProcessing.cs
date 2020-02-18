using DepthSensor.Buffer;
using Unity.Mathematics;
using UnityEngine;

namespace DepthSensorSandbox.Processing {
    public class FixHolesProcessing : ProcessingBase {
        private const ushort _BAD_HOLE_FIX = 5000;

        private int2x4[] _holes;

        private const int _UP = 0;
        private const int _DOWN = 1;
        private const int _LEFT = 2;
        private const int _RIGHT = 3;

        private struct FindHolesLineState : Sampler.IParallelLineState {
            private int2 hStart;
            private int2 hEnd;
            
            public DepthBuffer depth;
            public int2x4[] holes;
            public int dirStart;
            public int dirEnd;
            public int mirrorId;
            public int mirrorIdStep;

            public void Handle(int id) {
                hStart = CheckHole(id, dirStart, hStart);
                hEnd = CheckHole(mirrorId, dirEnd, hEnd);
                mirrorId -= mirrorIdStep;
            }
            
            //      w.3
            //x.0->     <-z.2
            //      y.1
            private int2 CheckHole(int i, int dir, int2 h) {
                var d = depth.data[i];
                if (d == Sampler.INVALID_DEPTH)
                    ++h.x;
                else {
                    h.x = 0;
                    h.y = d;
                }
                holes[i][dir] = h;
                return h;
            }
        }
        
        protected override void ProcessInternal() {
            ReCreateIfNeed(ref _holes, _out.length);
            _s.EachParallelVertical(InitFindHolesLineStateVertical);
            _s.EachParallelHorizontal(InitFindHolesLineStateHorizontal);
            _s.EachParallelHorizontal(FixDepthHolesBody);
        }

        private FindHolesLineState InitFindHolesLineStateVertical(int x) {
            return new FindHolesLineState {
                depth = _inDepth,
                holes = _holes,
                dirStart = _DOWN,
                dirEnd = _UP,
                mirrorIdStep = _inDepth.width,
                mirrorId = _s.GetIFrom(x, _s.GetRect().yMax-1)
            };
        }
        
        private FindHolesLineState InitFindHolesLineStateHorizontal(int y) {
            return new FindHolesLineState {
                depth = _inDepth,
                holes = _holes,
                dirStart = _LEFT,
                dirEnd = _RIGHT,
                mirrorIdStep = 1,
                mirrorId = _s.GetIFrom(_s.GetRect().xMax-1, y)
            };
        }

        //      w.3
        //x.0->     <-z.2
        //      y.1
        private void FixDepthHolesBody(int i) {
            var d = _inDepth.data[i];
            var hole = _holes[i];
            if (d == Sampler.INVALID_DEPTH) {
                var up = hole[_UP];
                var down = hole[_DOWN];
                var left = hole[_LEFT];
                var right = hole[_RIGHT];
                up.y = SetPriorityToIfInvalid(up.y, down.y, left.y, right.y);
                down.y = SetPriorityToIfInvalid(down.y, up.y, left.y, right.y);
                left.y = SetPriorityToIfInvalid(left.y, right.y, up.y, down.y);
                right.y = SetPriorityToIfInvalid(right.y, left.y, up.y, down.y);
                var dd = FixDepthHole(up, down) + FixDepthHole(left, right);
                _out.data[i] = (ushort) (dd / 2);
            } else if (OnlyRawBufferIsInput) {
                _out.data[i] = d;
            }
        }

        private static ushort FixDepthHole(int2 v1, int2 v2) {
            var k = (float) (v1.x + 1) / (v1.x + v2.x + 2);
            return (ushort) Mathf.Lerp(v1.y, v2.y, k);
        }
        
        private static int SetPriorityToIfInvalid(int val, int v1, int v2, int v3) {
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