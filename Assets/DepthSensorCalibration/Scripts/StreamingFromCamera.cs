using UnityEngine;
using UnityEngine.UI;
using Utilities;

namespace DepthSensorCalibration {
    [RequireComponent(typeof(RawImage))]
    public class StreamingFromCamera : MonoBehaviour {
        [SerializeField] private Camera _source;

        private RawImage _img;
        private RenderTexture _renderTexture;
        
        private void Awake() {
            _img = GetComponent<RawImage>();
        }

        private void UpdateTextureSizeIfNeed() {
            if (TexturesHelper.ReCreateIfNeed(ref _renderTexture, _source.pixelWidth, _source.pixelHeight)) {
                _img.texture = _renderTexture;
                _source.targetTexture = _renderTexture;
            }
        }

        private void Update() {
            UpdateTextureSizeIfNeed();
        }

        private void OnEnable() {
            UpdateTextureSizeIfNeed();
            _source.targetTexture = _renderTexture;
        }

        private void OnDisable() {
            if (_source != null)
                _source.targetTexture = null;
        }

        private void OnDestroy() {
            if (_renderTexture != null && _renderTexture.IsCreated())
                _renderTexture.Release();
        }
    }
}