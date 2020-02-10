#if !DISABLE_OPENNI2
using Unity.Mathematics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DepthSensor.Buffer;
using DepthSensor.Sensor;
using UnityEngine;
using OpenNIWrapper;
using Unity.Collections;

namespace DepthSensor.Device {
    public class OpenNI2Device : DepthSensorDevice {
        private const int _FPS = 30;
        private const int _FRAME_TIME = 1000 / _FPS;
        private const int _DEPTH_MUL = 1000;

        public class OpenNI2Exception : Exception {
            public readonly OpenNI.Status status;

            internal OpenNI2Exception(OpenNI.Status status, string message) : base(message) {
                this.status = status;
            } 
        }

        private class InitInfoOpenNI2 : InitInfo {
            public OpenNIWrapper.Device device;
            public VideoMode modeDepth;
            public VideoMode modeColor;
            public VideoMode modeInfrared;
        }

        private class NI2Sensor {
            public VideoStream stream;
            public bool started;
            public AbstractSensor Sensor;
            public readonly AutoResetEvent frameEvent = new AutoResetEvent(false);

            public void SafeStart() {
                if (stream != null && !started) {
                    stream.Start();
                    started = true;
                }
            }

            public void SafeStop() {
                if (stream != null && started) {
                    stream.Stop();
                    started = false;
                }
            }
        }

        private OpenNIWrapper.Device _device;
        private NI2Sensor _niDepth;
        private NI2Sensor _niColor;
        private NI2Sensor _niInfrared;
        private readonly List<NI2Sensor> _niSensors = new List<NI2Sensor>();

        private volatile bool _needUpdateMapDepthToColorSpace;
        private volatile bool _pollFramesLoop = true;
        private volatile bool _mapDepthToCameraUpdated;
        private readonly Thread _pollFrames;
        private readonly AutoResetEvent _framesArrivedEvent = new AutoResetEvent(false);
        private readonly AutoResetEvent _sensorActiveChangedEvent = new AutoResetEvent(false);
        private readonly HashSet<NI2Sensor> _sensorsActiveChanged = new HashSet<NI2Sensor>();

        public OpenNI2Device() : base(Init()) {
            var init = (InitInfoOpenNI2) _initInfo;
            _device = init.device;
            if (_device != null)
                _device.DepthColorSyncEnabled = true;
            _niDepth = CreateNi2Sensor(OpenNIWrapper.Device.SensorType.Depth, init.modeDepth, _internalDepth);
            _niColor = CreateNi2Sensor(OpenNIWrapper.Device.SensorType.Color, init.modeColor, _internalColor);
            _niInfrared = CreateNi2Sensor(OpenNIWrapper.Device.SensorType.Ir, init.modeInfrared, _internalInfrared);
            _initInfo = null;
            
            _pollFrames = new Thread(PollFrames) {
                Name = GetType().Name
            };
            _pollFrames.Start();
        }

        private NI2Sensor CreateNi2Sensor<T>(OpenNIWrapper.Device.SensorType type, VideoMode mode, Sensor<T>.Internal internalSensor) where T : AbstractBuffer {
            var ni = new NI2Sensor {
                Sensor = internalSensor.sensor
            };
            
            if (internalSensor.sensor.Available && _device != null) {
                ni.stream = _device.CreateVideoStream(type);
                ni.stream.VideoMode = mode;
                ni.stream.OnNewFrame += s => ni.frameEvent.Set();
                internalSensor.SetTargetFps(mode.Fps);
                internalSensor.SetFov(new Vector2(ni.stream.HorizontalFieldOfView, ni.stream.VerticalFieldOfView) * Mathf.Rad2Deg);
            }
            
            _niSensors.Add(ni);
            return ni;
        }

