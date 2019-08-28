using System;
using System.Collections.Generic;
using System.Linq;
using OpenCvSharp;
using OpenCvSharp.Flann;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;
using Utilities;

namespace DepthSensorCalibration {
    public class ImageTracker : MonoBehaviour {
        [SerializeField] private int _featuresCount = 500;
        [SerializeField] private int _minMatches = 10;
        
        public Action OnFramePrepared;

        private NativeArray<byte> _arrayFrame;
        private Mat _matEmpty = new Mat();
        private Mat _matFrame;
        private Mat _matTarget;
        private ORB _detector;
        private DescriptorMatcher _matcher;
        private Mat _descriptorsTarget = new Mat();
        private Mat _descriptorsFrame = new Mat();
        private KeyPoint[] _keyPointsTarget;
        private KeyPoint[] _keyPointsFrame;
        
        private Mat _visualizeMat;
        private readonly Point2f[] _IMG_CORNERS = {new Point2f(0, 0), new Point2f(0, 1), new Point2f(1, 1), new Point2f(1, 0)};
        private Point2f[] _detectedCorners;
        private bool _frameDetected;

        private void OnDestroy() {
            _matEmpty?.DisposeManual();
            _matFrame?.DisposeManual();
            _matTarget?.DisposeManual();
            _detector?.Dispose();
            _descriptorsTarget?.DisposeManual();
            _descriptorsFrame?.DisposeManual();
            _visualizeMat?.DisposeManual();
        }

        private void PrepareDetect() {
            var isDetectorChanged = true;
            if (_detector == null) {
                _detector = ORB.Create(_featuresCount);
            } else if (_detector.MaxFeatures != _featuresCount) {
                _detector.MaxFeatures = _featuresCount;
            } else {
                isDetectorChanged = false;
            }

            if ((isDetectorChanged || _keyPointsTarget == null) && _matTarget != null) {
                _detector.DetectAndCompute(_matTarget, _matEmpty, out _keyPointsTarget, _descriptorsTarget);
                
                _matcher?.Dispose();
                _matcher = new FlannBasedMatcher(new LshIndexParams(12, 20, 2));
                _matcher.Add(new[] {_descriptorsTarget});
                _matcher.Train();
            }
        }

        public void SetTarget(Texture tex) {
            Set(ref _matTarget, tex, OnTargetChanged);
        }

        public void SetFrame(Texture tex) {
            _frameDetected = false;
            Set(ref _matFrame, tex, OnFrameChanged);
        }

        private static void Set(ref Mat m, Texture t, Action onDone) {
            OpenCVSharpHelper.ReCreateIfNeedCompatible(ref m, t);
#if USE_MAT_ASYNC_SET
            m.AsyncSetFrom(t, onDone);
#else
            m.SetFrom(t);
            onDone?.Invoke();
#endif
        }

        private void OnTargetChanged() {
            _keyPointsTarget = null;
            PrepareDetect();
        }

        private void OnFrameChanged() {
            PrepareDetect();
            _detector.DetectAndCompute(_matFrame, _matEmpty, out _keyPointsFrame, _descriptorsFrame);
            _frameDetected = Detect();
            OnFramePrepared?.Invoke();
        }

        private int MinFeaturesCount() {
            return _featuresCount / 2;
        }

        public float FrameFeaturesRank() {
            if (_keyPointsFrame == null)
                return 0f;
            var len = _keyPointsFrame.Length;
            if (len < MinFeaturesCount() )
                return 0f;
            
            var avg = _keyPointsFrame.Average(k => k.Response);
            return avg;
        }

        private bool Detect() {
            if (_keyPointsTarget == null || _keyPointsFrame.Length < MinFeaturesCount())
                return false;
            
            var matches = _matcher.Match(_descriptorsFrame);
            if (matches.Length < _minMatches)
                return false;
            
            var good = matches.OrderBy(m => m.Distance).Take(_minMatches).ToArray();
            try {
                using (var src = InputArray.Create(good.Select(m => _keyPointsTarget[m.TrainIdx].Pt)))
                using (var dst = InputArray.Create(good.Select(m => _keyPointsFrame[m.QueryIdx].Pt)))
                using (var matr = Cv2.FindHomography(src, dst, HomographyMethods.Ransac)) {
                    _detectedCorners = Cv2.PerspectiveTransform(ScaleCorners(_IMG_CORNERS, _matTarget), matr);
                }
            } catch (Exception e) {
                Debug.LogError(e);
                return false;
            }
            return true;
        }

        public void VisualizeDetection(RawImage img, Color detectedColor) {
            Texture2D visTex = null;
            if (img.texture != null)
                visTex = (Texture2D) img.texture; 
            if (TexturesHelper.ReCreateIfNeed(ref visTex, _matFrame.Width, _matFrame.Height, TextureFormat.RGB24)) {
                img.texture = visTex;
            }
            VisualizeDetection(visTex, detectedColor);
        }

        public void VisualizeDetection(Texture2D imgVisualize, Color detectedColor) {
            OpenCVSharpHelper.ReCreateWithLinkedDataIfNeed(ref _visualizeMat, imgVisualize);
            Cv2.DrawKeypoints(_matFrame, _keyPointsFrame, _visualizeMat, null,
                DrawMatchesFlags.DrawRichKeypoints);

            if (_frameDetected) {
                _visualizeMat.Polylines(new[] {_detectedCorners.Select(p => new Point(p.X, p.Y))}, true, 
                    OpenCVSharpHelper.GetScalarFrom(new Color(detectedColor.b, detectedColor.g, detectedColor.r)),
                    3, LineTypes.AntiAlias);
            }
            
            imgVisualize.Apply(false);
        }

        public bool GetDetectTargetCorners(Vector2[] forCorners) {
            if (!_frameDetected)
                return false;
            Convert(_detectedCorners, forCorners);
            return MathHelper.IsConvex(forCorners);
        }

        private static IEnumerable<Point2f> ScaleCorners(IEnumerable<Point2f> src, Mat t) {
            return src.Select(p => new Point2f(
                p.X * (t.Width - 1), 
                p.Y * (t.Height - 1)
            ));
        }

        private static void Convert(Point2f[] src, Vector2[] dst) {
            for (int i = 0; i < src.Length && i < dst.Length; ++i) {
                dst[i].x = src[i].X;
                dst[i].y = src[i].Y;
            }
        }
    }
}