using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DepthSensor.Buffer;
using UnityEngine;
using Utilities;

namespace DepthSensorSandbox.Processing {
    public class NoiseFilterMedianProcessing : ProcessingBase {
        public float Smooth = 0.9f;
        public ushort MaxError = 50;

        public class ParallelLocalState {
            public ushort[] medianArr;
            public bool used;
        }

        private readonly List<ParallelLocalState> _stateCaches = new List<ParallelLocalState>();
        private readonly Vector2Int[] _neighbors = {
            new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(0, -1),
            new Vector2Int(-1, 1), new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, -1),
            /*new Vector2Int(-2, 0), new Vector2Int(0, 2), new Vector2Int(2, 0), new Vector2Int(0, -2),*/
        };
        
        protected override void ProcessInternal() {
            Parallel.For(0, _inOut.data.Length,
                InitLocalState,
                FilterBody,
                FinallyLocalState
            );
        }

        private ParallelLocalState InitLocalState() {
            lock (_stateCaches) {
                var state = _stateCaches.FirstOrDefault(s => !s.used);
                if (state == null) {
                    state = new ParallelLocalState();
                    ReCreateIfNeed(ref state.medianArr, 1 + _neighbors.Length);
                    _stateCaches.Add(state);
                }
                state.used = true;
                return state;
            }
        }

        private static void FinallyLocalState(ParallelLocalState state) {
            state.used = false;
        }

        private ParallelLocalState FilterBody(int i, ParallelLoopState loop, ParallelLocalState local) {
            var depth = _rawBuffers[0];
            var actualVal = depth.data[i];
            if (actualVal == INVALID_DEPTH) {
                _inOut.data[i] = INVALID_DEPTH;
                return local;
            }

            local.medianArr[0] = actualVal;
            var j = 1;
            var p = _inOut.GetXYiFrom(i);
            for (int k = 0; k < _neighbors.Length; ++k) {
                var d = SafeGet(depth, p + _neighbors[k]);
                Accumulate(local.medianArr, j++, d, actualVal);
            }

            var median = MathHelper.GetMedian(local.medianArr);
            var prevVal = _inOut.data[i];
            var error = Mathf.Abs(median - prevVal);
            if (error > MaxError || prevVal == INVALID_DEPTH || median == INVALID_DEPTH) {
                _inOut.data[i] = median;
            } else {
                var k = Smooth * Mathf.Sqrt((float)(MaxError - error) / MaxError);
                _inOut.data[i] = (ushort) Mathf.Lerp(median, prevVal, k);
            }

            return local;
        }

        private static void Accumulate(ushort[] a, int i, ushort val, ushort def) {
            a[i] = val == INVALID_DEPTH ? def : val;
        }
    }
}