        private static InitInfo Init() {
            var init = new InitInfoOpenNI2 {
                Name = "OpenNI2"
            };

            try {
                var status = OpenNI.Initialize();
                if (status != OpenNI.Status.Ok) {
                    throw new OpenNI2Exception(status, $"Fail to init OpenNI2! status: {status}, message: {OpenNI.LastError}");
                }

                var deviceInfo = OpenNI.EnumerateDevices().FirstOrDefault(d => d.IsValid);
                if (deviceInfo == null || (init.device = deviceInfo.OpenDevice()) == null) {
                    throw new OpenNI2Exception(OpenNI.Status.NoDevice, $"Fail to open OpenNI2 device! message: {OpenNI.LastError}");
                }

                Debug.Log($"OpenNI2: selected device {deviceInfo.Name} {deviceInfo.Uri}");
                init.Name = $"{deviceInfo.Vendor} {deviceInfo.Name}";

                init.modeDepth = GetBestVideoMod(init.device, OpenNIWrapper.Device.SensorType.Depth);
                if (init.modeDepth != null) {
                    if (init.modeDepth.DataPixelFormat != VideoMode.PixelFormat.Depth1Mm)
                        throw new NotImplementedException("Not implemented infrared pixel format " + init.modeDepth.DataPixelFormat);
                    init.Depth = new SensorDepth(new DepthBuffer(
                        init.modeDepth.Resolution.Width,
                        init.modeDepth.Resolution.Height
                    ));
                    init.MapDepthToCamera = new SensorMapDepthToCamera(new MapDepthToCameraBuffer(
                        init.Depth.GetOldest().width, 
                        init.Depth.GetOldest().height
                    ));
                }

                init.modeColor = GetBestVideoMod(init.device, OpenNIWrapper.Device.SensorType.Color);
                if (init.modeColor != null) {
                    if (init.modeColor.DataPixelFormat != VideoMode.PixelFormat.Rgb888)
                        throw new NotImplementedException("Not implemented color pixel format " + init.modeColor.DataPixelFormat);
                    init.Color = new SensorColor(new ColorBuffer(
                        init.modeColor.Resolution.Width,
                        init.modeColor.Resolution.Height,
                        TextureFormat.RGB24
                    ));
                }
                
                init.modeInfrared = GetBestVideoMod(init.device, OpenNIWrapper.Device.SensorType.Ir);
                if (init.modeInfrared != null) {
                    TextureFormat format;
                    if (init.modeInfrared.DataPixelFormat == VideoMode.PixelFormat.Gray8)
                        format = TextureFormat.R8;
                    else if (init.modeInfrared.DataPixelFormat == VideoMode.PixelFormat.Gray16)
                        format = TextureFormat.R16;
                    else
                        throw new NotImplementedException("Not implemented infrared pixel format " + init.modeInfrared.DataPixelFormat);
                    init.Infrared = new SensorInfrared(new InfraredBuffer(
                        init.modeInfrared.Resolution.Width,
                        init.modeInfrared.Resolution.Height,
                        format
                    ));
                }
            } catch (Exception) {
                Close(ref init.device);
                OpenNI.Shutdown();
                throw;
            }
            
            return init;
        }

