using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using DepthSensor.Buffer;
using DepthSensor.Sensor;
using Utilities;

namespace DepthSensor.Recorder {
    public abstract class SensorRecorder<TBuf> : IDisposable where TBuf : IBuffer {
        private ISensor<TBuf> _sensor;
        private Stopwatch _timer;
        private bool _recordFramesLoop;
        private Thread _recordFrames;
        private readonly AutoResetEvent _framesReadyEvent = new AutoResetEvent(false);
        private readonly AutoResetEvent _updateBuffersCountEvent = new AutoResetEvent(false);
        private int _buffersCountOnStart;
        private int _recordedFramesCount;

        private struct FrameInfo {
            public long time;
            public TBuf buffer;
        }

        private readonly Queue<FrameInfo> _framesQueue = new Queue<FrameInfo>();
        private int _neededBuffersCount;
        

        public bool Recording => _recordFramesLoop;

        public void StartRecord(Stopwatch timer, ISensor<TBuf> sensor, string path) {
            StopRecord();
            _timer = timer;
            _sensor = sensor;
            _buffersCountOnStart = _sensor.BuffersCount;
            _recordedFramesCount = 0;
            _recordFramesLoop = true;
            sensor.OnNewFrameBackground += OnNewFrame;
            _recordFrames = new Thread(RecordFrames) {
                Name = GetType().Name
            };
            _recordFrames.Start(path);

            if (sensor.FPS == 0 && sensor.BuffersValid > 0) {
                OnNewFrame(sensor);
            }
        }

        public int StopRecord() {
            _recordFramesLoop = false;
            if (_sensor != null) {
                _sensor.OnNewFrameBackground -= OnNewFrame;
                _sensor.BuffersCount = _buffersCountOnStart;
                if (!_sensor.AnySubscribedToNewFrames)
                    _sensor.Active = false;
                _sensor = null;
            }
            lock (_framesQueue) {
                _framesQueue.Clear();
            }
            if (_recordFrames != null && _recordFrames.IsAlive && !_recordFrames.Join(5000))
                _recordFrames.Abort();
            return _recordedFramesCount;
        }

        private void OnNewFrame(ISensor sensor) {
            if (!_recordFramesLoop) return;

            var frame = new FrameInfo {
                time = _timer.ElapsedMilliseconds,
                buffer = _sensor.GetNewest()
            };
            
            var needUpdateBuffersCount = false;
            lock (_framesQueue) {
                _framesQueue.Enqueue(frame);
                _neededBuffersCount = _framesQueue.Count + 1;
                if (_sensor.BuffersCount < _neededBuffersCount) {
                    needUpdateBuffersCount = true;
                }
            }

            if (needUpdateBuffersCount) {
                _updateBuffersCountEvent.Reset();
                if (!MainThread.ExecuteOrPush(UpdateBuffersCountMainThread)) {
                    _updateBuffersCountEvent.WaitOne(1000);
                }
            }

            _framesReadyEvent.Set();
        }

        private void UpdateBuffersCountMainThread() {
            if (_sensor != null) {
                _sensor.BuffersCount = _neededBuffersCount;
                _updateBuffersCountEvent.Set();
            }
        }

        public void Dispose() {
            StopRecord();
            _framesReadyEvent.Dispose();
            _updateBuffersCountEvent.Dispose();
        }
        
        private void RecordFrames(object path) {
            int fps = _sensor.FPS == 0 ? 2 : _sensor.FPS;
            int frameTime = 1000 / fps;
            
            using (var stream = new FileStream(path.ToString(), FileMode.CreateNew))
            using (var binary = new BinaryWriter(stream)) {
                while (_recordFramesLoop) {
                    if (_framesReadyEvent.WaitOne(frameTime * 5)) {
                        FrameInfo info;
                        lock (_framesQueue) {
                            if (_framesQueue.Count <= 0)
                                continue;
                            info = _framesQueue.Peek();
                        }
                        
                        binary.Write(info.time);
                        WriteFrame(stream, binary, info.buffer);
                        
                        ++_recordedFramesCount;
                        lock (_framesQueue) {
                            _framesQueue.Dequeue();
                        }
                    } else {
                        Thread.Sleep(frameTime / 3);
                    }
                }
            }
        }

        protected abstract void WriteFrame(Stream fileStream, BinaryWriter stream, TBuf buffer);
    }
    
    public class SensorRecorderArrayBuffer<T> : SensorRecorder<ArrayBuffer<T>> where T : struct {
        protected override void WriteFrame(Stream stream, BinaryWriter binary, ArrayBuffer<T> buffer) {
            var bytes = buffer.data.GetLengthInBytes();
            binary.Write(bytes);
            MemUtils.CopyBytes(buffer.data, stream, bytes);
        }
    }
    
    public class SensorRecorderBuffer2D<T> : SensorRecorder<Buffer2D<T>> where T : struct {
        protected override void WriteFrame(Stream stream, BinaryWriter binary, Buffer2D<T> buffer) {
            var bytes = buffer.data.GetLengthInBytes();
            binary.Write(bytes);
            MemUtils.CopyBytes(buffer.data, stream, bytes);
        }
    }
}