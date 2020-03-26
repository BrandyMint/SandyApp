//#define TEST_COMPARE_PLAYER_WITH_DEVICE
//#define TEST_CAMPARE_PLAYER_WITH_MAP
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DepthSensor.Buffer;
using DepthSensor.Recorder;
using DepthSensor.Sensor;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Convert = Utilities.OpenCVSharpUnity.Convert;
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

        private readonly RecordCalibrationResult _calibration;
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
        private readonly string _recordPath;
#if TEST_COMPARE_PLAYER_WITH_DEVICE
        private readonly DepthSensorDevice _testDev;
        private readonly DepthSensorDevice.Internal _testDevInternal;
#endif

        public RecordPlayerDevice(string path) : base(Init(path)) {
            _recordPath = path;
            _calibration = new RecordCalibrationResult(path);
            _calibration.Load();

            var depthFov = _calibration.IntrinsicDepth.GetFOV();
            var colorFov = _calibration.IntrinsicColor.GetFOV();
            
            _colorStream = InitStream(_internalColor, path, nameof(Color), colorFov);
            _infraredStream = InitStream(_internalInfrared, path, nameof(Infrared), depthFov);
            _mapStream = InitStream(_internalMapDepthToCamera, path, nameof(MapDepthToCamera), depthFov);
            _depthStream = InitStream(_internalDepth, path, nameof(Depth), depthFov);

            _initInfo = null;
            
#if TEST_COMPARE_PLAYER_WITH_DEVICE
            _testDev = new OpenNI2Device();
            _testDevInternal = new Internal(_testDev);
#endif
            InitCamMatrices();
            
            _pollFrames = new Thread(PollFrames) {
                Name = GetType().Name
            };
            _pollFrames.Start();
        }

        private SensorStream InitStream<T>(Sensor<T>.Internal internalSensor, string path, string name, Vector2 fov) where T : AbstractBuffer2D {
            var stream = new SensorStream(Path.Combine(path, name)) {
                SensorInternal = internalSensor,
                Sensor = internalSensor.sensor
            };
            var info = ((InitInfoRecordPlayer) _initInfo).manifest.Get<StreamInfo>(name);
            if (info != null) {
                internalSensor.SetTargetFps(info.fps);
                internalSensor.SetFov(fov);
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
                init.Name = $"{init.manifest.DeviceName} ▶️'{new DirectoryInfo(path).Name}'";

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

        protected override void OnCloseInternal() {
            _pollFramesLoop = false;
            if (_pollFrames != null && _pollFrames.IsAlive && !_pollFrames.Join(5000))
                _pollFrames.Abort();
            _framesArrivedEvent.Dispose();
            _sensorActiveChangedEvent.Dispose();
            foreach (var stream in _streams) {
                stream.Dispose();
            }
#if TEST_COMPARE_PLAYER_WITH_DEVICE
            _testDevInternal.Close();
#endif
        }

        public string RecordPath => _recordPath;

        public override bool IsAvailable() {
            //TODO: check IO errors
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
#if TEST_CAMPARE_PLAYER_WITH_MAP
                    TestWithMap();
#endif
#if TEST_COMPARE_PLAYER_WITH_DEVICE
                    TestWithDevice();
#endif
                }
                yield return null;
            }
        }
        