        private static VideoMode GetBestVideoMod(OpenNIWrapper.Device device, OpenNIWrapper.Device.SensorType type) {
            VideoMode mode = null;
            if (device.HasSensor(type)) {
                var stream = device.CreateVideoStream(type);
                if (stream != null) {
                    var supportedModes = stream.SensorInfo.GetSupportedVideoModes().ToArray();

                    var def = StreamParams.Default;
                    var isFreenect2 = device.DeviceInfo.Uri.StartsWith("freenect2");
                    switch (type) {
                        case OpenNIWrapper.Device.SensorType.Ir when isFreenect2:
                        case OpenNIWrapper.Device.SensorType.Depth when isFreenect2:
                            //for Kinect2 Depth 640x480@30 is invalid
                            def = new StreamParams(512, 424, 30, true);
                            break;
                        case OpenNIWrapper.Device.SensorType.Ir:
                            def = Prefs.Sensor.IR;
                            break;
                        case OpenNIWrapper.Device.SensorType.Color:
                            def = Prefs.Sensor.Color;
                            break;
                        case OpenNIWrapper.Device.SensorType.Depth:
                            def = Prefs.Sensor.Depth;
                            break;
                    }

                    foreach (var m in supportedModes) {
                        Debug.Log("available " + m);
                    }
                    mode = supportedModes.Aggregate((m1, m2) => {
                        if (def.use) {
                            var match1 = MatchDefault(def, m1);
                            var match2 = MatchDefault(def, m2);
                            if (Math.Abs(match1 - match2) > 0.01f)
                                return match1 > match2 ? m1 : m2;
                        }
                        
                        if (m1.Fps != m2.Fps) {
                            return Math.Abs(m1.Fps - _FPS) < Math.Abs(m2.Fps - _FPS) ? m1 : m2;
                        }

                        var res1 = m1.Resolution.Width + m1.Resolution.Height;
                        var res2 = m2.Resolution.Width + m2.Resolution.Height;
                        if (res1 != res2) {
                            return res1 > res2 ? m1 : m2;
                        }

                        switch (type) {
                            case OpenNIWrapper.Device.SensorType.Color when m2.DataPixelFormat == VideoMode.PixelFormat.Rgb888:
                            case OpenNIWrapper.Device.SensorType.Depth when m2.DataPixelFormat == VideoMode.PixelFormat.Depth1Mm:
                            case OpenNIWrapper.Device.SensorType.Ir when m2.DataPixelFormat == VideoMode.PixelFormat.Gray8:
                                return m2;
                            default:
                                return m1;
                        }
                    });
                };
            }
            
            Debug.Log($"OpenNI2: for {type} selected mode {mode}");
            return mode;
        }

        private static float MatchDefault(StreamParams def, VideoMode m) {
            if (m.Resolution.Width > def.width || m.Resolution.Height > def.height)
                return -1f;
            var resolutionMatch = (float) m.Resolution.Height / def.height * m.Resolution.Width / def.width;
            if (m.Fps > def.fps)
                return resolutionMatch / 2f * def.fps / m.Fps;
            return resolutionMatch * m.Fps / def.fps;
        }

        private static void Close(ref OpenNIWrapper.Device device) {
            if (device != null) {
                device.Close();
                device = null;
            }
        }

        protected override void Close() {
            _pollFramesLoop = false;
            if (_pollFrames != null && _pollFrames.IsAlive && !_pollFrames.Join(5000))
                _pollFrames.Abort();
            Close(ref _device);
            OpenNI.Shutdown();
            foreach (var s in _niSensors) {
                s.frameEvent.Dispose();
            }
            _framesArrivedEvent.Dispose();
            _sensorActiveChangedEvent.Dispose();
            base.Close();
        }

        public override bool IsAvailable() {
            return _device != null && _device.IsValid;
        }

        protected override void SensorActiveChanged(AbstractSensor sensor) {
            if (sensor == MapDepthToCamera) {
                _needUpdateMapDepthToColorSpace = true;
                return;
            }
            
            var niSensor = _niSensors.FirstOrDefault(s => s.Sensor == sensor);
            if (niSensor == null) return;
            
            lock (_sensorsActiveChanged) {
                _sensorsActiveChanged.Add(niSensor);
            }
            _sensorActiveChangedEvent.Set();
        }

#region Background
        private AutoResetEvent[] ActivateSensors() {
            lock (_sensorsActiveChanged) {
                foreach (var s in _sensorsActiveChanged) {
                    if (s.Sensor.Active) {
                        s.SafeStart();
                    } else {
                        s.SafeStop();
                    }
                }
                _sensorsActiveChanged.Clear();
            }
            var events = _niSensors
                .Where(s => s.Sensor.Active && s.stream != null)
                .Select(s => s.frameEvent).ToArray();
            return events;
        }

