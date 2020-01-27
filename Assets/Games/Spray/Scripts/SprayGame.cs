using System.Collections.Generic;
using System.Linq;
using DepthSensorCalibration;
using DepthSensorSandbox.Visualisation;
using Games.Balloons;
using Games.Common;
using Games.Common.Game;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using Utilities;

namespace Games.Spray {
    public class SprayGame : MonoBehaviour {
        [SerializeField] private Camera _cam;
        [SerializeField] private Spray[] _items;
        [SerializeField] private GameField _gameField;
        [SerializeField] private int _depthHeight = 64;
        [SerializeField] private SandboxMesh _sandbox;
        [SerializeField] private Material _matDepth;
        [SerializeField] private ProjectorDestination _projector;
        
        private HashSet<Spray> _fired = new HashSet<Spray>();
        private int _hitMask;
        private CameraRenderToTexture _renderDepth;
        private readonly DelayedDisposeNativeArray<byte> _depth = new DelayedDisposeNativeArray<byte>();
        private int2 _depthSize; 
        private float _initialItemSize;
        private int _score;
        private bool _isGameStarted;

        private void Start() {
            _hitMask = LayerMask.GetMask("interactable");

            _renderDepth = _cam.gameObject.AddComponent<CameraRenderToTexture>();
            _renderDepth.MaxResolution = _depthHeight;
            _renderDepth.InvokesOnlyOnProcessedFrame = true;
            _renderDepth.Enable(_matDepth, RenderTextureFormat.R8, OnNewDepthFrame, CreateCommandBufferDepth);

            _initialItemSize = math.cmax(_items.First().transform.localScale);

            Prefs.Calibration.OnChanged += OnCalibrationChanged;
            Prefs.Sandbox.OnChanged += OnCalibrationChanged;
            OnCalibrationChanged();

            GameEvent.OnStart += StartGame;
            GameEvent.OnStop += StopGame;
            ShowItems(false);
        }

        private void OnDestroy() {
            GameEvent.OnStart -= StartGame;
            GameEvent.OnStop -= StopGame;
            
            Prefs.Sandbox.OnChanged -= OnCalibrationChanged;
            Prefs.Calibration.OnChanged -= OnCalibrationChanged;
            if (_renderDepth != null) {
                _renderDepth.Disable();
            }
            _depth.Dispose();
        }
        
        private void Spawn() {
            var i = 0;
            foreach (var area in SpawnArea.Areas) {
                foreach (var spawn in area.Spawns) {
                    var item = _items[i++];
                    item.transform.position = spawn.position;
                    item.transform.rotation = spawn.rotation;
                }
            }
        }

        private void Update() {
            if (Input.GetMouseButton(0)) {
                var screen = new float2(_cam.pixelWidth, _cam.pixelHeight);
                var pos = new float2(Input.mousePosition.x, Input.mousePosition.y);
                Fire(pos / screen);
            }
        }

        private void Fire(Vector2 viewPos) {
            if (!_isGameStarted) return;
            
            var ray = _cam.ViewportPointToRay(viewPos);
            if (Physics.Raycast(ray, out var hit, _cam.farClipPlane, _hitMask)) {
                var item = hit.collider.GetComponentInParent<Spray>();
                if (item != null && !item.Fire && !_fired.Contains(item)) {
                    item.Fire = true;
                    _fired.Add(item);
                }
            }
        }

        private void CheckStopFire() {
            foreach (var item in _items) {
                if (!_fired.Contains(item))
                    item.Fire = false;
            }
        }

        private void CreateCommandBufferDepth(CommandBuffer cmb, Material mat, RenderTexture rt, RenderTargetIdentifier src) {
            cmb.SetRenderTarget(rt);
            _sandbox.AddDrawToCommandBuffer(cmb, mat);
        }

        private void OnNewDepthFrame(RenderTexture t) {
            _depthSize = new int2(t.width, t.height);
            _renderDepth.RequestData(_depth, ProcessDepthFrame);
        }
        
        private void ProcessDepthFrame() {
            for (int x = 0; x < _depthSize.x; ++x) {
                for (int y = 0; y < _depthSize.y; ++y) {
                    if (_depth.o[x + y * _depthSize.x] > 0) {
                        Fire(new float2(x, y) / _depthSize);
                    }
                }
            }
            CheckStopFire();
            _fired.Clear();
        }

        protected virtual void OnCalibrationChanged() {
            var cam = _cam.GetComponent<SandboxCamera>();
            var spawnArea = SpawnArea.Areas.First().transform;
            if (cam != null) {
                cam.OnCalibrationChanged();
                SetSizes(Prefs.Sandbox.ZeroDepth - Prefs.Sandbox.OffsetMaxDepth);
                CorrectSpraySpawns(spawnArea, Prefs.Sandbox.OffsetMaxDepth, Prefs.Sandbox.OffsetMinDepth, _gameField.transform, _items.First());
            } else {
                SetSizes(1.66f - 0.2f); //for testing
                CorrectSpraySpawns(spawnArea, 0.2f, 0.2f, _gameField.transform, _items.First());
            }
            
            Spawn();
        }

        private void CorrectSpraySpawns(Transform spawnArea, float h, float minH, Transform field, Spray spray) {
            var hSpray = math.cmax(spray.transform.lossyScale);
            h = Mathf.Min(h + hSpray, hSpray * 3f) - h;
            var hVec = Vector3.up * h;
            hVec = field.InverseTransformVector(hVec);
            
            var spawnPos = spawnArea.localPosition;
            spawnPos.z = hVec.z;
            spawnArea.localPosition = spawnPos;

            var s = 0.5f - spawnPos.y;
            var sprayAngle = spray.GetSprayAngle();
            var a = MathHelper.RightTriangleAngle(Mathf.Abs(s), Mathf.Abs(hVec.z + minH)) - sprayAngle / 2f;
            var spawnRot = spawnArea.localEulerAngles;
            spawnRot.x = 90f - a;
            spawnArea.localEulerAngles = spawnRot;
        }

        private void SetSizes(float dist) {
            _gameField.AlignToCamera(_cam, dist);
            _gameField.SetWidth(0f);
            var size = _gameField.Scale * _initialItemSize;
            foreach (var item in _items) {
                item.transform.localScale = Vector3.one * size;
            }
        }
        
        private void ShowItems(bool show) {
            foreach (var item in _items) {
                item.Show(show);
            }
        }

        private void StartGame() {
            _isGameStarted = true;
            ShowItems(true);
        }

        private void StopGame() {
            _isGameStarted = false;
            ShowItems(false);
            _fired.Clear();
            _projector.Clear();
        }
    }
}