#region Coordinate Map

        private struct Distortion {
            public float k1;
            public float k2;
            public float p1;
            public float p2;
            public float k3;

            public Distortion(double[] d) {
                k1 = (float)d[0];
                k2 = (float)d[1];
                p1 = (float)d[2];
                p2 = (float)d[2];
                k3 = (float)d[4];
            }
        }
        private float3x3 _mDepth = float3x3.identity;
        private float3x3 _mDepthInv = float3x3.identity;
        private float3x3 _mColor = float3x3.identity;
        private float4x4 _mColorRT = float4x4.identity;
        private Distortion _distDepth = new Distortion();
        private Distortion _distColor = new Distortion();
        private int _depthHeight;
        private int _colorHeight;

        private void InitCamMatrices() {
            if (_calibration.IntrinsicDepth != null) {
                _mDepth = Convert.ToFloat3x3(_calibration.IntrinsicDepth.cameraMatrix);
                _mDepthInv = math.inverse(_mDepth);
                _distDepth = new Distortion(_calibration.IntrinsicDepth.distCoeffs);
                _depthHeight = _calibration.IntrinsicDepth.imgSizeUsedOnCalculate.Height;
            }
            if (_calibration.IntrinsicColor != null) {
                _mColor = Convert.ToFloat3x3(_calibration.IntrinsicColor.cameraMatrix);
                var tvec = Convert.ToVec3d(_calibration.T);
                _mColorRT = math.inverse(Convert.RodriguesTVecToFloat4x4(_calibration.R, tvec));
                //_mColorRT = new float4x4(float3x3.identity, new float3((float) -tvec.Item0, 0f, 0f));
                _distColor = new Distortion(_calibration.IntrinsicColor.distCoeffs);
                _colorHeight = _calibration.IntrinsicColor.imgSizeUsedOnCalculate.Height;
            }
        }

        public override Vector2 CameraPosToDepthMapPos(Vector3 pos) {
            var pp = new float2(pos.x / pos.z, pos.y / pos.z);
            var uv = math.mul(_mDepth, new float3(pp /*Distort(pp, _distDepth)*/, 1f));
            return new Vector2(uv.x, _depthHeight - uv.y);
        }
        
        public override Vector2 CameraPosToColorMapPos(Vector3 pos) {
            var p = math.mul(_mColorRT, new float4(pos, 1f));
            var pp = new float2(p.x / p.z, p.y / p.z);
            var uv = math.mul(_mColor, new float3(Distort(pp, _distColor), 1f));
            return new Vector2(uv.x, _colorHeight - uv.y);
        }

        public override Vector2 DepthMapPosToColorMapPos(Vector2 pos, ushort depth) {
            return CameraPosToColorMapPos(DepthMapPosToCameraPos(pos, depth));
        }

        public override Vector3 DepthMapPosToCameraPos(Vector2 pos, ushort depth) {
            var z = (float) depth / 1000f;
            pos.y = _depthHeight - pos.y;
            var p = math.mul(_mDepthInv, new float3(pos, 1f));
            var pp = p;//new float3(UnDistort(new float2(p.x, p.y), _distDepth), p.z);
            return new Vector3(pp.x, pp.y, pp.z) * z;
        }

        private static float2 Distort(float2 p, Distortion d) {
            var r2 = p.x * p.x + p.y * p.y;
            var r4 = r2 * r2;
            var r6 = r4 * r2;
            var a1 = 2f * p.x * p.y;
            var a2 = r2 + 2f * p.x * p.x;
            var a3 = r2 + 2f * p.y * p.y;
            var cdist = 1f + d.k1 * r2 + d.k2 * r4 + d.k3 * r6;
            return new float2(
                p.x*cdist + d.p1*a1 + d.p2*a2,
                p.y*cdist + d.p1*a3 + 2*d.p2*a1
            );
        }
        
        /*private static float2 UnDistort(float2 p, Distortion d) {
            return p;
            return p;
            var r = p.x * p.x + p.y * p.y;
            var k = 1f + d.k1*r + d.k2*r*r + d.k3*r*r*r;
            return new float2(
                p.x*k + 2*d.p1*p.x*p.y + d.p2*(r + 2*p.x*p.x),
                p.y*k + d.p1*(r + 2*p.y*p.y) + 2*d.p2*p.x*p.y
            );
        }*/

        private DepthBuffer _parBuf;
        private NativeArray<ushort> _parDepth;
        private NativeArray<Vector2> _parColor;
        public override void DepthMapToColorMap(NativeArray<ushort> depth, NativeArray<Vector2> color) {
            _parBuf = Depth.GetOldest();
            _parDepth = depth;
            _parColor = color;
            Parallel.For(0, depth.Length, DepthMapToColorMapBody);
        }

        private void DepthMapToColorMapBody(int i) {
            var p = _parBuf.GetXYFrom(i);
            _parColor[i] = DepthMapPosToColorMapPos(p, _parDepth[i]);
        }
#endregion

#region TEST Coordinate map
#if TEST_CAMPARE_PLAYER_WITH_MAP
        private void TestWithMap() {
            var map = MapDepthToCamera.GetNewest();
            var maxError = 0f;
            var maxErrRightD = float3.zero;
            var maxErrActualD = float3.zero;
            var maxErrRightP = float2.zero;
            var maxErrActualP = float2.zero;
            for (int x = 0; x < map.width; x++) {
                for (int y = 0; y < map.height; y++) {
                    var d = map.data[map.GetIFrom(x, y)];
                    var rightD = new float3(d, 1f) * 5f;
                    var actualD = DepthMapPosToCameraPos(new Vector2(x, y), 5000);
                    var err = math.distance(rightD, actualD);
                    if (err > maxError) {
                        maxError = err;
                        maxErrRightD = rightD;
                        maxErrActualD = actualD;
                        maxErrRightP = new float2(x, y);
                        maxErrActualP = CameraPosToDepthMapPos(actualD);
                    }
                }
            }
            Debug.Log("Error " + maxError + "\t" + maxErrRightD + "\t" + maxErrActualD);
            Debug.Log("pix err " + math.distance(maxErrRightP, maxErrActualP) + "\t" + maxErrRightP + "\t" + maxErrActualP);
        }
#endif
        
#if TEST_COMPARE_PLAYER_WITH_DEVICE
        private void TestWithDevice() {
            Debug.Log("cam to depth");
            TestWithDevice(_testDev.DepthMapPosToCameraPos, _testDev.CameraPosToDepthMapPos, CameraPosToDepthMapPos);
            Debug.Log("cam to color");
            TestWithDevice(_testDev.DepthMapPosToCameraPos, _testDev.CameraPosToColorMapPos, CameraPosToColorMapPos);
        }
        
        private void TestWithDevice(Func<Vector2, ushort, Vector3> getP, Func<Vector3, Vector2> fDev, Func<Vector3, Vector2> fRec) {
            var map = MapDepthToCamera.GetNewest();
            var maxError = 0f;
            var maxErrRightD = float2.zero;
            var maxErrActualD = float2.zero;
            var maxErrP = float2.zero;
            for (int x = 0; x < map.width; x++) {
                for (int y = 0; y < map.height; y++) {
                    var pp = new float2(x, y);
                    var p = getP.Invoke(pp, 5000);
                    var rightD = fDev.Invoke(p);
                    var actualD = fRec.Invoke(p);
                    var err = math.distance(rightD, actualD);
                    if (err > maxError) {
                        maxError = err;
                        maxErrRightD = rightD;
                        maxErrActualD = actualD;
                        maxErrP = pp;
                    }
                }
            }
            Debug.Log(maxErrP);
            Debug.Log("Error " + maxError + "\t" + maxErrRightD + "\t" + maxErrActualD);
        }
#endif
#endregion
    }
}