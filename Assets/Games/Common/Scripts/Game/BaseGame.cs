using System.Collections.Generic;
using System.Linq;
using DepthSensorCalibration;
using DepthSensorSandbox.Visualisation;
using Games.Common.GameFindObject;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using Utilities;

namespace Games.Common.Game {
    public class BaseGame : MonoBehaviour {
        [SerializeField] protected Camera _cam;
        [SerializeField] protected GameField _gameField;
        [SerializeField] private int _depthHeight = 64;
        [SerializeField] private SandboxMesh _sandbox;
        [SerializeField] private Material _matDepth;
        
        private int _hitMask;
        private CameraRenderToTexture _renderDepth;
        protected readonly DelayedDisposeNativeArray<byte> _depth = new DelayedDisposeNativeArray<byte>();
        protected int2 _depthSize;
        protected bool _isGameStarted;
        protected bool _testMouseModeHold;
        protected readonly Dictionary<string, Vector3> _initialSizes = new Dictionary<string, Vector3>();

        protected virtual void Start() {
            _hitMask = LayerMask.GetMask("interactable");
            
            _renderDepth = CreateRenderDepth();
            _renderDepth.MaxResolution = _depthHeight;
            _renderDepth.Enable(_matDepth, RenderTextureFormat.R8, OnNewDepthFrame, CreateCommandBufferDepth);
            
            Prefs.Calibration.OnChanged += OnCalibrationChanged;
            Prefs.Sandbox.OnChanged += OnCalibrationChanged;
            OnCalibrationChanged();

            GameEvent.OnStart += StartGame;
            GameEvent.OnStop += StopGame;
        }

        protected virtual CameraRenderToTexture CreateRenderDepth() {
            return _cam.gameObject.AddComponent<CameraRenderToTexture>();
        }

        protected virtual void OnDestroy() {
            GameEvent.OnStart -= StartGame;
            GameEvent.OnStop -= StopGame;
            
            Prefs.Sandbox.OnChanged -= OnCalibrationChanged;
            Prefs.Calibration.OnChanged -= OnCalibrationChanged;
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

        protected virtual void OnCalibrationChanged() {
            var cam = _cam.GetComponent<SandboxCamera>();
            if (cam != null) {
                cam.OnCalibrationChanged();
                SetSizes(Prefs.Sandbox.ZeroDepth);
            } else {
                SetSizes(1.66f); //for testing
            }
        }

        protected virtual void SetSizes(float dist) {
            _gameField.AlignToCamera(_cam, dist);
        }
        
        protected void SaveInitialSizes(IEnumerable<Component> objs) {
            foreach (var obj in objs) {
                _initialSizes[obj.name] = obj.transform.localScale;
            }
        }

        protected void SetSizes(float mult, IEnumerable<Component> objs) {
            foreach (var obj in objs) {
                var initial = _initialSizes.FirstOrDefault(kv => kv.Key.Contains(obj.name));
                obj.transform.localScale = initial.Value * mult;
            }
        }

        protected virtual void StartGame() {
            GameScore.Score = 0;
            for (int i = 0; i < GameScore.PlayerScore.Count; ++i) {
                GameScore.PlayerScore[i] = 0;
            }
            _isGameStarted = true;;
        }

        protected virtual void StopGame() {
            _isGameStarted = false;
        }
    }
}