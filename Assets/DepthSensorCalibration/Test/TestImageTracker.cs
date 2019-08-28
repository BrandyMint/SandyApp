using UnityEngine;
using UnityEngine.UI;

namespace DepthSensorCalibration.Test {
    [RequireComponent(typeof(ImageTracker))]
    public class TestImageTracker : MonoBehaviour {
        private const string _AUTO_CONTRAST = "AUTO_CONTRAST";
        private static readonly int _CONTRAST_WIDTH = Shader.PropertyToID("_ContrastWidth");
        private static readonly int _CONTRAST_CENTER = Shader.PropertyToID("_ContrastCenter");
        
        [SerializeField] private Texture2D _texTarget;
        [SerializeField] private Texture2D _texFrame;
        [SerializeField] private Material _matGrayScale;
        [SerializeField] private RawImage _imgTest;

        private ImageTracker _tracker;
        private RenderTexture _texTargetGray;
        private RenderTexture _texFrameGray;

        private void Awake() {
            _tracker = GetComponent<ImageTracker>();
            _tracker.OnFramePrepared += OnFramePrepared;
        }

        private void Start() {
            _texTargetGray = GrayScale(_texTarget);
            _texFrameGray = GrayScale(_texFrame);
            _tracker.SetTarget(_texTargetGray);
            _tracker.SetFrame(_texFrameGray);
        }

        private void OnDestroy() {
            if (_texTargetGray != null)
                _texTargetGray.Release();
            if (_texFrameGray != null)
                _texFrameGray.Release();
        }

        private void OnFramePrepared() {
            _tracker.VisualizeDetection(_imgTest, Color.red);
        }

        private RenderTexture GrayScale(Texture src) {
            var dst = new RenderTexture(src.width, src.height, 0, RenderTextureFormat.R8);
            _matGrayScale.DisableKeyword(_AUTO_CONTRAST);
            _matGrayScale.SetFloat(_CONTRAST_CENTER, 0.5f);
            _matGrayScale.SetFloat(_CONTRAST_WIDTH, 1f);
            Graphics.Blit(src, dst, _matGrayScale);
            return dst;
        }
    }
}