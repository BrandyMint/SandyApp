using System;
using System.Collections.Generic;
using System.Threading;
using DepthSensor.Buffer;
using DepthSensor.Device;
using DepthSensor.Sensor;
using OpenCvSharp;
using Utilities;
using Utilities.OpenCVSharpUnity;
using Convert = Utilities.OpenCVSharpUnity.Convert;
using Random = UnityEngine.Random;
using ThreadPriority = System.Threading.ThreadPriority;

namespace DepthSensor.Recorder {
    public class RecorderCalibration: IDisposable {
        public int collectFramesCount = 20;
        public int collectTakeEvery = 2;
        public int collectPointsInFrame = 100;
        
        private bool _isCalibrating;
        private bool _isCollecting;
        private bool _doCalibrateColor;
        private DepthSensorDevice _device;
        
        private int _collectingCounter;
        private List<Point3f[]> _objPointsData = new List<Point3f[]>();
        private List<Point2f[]> _imgPointsDataDepth = new List<Point2f[]>();
        private List<Point2f[]> _imgPointsDataColor = new List<Point2f[]>();
        private Thread _calibrating;

        public bool Working => _isCollecting || _isCalibrating;

        public bool Start(DepthSensorDevice device, string path) {
            Stop(true);
            
            _device = device;
            
            _isCollecting = true;
            _collectingCounter = 0;
            _doCalibrateColor = device.Color.Available;
            device.Depth.OnNewFrameBackground += OnNewFrameBackground;
            device.Depth.Active = true;

            if (!device.Depth.Active) {
                Stop(true);
                return false;
            }
            return true;
        }

        public void Stop(bool force = false) {
            _isCollecting = false;
            _isCalibrating = false;
            if (_device?.Depth != null) {
                _device.Depth.OnNewFrameBackground -= OnNewFrameBackground;
                if (!_device.Depth.AnySubscribedToNewFrames)
                    _device.Depth.Active = false;
                _device = null;
            }

            if (_calibrating != null) { 
                if (_calibrating.IsAlive && !_calibrating.Join(5000))
                    _calibrating.Abort();
                _calibrating = null;
            }

            _objPointsData.Clear();
            _imgPointsDataColor.Clear();
            _imgPointsDataDepth.Clear();
        }

        private void OnNewFrameBackground(ISensor sensor) {
            if (!_isCollecting) return;

            ++_collectingCounter;
            if (_collectingCounter % collectTakeEvery == 0) {
                var depth = _device.Depth.GetNewest();
                var pointsObj = new Point3f[collectPointsInFrame];
                var pointsDepth = new Point2f[collectPointsInFrame];
                Point2f[] pointsColor = null;
                if (_doCalibrateColor)
                    pointsColor = new Point2f[collectPointsInFrame];
                for (int i = 0; i < pointsDepth.Length; ++i) {
                    var idx = Random.Range(0, depth.data.Length);
                    var pDepth = depth.GetXYFrom(idx);
                    var d = depth.data[idx];
                    if (d < 1) d = 1000;
                    var pObj = _device.DepthMapPosToCameraPos(pDepth, d);
                    var pColor = _device.CameraPosToColorMapPos(pObj);
                    pointsObj[i] = Convert.ToPoint3f(pObj);
                    pointsDepth[i] = Convert.ToPoint2f(pDepth);
                    if (_doCalibrateColor)
                        pointsColor[i] = Convert.ToPoint2f(pColor);
                }
                _objPointsData.Add(pointsObj);
                _imgPointsDataDepth.Add(pointsDepth);
                if (_doCalibrateColor)
                    _imgPointsDataColor.Add(pointsColor);

                if (_objPointsData.Count >= collectFramesCount) {
                    _isCalibrating = true;
                    _isCollecting = false;
                    MainThread.Push(StartCalibrate);
                }
            }
        }

        private void StartCalibrate() {
            if (!_isCalibrating)
                return;
            _calibrating = new Thread(Calibrate) {
                Name = GetType().Name,
                Priority = ThreadPriority.Lowest
            };
            _calibrating.Start();
        }

        private void Calibrate() {
            _isCalibrating = true;
            var result = SerializableParams.Default<RecordCalibrationResult>();
            result.IntrinsicDepth = GetInitialIntrinsic(_device.Depth, out var isFovValidDepth);
            bool isFovValidColor = false;
            if (_doCalibrateColor)
                result.IntrinsicColor = GetInitialIntrinsic(_device.Color, out isFovValidColor);
            var flags = CalibrationFlags.None;
            if (isFovValidDepth && (!_doCalibrateColor || isFovValidColor))
                flags |= CalibrationFlags.FixFocalLength;
            using (var R = new Mat()) using (var T = new Mat())
            using (var E = new Mat()) using (var F = new Mat()) {
                result.IntrinsicDepth.reprojError = result.IntrinsicColor.reprojError = Cv2.StereoCalibrate(
                    _objPointsData, _imgPointsDataDepth, _imgPointsDataColor,
                    result.IntrinsicDepth.cameraMatrix, result.IntrinsicDepth.distCoeffs,
                    result.IntrinsicColor.cameraMatrix, result.IntrinsicColor.distCoeffs,
                    result.IntrinsicDepth.imgSizeUsedOnCalculate,
                    R, T, E, F, flags
                );
                result.R = new double[3, 3];
                R.GetArray(0, 0, result.R);
                result.T = T.GetArray(0, 0);
                result.Save();
            }
            _isCalibrating = false;
        }

        private static CameraIntrinsicParams GetInitialIntrinsic<T>(ISensor<T> deviceDepth, out bool isFovValid) where T : AbstractBuffer2D {
            var fov = deviceDepth.FOV;
            var frame = deviceDepth.GetNewest();
            var size = new Size(frame.width, frame.height);
            isFovValid = fov.x > 0f && fov.y > 0f;
            return isFovValid 
                ? new CameraIntrinsicParams(fov, size) 
                : new CameraIntrinsicParams(size);
        }

        public void Dispose() {
            Stop();
        }
    }
}