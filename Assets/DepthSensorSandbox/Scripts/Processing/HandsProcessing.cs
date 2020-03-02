﻿#if !UNITY_EDITOR
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
        public SensorDepth HandsDepthDecreased => _sensorHandsDepthDecreased;
        public IndexBuffer CurrHandsMask => _currHandsMask;
#if HANDS_WAVE_STEP_DEBUG
        public int CurrWave { get;  private set; }
        public readonly Barrier WaveBarrier = new Barrier(1);
#endif

        private IndexBuffer _currHandsMask;
        private SensorIndex _sensorHandsMask;
        private SensorIndex.Internal _sensorHandsMaskInternal;
        
        private DepthBuffer _currHandsDepthDecreased;
        private SensorDepth _sensorHandsDepthDecreased;
        private SensorDepth.Internal _sensorHandsDepthDecreasedInternal;
        
        private DepthBuffer _currHandsDepth;
        private SensorDepth _sensorHandsDepth;
        private SensorDepth.Internal _sensorHandsDepthInternal;
        private Sampler _samplerDecreased = Sampler.Create();
        
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
            _sensorHandsDepthDecreased?.Dispose();
        }
        
        protected override void InitInMainThreadInternal(DepthBuffer buffer) {
            if (ReCreateIfNeed(ref _depthLongExpos, buffer.length)) {
                buffer.data.CopyTo(_depthLongExpos);
                RecreateSensor(buffer.width, buffer.height, 
                    ref _sensorHandsMask, out _currHandsMask, out _sensorHandsMaskInternal);
                RecreateSensor(buffer.width, buffer.height, 
                    ref _sensorHandsDepth, out _currHandsDepth, out _sensorHandsDepthInternal);
                RecreateSensor(buffer.width / _HANDS_DEPTH_DECREASE, buffer.height / _HANDS_DEPTH_DECREASE,
                    ref _sensorHandsDepthDecreased, out _currHandsDepthDecreased, out _sensorHandsDepthDecreasedInternal);

                _samplerDecreased.SetDimens(_currHandsDepthDecreased.width, _currHandsDepthDecreased.height);
                if (ReCreateIfNeed(ref _decreasedHandsSumCounts, _currHandsDepthDecreased.length))
                    Array.Clear(_decreasedHandsSumCounts, 0, _decreasedHandsSumCounts.Length);

                _queue.MaxSize = buffer.length;
                _queueErrorAura.MaxSize = buffer.length;
                _queueErrorAuraExtend.MaxSize = buffer.length;
            }
        }

        private static void RecreateSensor<S, B>(int w, int h, ref S sensor, out B buffer, out Sensor<B>.Internal intern)
            where S : Sensor<B> where B : AbstractBuffer {
            sensor?.Dispose();
            buffer = AbstractBuffer.Create<B>(new object[] {w, h});
            sensor = AbstractSensor.Create<S, B>(buffer);
            sensor.BuffersCount = _HANDS_MASK_BUFFERS_COUNT;
            intern = new Sensor<B>.Internal(sensor);
        }
        
        public override void SetCropping(Rect cropping01) {
            base.SetCropping(cropping01);
            _samplerDecreased.SetCropping01(cropping01);
        }
        
        public Sampler GetSamplerHandsDecreased() {
            return _samplerDecreased;
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
            _currHandsDepthDecreased = _sensorHandsDepthDecreased.GetOldest();
            _currHandsDepthDecreased.Clear();
            //Array.Clear(_decreasedHandsSumCounts, 0, _decreasedHandsSumCounts.Length);
            
            Parallel.Invoke(FillBorderUp, FillBorderDown, FillBorderLeft, FillBorderRight);
#if HANDS_WAVE_STEP_DEBUG
            CurrWave = 0;
#endif
            WaveFill(_queue, FillHands);
            WaveFill(_queueErrorAura, FillHandsErrorAura);
            WaveFill(_queueErrorAuraExtend, FillHandsErrorAuraExtend, WavesCountErrorAuraExtend);
            _s.EachParallelDownsizeSafe(WriteMaskResultBody, _HANDS_DEPTH_DECREASE);
            _samplerDecreased.EachParallelHorizontal(FixAverageHandsDepthBody);
            
            _sensorHandsMaskInternal.OnNewFrameBackground();
            _sensorHandsDepthInternal.OnNewFrameBackground();
            _sensorHandsDepthDecreasedInternal.OnNewFrameBackground();
        }

        public int FullToDecreased(int i) {
            var p = _currHandsMask.GetXYFrom(i);
            p /= _HANDS_DEPTH_DECREASE;
            return _currHandsDepthDecreased.GetIFrom((int) p.x, (int) p.y);
        }
        
        public int DecreasedToFull(int i) {
            var p = DecreasedToFullXY(i);
            return _currHandsMask.GetIFrom((int) p.x, (int) p.y);
        }
        
        public Vector2 DecreasedToFullXY(int i) {
            var p = _currHandsDepthDecreased.GetXYFrom(i);
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
        
        private Cell CheckCell(int i, int prevI, ushort minDiffer) {
            if (IsCellInvalid(i))
                return Cell.INVALID;

            var longExp = _depthLongExpos[i];
            if (longExp != Sampler.INVALID_DEPTH) {
                var val = _inDepth.data[i];
                if (val != Sampler.INVALID_DEPTH) {
                    var diff = longExp - val;
                    if (diff > minDiffer) {
                        if (Mathf.Abs(val - _inDepth.data[prevI]) < MinDistanceAtBorder)
                            return Cell.HAND;
                        return Cell.ERROR_AURA;
                    }
                    if (-diff > MaxErrorAura)
                        return Cell.ERROR_AURA;
                } else {
                    return Cell.ERROR_AURA;
                }
            }
            return Cell.CLEAR;
        }

        private void WaveFill(ArrayIntQueue queue, Action<int, int> fill, int maxWave = int.MaxValue) {
            int countInCurrWave = queue.GetCount();
            int currWave = 0;
#if HANDS_WAVE_STEP_DEBUG
            WaveBarrier.SignalAndWait();
#endif
            while (queue.GetCount() > 0) {
                int i = queue.Dequeue();
                for (int n = 0; n < 4; ++n) {
                    int j = _s.GetIndexOfNeighbor(i, n);
                    fill(j, i);
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

        private void FillHands(int id, int prevId) {
            switch (CheckCell(id, prevId, MaxError)) {
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
        
        private void FillHandsErrorAura(int id, int prevId) {
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
        
        private void FillHandsErrorAuraExtend(int id, int prevId) {
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
                    var diff = (ushort) (valLongExpos - inVal);
                    _currHandsDepth.data[i] = diff;
                    var j = FullToDecreased(i);
                    if (_currHandsMask.data[DecreasedToFull(j)] == COLOR) { //central point is colored
                        _currHandsDepthDecreased.data[j] += diff;
                        ++_decreasedHandsSumCounts[j];
                    }
                } else {
                    _currHandsDepth.data[i] = Sampler.INVALID_DEPTH;
                }
            } else {
                var outVal = inVal;
                if (inVal != Sampler.INVALID_DEPTH) {
                    if (valLongExpos != Sampler.INVALID_DEPTH)
                        outVal = (ushort) Mathf.Lerp(inVal, valLongExpos, Exposition);
                    _depthLongExpos[i] = outVal;
                }
                _currHandsDepth.data[i] = Sampler.INVALID_DEPTH;
                _out.data[i] = outVal;
            }
        }

        private void FixAverageHandsDepthBody(int i) {
            var count = _decreasedHandsSumCounts[i];
            if (count > 1)
                _currHandsDepthDecreased.data[i] /= count;
            //clear for next frame
            _decreasedHandsSumCounts[i] = 0;
        }
    }
}