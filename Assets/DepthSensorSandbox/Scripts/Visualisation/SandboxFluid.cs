using System;
using UnityEngine;
using UnityEngine.Rendering;
using Utilities;

namespace DepthSensorSandbox.Visualisation {
    public class SandboxFluid : SandboxVisualizerBase {
        private const CameraEvent _FLUID_SET_EVENT = CameraEvent.AfterEverything;
        private static readonly int _FLUID_PREV_TEX = Shader.PropertyToID("_FluidPrevTex");
        
        [SerializeField] private Camera _cam;

        private CommandBuffer _commandBuffer;
        private readonly RenderTexture[] _texFluidBuffers = new RenderTexture[2];
        private readonly RenderTargetIdentifier[] _renderTargets = new RenderTargetIdentifier[2];
        //private RenderTexture _resultTarget;
        private int _currFluidBuffer;
        private bool _needClearFluidFlow;
        private Renderer _renderer;

        protected override void Awake() {
            base.Awake();
            _renderer = _sandbox.GetComponent<Renderer>();
            _renderTargets[0] = BuiltinRenderTextureType.CameraTarget;
        }
        
        private void Start() {
            SetEnable(true);
        }

        protected override void OnDestroy() {
            base.OnDestroy();
            DisposeCommandBuffer(ref _commandBuffer, _FLUID_SET_EVENT);
            foreach (var t in _texFluidBuffers) {
                if (t != null) t.Release();
            }
        }

        public override void SetEnable(bool enable) {
            base.SetEnable(enable);
            DisposeCommandBuffer(ref _commandBuffer, _FLUID_SET_EVENT);
            if (enable) {
                BindBuffers();
                _commandBuffer = CreateCommandBuffer(_cam);
            }
        }

        public void ClearFluidFlows() {
            _needClearFluidFlow = true;
        }

        private void Update() {
            if (_needClearFluidFlow) {
                BindBuffers();
                _cam.RenderWithShader(_material.shader, "ClearFluidFlows");
                _needClearFluidFlow = false;
            }
            SwapBuffers();
        }

        private void SwapBuffers() {
            _currFluidBuffer = NextCircleId(_currFluidBuffer, _texFluidBuffers);
            BindBuffers();
            DisposeCommandBuffer(ref _commandBuffer, _FLUID_SET_EVENT);
            _commandBuffer = CreateCommandBuffer(_cam);
        }

        private static int NextCircleId(int id, Array a) {
            return (id + 1) % a.Length;
        }

        private void BindBuffers() {
            for (int i = 0; i < _texFluidBuffers.Length; ++i) {
                if (TexturesHelper.ReCreateIfNeed(ref _texFluidBuffers[i], _cam.pixelWidth, _cam.pixelHeight, 
                    0, RenderTextureFormat.ARGBHalf)) {
                    //Graphics.Blit(Texture2D.blackTexture, _texFluidBuffers[i]);
                }

                //TexturesHelper.ReCreateIfNeed(ref _resultTarget, _cam.pixelWidth, _cam.pixelHeight, 24);
            }

            _renderTargets[1] = _texFluidBuffers[_currFluidBuffer].colorBuffer;
            var prevBuffer = NextCircleId(_currFluidBuffer, _texFluidBuffers);
            _material.SetTexture(_FLUID_PREV_TEX, _texFluidBuffers[prevBuffer], RenderTextureSubElement.Color);
            //_cam.SetTargetBuffers(new [] {_resultTarget.colorBuffer, _texFluidBuffers[_currFluidBuffer].colorBuffer}, _resultTarget.depthBuffer);
        }

        private CommandBuffer CreateCommandBuffer(Camera cam) {
            var cmb = new CommandBuffer() {
                name = nameof(SandboxFluid)
            };
            /*cmb.SetRenderTarget(_renderTargets[0], _renderTargets[0]);
            cmb.ClearRenderTarget(true, true, _cam.backgroundColor);*/
            cmb.SetRenderTarget(_renderTargets, _renderTargets[0]);
            cmb.DrawRenderer(_renderer, _material);
            //cmb.Blit(_resultTarget, BuiltinRenderTextureType.CameraTarget);
            cam.AddCommandBuffer(_FLUID_SET_EVENT, cmb);
            return cmb;
        }

        private void DisposeCommandBuffer(ref CommandBuffer cmb, CameraEvent ev) {
            if (cmb != null) {
                _cam.RemoveCommandBuffer(ev, cmb);
                cmb.Dispose();
                cmb = null;
            }
        }
    }
}