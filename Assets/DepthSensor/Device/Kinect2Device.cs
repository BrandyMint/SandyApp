﻿#if UNITY_STANDALONE_WIN
using Unity.Mathematics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.Kinect;
using DepthSensor.Buffer;
using DepthSensor.Sensor;
using Unity.Collections;
using UnityEngine;
using Body = DepthSensor.Buffer.Body;
using Joint = DepthSensor.Buffer.Joint;

namespace DepthSensor.Device {
    public class Kinect2Device : DepthSensorDevice {
        private const ColorImageFormat _COLOR_FORMAT = ColorImageFormat.Rgba;
        
        private class InitInfoKinect2 : InitInfo {
            public KinectSensor kinect;
        }
        
        private KinectSensor _kinect;
        private MultiSourceFrameReader _multiReader;
        private readonly Windows.Kinect.Body[] _bodyKinect;
        private readonly Dictionary<BodyBuffer, BodyBuffer.Internal<Windows.Kinect.Body>> _internalBodyBuffers = 
            new Dictionary<BodyBuffer, BodyBuffer.Internal<Windows.Kinect.Body>>();
        
        private volatile bool _pollFramesLoop = true;
        private volatile bool _needUpdateMapDepthToColorSpace;
        private volatile bool _mapDepthToCameraUpdated;
        private readonly AutoResetEvent _frameArrivedEvent = new AutoResetEvent(false);
        private readonly AutoResetEvent _sensorActiveChangedEvent = new AutoResetEvent(false);
        private readonly Thread _reactivateSensors;
        private readonly object _multiReaderSync = new object();

        public Kinect2Device() : base(Init()) {
            var init = (InitInfoKinect2) _initInfo;
            _kinect = init.kinect;
            
            _bodyKinect = new Windows.Kinect.Body[Body.GetOldest().data.Length];
            if (!_kinect.IsOpen)
                _kinect.Open();
            _reactivateSensors = new Thread(ReactivateSensorsBGLoop) {
                Name = GetType().Name
            };
            _kinect.CoordinateMapper.CoordinateMappingChanged += OnCoordinateMappingChanged;
            _reactivateSensors.Start();
        }

        private static InitInfo Init() {
            var init = new InitInfoKinect2 {
                Name = "Kinect2"
            };
            
            try {
                init.kinect = KinectSensor.GetDefault();
                init.Depth = new SensorDepth(new DepthBuffer( 
                    init.kinect.DepthFrameSource.FrameDescription.Width,
                    init.kinect.DepthFrameSource.FrameDescription.Height)
                );
                init.SensorIndex = new SensorIndex(new IndexBuffer(
                    init.kinect.BodyIndexFrameSource.FrameDescription.Width,
                    init.kinect.BodyIndexFrameSource.FrameDescription.Height
                ));
                var colorDesc = init.kinect.ColorFrameSource.CreateFrameDescription(_COLOR_FORMAT);
                init.SensorColor = new SensorColor(new ColorBuffer(
                    colorDesc.Width, colorDesc.Height, TextureFormat.RGBA32
                ));
                init.Body = new SensorBody(new BodyBuffer(init.kinect.BodyFrameSource.BodyCount));
                init.MapDepthToCamera = new SensorMapDepthToCamera(new MapDepthToCameraBuffer(
                    init.Depth.GetOldest().width, init.Depth.GetOldest().height)
                );
            } catch (DllNotFoundException) {
                Debug.LogWarning("Kinect 2 runtime is not is not installed");
                Close(ref init.kinect);
                throw;
            }
            
            return init;
        }

        public override bool IsAvailable() {
            return _kinect != null && _kinect.IsAvailable;
        }

        protected override void SensorActiveChanged(AbstractSensor sensor) {
            if (sensor == MapDepthToCamera)
                _needUpdateMapDepthToColorSpace = true;
            _sensorActiveChangedEvent.Set();
        }

        private void CloseSensors() {
            if (_multiReader != null) {
                _multiReader.MultiSourceFrameArrived -= PollFrames;
                _multiReader.Dispose();
                _multiReader = null;
            }
        }

        private void ReActivateSensors() {
            CloseSensors();
            if (!Depth.Active && !Index.Active && !Body.Active && !Color.Active) 
                return;
            _multiReader = _kinect.OpenMultiSourceFrameReader(
                (Depth.Active ? FrameSourceTypes.Depth : 0) |
                (Index.Active ? FrameSourceTypes.BodyIndex : 0) | 
                (Body.Active ? FrameSourceTypes.Body : 0) |
                (Color.Active ? FrameSourceTypes.Color : 0));
            _multiReader.MultiSourceFrameArrived += PollFrames;
        }

