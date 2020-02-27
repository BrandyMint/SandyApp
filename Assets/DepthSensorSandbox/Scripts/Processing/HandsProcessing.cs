#if !UNITY_EDITOR
    #undef HANDS_WAVE_STEP_DEBUG
#endif

#if HANDS_WAVE_STEP_DEBUG
    using System.Threading;
#endif
using System;
using System.Threading.Tasks;
using DepthSensor.Buffer;
using DepthSensor.Sensor;
using UnityEngine;

namespace DepthSensorSandbox.Processing {
    public class HandsProcessing : ProcessingBase {
        private const int _HANDS_MASK_BUFFERS_COUNT = 3;
        private const int _HANDS_DEPTH_DECREASE = 8;
    
        public const byte CLEAR_COLOR = 0;
        public const byte COLOR_ERROR_AURA = 1;
        public const byte COLOR = 2;
        
        public float Exposition = 0.9f;
        public ushort MaxError = 10;
        public ushort MaxErrorAura = 10;
        public ushort MinDistanceAtBorder = 100;
        public int WavesCountErrorAuraExtend = 4;
        
        public SensorIndex HandsMask => _sensorHandsMask;
        public SensorDepth HandsDepth => _sensorHandsDepth;
        public IndexBuffer CurrHandsMask => _currHandsMask;
#if HANDS_WAVE_STEP_DEBUG
        public int CurrWave { get;  private set; }
        public readonly Barrier WaveBarrier = new Barrier(1);
#endif

        private IndexBuffer _currHandsMask;
        private SensorIndex _sensorHandsMask;
        private SensorIndex.Internal _sensorHandsMaskInternal;
        
        private DepthBuffer _currHandsDepth;
        private SensorDepth _sensorHandsDepth;
        private SensorDepth.Internal _sensorHandsDepthInternal;
        private Sampler _samplerHandsDepth = Sampler.Create();
        
        private ushort[] _depthLongExpos;
        private byte[] _decreasedHandsSumCounts;
        private readonly ArrayIntQueue _queue = new ArrayIntQueue();
        private readonly ArrayIntQueue _queueErrorAura = new ArrayIntQueue();
        private readonly ArrayIntQueue _queueErrorAuraExtend = new ArrayIntQueue();

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
            _sensorHandsMask?.Dispose();
            _sensorHandsDepth?.Dispose();
        }
        
        protected override void InitInMainThreadInternal(DepthBuffer buffer) {
            if (ReCreateIfNeed(ref _depthLongExpos, buffer.length)) {
                buffer.data.CopyTo(_depthLongExpos);
                _currHandsMask = new IndexBuffer(buffer.width, buffer.height);
                _sensorHandsMask?.Dispose();
                _sensorHandsMask = new SensorIndex(_currHandsMask) {
                    BuffersCount = _HANDS_MASK_BUFFERS_COUNT
                };
                _sensorHandsMaskInternal = new Sensor<IndexBuffer>.Internal(_sensorHandsMask);

                _currHandsDepth = new DepthBuffer(buffer.width / _HANDS_DEPTH_DECREASE, buffer.height / _HANDS_DEPTH_DECREASE);
                _sensorHandsDepth?.Dispose();
                _sensorHandsDepth = new SensorDepth(_currHandsDepth) {
                    BuffersCount = _HANDS_MASK_BUFFERS_COUNT
                };
                _sensorHandsDepthInternal = new Sensor<DepthBuffer>.Internal(_sensorHandsDepth);
                if (ReCreateIfNeed(ref _decreasedHandsSumCounts, _currHandsDepth.length))
                    Array.Clear(_decreasedHandsSumCounts, 0, _decreasedHandsSumCounts.Length);
                _samplerHandsDepth.SetDimens(_currHandsDepth.width, _currHandsDepth.height);

                _queue.MaxSize = buffer.length;
                _queueErrorAura.MaxSize = buffer.length;
                _queueErrorAuraExtend.MaxSize = buffer.length;
            }
        }
        
        public override void SetCropping(Rect cropping01) {
            base.SetCropping(cropping01);
            _samplerHandsDepth.SetCropping01(cropping01);
        }
        
        public Sampler GetSamplerHandsDecreased() {
            return _samplerHandsDepth;
        }

        protected override void ProcessInternal() {
            if (!CheckValid(_currHandsMask))
                return;
            
            _queue.Clear();
            _queueErrorAura.Clear();
            _queueErrorAuraExtend.Clear();
            _currHandsMask = _sensorHandsMask.GetOldest();
            _currHandsMask.Clear();
            _currHandsDepth = _sensorHandsDepth.GetOldest();
            _currHandsDepth.Clear();
            //Array.Clear(_decreasedHandsSumCounts, 0, _decreasedHandsSumCounts.Length);
            
            Parallel.Invoke(FillBorderUp, FillBorderDown, FillBorderLeft, FillBorderRight);
#if HANDS_WAVE_STEP_DEBUG
            CurrWave = 0;
#endif
            WaveFill(_queue, FillHands);
            WaveFill(_queueErrorAura, FillHandsErrorAura);
            WaveFill(_queueErrorAuraExtend, FillHandsErrorAuraExtend, WavesCountErrorAuraExtend);
            _s.EachParallelDownsizeSafe(WriteMaskResultBody, _HANDS_DEPTH_DECREASE);
            _samplerHandsDepth.EachParallelHorizontal(FixAverageHandsDepthBody);
            
            _sensorHandsMaskInternal.OnNewFrameBackground();
            _sensorHandsDepthInternal.OnNewFrameBackground();
        }

