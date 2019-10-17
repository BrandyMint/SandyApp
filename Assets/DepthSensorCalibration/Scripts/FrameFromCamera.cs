using UnityEngine;
using UnityEngine.UI;
using Utilities;

namespace DepthSensorCalibration {
    [RequireComponent(typeof(RawImage))]
    public class FrameFromCamera : MonoBehaviour {
        [SerializeField] private Camera _source;

        private RawImage _img;
        private RenderTexture _renderTexture;
        private RenderTexture _frame;
        
        private void Awake() {
            _img = GetComponent<RawImage>();
            CreateTexturesIfNeed();
        }

        private void CreateTexturesIfNeed() {
            if (TexturesHelper.ReCreateIfNeed(ref _frame, _source.pixelWidth, _source.pixelHeight)) {
                _img.texture = _frame;
            }
            if (TexturesHelper.ReCreateIfNeed(ref _renderTexture, _source.pixelWidth, _source.pixelHeight)) {
                _source.targetTexture = _renderTexture;
            }
        }

        public void TakeFrame() {
            CreateTexturesIfNeed();
            Graphics.Blit(_renderTexture, _frame);
        }

        private void OnDestroy() {
            _img.texture = null;
            if (_frame != null)
                Destroy(_frame);
            if (_renderTexture != null && _renderTexture.IsCreated())
                _renderTexture.Release();
        }
    }
}