#if !UNITY_EDITOR
    #undef HANDS_WAVE_STEP_DEBUG
#endif

#if HANDS_WAVE_STEP_DEBUG
    using System.Threading;
#endif
using System.Threading.Tasks;
using DepthSensor.Buffer;
using DepthSensor.Sensor;
using UnityEngine;

namespace DepthSensorSandbox.Processing {
    public class HandsProcessing : ProcessingBase {
        private const int _HANDS_MASK_BUFFERS_COUNT = 3;
    
        public const byte CLEAR_COLOR = 0;
        public const byte COLOR_ERROR_AURA = 1;
        public const byte COLOR = 2;
        
        public float Exposition = 0.9f;
        public ushort MaxError = 10;
        public ushort MaxErrorAura = 10;
        public ushort MinDistanceAtBorder = 100;
        
        public SensorIndex HandsMask => _sensorHands;
        public IndexBuffer CurrHandsMask => _currHandsMask;
#if HANDS_WAVE_STEP_DEBUG
        public int CurrWave { get;  private set; }
        public readonly Barrier WaveBarrier = new Barrier(1);
#endif

        private SensorIndex _sensorHands;
        private SensorIndex.Internal _sensorHandsInternal;
        private IndexBuffer _currHandsMask;
        private Buffer2D<ushort> _depthLongExpos;
        private readonly ArrayIntQueue _queue = new ArrayIntQueue();
        private readonly ArrayIntQueue _queueErrorAura = new ArrayIntQueue();

        private enum Cell {
            INVALID,
            CLEAR,
            HAND,
            ERROR_AURA,
        }

        public override void Dispose() {
#if HANDS_WAVE_STEP_DEBUG
            WaveBarrier?.Dispose();
#endif
            _sensorHands?.Dispose();
        }
        
        protected override void InitInMainThreadInternal(DepthBuffer buffer) {
            if (_depthLongExpos == null) {
                _depthLongExpos = new Buffer2D<ushort>(1, 1);
            }
            
            if (AbstractBuffer2D.ReCreateIfNeed(ref _depthLongExpos, buffer.width, buffer.height)) {
                buffer.data.CopyTo(_depthLongExpos.data);
                _currHandsMask = null;
                _sensorHands?.Dispose();
                _sensorHands = new SensorIndex(new IndexBuffer(buffer.width, buffer.height)) {
                    BuffersCount = _HANDS_MASK_BUFFERS_COUNT
                };
                _sensorHandsInternal = new Sensor<IndexBuffer>.Internal(_sensorHands);
                _queue.MaxSize = buffer.length;
                _queueErrorAura.MaxSize = buffer.length;
            }
        }

        protected override void ProcessInternal() {
            if (!CheckValid(_depthLongExpos))
                return;
            
            _queue.Clear();
            _queueErrorAura.Clear();
            _currHandsMask = _sensorHands.GetOldest();
            _currHandsMask.Clear();
            
            Parallel.Invoke(FillBorderUp, FillBorderDown, FillBorderLeft, FillBorderRight);
            FillHandsMask();
            FillHandsMaskErrorAura();
            _s.EachParallelHorizontal(WriteMaskResultBody);
            
            _sensorHandsInternal.OnNewFrameBackground();
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
            if (CheckCell(id, MinDistanceAtBorder) == Cell.HAND)
                Fill(COLOR, id);
        }

        private void Fill(byte color, int i) {
            _currHandsMask.data[i] = color;
            _queue.Enqueue(i);
        }

        private void FillErrorAura(byte color, int i) {
            _currHandsMask.data[i] = color;
            _queueErrorAura.Enqueue(i);
        }

        private Cell CheckCell(int i, ushort minDiffer) {
            if (i == Sampler.INVALID_ID 
            || _currHandsMask.data[i] != CLEAR_COLOR)
                return Cell.INVALID;

            var longExp = _depthLongExpos.data[i];
            if (longExp != Sampler.INVALID_DEPTH) {
                var val = _inDepth.data[i];
                if (val != Sampler.INVALID_DEPTH) {
                    var diff = longExp - val;
                    if (diff > minDiffer)
                        return Cell.HAND;
                    else if (-diff > MaxErrorAura)
                        return Cell.ERROR_AURA;
                } else {
                    return Cell.ERROR_AURA;
                }
            }
            return Cell.CLEAR;
        }
        
        private bool IsHand(int i, ushort minDiffer) {
            ushort longExp;
            ushort val;
            return i != Sampler.INVALID_ID 
                   && _currHandsMask.data[i] == CLEAR_COLOR
                   && (val = _inDepth.data[i]) != Sampler.INVALID_DEPTH
                   && (longExp = _depthLongExpos.data[i]) != Sampler.INVALID_DEPTH
                   && longExp - val > minDiffer;
        }

        private void FillHandsMask() {
#if HANDS_WAVE_STEP_DEBUG
            int countInCurrWave = _queue.GetCount();
            CurrWave = 0;
            WaveBarrier.SignalAndWait();
#endif
            while (_queue.GetCount() > 0) {
                int i = _queue.Dequeue();
                for (int n = 0; n < 8; ++n) {
                    int j = _s.GetIndexOfNeighbor(i, n);
                    switch (CheckCell(j, MaxError)) {
                        case Cell.HAND:
                            Fill(COLOR, j);
                            break;
                        case Cell.ERROR_AURA:
                            FillErrorAura(COLOR_ERROR_AURA, j);
                            break;
                    }
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
        
        private void FillHandsMaskErrorAura() {
#if HANDS_WAVE_STEP_DEBUG
            int countInCurrWave = _queueErrorAura.GetCount();
            WaveBarrier.SignalAndWait();
#endif
            while (_queueErrorAura.GetCount() > 0) {
                int i = _queueErrorAura.Dequeue();
                for (int n = 0; n < 8; ++n) {
                    int j = _s.GetIndexOfNeighbor(i, n);
                    switch (CheckCell(j, MaxError)) {
                        case Cell.ERROR_AURA:
                            FillErrorAura(COLOR_ERROR_AURA, j);
                            break;
                        case Cell.CLEAR:
                            _currHandsMask.data[i] = COLOR_ERROR_AURA;
                            break;
                    }
                }
#if HANDS_WAVE_STEP_DEBUG
                --countInCurrWave;
                if (countInCurrWave == 0) {
                    countInCurrWave = _queueErrorAura.GetCount();
                    ++CurrWave;
                    WaveBarrier.SignalAndWait();
                }
#endif
            }
        }

        private void WriteMaskResultBody(int i) {
            var valLongExpos = _depthLongExpos.data[i];
            var isHand = _currHandsMask.data[i] != CLEAR_COLOR;
            
            if (isHand) {
                _out.data[i] = valLongExpos;
            } else {
                var inVal = _inDepth.data[i];
                var outVal = inVal;
                if (inVal != Sampler.INVALID_DEPTH && valLongExpos != Sampler.INVALID_DEPTH) {
                    _depthLongExpos.data[i] = outVal = (ushort) Mathf.Lerp(inVal, valLongExpos, Exposition);
                }
                _out.data[i] = outVal;
            }
        }
    }
}