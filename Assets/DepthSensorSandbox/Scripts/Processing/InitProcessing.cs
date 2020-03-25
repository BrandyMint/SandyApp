using System;
using DepthSensor.Buffer;
using DepthSensor.Device;
using DepthSensor.Sensor;
using UnityEngine;

namespace DepthSensorSandbox.Processing {
    public class InitProcessing : ProcessingBase {
        private const int _MAX_VALID_ERROR = 15; 
        public event Action OnDone;

        public int CalcErrorFrameCount = 10;
        public DepthBuffer ErrorsMap => _errorsMap;

        private enum Stage : int {
            WAIT_VALID,
            FILL_PROCESSING_SENSOR,
            CALC_ERRORS_MAP
        }

        private Stage _stage;
        private SensorDepth _processingSensor;
        private ProcessingBase[] _processings;
        private int _currCalcErrorFrame;
        private readonly FixHolesProcessing _internalFixHoles = new FixHolesProcessing();
        private IInitProcessing[] _initProcessings;

        public override void Dispose() {
            _errorsMap?.Dispose();
            _internalFixHoles?.Dispose();
            base.Dispose();
        }

        public void StartInit(SensorDepth processingSensor, ProcessingBase[] processings, IInitProcessing[] initProcessings) {
            _stage = Stage.WAIT_VALID;
            _processingSensor = processingSensor;
            _processings = processings;
            _initProcessings = initProcessings;
            _currCalcErrorFrame = 0;
            _errorsMap.Clear();
            Active = true;
        }
        
        protected override void InitInMainThreadInternal(DepthSensorDevice device) {
            var buffer = device.Depth.GetNewest();
            AbstractBuffer2D.ReCreateIfNeed(ref _errorsMap, buffer.width, buffer.height);
            _internalFixHoles.InitInMainThread(device);
            base.InitInMainThreadInternal(device);
        }

        protected override void ProcessInternal() {
            if (!CheckValid(_errorsMap))
                return;

            if (_stage == Stage.WAIT_VALID) {
                if (!_s.Each(i => _inDepth.data[i] == Sampler.INVALID_DEPTH))
                    ++_stage;
                else
                    return;
            }
            
            if (_stage == Stage.FILL_PROCESSING_SENSOR) {
                if (_processingSensor.BuffersCount == _processingSensor.BuffersValid) {
                    ++_stage;
                } else {
                    DoProcessings();
                    return;
                }
            }

            if (_stage == Stage.CALC_ERRORS_MAP) {
                if (_currCalcErrorFrame == 0)
                    DoPrepareInits();
                if (_currCalcErrorFrame < CalcErrorFrameCount) {
                    DoProcessings();
                    DoInits();
                    ++_currCalcErrorFrame;
                    if (_currCalcErrorFrame < CalcErrorFrameCount)
                        return;
                    else {
                        FixHolesInits();
                    }
                }
            }

            Active = false;
            OnDone?.Invoke();
#if UNITY_EDITOR
            DebugLogMaxError();
#endif
        }

        private void DoProcessings() {
            var bufferChanged = !OnlyRawBufferIsInput;
            foreach (var p in _processings) {
                p.OnlyRawBufferIsInput = !bufferChanged;
                p.UseFullRectNextFrame = true;
                bufferChanged |= p.Process(_inDepth, _out, _prev);
            }

            if (!bufferChanged) {
                _sFull.EachParallelHorizontal(CopyBody);
            }
        }

        private void CopyBody(int i) {
            _out.data[i] = _inDepth.data[i];
        }

        private void DoPrepareInits() {
            foreach (var initProcessing in _initProcessings) {
                initProcessing.PrepareInitProcess();
            }
        }

        private void DoInits() {
            _sFull.EachParallelHorizontal(CalcErrorsBody);
            foreach (var initProcessing in _initProcessings) {
                initProcessing.InitProcess(_inDepth, _out, _prev);
            }
        }

        private void CalcErrorsBody(int i) {
            var curr = _rawBuffer.data[i];
            if (curr != Sampler.INVALID_DEPTH) {
                var prev = _prev.data[i];
                if (prev != Sampler.INVALID_DEPTH) {
                    var diff = Mathf.Abs(curr - prev);
                    var old = _errorsMap.data[i];
                    if (old != Sampler.INVALID_DEPTH && diff != Sampler.INVALID_DEPTH) {
                        var max = Mathf.Max(diff, old);
                        var min = Mathf.Min(diff, old);
                        if ((max > _MAX_VALID_ERROR || min > _MAX_VALID_ERROR) && (float)max / min >= 2f)
                            diff = min;
                        else
                            diff = max;
                        _errorsMap.data[i] = (ushort) diff;
                    } else if (diff < _MAX_VALID_ERROR) {
                        _errorsMap.data[i] = (ushort) diff;
                    }
                }
            }
        }

        private void FixHolesInits() {
            _internalFixHoles.OnlyRawBufferIsInput = false;
            _internalFixHoles.Active = true;
            _internalFixHoles.Process(_errorsMap, _errorsMap, _errorsMap);
            foreach (var initProcessing in _initProcessings) {
                foreach (var buffer in initProcessing.GetMapsForFixHoles()) {
                    _internalFixHoles.Process(buffer, buffer, buffer);
                }
            }
        }

#if UNITY_EDITOR
        public void DebugLogMaxError() {
            ushort maxError = 0;
            float average = 0f;
            int count = 0;
            _s.Each(i => {
                var err = _errorsMap.data[i];
                average += err;
                ++count;
                if (err > maxError)
                    maxError = err;
            });
            Debug.Log($"Max depth error {maxError}, average {average / count:F1}");
        }

        public void DebugShowErrorsMap(DepthBuffer depth, DepthSensorDevice device) {
            _sFull.Each(i => {
                var p = depth.GetXYFrom(i);
                var d = depth.data[i];
                var e = _errorsMap.data[i];
                var p1 = device.DepthMapPosToCameraPos(p, d);
                var p2 = device.DepthMapPosToCameraPos(p, (ushort) (d + e));
                Debug.DrawLine(p1, p2, Color.magenta);
            });
        }
#endif
    }
}