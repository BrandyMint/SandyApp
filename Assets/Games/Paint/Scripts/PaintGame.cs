using DepthSensorCalibration;
using DepthSensorSandbox.Visualisation;
using UnityEngine;
using UnityEngine.Rendering;

namespace Games.Paint {
    public class PaintGame : MonoBehaviour {
        [SerializeField] private Camera _cam;
        [SerializeField] private SandboxMesh _sandbox;
        [SerializeField] private Material _matDepth;
        [SerializeField] private Material _matPaint;
        [SerializeField] private int _depthHeight = 128;

        private CameraRenderToTexture _renderPaint;
        private RenderTexture _renderTexture;
        private static readonly int _HANDS_TEX = Shader.PropertyToID("_HandsTex");

        private void Start() {
            _renderPaint = _cam.gameObject.AddComponent<CameraRenderToTexture>();
            _renderPaint.MaxResolution = _depthHeight;
            _renderPaint.Enable(_matDepth, RenderTextureFormat.R8, CameraEvent.AfterEverything, OnNewDepthFrame, CreateCommandBufferDepth);
        }

        private void OnDestroy() {
            if (_renderPaint != null) {
                _renderPaint.Disable();
            }
        }

        private void CreateCommandBufferDepth(CommandBuffer cmb, Material mat, RenderTexture rt, RenderTargetIdentifier src) {
            cmb.SetRenderTarget(rt);
            _sandbox.AddDrawToCommandBuffer(cmb, mat);
            cmb.SetRenderTarget(src);
            cmb.Blit(src, src, _matPaint);
        }

        private void OnNewDepthFrame(RenderTexture t) {
            _matPaint.SetTexture(_HANDS_TEX, t);
        }
    }
}