using DepthSensorCalibration;
using DepthSensorSandbox.Visualisation;
using Games.Common.GameFindObject;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using Utilities;

namespace Games.Common.Game {
    public class BaseGameWithGetDepth : BaseGame {
        [SerializeField] private int _depthHeight = 64;
        [SerializeField] private SandboxMesh _sandbox;
        [SerializeField] private Material _matDepth;

        protected int _hitMask;
        private CameraRenderToTexture _renderDepth;
        protected readonly DelayedDisposeNativeArray<byte> _depth = new DelayedDisposeNativeArray<byte>();
        protected int2 _depthSize;
        protected bool _testMouseModeHold;

        protected override void Start() {
            _hitMask = LayerMask.GetMask("interactable");
            
            _renderDepth = CreateRenderDepth();
            _renderDepth.InvokesOnlyOnProcessedFrame = true;
            _renderDepth.MaxResolution = _depthHeight;
            _renderDepth.Enable(_matDepth, RenderTextureFormat.R8, OnNewDepthFrame, CreateCommandBufferDepth);
            
            base.Start();
        }

        protected virtual CameraRenderToTexture CreateRenderDepth() {
            return _cam.gameObject.AddComponent<CameraRenderToTexture>();
        }

        protected override void OnDestroy() {
            base.OnDestroy();
            if (_renderDepth != null) {
                _renderDepth.Disable();
            }
            _depth.Dispose();
        }

        protected virtual void Update() {
            if (_testMouseModeHold ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0)) {
                var screen = new float2(_cam.pixelWidth, _cam.pixelHeight);
                var pos = new float2(Input.mousePosition.x, Input.mousePosition.y);
                Fire(pos / screen);
            }
        }

        protected virtual void Fire(Vector2 viewPos) {
            if (!_isGameStarted) return;
            
            var ray = _cam.ViewportPointToRay(viewPos);
            if (Physics.Raycast(ray, out var hit, _cam.farClipPlane, _hitMask)) {
                var item = hit.collider.GetComponent<Interactable>() ?? hit.collider.GetComponentInParent<Interactable>();
                if (item != null) {
                    OnFireItem(item, viewPos);
                }
            }
        }

        protected virtual void OnFireItem(Interactable item, Vector2 viewPos) {
        }

        private void CreateCommandBufferDepth(CommandBuffer cmb, Material mat, RenderTexture rt, RenderTargetIdentifier src) {
            cmb.SetRenderTarget(rt);
            _sandbox.AddDrawToCommandBuffer(cmb, mat);
        }

        private void OnNewDepthFrame(RenderTexture t) {
            _depthSize = new int2(t.width, t.height);
            _renderDepth.RequestData(_depth, ProcessDepthFrame);
        }
        
        protected virtual void ProcessDepthFrame() {
            for (int x = 0; x < _depthSize.x; ++x) {
                for (int y = 0; y < _depthSize.y; ++y) {
                    if (_depth.o[x + y * _depthSize.x] > 0) {
                        Fire(new float2(x, y) / _depthSize);
                    }
                }
            }
        }
    }
}