        public int MaskToHands(int i) {
            var p = _currHandsMask.GetXYFrom(i);
            p /= _HANDS_DEPTH_DECREASE;
            return _currHandsDepth.GetIFrom((int) p.x, (int) p.y);
        }
        
        public int HandsToMask(int i) {
            var p = HandsToMaskXY(i);
            return _currHandsMask.GetIFrom((int) p.x, (int) p.y);
        }
        
        public Vector2 HandsToMaskXY(int i) {
            var p = _currHandsDepth.GetXYFrom(i);
            return p * _HANDS_DEPTH_DECREASE + Vector2.one / 2f * _HANDS_DEPTH_DECREASE;
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
            if (CheckCell(id, MinDistanceAtBorder) == Cell.HAND) {
                lock (_queue) {
                    Fill(COLOR, id, _queue);
                }
            }
        }

        private void Fill(byte color, int i, ArrayIntQueue queue) {
            _currHandsMask.data[i] = color;
            queue.Enqueue(i);
        }

        private bool IsCellInvalid(int i) {
            return i == Sampler.INVALID_ID || _currHandsMask.data[i] != CLEAR_COLOR;
        }

        private Cell CheckCell(int i, ushort minDiffer) {
            if (IsCellInvalid(i))
                return Cell.INVALID;

            var longExp = _depthLongExpos[i];
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

        private void WaveFill(ArrayIntQueue queue, Action<int> fill, int maxWave = int.MaxValue) {
            int countInCurrWave = queue.GetCount();
            int currWave = 0;
#if HANDS_WAVE_STEP_DEBUG
            WaveBarrier.SignalAndWait();
#endif
            while (queue.GetCount() > 0) {
                int i = queue.Dequeue();
                for (int n = 0; n < 4; ++n) {
                    int j = _s.GetIndexOfNeighbor(i, n);
                    fill(j);
                }
                --countInCurrWave;
                if (countInCurrWave == 0) {
                    countInCurrWave = queue.GetCount();
                    ++currWave;
#if HANDS_WAVE_STEP_DEBUG
                    ++CurrWave;
                    WaveBarrier.SignalAndWait();
#endif
                    if (currWave >= maxWave)
                        return;
                }
            }
        }

        private void FillHands(int id) {
            switch (CheckCell(id, MaxError)) {
                case Cell.HAND:
                    Fill(COLOR, id, _queue);
                    break;
                case Cell.ERROR_AURA:
                    Fill(COLOR_ERROR_AURA, id, _queueErrorAura);
                    break;
                case Cell.CLEAR:
                    Fill(COLOR_ERROR_AURA, id, _queueErrorAuraExtend);
                    break;
            }
        }
        
        private void FillHandsErrorAura(int id) {
            switch (CheckCell(id, MaxError)) {
                case Cell.ERROR_AURA:
                    Fill(COLOR_ERROR_AURA, id, _queueErrorAura);
                    break;
                case Cell.HAND:
                case Cell.CLEAR:
                    Fill(COLOR_ERROR_AURA, id, _queueErrorAuraExtend);
                    break;
            }
        }
        
        private void FillHandsErrorAuraExtend(int id) {
            if (!IsCellInvalid(id))
                Fill(COLOR_ERROR_AURA, id, _queueErrorAuraExtend);
        }

        private void WriteMaskResultBody(int i) {
            var valLongExpos = _depthLongExpos[i];
            var inVal = _inDepth.data[i];
            var mask = _currHandsMask.data[i];
            var isHand = _currHandsMask.data[i];
            
            if (mask != CLEAR_COLOR) {
                _out.data[i] = valLongExpos;
                if (mask == COLOR) {
                    var j = MaskToHands(i);
                    if (_currHandsMask.data[HandsToMask(j)] == COLOR) { //central point is colored
                        _currHandsDepth.data[j] += (ushort) (valLongExpos - inVal);
                        ++_decreasedHandsSumCounts[j];
                    }
                }
            } else {
                var outVal = inVal;
                if (inVal != Sampler.INVALID_DEPTH) {
                    if (valLongExpos != Sampler.INVALID_DEPTH)
                        outVal = (ushort) Mathf.Lerp(inVal, valLongExpos, Exposition);
                    _depthLongExpos[i] = outVal;
                }
                _out.data[i] = outVal;
            }
        }

        private void FixAverageHandsDepthBody(int i) {
            var count = _decreasedHandsSumCounts[i];
            if (count > 1)
                _currHandsDepth.data[i] /= count;
            //clear for next frame
            _decreasedHandsSumCounts[i] = 0;
        }
    }
}