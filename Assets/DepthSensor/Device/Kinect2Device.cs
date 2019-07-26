﻿#if UNITY_STANDALONE_WIN
using Unity.Mathematics;
using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using Windows.Kinect;
using DepthSensor.Stream;
using UnityEngine;
using Body = DepthSensor.Stream.Body;
using Joint = DepthSensor.Stream.Joint;

namespace DepthSensor.Device {
    public class Kinect2Device : DepthSensorDevice {
        private const ColorImageFormat _COLOR_FORMAT = ColorImageFormat.Rgba;
        
        private class InitInfoKinect2 : InitInfo {
            public KinectSensor kinect;
        }
        
        private KinectSensor _kinect;
        private MultiSourceFrameReader _multiReader;
        private readonly Windows.Kinect.Body[] _bodyBuffer;
        private readonly BodyStream.Internal<Windows.Kinect.Body> _internalBody;
        
        private volatile bool _pollFramesLoop = true;
        private volatile bool _needUpdateMapDepthToColorSpace;
        private volatile bool _mapDepthToCameraUpdated;
        private volatile bool _isManualUpdate;
        private readonly AutoResetEvent _frameArrivedEvent = new AutoResetEvent(false);
        private readonly AutoResetEvent _sensorActiveChangedEvent = new AutoResetEvent(false);
        private readonly AutoResetEvent _manualUpdateEvent = new AutoResetEvent(false);
        private readonly Thread _pollFrames;

        public Kinect2Device() : base("Kinect2", Init()) {
            var init = (InitInfoKinect2) _initInfo;
            _kinect = init.kinect;
            
            _internalBody = new BodyStream.Internal<Windows.Kinect.Body>(Body);
            _internalBody.SetOnActiveChanged(SensorActiveChanged);
            
            _bodyBuffer = new Windows.Kinect.Body[Body.data.Length];
            if (!_kinect.IsOpen)
                _kinect.Open();
            _pollFrames = new Thread(PollFrames) {
                Name = GetType().Name
            };
            _kinect.CoordinateMapper.CoordinateMappingChanged += OnCoordinateMappingChanged;
            _pollFrames.Start();
        }

        private static InitInfo Init() {
            var init = new InitInfoKinect2();;
            
            try {
                init.kinect = KinectSensor.GetDefault();
                init.Depth = new DepthStream(
                    init.kinect.DepthFrameSource.FrameDescription.Width,
                    init.kinect.DepthFrameSource.FrameDescription.Height);
                init.Index = new IndexStream(
                    init.kinect.BodyIndexFrameSource.FrameDescription.Width,
                    init.kinect.BodyIndexFrameSource.FrameDescription.Height);
                var colorDesc = init.kinect.ColorFrameSource.CreateFrameDescription(_COLOR_FORMAT);
                init.Color = new ColorStream(
                    colorDesc.Width, colorDesc.Height, TextureFormat.RGBA32);
                init.Body = new BodyStream(init.kinect.BodyFrameSource.BodyCount);
                init.MapDepthToColor = new MapDepthToCameraStream(init.Depth.width, init.Depth.height);
            } catch (DllNotFoundException) {
                Debug.LogWarning("Kinect 2 runtime is not is not installed");
                Close(ref init.kinect);
                throw;
            }
            
            return init;
        }

        public override bool IsAvailable() {
            return _kinect.IsAvailable;
        }
        
        public override bool IsManualUpdate {
            get => _isManualUpdate; 
            set => _isManualUpdate = value;
        }
        
        public override void ManualUpdate() {
            if (_isManualUpdate)
                _manualUpdateEvent.Set();
        }

        protected override void SensorActiveChanged(AbstractStream sensor) {
            if (sensor == MapDepthToCamera)
                _needUpdateMapDepthToColorSpace = true;
            _sensorActiveChangedEvent.Set();
        }

