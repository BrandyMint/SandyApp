using System;
using UnityEngine;
using UnityEngine.Rendering;
using Utilities;

namespace DepthSensorCalibration {
    [RequireComponent(typeof(Camera))]
    public class CameraRenderToTexture : MonoBehaviour {
        private Camera _cam;
        private RenderTexture _renderTarget;
        private RenderTexture _tempCopy;
        private CommandBuffer _commandBuffer;
        private Material _mat;
        private RenderTextureFormat _format;
        private Action<RenderTexture> _onNewFrame;
        private CreateCommandBuffer _createCommandBuffer;
        private RenderTargetIdentifier _renderSrc;
        private CameraEvent _cameraEvent;

        public delegate void CreateCommandBuffer(CommandBuffer cmb, Material mat, RenderTexture rt, RenderTargetIdentifier src);

        public int MaxResolution = 2048;

        private void Awake() {
            _cam = GetComponent<Camera>();
            enabled = false;
        }

        private void Start() {
            //_cam.depthTextureMode = DepthTextureMode.Depth;
        }

        private void OnDestroy() {
            DisposeCommandBuffer(ref _commandBuffer, _cameraEvent);
            if (_renderTarget != null) {
                Destroy(_renderTarget);
            }
            if (_tempCopy != null) {
                Destroy(_tempCopy);
            }
        }

        private static void CreateCommandBufferBlit(
            CommandBuffer cmb, Material mat, RenderTexture rt, RenderTargetIdentifier src
        ) {
            if (mat == null) {
                cmb.Blit(src, rt);
            } else {
                cmb.Blit(src, rt, mat);
            }
        }

        private void DisposeCommandBuffer(ref CommandBuffer cmb, CameraEvent ev) {
            if (cmb != null) {
                _cam.RemoveCommandBuffer(ev, cmb);
                cmb.Dispose();
                cmb = null;
            }
        }

        private void UpdateCommandBuffer() {
            DisposeCommandBuffer(ref _commandBuffer, _cameraEvent);
            var cmdName = $"{nameof(CameraRenderToTexture)}_{_mat.shader.name}";
            _commandBuffer = new CommandBuffer {name = name};
            _createCommandBuffer(_commandBuffer, _mat, _renderTarget, _renderSrc);
            _cam.AddCommandBuffer(_cameraEvent, _commandBuffer);
        }

        private bool UpdateRenderTarget() {
            int height = Mathf.Min(_cam.pixelHeight, MaxResolution);
            int width = Mathf.Min(_cam.pixelWidth, (int) (_cam.aspect * MaxResolution));
            return TexturesHelper.ReCreateIfNeed(ref _renderTarget, width, height, 0, _format);
        }

        public void Enable(
            Material mat, RenderTextureFormat rtFormat, 
            RenderTargetIdentifier src, CameraEvent ev, Action<RenderTexture> onNewFrame = null, 
            CreateCommandBuffer createCommandBuffer = null
        ) {
            Disable();
            _mat = mat;
            _format = rtFormat;
            _onNewFrame = onNewFrame;
            _renderSrc = src;
            _cameraEvent = ev;
            _createCommandBuffer = createCommandBuffer ?? CreateCommandBufferBlit;
            UpdateRenderTarget();
            UpdateCommandBuffer();
            this.enabled = true;
        }
        
        public void Enable(
            Material mat, RenderTextureFormat rtFormat, 
            RenderTargetIdentifier src, Action<RenderTexture> onNewFrame = null,
            CreateCommandBuffer createCommandBuffer = null
        ) {
            Enable(
                mat, rtFormat, 
                src, CameraEvent.AfterForwardOpaque, 
                onNewFrame, createCommandBuffer
            );
        }
        
        public void Enable(
            Material mat, RenderTextureFormat rtFormat, 
            CameraEvent cameraEvent, Action<RenderTexture> onNewFrame = null, 
            CreateCommandBuffer createCommandBuffer = null
        ) {
            Enable(
                mat, rtFormat, 
                BuiltinRenderTextureType.CameraTarget, cameraEvent,
                onNewFrame, createCommandBuffer
            );
        }

        public void Enable(
            Material mat, RenderTextureFormat rtFormat, 
            Action<RenderTexture> onNewFrame = null,
            CreateCommandBuffer createCommandBuffer = null
        ) {
            Enable(
                mat, rtFormat, 
                BuiltinRenderTextureType.CameraTarget, CameraEvent.AfterForwardOpaque, 
                onNewFrame, createCommandBuffer
            );
        }

        public void Disable() {
            this.enabled = false;
            DisposeCommandBuffer(ref _commandBuffer, _cameraEvent);
            _createCommandBuffer = null;
            _onNewFrame = null;
        }

        public RenderTexture GetTempCopy() {
            TexturesHelper.ReCreateIfNeedCompatible(ref _tempCopy, _renderTarget);
            Graphics.Blit(_renderTarget, _tempCopy);
            return _tempCopy;
        }

        private void Update() {
            if (UpdateRenderTarget())
                UpdateCommandBuffer();
        }

        private void OnPostRender() {
            if (this.enabled)
                _onNewFrame?.Invoke(_renderTarget);
        }
    }
}