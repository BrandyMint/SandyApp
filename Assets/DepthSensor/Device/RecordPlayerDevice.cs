using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using DepthSensor.Buffer;
using DepthSensor.Recorder;
using DepthSensor.Sensor;
using Unity.Collections;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace DepthSensor.Device {
    public class RecordPlayerDevice : DepthSensorDevice {
        private class InitInfoRecordPlayer : InitInfo {
            public RecordManifest manifest;
            public string path;
        }

        private class SensorStream {
            public Stream stream;
            public BinaryReader binary;
            public bool Opened => stream != null;
            public AbstractSensor.Internal SensorInternal;
            public ISensor<AbstractBuffer2D> Sensor;
            public long frameTime;
            public byte[] frame;
            private bool _isFrameReaded;
            private string _path;

            public SensorStream(string path) {
                _path = path;
            }

            public void Open(long seek = -1) {
                if (!Opened && Sensor.Available) {
                    _isFrameReaded = false;
                    try {
                        var frameLen = (int)Sensor.GetOldest().LengthInBytes() + sizeof(long) * 2;
                        stream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.None, frameLen);
                        if (seek >= 0) {
                            stream.Seek(seek, SeekOrigin.Begin);
                        }
                        binary = new BinaryReader(stream);
                    } catch (Exception e) {
                        Debug.LogException(e);
                        stream = null;
                    }
                }
            }

            public void Close() {
                if (Opened) {
                    binary.Close();
                    stream.Close();
                    stream = null;
                    binary = null;
                    _isFrameReaded = false;
                }
            }

            public void Dispose() {
                Close();
            }

            public bool ReadNextFrame() {
                if (_isFrameReaded) {
                    return true;
                }

                try {
                    frameTime = binary.ReadInt64();
                    var bytes = binary.ReadInt64();
                    ReCreateIfNeed(ref frame, bytes);
                    if (bytes < int.MaxValue) {
                        var readed = stream.Read(frame, 0, (int) bytes);
                        if (readed < bytes) {
                            Debug.LogWarning($"{_path} is broken? Readed {readed} bytes instead of {bytes}");
                        }

                        if (readed < 1)
                            return false;
                    } else {
                        throw new NotImplementedException("Bytes count is more then int32.MaxValue");
                    }

                    _isFrameReaded = true;
                    return true;
                } catch (EndOfStreamException) {
                    return false;
                }
            }

            public bool PollFrame() {
                if (_isFrameReaded) {
                    var buff = Sensor.GetOldest();
                    buff.Set(frame);
                    SensorInternal.OnNewFrameBackground();
                    _isFrameReaded = false;
                    return true;
                }
                return false;
            }

            private static bool ReCreateIfNeed<T>(ref T[] a, long len) {
                if (a == null || a.LongLength != len) {
                    a = new T[len];
                    return true;
                }
                return false;
            }
        }
        
        private volatile bool _pollFramesLoop = true;
        private volatile bool _mapDepthToCameraUpdated;
        private readonly Thread _pollFrames;
        private readonly AutoResetEvent _framesArrivedEvent = new AutoResetEvent(false);
        private readonly AutoResetEvent _sensorActiveChangedEvent = new AutoResetEvent(false);

        private readonly SensorStream _depthStream;
        private readonly SensorStream _colorStream;
        private readonly SensorStream _infraredStream;
        private readonly SensorStream _mapStream;
        private readonly List<SensorStream> _streams = new List<SensorStream>();

        public RecordPlayerDevice(string path) : base(Init(path)) {
            _colorStream = InitStream(_internalColor, path, nameof(Color));
            _infraredStream = InitStream(_internalInfrared, path, nameof(Infrared));
            _mapStream = InitStream(_internalMapDepthToCamera, path, nameof(MapDepthToCamera));
            _depthStream = InitStream(_internalDepth, path, nameof(Depth));

            _initInfo = null;
            
            _pollFrames = new Thread(PollFrames) {
                Name = GetType().Name
            };
            _pollFrames.Start();
        }

        private SensorStream InitStream<T>(Sensor<T>.Internal internalSensor, string path, string name) where T : AbstractBuffer2D {
            var stream = new SensorStream(Path.Combine(path, name)) {
                SensorInternal = internalSensor,
                Sensor = internalSensor.sensor
            };
            var info = ((InitInfoRecordPlayer) _initInfo).manifest.Get<StreamInfo>(name);
            if (info != null) {
                internalSensor.SetTargetFps(info.fps);
                //TODO: internalSensor.SetFov();
            }
            _streams.Add(stream);

            return stream;
        }

        private static InitInfo Init(string path) {
            var init = new InitInfoRecordPlayer {
                Name = nameof(RecordPlayerDevice)
            };
            try {
                init.manifest = new RecordManifest(path);
                if (!init.manifest.Load()) {
                    throw new FileNotFoundException($"Fail read RecordManifest.json in {path}");
                }

                Debug.Log($"Load record {init.manifest.DeviceName} in {path}");
                init.Name = $"{init.manifest.DeviceName} (Record)";

                if (init.manifest.Depth != null && Check(init.manifest, path, nameof(init.manifest.Depth))) {
                    init.Depth = new SensorDepth(new DepthBuffer(
                        init.manifest.Depth.width, 
                        init.manifest.Depth.height
                    ));
                }
                if (init.manifest.Color != null && Check(init.manifest, path, nameof(init.manifest.Color))) {
                    init.Color = new SensorColor(new ColorBuffer(
                        init.manifest.Color.width, 
                        init.manifest.Color.height,
                        init.manifest.Color.textureFormat
                    ));
                }
                if (init.manifest.Infrared != null && Check(init.manifest, path, nameof(init.manifest.Infrared))) {
                    init.Infrared = new SensorInfrared(new InfraredBuffer(
                        init.manifest.Infrared.width, 
                        init.manifest.Infrared.height,
                        init.manifest.Infrared.textureFormat
                    ));
                }
                if (init.manifest.MapDepthToCamera != null && Check(init.manifest, path, nameof(init.manifest.MapDepthToCamera))) {
                    init.MapDepthToCamera = new SensorMapDepthToCamera(new MapDepthToCameraBuffer(
                        init.manifest.MapDepthToCamera.width, 
                        init.manifest.MapDepthToCamera.height
                    ));
                }
            } catch { }

            return init;
        }

        private static bool Check(RecordManifest manifest, string path, string name) {
            if (!File.Exists(Path.Combine(path, name)))
                throw new FileNotFoundException($"Fail read {name} in {path}");
            var info = manifest.Get<StreamInfo>(name);
            if (info.framesCount < 1)
                return false;
            Debug.Log($"{nameof(RecordPlayerDevice)}: for {name} selected mode {info.width}x{info.height}@{info.fps} {info.textureFormat}");
            return true;
        }

        protected override void Close() {
            _pollFramesLoop = false;
            if (_pollFrames != null && _pollFrames.IsAlive && !_pollFrames.Join(5000))
                _pollFrames.Abort();
            _framesArrivedEvent.Dispose();
            _sensorActiveChangedEvent.Dispose();
            foreach (var stream in _streams) {
                stream.Dispose();
            }
            base.Close();
        }

        public override bool IsAvailable() {
            return true;
        }

        protected override void SensorActiveChanged(AbstractSensor sensor) {
            _sensorActiveChangedEvent.Set();
        }

#region Background
        private void ReOpenStreamsBackground() {
            //TODO: seek to current time instead of reopen
            foreach (var stream in _streams) {
                stream.Close();
                if (stream.Sensor.Active) {
                    stream.Open();
                }
            }
        }

        private void PollFrames() {
            var fps = _streams.Min(s => s.Sensor.FPS < 1 ? int.MaxValue : s.Sensor.FPS);
            var frameTime = 1000 / fps;
            bool isFirstTime = true;
            try {
                while (_pollFramesLoop) {
                    var timer = Stopwatch.StartNew();
                    ReOpenStreamsBackground();
                    if (isFirstTime) _mapStream.Open();
                    var streamsEof = false;
                    while (_pollFramesLoop && !streamsEof) {
                        var minWaitTime = frameTime * 5;
                        foreach (var stream in _streams) {
                            if (stream.Opened) {
                                if (stream.ReadNextFrame()) {
                                    var waitTime = Math.Max(1, (int) (stream.frameTime - timer.ElapsedMilliseconds));   
                                    if (waitTime < minWaitTime) minWaitTime = waitTime;
                                } else {
                                    if (stream.Sensor.FPS > 0)
                                        streamsEof = true;
                                    stream.Close();
                                }
                            }
                        }
                        Thread.Sleep(minWaitTime);
                        var anyPolled = false;
                        foreach (var stream in _streams) {
                            if (stream.Opened && stream.frameTime - timer.ElapsedMilliseconds < frameTime 
                            && stream.PollFrame()) {
                                anyPolled = true;
                                if (stream.Sensor == MapDepthToCamera) _mapDepthToCameraUpdated = true;
                            }
                        }
                        if (anyPolled)
                            _framesArrivedEvent.Set();
                        if (_sensorActiveChangedEvent.WaitOne(0)) {
                            ReOpenStreamsBackground();
                            timer = Stopwatch.StartNew();
                        }
                    }
                    isFirstTime = false;
                }
            } catch (ThreadAbortException) {
                Debug.Log(GetType().Name + ": Thread aborted");
            } catch (Exception e) {
                Debug.LogException(e);
            }
        }
#endregion

        protected override IEnumerator Update() {
            while (_pollFramesLoop) {
                if (_framesArrivedEvent.WaitOne(0)) {
                    if (Depth.Active) _internalDepth.OnNewFrame();
                    if (Infrared.Active) _internalInfrared.OnNewFrame();
                    if (Color.Active) _internalColor.OnNewFrame();
                }
                if (_mapDepthToCameraUpdated) {
                    _internalMapDepthToCamera.OnNewFrame();
                    _mapDepthToCameraUpdated = false;
                }
                yield return null;
            }
        }
        
#region Coordinate Map
        public override Vector2 CameraPosToDepthMapPos(Vector3 pos) {
            /*pos *= _DEPTH_MUL;
            Vector2 v = Vector3.zero;
            if (_niDepth.stream != null)
                CoordinateConverter.ConvertWorldToDepth(
                    _niDepth.stream,
                    pos.x, pos.y, pos.z,
                    out v.x, out v.y, out _
                );
            return v;*/
            return Vector2.zero;
        }

        public override Vector2 CameraPosToColorMapPos(Vector3 pos) {
            /*pos *= _DEPTH_MUL;
            int vx = 0, vy = 0;
            if (_niDepth.stream != null && _niColor.stream != null) {
                CoordinateConverter.ConvertWorldToDepth(
                    _niDepth.stream,
                    pos.x, pos.y, pos.z,
                    out var dx, out var dy, out ushort dz
                );
                CoordinateConverter.ConvertDepthToColor(
                    _niDepth.stream, _niColor.stream,
                    dx, dy, dz,
                    out vx, out vy
                );
            }

            return new Vector2(vx, vy);*/
            return Vector2.zero;
        }

        public override Vector2 DepthMapPosToColorMapPos(Vector2 pos, ushort depth) {
            /*int vx = 0, vy = 0;
            if (_niDepth.stream != null)
                CoordinateConverter.ConvertDepthToColor(
                    _niDepth.stream, _niColor.stream,
                    (int)pos.x, (int)pos.y, depth,
                    out vx, out vy
                );
            
            return new Vector2(vx, vy);*/
            return Vector2.zero;
        }

        public override Vector3 DepthMapPosToCameraPos(Vector2 pos, ushort depth) {
            return Vector3.zero;
        }

        private DepthBuffer _parBuf;
        private NativeArray<ushort> _parDepth;
        private NativeArray<Vector2> _parColor;
        public override void DepthMapToColorMap(NativeArray<ushort> depth, NativeArray<Vector2> color) {
            /*_parBuf = Depth.GetOldest();
            _parDepth = depth;
            _parColor = color;
            Parallel.For(0, depth.Length, DepthMapToColorMapBody);*/
        }

        /*private void DepthMapToColorMapBody(int i) {
            var p = _parBuf.GetXYFrom(i);
            _parColor[i] = DepthMapPosToColorMapPos(p, _parDepth[i]);
        }

        private Vector3 DepthMapPosToCameraPos(Vector2 pos, ushort depth) {
            Vector3 v = Vector3.zero;
            if (_niDepth.stream != null)
                CoordinateConverter.ConvertDepthToWorld(
                    _niDepth.stream,
                    (int)pos.x, (int)pos.y, depth,
                    out v.x, out v.y, out v.z
                );
            
            return v / _DEPTH_MUL;
        }

        private MapDepthToCameraBuffer _parMap;
        private void UpdateMapDepthToCamera() {
            _parMap = MapDepthToCamera.GetOldest();
            lock (_parMap.SyncRoot) {
                Parallel.For(0, _parMap.data.Length, UpdateMapDepthToCameraBody);
            }
            _needUpdateMapDepthToColorSpace = false;
            _mapDepthToCameraUpdated = true;
        }

        private void UpdateMapDepthToCameraBody(int i) {
            var p = DepthMapPosToCameraPos(_parMap.GetXYFrom(i), _DEPTH_MUL);
            _parMap.data[i] = new half2((half) p.x, (half) p.y);
        }*/
#endregion
    }
}