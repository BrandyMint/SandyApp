﻿#if !UNITY_EDITOR
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
        public ushort MinDistanceAtBorder = 100;
        
        public IndexBuffer HandsMask => _handsMask;
#if HANDS_WAVE_STEP_DEBUG
        public int CurrWave { get;  private set; }
        public readonly Barrier WaveBarrier = new Barrier(1);
#endif

        private IndexBuffer _handsMask;
        private Buffer2D<ushort> _depthLongExpos;
        private readonly ArrayIntQueue _queue = new ArrayIntQueue();

        public override void Dispose() {
#if HANDS_WAVE_STEP_DEBUG
            WaveBarrier?.Dispose();
#endif
            _handsMask?.Dispose();
        }
        
        protected override void InitInMainThreadInternal(DepthBuffer buffer) {
            if (_handsMask == null) {
                _handsMask = new IndexBuffer(1, 1);
                _depthLongExpos = new Buffer2D<ushort>(1, 1);
            }
            
            if (AbstractBuffer2D.ReCreateIfNeed(ref _depthLongExpos, buffer.width, buffer.height)) {
                buffer.data.CopyTo(_depthLongExpos.data);
                AbstractBuffer2D.ReCreateIfNeed(ref _handsMask, buffer.width, buffer.height);
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
            _s.EachParallelHorizontal(WriteMaskResultBody);
        }

        private void FillBorderUp() {
            _s.EachInHorizontal(0, FillMaskLine, 1, 1);
        }

        private void FillBorderDown() {
            _s.EachInHorizontal(_out.height, FillMaskLine, 1, 1);
        }

        private void FillBorderLeft() {
            _s.EachInVertical(0, FillMaskLine);
        }

        private void FillBorderRight() {
            _s.EachInVertical(_out.width, FillMaskLine);
        }
        
        private void FillMaskLine(int id) {
            Fill(COLOR, id, MinDistanceAtBorder, true);
        }

        private bool Fill(byte color, int i, ushort minDiffer, bool doLock = false) {
            ushort longExp;
            if (i !=  Sampler.INVALID_ID && _handsMask.data[i] == CLEAR_COLOR 
            && (longExp = _depthLongExpos.data[i]) != Sampler.INVALID_DEPTH 
            &&  _inDepth.data[i] - longExp > minDiffer) {
                _handsMask.data[i] = color;
                if (doLock) lock (_queue) _queue.Enqueue(i);
                else _queue.Enqueue(i);
                return true;
            }
            return false;
        }

        private void FillHandsMask() {
#if HANDS_WAVE_STEP_DEBUG
            int countInCurrWave = _queue.GetCount();
            CurrWave = 0;
            WaveBarrier.SignalAndWait();
#endif
            while (_queue.GetCount() > 0) {
                int i = _queue.Dequeue();
                for (int n = 0; n < 4; ++n) {
                    int j = _s.GetIndexOfNeighbor(i, n);
                    Fill(COLOR, j, MaxError);
                }
#if HANDS_WAVE_STEP_DEBUG
                --countInCurrWave;
                if (countInCurrWave == 0) {
                    countInCurrWave = _queue.GetCount();
                    ++CurrWave;
                    WaveBarrier.SignalAndWait();
                }
#endif
            }
        }

        private void WriteMaskResultBody(int i) {
            var valLongExpos = _depthLongExpos.data[i];
            var color = _handsMask.data[i];
            if (color != CLEAR_COLOR) {
                _out.data[i] = valLongExpos;
            } else {
                var val = _inDepth.data[i];
                _depthLongExpos.data[i] = (ushort) Mathf.Lerp(val, valLongExpos, Exposition);
            }
        }
    }
}