        private void CloseSensors() {
            if (_multiReader != null) {
                //_multiReader.Dispose();
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
        }

        private void PollFrames() {
            try {
                while (_pollFramesLoop) {
                    if (_sensorActiveChangedEvent.WaitOne(0))
                        ReActivateSensors();
                    MultiSourceFrame frame;
                    if (_multiReader != null && (frame = _multiReader.AcquireLatestFrame()) != null 
                                             && (!_isManualUpdate || _manualUpdateEvent.WaitOne(0))) {
                        using (var depth = _kinect.DepthFrameSource.IsActive ? frame.DepthFrameReference.AcquireFrame() : null) 
                            if (depth != null || !Depth.Active)
                        using (var index = _kinect.BodyIndexFrameSource.IsActive ? frame.BodyIndexFrameReference.AcquireFrame() : null)
                            if (index != null || !Index.Active)
                        using (var color = _kinect.ColorFrameSource.IsActive ? frame.ColorFrameReference.AcquireFrame() : null)
                            if (color != null || !Color.Active)
                        using (var body = _kinect.BodyFrameSource.IsActive ? frame.BodyFrameReference.AcquireFrame() : null)
                            if (body != null || !Body.Active) 
                        {
                            if (body != null) {
                                UpdateBodies(body);
                                _internalBody.OnNewFrameBackground();
                            }
                            if (color != null) {
                                color.CopyConvertedFrameDataToIntPtr(Color.data.IntPtr(), (uint) Color.data.GetLengthInBytes(), _COLOR_FORMAT);
                                _internalColor.OnNewFrameBackground();
                            }
                            if (index != null) {
                                index.CopyFrameDataToIntPtr(Index.data.IntPtr(), (uint) Index.data.GetLengthInBytes());
                                _internalIndex.OnNewFrameBackground();
                            }
                            if (_needUpdateMapDepthToColorSpace || (MapDepthToCamera.Active && !IsMapDepthToCameraValid())) {
                                UpdateMapDepthToCamera();
                                _internalMapDepthToCamera.OnNewFrameBackground();
                            }
                            if (depth != null) {
                                depth.CopyFrameDataToIntPtr(Depth.data.IntPtr(), (uint) Depth.data.GetLengthInBytes());
                                _internalDepth.OnNewFrameBackground();
                            }
                            
                            _frameArrivedEvent.Set();
                            Thread.Sleep(15);
                        }
                    } else
                        Thread.Sleep(1);
                }
            } catch (ThreadAbortException) {
                Debug.Log(GetType().Name + ": Thread aborted");
            } catch (Exception e) {
                Debug.LogException(e);
            }
            CloseSensors();
        }

        private void UpdateBodies(BodyFrame frame) {
            frame.GetAndRefreshBodyData(_bodyBuffer);
            _internalBody.UpdateBodiesIndexed(_bodyBuffer, GetBodyId, UpdateBody);
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
            if (_pollFrames != null && _pollFrames.IsAlive && !_pollFrames.Join(5000))
                _pollFrames.Abort();
            Close(ref _kinect);
            _frameArrivedEvent.Dispose();
            _sensorActiveChangedEvent.Dispose();
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
            return ToVector2(_kinect.CoordinateMapper.
                MapCameraPointToDepthSpace(ToCameraPoint(pos)));
        }

        public override Vector2 CameraPosToColorMapPos(Vector3 pos) {
            return ToVector2(_kinect.CoordinateMapper.
                MapCameraPointToColorSpace(ToCameraPoint(pos)));
        }
        
        public override Vector2 DepthMapPosToColorMapPos(Vector2 pos, ushort depth) {
            return ToVector2(_kinect.CoordinateMapper.
                MapDepthPointToColorSpace(ToDepthPoint(pos), depth));
        }

        private void UpdateMapDepthToCamera() {
            //TODO: broken table in SDK, workaround with MapDepthFrameToCameraSpace
            //var map = _kinect.CoordinateMapper.GetDepthFrameToCameraSpaceTable();
            var map = new CameraSpacePoint[Depth.data.Length];
            var depth = new ushort[Depth.data.Length];
            Parallel.For(0, depth.Length, i => {
                depth[i] = 1000;
            });
            _kinect.CoordinateMapper.MapDepthFrameToCameraSpace(depth, map);
            Parallel.For(0, map.Length, i => {
                var mapPoint = map[i];
                MapDepthToCamera.data[i] = new half2(new float2(mapPoint.X, mapPoint.Y));
            });
            _needUpdateMapDepthToColorSpace = false;
            _mapDepthToCameraUpdated = true;
        }

        private bool IsMapDepthToCameraValid() {
            var i = MapDepthToCamera.data.Length - 1;
            var x = MapDepthToCamera.data[i].x;
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