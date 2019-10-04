#if !UNITY_EDITOR
    #undef HANDS_WAVE_STEP_DEBUG
#endif

#if HANDS_WAVE_STEP_DEBUG
    using System.Threading;
#endif
using System.Threading.Tasks;
using DepthSensor.Buffer;
using UnityEngine;

namespace DepthSensorSandbox.Processing {
    public class HandsProcessing : ProcessingBase {
        public const byte CLEAR_COLOR = 0;
        public const byte COLOR = 1;
        
        public float Exposition = 0.99f;
        public ushort MaxError = 10;
        public ushort MinDistance = 100;
        
        public Buffer2D<byte> HandsMask => _handsMask;
#if HANDS_WAVE_STEP_DEBUG
        public int CurrWave { get;  private set; }
        public readonly AutoResetEvent EvRequestNextWave = new AutoResetEvent(false);
        public readonly AutoResetEvent EvWaveReady = new AutoResetEvent(false);
#endif

        private Buffer2D<byte> _handsMask;
        private Buffer2D<ushort> _depthLongExpos;
        private readonly ArrayIntQueue _queue = new ArrayIntQueue();

        public override void Dispose() {
#if HANDS_WAVE_STEP_DEBUG
            EvRequestNextWave?.Dispose();
            EvWaveReady?.Dispose();
#endif
            _handsMask?.Dispose();
        }
        
        protected override void InitInMainThreadInternal(DepthBuffer buffer) {
            if (_handsMask == null) {
                _handsMask = new Buffer2D<byte>(1, 1);
                _depthLongExpos = new Buffer2D<ushort>(1, 1);

            }
            
            if (Buffer2D.ReCreateIfNeed(ref _depthLongExpos, buffer.width, buffer.height)) {
                buffer.data.CopyTo(_depthLongExpos.data);
                Buffer2D.ReCreateIfNeed(ref _handsMask, buffer.width, buffer.height);
                _queue.MaxSize = buffer.length;
            }
        }

        protected override void ProcessInternal() {
            if (!CheckValid(_depthLongExpos) || !CheckValid(_handsMask))
                return;
            
            _queue.Clear();
            _handsMask.Clear();
            
            Parallel.Invoke(FillBorderUp, FillBorderDown, FillBorderLeft, FillBorderRight);
            FillHandsMask();
            Parallel.For(0, _inOut.length, WriteMaskResultBody);
        }

        private void FillBorderUp() {
            FillMaskLine(1, 1, _inOut.width - 2);
        }

        private void FillBorderDown() {
            FillMaskLine(_inOut.length - _inOut.width + 1, 1, _inOut.width - 2);
        }

        private void FillBorderLeft() {
            FillMaskLine(0,  _inOut.width, _inOut.height);
        }

        private void FillBorderRight() {
            FillMaskLine(_inOut.width,  _inOut.width, _inOut.height);
        }

        private void FillMaskLine(int start, int step, int n) {
            var id = start;
            for (int i = 0; i < n; ++i) {
                Fill(COLOR, i, MinDistance);
                id += step;
            }
        }

        private bool Fill(byte color, int i, ushort minDiffer) {
            if (i !=  Sampler.INVALID_ID && _handsMask.data[i] == CLEAR_COLOR 
                                && _inDepth.data[i] - _depthLongExpos.data[i] > minDiffer) {
                _handsMask.data[i] = color;
                lock (_queue) {
                    _queue.Enqueue(i);
                }
                return true;
            }
            return false;
        }

        private void FillHandsMask() {
#if HANDS_WAVE_STEP_DEBUG
            int countInCurrWave = _queue.GetCount();
            CurrWave = 0;
            EvWaveReady.Set();
            EvRequestNextWave.WaitOne();
#endif
            while (_queue.GetCount() > 0) {
                int i = _queue.Dequeue();
                for (int n = 0; n < 8; ++n) {
                    int j = _s.GetIndexOfNeighbor(i, n);
                    if (Fill(COLOR, j, MaxError)) {
                        _queue.Enqueue(j);
                    }
                }
#if HANDS_WAVE_STEP_DEBUG
                --countInCurrWave;
                if (countInCurrWave == 0) {
                    countInCurrWave = _queue.GetCount();
                    ++CurrWave;
                }

                EvWaveReady.Set();
                EvRequestNextWave.WaitOne();
#endif
            }
        }

        private void WriteMaskResultBody(int i) {
            var val = _inDepth.data[i];
            var valLongExpos = _depthLongExpos.data[i];
            var color = _handsMask.data[i];
            if (color != CLEAR_COLOR) {
                _inOut.data[i] = valLongExpos;
            } else {
                _depthLongExpos.data[i] = (ushort) Mathf.Lerp(val, valLongExpos, Exposition);
            }
        }
    }
}