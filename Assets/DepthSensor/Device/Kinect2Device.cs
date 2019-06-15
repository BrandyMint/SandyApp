using System;
using System.Collections;
using System.Threading;
using Windows.Kinect;
using DepthSensor.Sensor;
using UnityEngine;
using Body = DepthSensor.Sensor.Body;
using Joint = DepthSensor.Sensor.Joint;

namespace DepthSensor.Device {
    public class Kinect2Device : DepthSensorDevice {
        private const ColorImageFormat _COLOR_FORMAT = ColorImageFormat.Rgba;
        
        private KinectSensor _kinect;
        private MultiSourceFrameReader _multiReader;
        private readonly Windows.Kinect.Body[] _bodyBuffer;
        private readonly BodySensor.Internal<Windows.Kinect.Body> _internalBody;
        
        private volatile bool _pollFramesLoop = true;
        private readonly AutoResetEvent _frameArrivedEvent = new AutoResetEvent(false);
        private readonly AutoResetEvent _sensorActiveChangedEvent = new AutoResetEvent(false);
        private readonly Thread _pollFrames;
        
        public Kinect2Device() : base("Kinect2", Init()) {
            _internalBody = new BodySensor.Internal<Windows.Kinect.Body>(Body);
            _internalBody.SetOnActiveChanged(SensorActiveChanged);
            
            _kinect = KinectSensor.GetDefault();
            _bodyBuffer = new Windows.Kinect.Body[Body.data.Length];
            if (!_kinect.IsOpen)
                _kinect.Open();
            _pollFrames = new Thread(PollFrames) {
                Name = GetType().Name
            };
            _pollFrames.Start();
        }

        private static InitInfo Init() {
            var init = new InitInfo();
            
            try {
                var kinect = KinectSensor.GetDefault();
                init.Depth = new Sensor<ushort>(
                    kinect.DepthFrameSource.FrameDescription.Width,
                    kinect.DepthFrameSource.FrameDescription.Height);
                init.Index = new Sensor<byte>(
                    kinect.BodyIndexFrameSource.FrameDescription.Width,
                    kinect.BodyIndexFrameSource.FrameDescription.Height);
                var colorDesc = kinect.ColorFrameSource.CreateFrameDescription(_COLOR_FORMAT);
                init.Color = new ColorByteSensor(
                    colorDesc.Width, colorDesc.Height, (int) colorDesc.BytesPerPixel, TextureFormat.RGBA32);
                init.Body = new BodySensor(kinect.BodyFrameSource.BodyCount);
            } catch (DllNotFoundException) {
                Debug.LogWarning("Kinect 2 runtime is not is not installed");
                throw;
            }
            
            return init;
        }
        
        public override bool IsAvaliable() {
            return _kinect.IsAvailable;
        }

        protected override void SensorActiveChanged<T>(Sensor<T> sensor) {
            _sensorActiveChangedEvent.Set();
        }

        private void CloseSensors() {
            if (_multiReader != null) {
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
        }
        
        private void PollFrames() {
            try {
                while (_pollFramesLoop) {
                    if (_sensorActiveChangedEvent.WaitOne(0))
                        ReActivateSensors();
                    MultiSourceFrame frame;
                    if (_multiReader != null && (frame = _multiReader.AcquireLatestFrame()) != null) {
                        using (var depth = Depth.Active ? frame.DepthFrameReference.AcquireFrame() : null) 
                            if (depth != null || !Depth.Active)
                        using (var index = Index.Active ? frame.BodyIndexFrameReference.AcquireFrame() : null)
                            if (index != null || !Index.Active)
                        using (var color = Color.Active ? frame.ColorFrameReference.AcquireFrame() : null)
                            if (color != null || !Color.Active)
                        using (var body = Body.Active ? frame.BodyFrameReference.AcquireFrame() : null)
                            if (body != null || !Body.Active) 
                        {
                            depth?.CopyFrameDataToArray(Depth.data);
                            index?.CopyFrameDataToArray(Index.data);
                            color?.CopyConvertedFrameDataToArray(Color.data, _COLOR_FORMAT);
                            if (body != null) UpdateBodies(body);
                            _frameArrivedEvent.Set();
                            Thread.Sleep(30);
                        }
                    } else
                        Thread.Sleep(1);
                }
            } catch (ThreadAbortException) {
                Debug.Log(GetType().Name + ": Thread aborted");
            } catch (Exception e) {
                Debug.LogException(e);
                throw;
            }
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
                yield return new WaitForEndOfFrame();
            }
        }

        protected override void Close() {
            _pollFramesLoop = false;
            if (_pollFrames != null && _pollFrames.IsAlive)
                _pollFrames.Abort();
            CloseSensors();
            if (_kinect != null) {
                if (_kinect.IsOpen)
                    _kinect.Close();
                _kinect = null;
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