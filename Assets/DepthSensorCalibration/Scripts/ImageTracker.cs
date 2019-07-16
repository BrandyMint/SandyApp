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

        private void Awake() {
            /*_debugMatchImgPath = Path.Combine(Application.persistentDataPath, "debugMatch");
            if (!Directory.Exists(_debugMatchImgPath))
                Directory.CreateDirectory(_debugMatchImgPath);*/
        }

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
            var matches = _matcher.Match(_descriptorsFrame);
            var good = matches.OrderBy(m => m.Distance).Take(10);
            SaveDebugImgMatch(_matFrame, keyPointsFrame, _matTarget, _keyPointsTarget, good);
        }

        /*private string _debugMatchImgPath;
        private int _debugMatchImgCount;*/
        private Texture2D _debugTex;
        private Mat _debugMat;
        [SerializeField] private RawImage _debugImg;

        private void SaveDebugImgMatch(Mat img1, KeyPoint[] keyPoints1, Mat img2, KeyPoint[] keyPoints2, IEnumerable<DMatch> matches1To2) {
            //var path = Path.Combine(_debugMatchImgPath, $"{_debugMatchImgCount++:000}.png");
            var height = Mathf.Max(img1.Height, img2.Height);
            var width = img1.Width + img2.Width;
            if (TexturesHelper.ReCreateIfNeed(ref _debugTex, width, height, TextureFormat.RGB24)) {
                _debugImg.texture = _debugTex;
                _debugImg.GetComponent<AspectRatioFitter>().aspectRatio = (float) width / height;
                _debugMat?.Dispose();
                var a = _debugTex.GetRawTextureData<byte>();
                _debugMat = new Mat(height, width, MatType.CV_8UC3, a.IntPtr());
            }

            var matches = matches1To2.ToArray();
            Mat mat; Mat mask = new Mat();
            using (var src = InputArray.Create(matches.Select(m => keyPoints2[m.TrainIdx].Pt)))
            using (var dst = InputArray.Create(matches.Select(m => keyPoints1[m.QueryIdx].Pt))) {
                mat = Cv2.FindHomography(src, dst, HomographyMethods.Ransac, 5D, mask);
            }
            var pts = Cv2.PerspectiveTransform(new[] {
                new Point2f(0, 0), new Point2f(0, img2.Height - 1),
                new Point2f(img2.Width - 1, img2.Height - 1), new Point2f(img2.Width - 1, 0),
            }, mat);
            img1.Polylines(new[] {pts.Select(p => new Point(p.X, p.Y))}, true, 255, 3, LineTypes.AntiAlias);
            
            Cv2.DrawMatches(img1, keyPoints1, img2, keyPoints2, matches, _debugMat, 
                null, null, null, DrawMatchesFlags.NotDrawSinglePoints);
            _debugTex.Apply(false);
            //Cv2.ImWrite(path, matMatches);
        }
    }
}