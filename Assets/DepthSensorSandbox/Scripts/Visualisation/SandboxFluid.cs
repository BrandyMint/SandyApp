using System;
using System.Collections.Generic;
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
        
        private readonly RenderTexture[] _texFluxBuffers = new RenderTexture[2];
        private readonly RenderTexture[] _texHeightBuffers = new RenderTexture[2];
        private readonly CommandBuffer[] _commandBuffers = new CommandBuffer[2];
        private int _currFluidBuffer;
        private int _prevCamCullingMask = -1;
        private Camera _clearCam;
        private int _clearStep;

        private void Start() {
            _clearCam = new GameObject("CameraClearFluid").AddComponent<Camera>();
            _clearCam.enabled = false;
            _clearCam.transform.parent = transform.parent;
            SetEnable(true);
        }

        protected override void OnDestroy() {
            base.OnDestroy();
            for (int i = 0; i < _commandBuffers.Length; ++i) {
                DisposeCommandBuffer(ref _commandBuffers[i], _FLUID_EVENT);
            }
            foreach (var t in _texFluxBuffers) {
                if (t != null) t.Release();
            }
        }

        public override void SetEnable(bool enable) {
            base.SetEnable(enable);
            for (int i = 0; i < _commandBuffers.Length; ++i) {
                DisposeCommandBuffer(ref _commandBuffers[i], _FLUID_EVENT);
            }
            if (enable) {
                _prevCamCullingMask = _cam.cullingMask;
                _cam.cullingMask = 0;
                ClearFluidFlows();
            } else {
                if (_prevCamCullingMask >= 0)
                    _cam.cullingMask = _prevCamCullingMask;
            }
        }

        public void ClearFluidFlows() {
            CreateBuffersIfNeed();
            _clearStep = 0;
        }

        private void Update() {
            if (_clearStep <= _CLEAR_STEP_FINISH) {
                if (_clearStep == 0) {
                    _material.EnableKeyword(_CLEAR_FLUID);
                } else 
                if (_clearStep == _CLEAR_STEP_FINISH) {
                    _material.DisableKeyword(_CLEAR_FLUID);
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
            var created = TexturesHelper.ReCreateIfNeed(ref t,
                _cam.pixelWidth, _cam.pixelHeight, 0, f);
            if (created) {
                t.filterMode = FilterMode.Point;
                t.wrapMode = TextureWrapMode.Clamp;
                t.Create();
            }
            return created;
        }

        private void CreateBuffersIfNeed() {
            for (int i = 0; i < _texFluxBuffers.Length; ++i) {
                var newTexture = ReCreateBufferIfNeed(ref _texFluxBuffers[i], RenderTextureFormat.ARGBFloat);
                newTexture |= ReCreateBufferIfNeed(ref _texHeightBuffers[i], RenderTextureFormat.RGFloat);
                if (newTexture || _commandBuffers[i] == null) {
                    DisposeCommandBuffer(ref _commandBuffers[i], _FLUID_EVENT);
                    _commandBuffers[i] = CreateCommandBuffer(
                        new RenderTargetIdentifier[] {
                            BuiltinRenderTextureType.CameraTarget,
                            _texHeightBuffers[i].colorBuffer,
                            _texFluxBuffers[i].colorBuffer
                        }
                    );
                    _commandBuffers[i].name += i;
                }
            }
        }

        private void BindBuffers() {
            var prevBuffer = NextCircleId(_currFluidBuffer, _texFluxBuffers);
            _cam.RemoveCommandBuffer(_FLUID_EVENT, _commandBuffers[prevBuffer]);
            _cam.AddCommandBuffer(_FLUID_EVENT, _commandBuffers[_currFluidBuffer]);
            _material.SetTexture(_HEIGHT_PREV_TEX, _texHeightBuffers[prevBuffer], RenderTextureSubElement.Color);
            _material.SetTexture(_FLUX_PREV_TEX, _texFluxBuffers[prevBuffer], RenderTextureSubElement.Color);
        }

        private CommandBuffer CreateCommandBuffer(RenderTargetIdentifier[] targets) {
            var cmb = new CommandBuffer {
                name = nameof(SandboxFluid)
            };
            cmb.SetRenderTarget(new [] {targets[1], targets[2]}, targets[0]);
            cmb.ClearRenderTarget(false, true, Color.clear);
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