        private void PollFrames() {
            var waits = new WaitHandle[0];
            int idx, colorIdx = 0, infraredIdx = 0, depthIdx = 0;
            //frame idx checking because frame are duplicated when multisensor reading
            //TODO: but how avoid duplicate stream.ReadFrame() ?
            try {
                while (_pollFramesLoop) {
                    if (_sensorActiveChangedEvent.WaitOne(0)) {
                        waits = ActivateSensors();
                        Thread.Sleep(_FRAME_TIME);
                    }
                    if (waits.Length > 0 && WaitHandle.WaitAll(waits, _FRAME_TIME * 5)) {
                        using (var depth = _niDepth.started ? _niDepth.stream.ReadFrame() : null)
                        using (var infrared = _niInfrared.started ? _niInfrared.stream.ReadFrame() : null)
                        using (var color = _niColor.started ? _niColor.stream.ReadFrame() : null) {
                            if (color != null && colorIdx != (idx = color.FrameIndex)) {
                                colorIdx = idx;
                                Color.GetOldest().SetBytes(color.Data, color.DataSize);
                                _internalColor.OnNewFrameBackground();
                            }
                            
                            if (infrared != null && infraredIdx != (idx = infrared.FrameIndex)) {
                                infraredIdx = idx;
                                Infrared.GetOldest().SetBytes(infrared.Data, infrared.DataSize);
                                _internalInfrared.OnNewFrameBackground();
                            }

                            if (_needUpdateMapDepthToColorSpace) {
                                UpdateMapDepthToCamera();
                                _internalMapDepthToCamera.OnNewFrameBackground();
                            }

                            if (depth != null && depthIdx != (idx = depth.FrameIndex)) {
                                depthIdx = idx;
                                Depth.GetOldest().SetBytes(depth.Data, depth.DataSize);
                                _internalDepth.OnNewFrameBackground();
                            }
                            _framesArrivedEvent.Set();
                        }
                    } else {
                        Thread.Sleep(_FRAME_TIME / 3);
                    }
                }
            } catch (ThreadAbortException) {
                Debug.Log(GetType().Name + ": Thread aborted");
            } catch (Exception e) {
                Debug.LogException(e);
            } finally {
                foreach (var s in _niSensors) {
                    s.SafeStop();
                }
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
            pos *= _DEPTH_MUL;
            Vector2 v = Vector3.zero;
            if (_niDepth.stream != null)
                CoordinateConverter.ConvertWorldToDepth(
                    _niDepth.stream,
                    pos.x, pos.y, pos.z,
                    out v.x, out v.y, out _
                );
            return v;
        }

        public override Vector2 CameraPosToColorMapPos(Vector3 pos) {
            pos *= _DEPTH_MUL;
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

            return new Vector2(vx, vy);
        }

        public override Vector2 DepthMapPosToColorMapPos(Vector2 pos, ushort depth) {
            int vx = 0, vy = 0;
            if (_niDepth.stream != null)
                CoordinateConverter.ConvertDepthToColor(
                    _niDepth.stream, _niColor.stream,
                    (int)pos.x, (int)pos.y, depth,
                    out vx, out vy
                );
            
            return new Vector2(vx, vy);
        }
        
        public override Vector3 DepthMapPosToCameraPos(Vector2 pos, ushort depth) {
            var v = Vector3.zero;
            if (_niDepth.stream != null)
                CoordinateConverter.ConvertDepthToWorld(
                    _niDepth.stream,
                    pos.x, pos.y, depth,
                    out v.x, out v.y, out v.z
                );
            
            return v / _DEPTH_MUL;
        }

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

        private MapDepthToCameraBuffer _parMap;
        private void UpdateMapDepthToCamera() {
            _parMap = MapDepthToCamera.GetOldest();
            Parallel.For(0, _parMap.data.Length, UpdateMapDepthToCameraBody);
            _needUpdateMapDepthToColorSpace = false;
            _mapDepthToCameraUpdated = true;
        }

        private void UpdateMapDepthToCameraBody(int i) {
            var p = DepthMapPosToCameraPos(_parMap.GetXYFrom(i), _DEPTH_MUL);
            _parMap.data[i] = new half2((half) p.x, (half) p.y);
        }
#endregion
    }
}
#endif