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
        private bool _autoTakeFrame;

        private bool CreateTexturesIfNeed() {
            if (_source == null) return false;
            TexturesHelper.ReCreateIfNeed(ref _renderTexture, _source.pixelWidth, _source.pixelHeight);
            
            if (_img == null)
                _img = GetComponent<RawImage>();
            if (TexturesHelper.ReCreateIfNeed(ref _frame, _source.pixelWidth, _source.pixelHeight)) {
                UpdateImgTexture();
            }
            return TakeCamera();
        }

        private void UpdateImgTexture() {
            _img.texture = _autoTakeFrame ? _renderTexture : _frame;
        }

        public bool TakeFrame() {
            AutoTakeFrame = false;
            if (!CreateTexturesIfNeed()) {
                Graphics.Blit(_renderTexture, _frame);
                return true;
            }
            return false;
        }

        private void Update() {
            if (_autoTakeFrame) {
                CreateTexturesIfNeed();
            }
        }

        public bool AutoTakeFrame {
            get => _autoTakeFrame;
            set {
                if (value != _autoTakeFrame) {
                    _autoTakeFrame = value;
                    CreateTexturesIfNeed();
                    UpdateImgTexture();
                }
            }
        }

        public void FreeCamera() {
            if (_source != null)
                _source.targetTexture = null;
        }

        public bool TakeCamera() {
            if (_source == null) return false;
            if (_source.targetTexture != _renderTexture) {
                _source.targetTexture = _renderTexture;
                return true;
            }
            return false; 
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