using System;
using DepthSensor.Buffer;
using DepthSensor.Device;
using DepthSensor.Sensor;
using UnityEngine;

namespace DepthSensorSandbox.Processing {
    public class InitProcessing : ProcessingBase {
        public event Action OnDone;

        public int CalcErrorFrameCount = 10;
        public DepthBuffer ErrorsMap => _errorsMap;

        private enum Stage : int {
            WAIT_VALID,
            FILL_PROCESSING_SENSOR,
            CALC_ERRORS_MAP
        }

        private DepthBuffer _errorsMap;
        private Stage _stage;
        private SensorDepth _processingSensor;
        private ProcessingBase[] _processings;
        private Sampler _sFull = Sampler.Create();
        private int _currCalcErrorFrame;
        private readonly FixHolesProcessing _internalFixHoles = new FixHolesProcessing();

        public override void Dispose() {
            _errorsMap?.Dispose();
            _internalFixHoles?.Dispose();
            base.Dispose();
        }

        public void StartInit(SensorDepth processingSensor, ProcessingBase[] processings) {
            _stage = Stage.WAIT_VALID;
            _processingSensor = processingSensor;
            _processings = processings;
            _currCalcErrorFrame = 0;
            _errorsMap.Clear();
            Active = true;
        }
        
        protected override void InitInMainThreadInternal(DepthSensorDevice device) {
            var buffer = device.Depth.GetNewest();
            _sFull.SetDimens(buffer.width, buffer.height);
            AbstractBuffer2D.ReCreateIfNeed(ref _errorsMap, buffer.width, buffer.height);
            _internalFixHoles.InitInMainThread(device);
            base.InitInMainThreadInternal(device);
        }

        protected override void ProcessInternal() {
            if (!CheckValid(_errorsMap))
                return;

            if (_stage == Stage.WAIT_VALID) {
                for (int i = 0; i < _inDepth.length; ++i) {
                    if (_inDepth.data[i] != Sampler.INVALID_DEPTH) {
                        ++_stage;
                        break;
                    }
                }
            }
            
            if (_stage == Stage.FILL_PROCESSING_SENSOR) {
                if (_processingSensor.BuffersCount == _processingSensor.BuffersValid) {
                    ++_stage;
                } else {
                    DoInitialProcessing();
                    return;
                }
            }

            if (_stage == Stage.CALC_ERRORS_MAP) {
                if (_currCalcErrorFrame < CalcErrorFrameCount) {
                    DoInitialProcessing();
                    CalcErrorsMap();
                    ++_currCalcErrorFrame;
                    if (_currCalcErrorFrame < CalcErrorFrameCount)
                        return;
                    else {
                        FixErrorsMap();
                    }
                }
            }

            Active = false;
            OnDone?.Invoke();
        }

        private void DoInitialProcessing() {
            var bufferChanged = false;
            foreach (var p in _processings) {
                p.OnlyRawBufferIsInput = !bufferChanged;
                p.SetCropping(_sFull.GetCropping01());
                p.Process(_rawBuffer, _out, _prev);
                p.SetCropping(_s.GetCropping01());
                bufferChanged |= p.Active;
            }

            if (!bufferChanged) {
                _sFull.EachParallelHorizontal(CopyBody);
            }
        }

        private void CopyBody(int i) {
            _out.data[i] = _inDepth.data[i];
        }

        private void CalcErrorsMap() {
            _sFull.EachParallelHorizontal(CalcErrorsBody);
        }

        private void CalcErrorsBody(int i) {
            var curr = _inDepth.data[i];
            if (curr != Sampler.INVALID_DEPTH) {
                var prev = _prev.data[i];
                if (prev != Sampler.INVALID_DEPTH) {
                    var diff = Mathf.Abs(curr - prev);
                    if (diff > _errorsMap.data[i])
                        _errorsMap.data[i] = (ushort) diff;
                }
            }
        }

        private void FixErrorsMap() {
            _internalFixHoles.OnlyRawBufferIsInput = true;
            _internalFixHoles.Active = true;
            _internalFixHoles.Process(_errorsMap, _errorsMap, _errorsMap);
        }
    }
}