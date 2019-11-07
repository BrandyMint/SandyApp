using DepthSensorCalibration;
using DepthSensorSandbox.Visualisation;
using UnityEngine;
using UnityEngine.Rendering;

namespace Games.Paint {
    public class PaintGame : MonoBehaviour {
        private static readonly int _HANDS_TEX = Shader.PropertyToID("_HandsTex");
        
        [SerializeField] private Camera _cam;
        [SerializeField] private SandboxMesh _sandbox;
        [SerializeField] private Material _matDepth;
        [SerializeField] private Material _matPaint;
        //[SerializeField] private int _depthHeight = 128;

        private CameraRenderToTexture _renderHands;
        private CameraRenderToTexture _renderPaint;

        private void Start() {
            _renderHands = _cam.gameObject.AddComponent<CameraRenderToTexture>();
            _renderPaint = _cam.gameObject.AddComponent<CameraRenderToTexture>();
            //_renderPaint.MaxResolution = _depthHeight;
            _renderHands.Enable(_matDepth, RenderTextureFormat.R8, CameraEvent.BeforeForwardOpaque, OnNewDepthFrame, CreateCommandBufferDepth);
            _renderPaint.Enable(_matDepth, RenderTextureFormat.ARGB32, CameraEvent.BeforeImageEffects, null, CreateCommandBufferPaint);
        }

        private void OnDestroy() {
            if (_renderHands != null) {
                _renderHands.Disable();
            }
            if (_renderPaint != null) {
                _renderPaint.Disable();
            }
        }

        private void CreateCommandBufferDepth(CommandBuffer cmb, Material mat, RenderTexture rt, RenderTargetIdentifier src) {
            cmb.SetRenderTarget(rt);
            cmb.ClearRenderTarget(true, true, Color.black);
            _sandbox.AddDrawToCommandBuffer(cmb, mat);
        }

        private void CreateCommandBufferPaint(CommandBuffer cmb, Material mat, RenderTexture rt, RenderTargetIdentifier src) {
            cmb.Blit(rt, src, _matPaint);
            cmb.Blit(src, rt);
        }

        private void OnNewDepthFrame(RenderTexture t) {
            _matPaint.SetTexture(_HANDS_TEX, t);
        }
    }
}