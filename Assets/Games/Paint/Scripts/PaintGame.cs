using System.Collections;
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
        //[SerializeField] private int _depthHeight = 128;

        private CameraRenderToTexture _renderPaint;
        private RenderTexture _renderTexture;
        private static readonly int _HANDS_TEX = Shader.PropertyToID("_HandsTex");

        private void Start() {
            _renderPaint = _cam.gameObject.AddComponent<CameraRenderToTexture>();
            //_renderPaint.MaxResolution = _depthHeight;
            _renderPaint.Enable(_matDepth, RenderTextureFormat.R8, CameraEvent.BeforeImageEffectsOpaque, OnNewDepthFrame, CreateCommandBufferDepth);
            OneMomentBillboard.OnReady += OnReady;
        }

        private void OnDestroy() {
            OneMomentBillboard.OnReady -= OnReady;
            if (_renderPaint != null) {
                _renderPaint.Disable();
            }
        }

        private void OnReady() {
            StartCoroutine(NothingClearStartNextFrame());
        }

        private IEnumerator NothingClearStartNextFrame() {
            yield return null;
            _cam.clearFlags = CameraClearFlags.Nothing;
        }

        private void CreateCommandBufferDepth(CommandBuffer cmb, Material mat, RenderTexture rt, RenderTargetIdentifier src) {
            cmb.SetRenderTarget(rt);
            cmb.ClearRenderTarget(true, true, Color.black);
            _sandbox.AddDrawToCommandBuffer(cmb, mat);
            cmb.SetRenderTarget(src);
            cmb.Blit(src, src, _matPaint);
        }

        private void OnNewDepthFrame(RenderTexture t) {
            _matPaint.SetTexture(_HANDS_TEX, t);
        }
    }
}