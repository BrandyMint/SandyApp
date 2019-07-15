using System;
using System.Collections.Generic;
using System.IO;
using OpenCvSharp;
using OpenCvSharp.XFeatures2D;
using Unity.Collections;
using UnityEngine;
using Utilities;

namespace DepthSensorCalibration {
    public class ImageTracker : MonoBehaviour {
        [SerializeField] private double _hessianThreshold = 400;
        [SerializeField] private int _countMatches = 10;

        private NativeArray<byte> _arrayFrame;
        private Mat _matEmpty = new Mat();
        private Mat _matFrame;
        private Mat _matTarget;
        private Mat _matTargetColor;
        private SURF _detector;
        private DescriptorMatcher _matcher;
        private Mat _descriptorsTarget = new Mat();
        private Mat _descriptorsFrame = new Mat();
        private KeyPoint[] _keyPointsTarget;

        private void Awake() {
            _debugMatchImgPath = Path.Combine(Application.persistentDataPath, "debugMatch");
            if (!Directory.Exists(_debugMatchImgPath))
                Directory.CreateDirectory(_debugMatchImgPath);
        }

        private void OnDestroy() {
            _matEmpty?.Dispose();
            _matFrame?.Dispose();
            _matTarget?.Dispose();
            _matTargetColor?.Dispose();
            _detector?.Dispose();
            _descriptorsTarget?.Dispose();
            _descriptorsFrame?.Dispose();
        }

        public void PrepareDetect() {
            var isDetectorChanged = true;
            if (_detector == null) {
                _detector = SURF.Create(_hessianThreshold);
            } else if (Math.Abs(_detector.HessianThreshold - _hessianThreshold) > 0.0001) {
                _detector.HessianThreshold = _hessianThreshold;
            } else {
                isDetectorChanged = false;
            }

            if ((isDetectorChanged || _keyPointsTarget == null) && _matTarget != null) {
                _detector.DetectAndCompute(_matTarget, _matEmpty, out _keyPointsTarget, _descriptorsTarget);
                
                _matcher?.Dispose();
                _matcher = new FlannBasedMatcher();
                _matcher.Add(new[] {_descriptorsTarget});
                _matcher.Train();
            }
        }

        public void SetTarget(Texture tex) {
            Set(ref _matTarget, tex, OnTargetChanged);
        }

        public void SetFrame(Texture tex) {
            Set(ref _matFrame, tex, Detect);
        }

        private static void Set(ref Mat m, Texture t, Action onDone) {
            OpenCVSharpHelper.ReCreateIfNeedCompatible(ref m, t);
            m.AsyncSetFrom(t, onDone);
        }

        private void OnTargetChanged() {
            _keyPointsTarget = null;
            PrepareDetect();
        }

        public void Detect() {
            PrepareDetect();
            _detector.DetectAndCompute(_matFrame, _matEmpty, out var keyPointsFrame, _descriptorsFrame);
            var matches = _matcher.KnnMatch(_descriptorsFrame, _countMatches);
            SaveDebugImgMatch(_matTarget, _keyPointsTarget, _matFrame, keyPointsFrame, matches);
        }

        private string _debugMatchImgPath;
        private int _debugMatchImgCount;

        private void SaveDebugImgMatch(Mat img1, IEnumerable<KeyPoint> keyPoints1, Mat img2, IEnumerable<KeyPoint> keyPoints2, IEnumerable<DMatch[]> matches1To2) {
            var path = Path.Combine(_debugMatchImgPath, $"{_debugMatchImgCount++:000}.png");
            using (var matMatches = new Mat()) {
                Cv2.DrawMatches(img1, keyPoints1, img2, keyPoints2, matches1To2, matMatches);
                Cv2.ImWrite(path, matMatches);
            }
        }
    }
}