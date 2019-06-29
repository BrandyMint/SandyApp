#if ENABLE_OPENNI2
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DepthSensor.Sensor;
using UnityEngine;
using OpenNIWrapper;

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
        }

        private class NI2Sensor {
            public VideoStream stream;
            public AbstractSensor sensor;
            public readonly AutoResetEvent frameEvent = new AutoResetEvent(false);
        }

        private OpenNIWrapper.Device _device;
        private NI2Sensor _niDepth;
        private NI2Sensor _niColor;
        private readonly List<NI2Sensor> _niSensors = new List<NI2Sensor>();

        private volatile bool _needUpdateMapDepthToColorSpace;
        private volatile bool _pollFramesLoop = true;
        private readonly Thread _pollFrames;
        private readonly AutoResetEvent _framesArrivedEvent = new AutoResetEvent(false);
        private readonly AutoResetEvent _sensorActiveChangedEvent = new AutoResetEvent(false);
        private readonly HashSet<NI2Sensor> _sensorsActiveChanged = new HashSet<NI2Sensor>();

        public OpenNI2Device() : base("OpenNI2", Init()) {
            var init = (InitInfoOpenNI2) _initInfo;
            _device = init.device;
            if (_device != null)
                _device.DepthColorSyncEnabled = true;
            _niDepth = CreateNi2Sensor(OpenNIWrapper.Device.SensorType.Depth, init.modeDepth, Depth);
            _niColor = CreateNi2Sensor(OpenNIWrapper.Device.SensorType.Color, init.modeColor, Color);
            _initInfo = null;
            
            _pollFrames = new Thread(PollFrames) {
                Name = GetType().Name
            };
            _pollFrames.Start();
        }

        private NI2Sensor CreateNi2Sensor(OpenNIWrapper.Device.SensorType type, VideoMode mode, AbstractSensor sensor) {
            var ni = new NI2Sensor {
                sensor = sensor
            };
            
            if (sensor.Available && _device != null) {
                ni.stream = _device.CreateVideoStream(type);
                ni.stream.VideoMode = mode;
                ni.stream.OnNewFrame += s => ni.frameEvent.Set();
            }
            
            _niSensors.Add(ni);
            return ni;
        }

        private static InitInfo Init() {
            var init = new InitInfoOpenNI2();

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

                init.modeDepth = GetBestVideoMod(init.device, OpenNIWrapper.Device.SensorType.Depth);
                if (init.modeDepth != null) {
                    if (init.modeDepth.DataPixelFormat != VideoMode.PixelFormat.Depth1Mm)
                        throw new NotImplementedException();
                    init.Depth = new Sensor<ushort>(
                        init.modeDepth.Resolution.Width,
                        init.modeDepth.Resolution.Height
                    );
                    init.MapDepthToColor = new Sensor<Vector2>(init.Depth.width, init.Depth.height);
                } else {
                    init.Depth = new Sensor<ushort>(false);
                    init.MapDepthToColor = new Sensor<Vector2>(false);
                }

                init.modeColor = GetBestVideoMod(init.device, OpenNIWrapper.Device.SensorType.Color);
                if (init.modeColor != null) {
                    if (init.modeColor.DataPixelFormat != VideoMode.PixelFormat.Rgb888)
                        throw new NotImplementedException();
                    init.Color = new ColorByteSensor(
                        init.modeColor.Resolution.Width,
                        init.modeColor.Resolution.Height,
                        3, TextureFormat.RGB24
                    );
                } else {
                    init.Color = new ColorByteSensor(false);
                }


                init.Index = new Sensor<byte>(false);
                init.Body = new BodySensor(false);

            } catch (OpenNI2Exception e) {
                OpenNI.Shutdown();
                Close(ref init.device);
                if (e.status != OpenNI.Status.NoDevice) {
                    throw;
                }
            } catch (Exception) {
                OpenNI.Shutdown();
                Close(ref init.device);
                throw;
            }
            
            return init;
        }

        private static VideoMode GetBestVideoMod(OpenNIWrapper.Device device, OpenNIWrapper.Device.SensorType type) {
            VideoMode mode = null;
            if (device.HasSensor(type)) {
                var stream = device.CreateVideoStream(type);
                if (stream != null) {
                    var supportedModes = stream.SensorInfo.GetSupportedVideoModes();
                    
                    mode = supportedModes.Aggregate((m1, m2) => {
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
                                return m2;
                            case OpenNIWrapper.Device.SensorType.Depth when m2.DataPixelFormat == VideoMode.PixelFormat.Depth1Mm:
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
        }

        public override bool IsAvailable() {
            return _device != null && _device.IsValid;
        }

        protected override void SensorActiveChanged(AbstractSensor sensor) {
            if (sensor == MapDepthToCamera) {
                _needUpdateMapDepthToColorSpace = true;
                return;
            }
            
            var niSensor = _niSensors.FirstOrDefault(s => s.sensor == sensor);
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
                    if (s.stream == null) continue;
                    if (s.sensor.Active) {
                        s.stream.Start();
                    } else {
                        s.stream.Stop();
                    }
                }
                _sensorsActiveChanged.Clear();
            }

            return _niSensors
                .Where(s => s.sensor.Active && s.stream != null)
                .Select(s => s.frameEvent)
                .ToArray();
        }

        private void PollFrames() {
            var waits = new AutoResetEvent[0];
            try {
                while (_pollFramesLoop) {
                    if (_sensorActiveChangedEvent.WaitOne(0))
                        waits = ActivateSensors();
                    if (waits.Length > 0 && WaitHandle.WaitAll(waits, _FRAME_TIME * 3)) {
                        using (var depth = Depth.Active ? _niDepth.stream.ReadFrame() : null)
                        using (var color = Color.Active ? _niColor.stream.ReadFrame() : null) {
                            if (color != null) {
                                MemUtils.CopyBytes(color.Data, Color.data, color.DataSize);
                                _internalColor.OnNewFrameBackground();
                            }

                            if (_needUpdateMapDepthToColorSpace) {
                                UpdateMapDepthToCamera();
                                _internalMapDepthToCamera.OnNewFrameBackground();
                            }

                            if (depth != null) {
                                MemUtils.CopyBytes(depth.Data, Depth.data, depth.DataSize);
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
                    if (s.sensor.Active)
                        s.stream?.Stop();
                }
            }
        }
#endregion

        protected override IEnumerator Update() {
            while (_pollFramesLoop) {
                if (_framesArrivedEvent.WaitOne(0)) {
                    if (Depth.Active) _internalDepth.OnNewFrame();
                    if (Color.Active) _internalColor.OnNewFrame();
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

        public Vector3 DepthMapPosToCameraPos(Vector2 pos, ushort depth) {
            Vector3 v = Vector3.zero;
            if (_niDepth.stream != null)
                CoordinateConverter.ConvertDepthToWorld(
                    _niDepth.stream,
                    (int)pos.x, (int)pos.y, depth,
                    out v.x, out v.y, out v.z
                );
            
            return v / _DEPTH_MUL;
        }

        private void UpdateMapDepthToCamera() {
            Parallel.For(0, MapDepthToCamera.data.LongLength, i => {
                var p = new Vector2(
                    i % MapDepthToCamera.width,
                    i / MapDepthToCamera.height
                );
                MapDepthToCamera.data[i] = DepthMapPosToCameraPos(p, _DEPTH_MUL);
            });
            _needUpdateMapDepthToColorSpace = false;
        }
#endregion
    }
}
#endif