using System;
using Launcher.KeyMapping;
using UnityEngine;
using UnityEngine.Rendering;
using Utilities;

namespace DepthSensorSandbox.Visualisation {
    public class SandboxFluid : SandboxVisualizerBase {
        private const CameraEvent _FLUID_EVENT = CameraEvent.BeforeForwardOpaque;
        private const string _CLEAR_FLUID = "CLEAR_FLUID";
        private static readonly int _FLUX_PREV_TEX = Shader.PropertyToID("_FluxPrevTex");
        private static readonly int _HEIGHT_PREV_TEX = Shader.PropertyToID("_HeightPrevTex");
        private const int _CLEAR_STEP_FINISH = 4;
        
        [SerializeField] private Camera _cam;
        [SerializeField] protected Material _matFluidCalc;
        [SerializeField] protected int _fluidMapHeight = 256;
        
        private readonly RenderTexture[] _texFluxBuffers = new RenderTexture[2];
        private readonly RenderTexture[] _texHeightBuffers = new RenderTexture[2];
        private readonly CommandBuffer[] _commandBuffers = new CommandBuffer[2];
        private int _currFluidBuffer;
        private int _clearStep;

        private void Start() {
            if (_instantiateMaterial)
                _matFluidCalc = new Material(_matFluidCalc);
            SetEnable(true);
            KeyMapper.AddListener(KeyEvent.FLIP_DISPLAY, ClearFluidFlows);
        }

        protected override void OnDestroy() {
            base.OnDestroy();
            for (int i = 0; i < _commandBuffers.Length; ++i) {
                DisposeCommandBuffer(ref _commandBuffers[i], _FLUID_EVENT);
            }
            foreach (var t in _texFluxBuffers) {
                if (t != null) t.Release();
            }
            KeyMapper.RemoveListener(KeyEvent.FLIP_DISPLAY, ClearFluidFlows);
        }

        public override void SetEnable(bool enable) {
            base.SetEnable(enable);
            if (enable) {
                CreateBuffersIfNeed();
            }
        }

        public void ClearFluidFlows() {
            _clearStep = 0;
        }

        protected virtual void Update() {
            if (_clearStep <= _CLEAR_STEP_FINISH) {
                if (_clearStep == 0) {
                    _matFluidCalc.EnableKeyword(_CLEAR_FLUID);
                } else 
                if (_clearStep == _CLEAR_STEP_FINISH) {
                    _matFluidCalc.DisableKeyword(_CLEAR_FLUID);
                }
                ++_clearStep;
            }
            SwapBuffers();
        }

        private void SwapBuffers() {
            _currFluidBuffer = NextCircleId(_currFluidBuffer, _texFluxBuffers);
            BindBuffers();
        }

        private static int NextCircleId(int id, Array a) {
            return (id + 1) % a.Length;
        }

        private bool ReCreateBufferIfNeed(ref RenderTexture t, RenderTextureFormat f) {
            var width = (int) (_cam.aspect * _fluidMapHeight);
            var created = TexturesHelper.ReCreateIfNeed(ref t,
                width, _fluidMapHeight, 0, f);
            if (created) {
                t.filterMode = FilterMode.Bilinear;
                t.wrapMode = TextureWrapMode.Clamp;
                t.useMipMap = false;
                t.autoGenerateMips = false;
                t.Create();
            }
            return created;
        }

        protected void CreateBuffersIfNeed() {
            for (int i = 0; i < _texFluxBuffers.Length; ++i) {
                var newTexture = ReCreateBufferIfNeed(ref _texHeightBuffers[i], RenderTextureFormat.RGFloat);
                newTexture |= ReCreateBufferIfNeed(ref _texFluxBuffers[i], RenderTextureFormat.ARGBFloat);
                if (newTexture || _commandBuffers[i] == null) {
                    DisposeCommandBuffer(ref _commandBuffers[i], _FLUID_EVENT);
                    _commandBuffers[i] = CreateCommandBuffer(
                        new RenderTargetIdentifier[] {
                            _texHeightBuffers[i].colorBuffer,
                            _texFluxBuffers[i].colorBuffer
                        },
                        _texHeightBuffers[i].depthBuffer
                    );
                    _commandBuffers[i].name += i;
                    ClearFluidFlows();
                }
            }
        }

        private void BindBuffers() {
            var prevBuffer = NextCircleId(_currFluidBuffer, _texFluxBuffers);
            _cam.RemoveCommandBuffer(_FLUID_EVENT, _commandBuffers[prevBuffer]);
            _cam.AddCommandBuffer(_FLUID_EVENT, _commandBuffers[_currFluidBuffer]);
            var props = _sandbox.PropertyBlock;
            props.SetTexture(_HEIGHT_PREV_TEX, _texHeightBuffers[prevBuffer], RenderTextureSubElement.Color);
            props.SetTexture(_FLUX_PREV_TEX, _texFluxBuffers[prevBuffer], RenderTextureSubElement.Color);
            _sandbox.PropertyBlock = props;
        }

        private CommandBuffer CreateCommandBuffer(RenderTargetIdentifier[] targets, RenderTargetIdentifier depth) {
            var cmb = new CommandBuffer {
                name = nameof(SandboxFluid)
            };
            cmb.SetRenderTarget(targets, depth);
            cmb.ClearRenderTarget(false, true, Color.clear);
            _sandbox.AddDrawToCommandBuffer(cmb, _matFluidCalc);
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