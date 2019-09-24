using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Utilities;

namespace DepthSensorSandbox.Visualisation {
    public class SandboxFluid : SandboxVisualizerBase {
        private const CameraEvent _FLUID_SET_EVENT = CameraEvent.BeforeForwardOpaque;
        private const string _CLEAR_FLUID = "CLEAR_FLUID";
        private static readonly int _FLUID_PREV_TEX = Shader.PropertyToID("_FluidPrevTex");
        private static readonly int _NEIGHBOURS = Shader.PropertyToID("_Neighbours");
        private const int _CLEAR_STEP_FINISH = 4;
        
        [SerializeField] private Camera _cam;
        
        private readonly RenderTexture[] _texFluidBuffers = new RenderTexture[2];
        private readonly CommandBuffer[] _commandBuffers = new CommandBuffer[2];
        private int _currFluidBuffer;
        private int _prevCamCullingMask = -1;
        private Camera _clearCam;
        private readonly Vector4[] _neighbours = InitNeighboursArray();
        private int _clearStep;

        private void Start() {
            _clearCam = new GameObject("CameraClearFluid").AddComponent<Camera>();
            _clearCam.enabled = false;
            _clearCam.transform.parent = transform.parent;
            SetEnable(true);
        }

        private static Vector4[] InitNeighboursArray() {
            var array = new List<Vector4>();
            for (int x = -1; x <= 1; ++x) {
                for (int y = -1; y <= 1; ++y) {
                    if (x == 0 && y == 0) continue;
                    array.Add(new Vector3(x, y, new Vector2(x, y).sqrMagnitude));
                    //array.Add(new Vector2(x, y).normalized);
                }
            }
            return array.ToArray();
            //return new Vector4[] {Vector2.up, Vector2.left, Vector2.down, Vector2.right};
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
            _material.SetVectorArray(_NEIGHBOURS, _neighbours);
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
            _currFluidBuffer = NextCircleId(_currFluidBuffer, _texFluidBuffers);
            BindBuffers();
        }

        private static int NextCircleId(int id, Array a) {
            return (id + 1) % a.Length;
        }

        private void CreateBuffersIfNeed() {
            for (int i = 0; i < _texFluidBuffers.Length; ++i) {
                var newTexture = TexturesHelper.ReCreateIfNeed(ref _texFluidBuffers[i],
                    _cam.pixelWidth, _cam.pixelHeight, 0, RenderTextureFormat.ARGBFloat); 
                if (newTexture || _commandBuffers[i] == null) {
                    _texFluidBuffers[i].filterMode = FilterMode.Point;
                    _texFluidBuffers[i].wrapMode = TextureWrapMode.Clamp;
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
            cmb.SetRenderTarget(targets[1], targets[0]);
            cmb.ClearRenderTarget(false, true, Color.black);
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