        private void ReactivateSensorsBGLoop() {
            while (_pollFramesLoop) {
                if (_sensorActiveChangedEvent.WaitOne(100)) {
                    lock (_multiReaderSync) {
                        Thread.Sleep(100);
                        ReActivateSensors();
                    }
                }
            }
        }

        private void PollFrames(object sender, MultiSourceFrameArrivedEventArgs multiSourceFrame) {
            if (!Monitor.TryEnter(_multiReaderSync, 0))
                return;

            var frame = multiSourceFrame.FrameReference.AcquireFrame();;
            
            if (frame != null) {
                using (var body = frame.BodyFrameReference.AcquireFrame())
                using (var index = frame.BodyIndexFrameReference.AcquireFrame())
                using (var color = frame.ColorFrameReference.AcquireFrame())
                using (var depth = frame.DepthFrameReference.AcquireFrame()) {
                    if (body != null) {
                        UpdateBodies(body);
                        _internalBody.OnNewFrameBackground();
                    }

                    if (color != null) {
                        using (var raw = color.LockRawImageBuffer()) {
                            //Color.GetOldest().SetBytes(raw.UnderlyingBuffer, raw.Length);
                            var buff = Color.GetOldest();
                            lock (buff.SyncRoot) {
                                color.CopyConvertedFrameDataToIntPtr(buff.data.IntPtr(),
                                    (uint) buff.data.GetLengthInBytes(), _COLOR_FORMAT);
                            }
                        }
                        _internalColor.OnNewFrameBackground();
                    }

                    if (index != null) {
                        using (var buffer = index.LockImageBuffer()) {
                            Index.GetOldest().SetBytes(buffer.UnderlyingBuffer, buffer.Length);
                        }
                        _internalIndex.OnNewFrameBackground();
                    }

                    if (_needUpdateMapDepthToColorSpace ||
                        (MapDepthToCamera.Active && !IsMapDepthToCameraValid())) {
                        UpdateMapDepthToCamera();
                        _internalMapDepthToCamera.OnNewFrameBackground();
                    }

                    if (depth != null) {
                        using (var buffer = depth.LockImageBuffer()) {
                            Depth.GetOldest().SetBytes(buffer.UnderlyingBuffer, buffer.Length);
                        }
                        _internalDepth.OnNewFrameBackground();
                    }

                    _frameArrivedEvent.Set();
                }
            }
            Monitor.Exit(_multiReaderSync);
        }

        private void UpdateBodies(BodyFrame frame) {
            frame.GetAndRefreshBodyData(_bodyKinect);
            var buff = Body.GetOldest();
            lock (buff.SyncRoot) {
                if (!_internalBodyBuffers.TryGetValue(buff, out var intern)) {
                    intern = new BodyBuffer.Internal<Windows.Kinect.Body>(buff);
                }

                intern.UpdateBodiesIndexed(_bodyKinect, GetBodyId, UpdateBody);
            }
        }

        private static bool GetBodyId(Windows.Kinect.Body body, out ulong id) {
            id = body.TrackingId;
            return body.IsTracked;
        }

        private static void UpdateBody(Body.Internal internalBody, Windows.Kinect.Body newBody) {
            internalBody.Set(newBody.IsTracked, newBody.TrackingId);
            foreach (var newJoint in newBody.Joints.Values) {
                var pos = ToVector3(newJoint.Position);
                internalBody.SetJoint(
                    (Joint.Type) newJoint.JointType, 
                    newJoint.TrackingState == TrackingState.Tracked,
                    pos);
            }
        }

        protected override IEnumerator Update() {
            while (_pollFramesLoop) {
                if (_frameArrivedEvent.WaitOne(0)) {
                    if (Depth.Active) _internalDepth.OnNewFrame();
                    if (Index.Active) _internalIndex.OnNewFrame();
                    if (Color.Active) _internalColor.OnNewFrame();
                    if (Body.Active) _internalBody.OnNewFrame();
                }
                if (_mapDepthToCameraUpdated) {
                    _internalMapDepthToCamera.OnNewFrame();
                    _mapDepthToCameraUpdated = false;
                }
                yield return null;
            }
        }

        private void OnCoordinateMappingChanged(object sender, CoordinateMappingChangedEventArgs e) {
            if (MapDepthToCamera.Active) {
                _needUpdateMapDepthToColorSpace = true;
            }
        }

