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
        private Mat _matTargetColor;
        private ORB _detector;
        private DescriptorMatcher _matcher;
        private Mat _descriptorsTarget = new Mat();
        private Mat _descriptorsFrame = new Mat();
        private KeyPoint[] _keyPointsTarget;
        private KeyPoint[] _keyPointsFrame;

        private void OnDestroy() {
            _matEmpty?.Dispose();
            _matFrame?.Dispose();
            _matTarget?.Dispose();
            _matTargetColor?.Dispose();
            _detector?.Dispose();
            _descriptorsTarget?.Dispose();
            _descriptorsFrame?.Dispose();
            _debugMat?.Dispose();
        }

        public void PrepareDetect() {
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
            Set(ref _matFrame, tex, OnFrameChanged);
        }

        private static void Set(ref Mat m, Texture t, Action onDone) {
            OpenCVSharpHelper.ReCreateIfNeedCompatible(ref m, t);
            m.AsyncSetFrom(t, onDone);
        }

        private void OnTargetChanged() {
            _keyPointsTarget = null;
            PrepareDetect();
        }

        private void OnFrameChanged() {
            PrepareDetect();
            _detector.DetectAndCompute(_matFrame, _matEmpty, out _keyPointsFrame, _descriptorsFrame);
            OnFramePrepared?.Invoke();
        }

        private int MinFeaturesCount() {
            return _featuresCount / 2;
        }

        public float DetectRank() {
            if (_keyPointsFrame == null)
                return 0f;
            var len = _keyPointsFrame.Length;
            if (len < MinFeaturesCount() )
                return 0f;
            
            var avg = _keyPointsFrame.Average(k => k.Response);
            return avg;
        }

        public void Detect() {
            if (_keyPointsTarget == null || _keyPointsFrame.Length < MinFeaturesCount())
                return;
            
            var matches = _matcher.Match(_descriptorsFrame);
            if (matches.Length < _minMatches)
                return;
            
            var good = matches.OrderBy(m => m.Distance).Take(_minMatches).ToArray();
            try {
                using (var src = InputArray.Create(good.Select(m => _keyPointsTarget[m.TrainIdx].Pt)))
                using (var dst = InputArray.Create(good.Select(m => _keyPointsFrame[m.QueryIdx].Pt)))
                using (var matr = Cv2.FindHomography(src, dst, HomographyMethods.Ransac)) {
                    SaveDebugImgMatch(_matFrame, _keyPointsFrame, _matTarget, _keyPointsTarget,
                        good, matr);
                }
            } catch (Exception e) {
                Debug.LogError(e);
                return;
            }
        }

        /*private string _debugMatchImgPath;
        private int _debugMatchImgCount;*/
        private Texture2D _debugTex;
        private Mat _debugMat;
        [SerializeField] private RawImage _debugImg;

        private void SaveDebugImgMatch(
            Mat img1, IEnumerable<KeyPoint> keyPoints1, 
            Mat img2, IEnumerable<KeyPoint> keyPoints2, 
            IEnumerable<DMatch> matches1To2, Mat matr
        ) {
            var height = Mathf.Max(img1.Height, img2.Height);
            var width = img1.Width + img2.Width;
            if (TexturesHelper.ReCreateIfNeed(ref _debugTex, width, height, TextureFormat.RGB24)) {
                _debugImg.texture = _debugTex;
                _debugImg.GetComponent<AspectRatioFitter>().aspectRatio = (float) width / height;
                _debugMat?.Dispose();
                var a = _debugTex.GetRawTextureData<byte>();
                _debugMat = new Mat(height, width, MatType.CV_8UC3, a.IntPtr());
            }
            
            var pts = Cv2.PerspectiveTransform(new[] {
                new Point2f(0, 0), new Point2f(0, img2.Height - 1),
                new Point2f(img2.Width - 1, img2.Height - 1), new Point2f(img2.Width - 1, 0),
            }, matr);
            img1.Polylines(new[] {pts.Select(p => new Point(p.X, p.Y))}, 
                true, 255, 3, LineTypes.AntiAlias);
            
            Cv2.DrawMatches(img1, keyPoints1, img2, keyPoints2, matches1To2, _debugMat, 
                null, null, null, DrawMatchesFlags.NotDrawSinglePoints);
            _debugTex.Apply(false);
        }
    }
}