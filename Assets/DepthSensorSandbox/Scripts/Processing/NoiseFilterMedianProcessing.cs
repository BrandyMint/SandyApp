using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utilities;

namespace DepthSensorSandbox.Processing {
    public class NoiseFilterMedianProcessing : ProcessingBase {
        private const int _NEIGHBORS = 8;
        
        public float Smooth = 0.9f;
        public ushort MaxError = 50;

        public class ParallelLocalState : Sampler.IParallelLocalState {
            public ushort[] medianArr;
            public bool used;
            public Action<int, ParallelLocalState> handler;

            public void Handle(int id) {
                handler.Invoke(id, this);
            }
        }

        private readonly List<ParallelLocalState> _stateCaches = new List<ParallelLocalState>();
        
        protected override void ProcessInternal() {
            /*Parallel.For(0, _out.length,
                InitLocalState,
                FilterBody,
                FinallyLocalState
            );*/
            _s.EachParallelHorizontal(InitLocalState, FinallyLocalState);
        }

        private ParallelLocalState InitLocalState() {
            lock (_stateCaches) {
                var state = _stateCaches.FirstOrDefault(s => !s.used);
                if (state == null) {
                    state = new ParallelLocalState();
                    ReCreateIfNeed(ref state.medianArr, 1 + _NEIGHBORS);
                    _stateCaches.Add(state);
                    state.handler = FilterBody;
                }
                state.used = true;
                return state;
            }
        }

        private static void FinallyLocalState(ParallelLocalState state) {
            state.used = false;
        }

        private void FilterBody(int i, ParallelLocalState local) {
            var actualVal = _rawBuffer.data[i];
            if (actualVal == Sampler.INVALID_DEPTH) {
                _out.data[i] = Sampler.INVALID_DEPTH;
                return;
            }

            local.medianArr[0] = actualVal;
            var j = 1;
            var p = _s.GetXYiFrom(i);
            for (int k = 0; k < _NEIGHBORS; ++k) {
                var ki = _s.GetIndexOfNeighbor(i, k);
                var d = ki != Sampler.INVALID_ID ? _rawBuffer.data[ki] : Sampler.INVALID_DEPTH;
                Accumulate(local.medianArr, j++, d, actualVal);
            }

            var median = MathHelper.GetMedian(local.medianArr);
            var prevVal = _prev.data[i];
            var error = Mathf.Abs(median - prevVal);
            if (error > MaxError || prevVal == Sampler.INVALID_DEPTH || median == Sampler.INVALID_DEPTH) {
                _out.data[i] = median;
            } else {
                var k = Smooth * Mathf.Sqrt((float)(MaxError - error) / MaxError);
                _out.data[i] = (ushort) Mathf.Lerp(median, prevVal, k);
            }
        }

        private static void Accumulate(ushort[] a, int i, ushort val, ushort def) {
            a[i] = val == Sampler.INVALID_DEPTH ? def : val;
        }
    }
}