        protected override void Close() {
            _pollFramesLoop = false;
            if (_reactivateSensors != null && _reactivateSensors.IsAlive && !_reactivateSensors.Join(5000))
                _reactivateSensors.Abort();
            lock (_multiReaderSync) {
                CloseSensors();
            }
            Close(ref _kinect);
            _frameArrivedEvent.Dispose();
            base.Close();
        }

        private static void Close(ref KinectSensor kinect) {
            if (kinect != null) {
                if (kinect.IsOpen)
                    kinect.Close();
                kinect = null;
            }
        }

        #region Coordinate Map
        public override Vector2 CameraPosToDepthMapPos(Vector3 pos) {
            if (_kinect == null) return Vector2.zero;
            return ToVector2(_kinect.CoordinateMapper.
                MapCameraPointToDepthSpace(ToCameraPoint(pos)));
        }

        public override Vector2 CameraPosToColorMapPos(Vector3 pos) {
            if (_kinect == null) return Vector2.zero;
            return ToVector2(_kinect.CoordinateMapper.
                MapCameraPointToColorSpace(ToCameraPoint(pos)));
        }
        
        public override Vector2 DepthMapPosToColorMapPos(Vector2 pos, ushort depth) {
            if (_kinect == null) return Vector2.zero;
            return ToVector2(_kinect.CoordinateMapper.
                MapDepthPointToColorSpace(ToDepthPoint(pos), depth));
        }

        public override void DepthMapToColorMap(NativeArray<ushort> depth, NativeArray<Vector2> color) {
            if (_kinect == null) return;
            _kinect.CoordinateMapper.MapDepthFrameToColorSpaceUsingIntPtr(
                depth.IntPtr(), depth.Length * sizeof(ushort),
                color.IntPtr(), (uint) color.Length);
        }

        private void UpdateMapDepthToCamera() {
            if (_kinect == null) return;
            //TODO: broken table in SDK, workaround with MapDepthFrameToCameraSpace
            //var map = _kinect.CoordinateMapper.GetDepthFrameToCameraSpaceTable();
            var depthBuff = Depth.GetOldest();
            lock (depthBuff.SyncRoot) {
                var map = new CameraSpacePoint[depthBuff.data.Length];
                var depth = new ushort[depthBuff.data.Length];
                Parallel.For(0, depth.Length, i => {
                    depth[i] = 1000;
                });
                _kinect.CoordinateMapper.MapDepthFrameToCameraSpace(depth, map);
                var mapBuff = MapDepthToCamera.GetOldest();
                Parallel.For(0, map.Length, i => {
                    var mapPoint = map[i];
                    mapBuff.data[i] = new half2(new float2(mapPoint.X, mapPoint.Y));
                });
            }
            
            _needUpdateMapDepthToColorSpace = false;
            _mapDepthToCameraUpdated = true;
        }

        private bool IsMapDepthToCameraValid() {
            var mapBuff = MapDepthToCamera.GetOldest();
            var i = mapBuff.data.Length - 1;
            var x = mapBuff.data[i].x;
            return !float.IsNaN(x) && !float.IsInfinity(x) && x != 0f;
        }
#endregion
        
#region Vector Conversions        
        private static Vector3 ToVector3(CameraSpacePoint p) {
            return new Vector3(p.X, p.Y, p.Z);
        }
        
        private static Vector2 ToVector2(DepthSpacePoint p) {
            return new Vector2(p.X, p.Y);
        }
        
        private static Vector2 ToVector2(ColorSpacePoint p) {
            return new Vector2(p.X, p.Y);
        }

        private static CameraSpacePoint ToCameraPoint(Vector3 p) {
            return new CameraSpacePoint {X = p.x, Y = p.y, Z = p.z};
        }
        
        private static DepthSpacePoint ToDepthPoint(Vector3 p) {
            return new DepthSpacePoint {X = p.x, Y = p.y};
        }
        
        private static DepthSpacePoint ToDepthPoint(Vector2 p) {
            return new DepthSpacePoint {X = p.x, Y = p.y};
        }

        private static ColorSpacePoint ToColorPoint(Vector3 p) {
            return new ColorSpacePoint {X = p.x, Y = p.y};
        }
        
        private static ColorSpacePoint ToColorPoint(Vector2 p) {
            return new ColorSpacePoint {X = p.x, Y = p.y};
        }
#endregion
    }
}
#endif