using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;
using Utilities;

namespace DepthSensorSandbox {
    [RequireComponent(typeof(MeshRenderer))]
    public class SandboxColor : MonoBehaviour {
        private Material _mat;
        private Texture2D _texColor;
        private Texture2D _texDepthToColor;
        private NativeArray<byte> _depthToColorNative;

        private void Awake() {
            _mat = GetComponent<MeshRenderer>().material;
        }

        private void Start() {
            DepthSensorSandboxProcessor.OnDepthToColorBackground += OnDepthToColorBackground;
            DepthSensorSandboxProcessor.OnNewFrame += OnNewFrame;
            DepthSensorSandboxProcessor.OnColor += OnColor;
        }

        private void OnDestroy() {
            DepthSensorSandboxProcessor.OnColor -= OnColor;
            DepthSensorSandboxProcessor.OnNewFrame -= OnNewFrame;
            DepthSensorSandboxProcessor.OnDepthToColorBackground -= OnDepthToColorBackground;
        }

        private void OnDepthToColorBackground(int width, int height, Vector2[] depthToColor) {
            var size = sizeof(ushort);
            if (_depthToColorNative.Length == depthToColor.Length * 2 * size) {
                Parallel.For(0, depthToColor.Length, i => {
                    var p = depthToColor[i];
                    var ux = Mathf.FloatToHalf(p.x);
                    var uy = Mathf.FloatToHalf(p.y);
                    var startI = i * 2 * size;
                    _depthToColorNative.ReinterpretStore(startI, ux);
                    _depthToColorNative.ReinterpretStore(startI + size, uy);
                });
            }
        }

        private void OnNewFrame(int width, int height, ushort[] depth, Vector2[] mapToCamera) {
            if (TexturesHelper.ReCreateIfNeed(ref _texDepthToColor, width, height, TextureFormat.RGHalf)) {
                _mat.SetTexture("_DepthToColorTex", _texDepthToColor);
                _depthToColorNative = _texDepthToColor.GetRawTextureData<byte>();
            }
            _texDepthToColor.Apply();
        }

        private void OnColor(int width, int height, byte[] data, TextureFormat format) {
            if (TexturesHelper.ReCreateIfNeed(ref _texColor, width, height, format)) {
                _mat.SetTexture("_ColorTex", _texColor);
            }

            _texColor.LoadRawTextureData(data);
            _texColor.Apply();
        }
    }
}