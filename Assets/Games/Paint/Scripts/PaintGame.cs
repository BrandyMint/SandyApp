using System.Collections;
using DepthSensorCalibration;
using DepthSensorSandbox.Visualisation;
using UnityEngine;
using UnityEngine.Rendering;

namespace Games.Paint {
    public class PaintGame : MonoBehaviour {
        private static readonly int _HANDS_TEX = Shader.PropertyToID("_HandsTex");
        private static readonly CameraEvent _PAINT_EVENT = CameraEvent.BeforeImageEffects;
        
        [SerializeField] private Camera _cam;
        [SerializeField] private SandboxMesh _sandbox;
        [SerializeField] private Material _matDepth;
        [SerializeField] private Material _matPaint;
        //[SerializeField] private int _depthHeight = 128;

        private CameraRenderToTexture _renderHands;
        private CommandBuffer _paintCmdBuffer;

        private void Start() {
            _renderHands = _cam.gameObject.AddComponent<CameraRenderToTexture>();
            //_renderPaint.MaxResolution = _depthHeight;
            _renderHands.Enable(_matDepth, RenderTextureFormat.R8, CameraEvent.BeforeForwardOpaque, OnNewDepthFrame, CreateCommandBufferDepth);

            _paintCmdBuffer = CreatePaintBuffer();
            _cam.AddCommandBuffer(_PAINT_EVENT, _paintCmdBuffer);
            OneMomentBillboard.OnReady += OnReady;
        }

        private void OnDestroy() {
            if (_renderHands != null) {
                _renderHands.Disable();
            }
            if (_paintCmdBuffer != null) {
                if (_cam != null)
                    _cam.RemoveCommandBuffer(_PAINT_EVENT, _paintCmdBuffer);
                _paintCmdBuffer.Dispose();
                _paintCmdBuffer = null;
            }
        }

        private void OnReady() {
            StartCoroutine(NothingClearStartNextFrame());
        }

        private IEnumerator NothingClearStartNextFrame() {
            yield return null;
            _cam.clearFlags = CameraClearFlags.Depth;
        }

        private void CreateCommandBufferDepth(CommandBuffer cmb, Material mat, RenderTexture rt, RenderTargetIdentifier src) {
            cmb.SetRenderTarget(rt);
            cmb.ClearRenderTarget(true, true, Color.black);
            _sandbox.AddDrawToCommandBuffer(cmb, mat);
        }

        private CommandBuffer CreatePaintBuffer() {
            var cmb = new CommandBuffer {name ="paint"};
            
            var t = BuiltinRenderTextureType.CameraTarget;
            cmb.Blit(t, t, _matPaint);
            
            return cmb;
        }

        private void OnNewDepthFrame(RenderTexture t) {
            _matPaint.SetTexture(_HANDS_TEX, t);
        }
    }
}