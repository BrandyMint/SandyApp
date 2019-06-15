using System.Collections;
using System.Linq;
using DepthSensor.Sensor;
using UnityEngine;
using Microsoft.Kinect;

namespace DepthSensor.Device {
    public class Kinect1Device : DepthSensorDevice {
        private const ColorImageFormat _COLOR_FORMAT = ColorImageFormat.RgbResolution640x480Fps30;
        private const DepthImageFormat _DEPTH_FORMAT = DepthImageFormat.Resolution640x480Fps30;
        
        private KinectSensor _kinect;
        private readonly BodySensor.Internal<Skeleton> _internalBody;
        
        public Kinect1Device() : base("Kinect1", Init()) {
            _internalBody = new BodySensor.Internal<Skeleton>(Body);
            _internalBody.SetOnActiveChanged(SensorActiveChanged);

            _kinect = FindConnectedSensor();
            _kinect.AllFramesReady += FramesReady;
            _kinect.Start();
        }

        private static InitInfo Init() {
            var init = new InitInfo();
            Debug.Log("init");
            var kinect = FindConnectedSensor();
            Debug.Log(kinect);
            Debug.Log(kinect.Status);
            kinect.DepthStream.Enable(_DEPTH_FORMAT);
            init.Depth = new Sensor<ushort>(
                kinect.DepthStream.FrameWidth,
                kinect.DepthStream.FrameHeight);
            init.Index = new Sensor<byte>(
                kinect.DepthStream.FrameWidth,
                kinect.DepthStream.FrameHeight);
            kinect.DepthStream.Disable();
            kinect.ColorStream.Enable(_COLOR_FORMAT);
            init.Color = new ColorByteSensor(
                kinect.ColorStream.FrameWidth,
                kinect.ColorStream.FrameHeight,
                kinect.ColorStream.FrameBytesPerPixel,
                TextureFormat.RGBA32);
            kinect.ColorStream.Disable();
            init.Body = new BodySensor(
                kinect.SkeletonStream.FrameSkeletonArrayLength);

            return init;
        }

        private static KinectSensor FindConnectedSensor() {
            var sen = KinectSensor.KinectSensors;
            return KinectSensor.KinectSensors.FirstOrDefault(sensor => 
                sensor.Status == KinectStatus.Connected);
        }

        protected override void SensorActiveChanged<T>(Sensor<T> sensor) {
            if (ReferenceEquals(sensor, Color)) {
                if (sensor.Active)
                    _kinect.ColorStream.Enable(_COLOR_FORMAT);
                else
                    _kinect.ColorStream.Disable();
            }
            if (ReferenceEquals(sensor, Body)) {
                if (sensor.Active)
                    _kinect.SkeletonStream.Enable();
                else
                    _kinect.SkeletonStream.Disable();
            }
            if (ReferenceEquals(sensor, Depth) || ReferenceEquals(sensor, Index)) {
                if (IsDepthIndexActive())
                    _kinect.DepthStream.Enable(_DEPTH_FORMAT);
                else
                    _kinect.DepthStream.Disable();
            }
        }

        private bool IsDepthIndexActive() {
            return Depth.Active || Index.Active;
        }
        
        private void FramesReady(object sender, AllFramesReadyEventArgs e) {
            if (e != null) {
                using (var depth = IsDepthIndexActive() ? e.OpenDepthImageFrame() : null) 
                    if (depth != null || IsDepthIndexActive())
                using (var color = Color.Active ? e.OpenColorImageFrame() : null) 
                    if (color != null || !Color.Active)
                using (var body = Body.Active ? e.OpenSkeletonFrame() : null)
                    if (body != null || !Body.Active) 
                {
                    //color?.Format
                }
            }
        }

        protected override IEnumerator Update() {
            while (true) {
                Debug.Log(_kinect.Status);
                yield return new WaitForFixedUpdate();
            }
        }

        protected override void Close() {
            if (_kinect != null) {
                if (_kinect.IsRunning)
                    _kinect.Stop();
                _kinect.Dispose();
                _kinect = null;
            }
        }

        public override bool IsAvaliable() {
            return _kinect.IsRunning;
        }

        public override Vector2 CameraPosToDepthMapPos(Vector3 pos) {
            return pos;
        }

        public override Vector2 CameraPosToColorMapPos(Vector3 pos) {
            return pos;
        }

        public override Vector2 DepthMapPosToColorMapPos(Vector2 pos, ushort depth) {
            return pos;
        }
    }
}