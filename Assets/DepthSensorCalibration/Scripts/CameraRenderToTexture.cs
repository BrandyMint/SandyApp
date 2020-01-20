#if USE_MAT_ASYNC_SET
    using AsyncGPUReadback = AsyncGPUReadbackPluginNs.AsyncGPUReadback;
#endif
using System;
using DepthSensor.Buffer;
using DepthSensorSandbox;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using Utilities;

namespace DepthSensorCalibration {
    [RequireComponent(typeof(Camera))]
    public class CameraRenderToTexture : MonoBehaviour {
        private Camera _cam;
        private CommandBuffer _commandBuffer;
        private Material _mat;
        private RenderTextureFormat _format;
        private Action<RenderTexture> _onNewFrame;
        private CreateCommandBuffer _createCommandBuffer;
        private RenderTargetIdentifier _renderSrc;
        private CameraEvent _cameraEvent;
        private Texture2D _tempTex;
        private readonly DelayedDisposeRenderTexture _renderTarget = new DelayedDisposeRenderTexture();
        
        private bool _newProcessedFrame;
        private bool _invokesOnlyOnProcessedFrame;
        private bool _cmdBufferAdded;

        public delegate void CreateCommandBuffer(CommandBuffer cmb, Material mat, RenderTexture rt, RenderTargetIdentifier src);

        public int MaxResolution = 2048;
        private bool _needUpdateCommandBuffer;

        public bool InvokesOnlyOnProcessedFrame {
            get => _invokesOnlyOnProcessedFrame;
            set {
                var val = value && DepthSensorSandboxProcessor.Instance != null;
                if (_invokesOnlyOnProcessedFrame != val) {
                    if (value) 
                        DepthSensorSandboxProcessor.OnNewFrame += OnNewProcessedFrame;
                    else
                        DepthSensorSandboxProcessor.OnNewFrame -= OnNewProcessedFrame;
                    _invokesOnlyOnProcessedFrame = val;
                }
            }
        }

        public bool ManualRender {
            get => !_cam.enabled;
            set => _cam.enabled = !value;
        }

        private void Awake() {
            _cam = GetComponent<Camera>();
            enabled = false;
            Prefs.App.OnChanged += OnAppParamChanged;
        }

        private void OnDestroy() {
            if (Prefs.App != null)
                Prefs.App.OnChanged -= OnAppParamChanged;
            InvokesOnlyOnProcessedFrame = false;
            DisposeCommandBuffer();
            _renderTarget.Dispose();
            if (_tempTex != null)
                Destroy(_tempTex);
        }

        private void OnAppParamChanged() {
            //for applying changed camera flip
            _needUpdateCommandBuffer = true;
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

        private void DisposeCommandBuffer() {
            RemoveCommandBuffer();
            if (_commandBuffer != null) {
                _commandBuffer.Dispose();
                _commandBuffer = null;
            }
        }
        
        private void AddCommandBuffer() {
            if (!_cmdBufferAdded) {
                _cam.AddCommandBuffer(_cameraEvent, _commandBuffer);
                _cmdBufferAdded = true;
            }
        }

        private void RemoveCommandBuffer() {
            if (_cmdBufferAdded) {
                if (_cam != null && _commandBuffer != null)
                    _cam.RemoveCommandBuffer(_cameraEvent, _commandBuffer);
                _cmdBufferAdded = false;
            }
        }

        private void UpdateCommandBuffer() {
            var cmdWasAdded = _cmdBufferAdded;
            DisposeCommandBuffer();
            var cmdName = $"{nameof(CameraRenderToTexture)}_{_mat?.shader.name}";
            _commandBuffer = new CommandBuffer {name = cmdName};
            //_commandBuffer.SetInvertCulling(CameraFlipper.GetInvertCulling(_cam, _cameraEvent));
            _createCommandBuffer(_commandBuffer, _mat, _renderTarget.o, _renderSrc);
            if (cmdWasAdded)
                AddCommandBuffer();
        }

        private bool UpdateRenderTarget() {
            if (!_renderTarget.Unlocked()) return false;

            int height = Mathf.Min(_cam.pixelHeight, MaxResolution);
            int width = Mathf.Min(_cam.pixelWidth, (int) (_cam.aspect * MaxResolution));
            return TexturesHelper.ReCreateIfNeed(ref _renderTarget.o, width, height, 0, _format);
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
            AddCommandBuffer();
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

        public bool RequestData<T>(DelayedDisposeNativeObject<NativeArray<T>> array, Action OnData) where T : struct {
            if (_renderTarget == null || !_renderTarget.Unlocked() || array == null || !array.Unlocked())
                return false;
            _renderTarget.LockDispose();
            array.LockDispose();
#if USE_MAT_ASYNC_SET
            array.DontDisposeObject = false;
            TexturesHelper.ReCreateIfNeed(ref array.o, _renderTarget.o.GetPixelsCount());
            AsyncGPUReadback.RequestIntoNativeArray(ref array.o, _renderTarget.o, 0, r => {
                OnDataReceived(!r.hasError, array, OnData);
            });
            if (!_renderTarget.Unlocked())
                RemoveCommandBuffer();
#else
            array.DontDisposeObject = true;
            TexturesHelper.ReCreateIfNeedCompatible(ref _tempTex, _renderTarget.o);
            TexturesHelper.Copy(_renderTarget.o, _tempTex);
            array.o = _tempTex.GetRawTextureData<T>();
            OnDataReceived(true, array, OnData);
#endif
            return true;
        }

        private void OnDataReceived<T>(bool success, DelayedDisposeNativeObject<NativeArray<T>> array, Action OnData) where T : struct {
            _renderTarget.UnLockDispose();
            array.UnLockDispose();
            if (success && array != null) {
                OnData?.Invoke();
            }
            if (_renderTarget != null)
                AddCommandBuffer();
        }

        public void Disable() {
            this.enabled = false;
            DisposeCommandBuffer();
            _createCommandBuffer = null;
            _onNewFrame = null;
        }

        private void Update() {
            if (!ManualRender) {
                UpdateAll();
            }
        }

        private void UpdateAll() {
            if (UpdateRenderTarget() || _needUpdateCommandBuffer) {
                _needUpdateCommandBuffer = false;
                UpdateCommandBuffer();
            }
        }

        public RenderTexture Render() {
            ManualRender = true;
            UpdateAll();
            if (_renderTarget == null || !_renderTarget.Unlocked())
                return null;
            _cam.Render();
            return _renderTarget.o;
        }

        private void OnPostRender() {
            if (this.enabled && _cmdBufferAdded) {
                if (InvokesOnlyOnProcessedFrame && !_newProcessedFrame) return;
                _newProcessedFrame = false;
                if (_renderTarget != null)
                    _onNewFrame?.Invoke(_renderTarget.o);
            }
        }

        private void OnNewProcessedFrame(DepthBuffer depth, MapDepthToCameraBuffer map) {
            _newProcessedFrame = true;
        }
    }
}