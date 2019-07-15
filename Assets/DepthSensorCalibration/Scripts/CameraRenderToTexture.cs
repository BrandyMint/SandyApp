using System;
using UnityEngine;
using UnityEngine.Rendering;
using Utilities;

namespace DepthSensorCalibration {
    [RequireComponent(typeof(Camera))]
    public class CameraRenderToTexture : MonoBehaviour {
        private const CameraEvent _CAM_EVENT_BLIT = CameraEvent.AfterForwardOpaque;
        
        private Camera _cam;
        private RenderTexture _renderTarget;
        private CommandBuffer _commandBuffer;
        private Material _mat;
        private RenderTextureFormat _format;
        private Action<RenderTexture> _onNewFrame;

        private void Awake() {
            _cam = GetComponent<Camera>();
        }

        private void OnDestroy() {
            DisposeCommandBuffer(ref _commandBuffer);
            if (_renderTarget != null)
                Destroy(_renderTarget);
        }

        private static CommandBuffer CreateCommandBufferBlit(string name, Material mat, RenderTexture rt) {
            var cmb = new CommandBuffer() {
                name = name
            };
            if (mat == null) {
                cmb.Blit(BuiltinRenderTextureType.CameraTarget, rt);
            } else {
                cmb.Blit(BuiltinRenderTextureType.CameraTarget, rt, mat);
            }
            
            return cmb;
        }

        private void DisposeCommandBuffer(ref CommandBuffer cmb) {
            if (cmb != null) {
                _cam.RemoveCommandBuffer(_CAM_EVENT_BLIT, cmb);
                cmb.Dispose();
                cmb = null;
            }
        }

        private void UpdateCommandBuffer() {
            DisposeCommandBuffer(ref _commandBuffer);
            _commandBuffer = CreateCommandBufferBlit(nameof(CameraRenderToTexture), _mat, _renderTarget);
            _cam.AddCommandBuffer(_CAM_EVENT_BLIT, _commandBuffer);
        }

        private bool UpdateRenderTarget() {
            return TexturesHelper.ReCreateIfNeed(ref _renderTarget, _cam.pixelWidth, _cam.pixelHeight, 0, _format);
        }

        public void Enable(Material mat, RenderTextureFormat rtFormat, Action<RenderTexture> onNewFrame = null) {
            Disable();
            _mat = mat;
            _format = rtFormat;
            _onNewFrame = onNewFrame;
            UpdateRenderTarget();
            UpdateCommandBuffer();
            this.enabled = true;
        }

        public void Disable() {
            this.enabled = false;
            DisposeCommandBuffer(ref _commandBuffer);
            _onNewFrame = null;
        }

        private void Update() {
            if (UpdateRenderTarget())
                UpdateCommandBuffer();
        }

        private void OnPostRender() {
            _onNewFrame?.Invoke(_renderTarget);
        }
    }
}