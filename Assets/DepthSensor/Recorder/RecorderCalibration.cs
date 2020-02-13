using System;
using System.Collections.Generic;
using System.Threading;
using DepthSensor.Buffer;
using DepthSensor.Device;
using DepthSensor.Sensor;
using OpenCvSharp;
using UnityEngine;
using Utilities;
using Utilities.OpenCVSharpUnity;
using Action = OpenCvSharp.Util.Action;
using Convert = Utilities.OpenCVSharpUnity.Convert;
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
        private Action _onDone;
        private RecordCalibrationResult _result;
        
        public event Action OnFail;

        public bool Working => _isCollecting || _isCalibrating;

        public static bool IsNeedCalibrate(DepthSensorDevice device) {
            var result = new RecordCalibrationResult();
            var depth = device.Depth.GetNewest();
            if (!result.Load() 
                || result.DeviceName != device.Name 
                || result.IntrinsicDepth == null
                || result.IntrinsicDepth.imgSizeUsedOnCalculate.Width != depth.width
                || result.IntrinsicDepth.imgSizeUsedOnCalculate.Height != depth.height)
                return true;
            if (device.Color.Available) {
                var color = device.Color.GetNewest();
                if (result.IntrinsicColor == null
                    || result.IntrinsicColor.imgSizeUsedOnCalculate.Width != color.width
                    || result.IntrinsicColor.imgSizeUsedOnCalculate.Height != color.height)
                    return true;
            }
            return false;
        }

        public bool Start(DepthSensorDevice device, Action OnDone) {
            Stop();
            
            _device = device;
            _onDone = OnDone;
            
            _isCollecting = true;
            _collectingCounter = 0;
            _doCalibrateColor = device.Color.Available;
            device.Depth.OnNewFrameBackground += OnNewFrameBackground;
            device.Depth.Active = true;

            if (!device.Depth.Active) {
                Stop();
                return false;
            }
            return true;
        }

        public void Stop() {
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

            _onDone = null;
            _result = null;

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
                    var idx = (i + 1) * depth.data.Length / (collectPointsInFrame + 1);
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
            _result = SerializableParams.Default<RecordCalibrationResult>();
            _calibrating = new Thread(Calibrate) {
                Name = GetType().Name,
                Priority = ThreadPriority.Lowest
            };
            _calibrating.Start();
        }

        private void Calibrate() {
            try { 
                var flags = CalibrationFlags.FixFocalLength | CalibrationFlags.UseIntrinsicGuess;
                _result.DeviceName = _device.Name;
                _result.IntrinsicDepth = GetInitialIntrinsic(_device.Depth);
                if (_doCalibrateColor)
                    _result.IntrinsicColor = GetInitialIntrinsic(_device.Color);
                
                using (var R = new Mat()) using (var T = new Mat())
                using (var E = new Mat()) using (var F = new Mat()) {
                    _result.IntrinsicDepth.reprojError = _result.IntrinsicColor.reprojError = Cv2.StereoCalibrate(
                        _objPointsData, _imgPointsDataDepth, _imgPointsDataColor,
                        _result.IntrinsicDepth.cameraMatrix, _result.IntrinsicDepth.distCoeffs,
                        _result.IntrinsicColor.cameraMatrix, _result.IntrinsicColor.distCoeffs,
                        _result.IntrinsicDepth.imgSizeUsedOnCalculate,
                        R, T, E, F, flags
                    );
                    var r = new double[R.Height, R.Width];
                    R.GetArray(0, 0, r);
                    var t = new double[T.Height, T.Width];
                    T.GetArray(0, 0, t);
                    _result.R = r;
                    _result.T = t;
                }
                MainThread.Push(OnDone);
            } catch (Exception e) {
                Debug.LogException(e);
                OnFail?.Invoke();
            }
        }

        private void OnDone() {
            _result.Save();
            _isCalibrating = false;
            _onDone?.Invoke();
            Stop();
        }

        private static CameraIntrinsicParams GetInitialIntrinsic<T>(ISensor<T> deviceDepth) where T : AbstractBuffer2D {
            var fov = deviceDepth.FOV;
            var frame = deviceDepth.GetNewest();
            var size = new Size(frame.width, frame.height);
            var isFovValid = fov.x > 0f && fov.y > 0f;
            if (!isFovValid)
                fov = new Vector2(60f, 60f / size.Width * size.Height);
            return new CameraIntrinsicParams(fov, size);
        }

        public void Dispose() {
            Stop();
        }
    }
}