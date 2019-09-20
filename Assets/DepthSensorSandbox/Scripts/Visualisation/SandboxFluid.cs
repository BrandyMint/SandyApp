using System;
using UnityEngine;
using UnityEngine.Rendering;
using Utilities;

namespace DepthSensorSandbox.Visualisation {
    public class SandboxFluid : SandboxVisualizerBase {
        private const CameraEvent _FLUID_SET_EVENT = CameraEvent.BeforeForwardOpaque;
        private static readonly int _FLUID_PREV_TEX = Shader.PropertyToID("_FluidPrevTex");
        
        [SerializeField] private Camera _cam;
        
        private readonly RenderTexture[] _texFluidBuffers = new RenderTexture[2];
        private readonly CommandBuffer[] _commandBuffers = new CommandBuffer[2];
        private int _currFluidBuffer;
        private bool _needClearFluidFlow;
        private int _prevCamCullingMask = -1;

        private void Start() {
            SetEnable(true);
        }

        protected override void OnDestroy() {
            base.OnDestroy();
            for (int i = 0; i < _commandBuffers.Length; ++i) {
                DisposeCommandBuffer(ref _commandBuffers[i], _FLUID_SET_EVENT);
            }
            foreach (var t in _texFluidBuffers) {
                if (t != null) t.Release();
            }
        }

        public override void SetEnable(bool enable) {
            base.SetEnable(enable);
            for (int i = 0; i < _commandBuffers.Length; ++i) {
                DisposeCommandBuffer(ref _commandBuffers[i], _FLUID_SET_EVENT);
            }
            if (enable) {
                _prevCamCullingMask = _cam.cullingMask;
                _cam.cullingMask = 0;
                CreateBuffersIfNeed();
                BindBuffers();
            } else {
                if (_prevCamCullingMask >= 0)
                    _cam.cullingMask = _prevCamCullingMask;
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
        }

        private static int NextCircleId(int id, Array a) {
            return (id + 1) % a.Length;
        }

        private void CreateBuffersIfNeed() {
            for (int i = 0; i < _texFluidBuffers.Length; ++i) {
                var newTexture = TexturesHelper.ReCreateIfNeed(ref _texFluidBuffers[i],
                    _cam.pixelWidth, _cam.pixelHeight, 0, RenderTextureFormat.ARGBHalf); 
                if (newTexture || _commandBuffers[i] == null) {
                    DisposeCommandBuffer(ref _commandBuffers[i], _FLUID_SET_EVENT);
                    _commandBuffers[i] = CreateCommandBuffer(
                        new RenderTargetIdentifier[] {BuiltinRenderTextureType.CameraTarget, _texFluidBuffers[i].colorBuffer}
                    );
                    _commandBuffers[i].name += i;
                }
            }
        }

        private void BindBuffers() {
            var prevBuffer = NextCircleId(_currFluidBuffer, _texFluidBuffers);
            _cam.RemoveCommandBuffer(_FLUID_SET_EVENT, _commandBuffers[prevBuffer]);
            _cam.AddCommandBuffer(_FLUID_SET_EVENT, _commandBuffers[_currFluidBuffer]);
            _material.SetTexture(_FLUID_PREV_TEX, _texFluidBuffers[prevBuffer], RenderTextureSubElement.Color);
        }

        private CommandBuffer CreateCommandBuffer(RenderTargetIdentifier[] targets) {
            var cmb = new CommandBuffer {
                name = nameof(SandboxFluid)
            };
            cmb.SetRenderTarget(targets, targets[0]);
            _sandbox.AddDrawToCommandBuffer(cmb);
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