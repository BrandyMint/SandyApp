using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DepthSensorCalibration;
using DepthSensorSandbox.Visualisation;
using Games.Balloons;
using Games.Common.Game;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using Utilities;
using Random = UnityEngine.Random;

namespace Games.Moles {
    public class MolesGame : MonoBehaviour {
        [SerializeField] private Camera _cam;
        [SerializeField] private Mole _tplMole;
        [SerializeField] private GameField _gameField;
        [SerializeField] private int _maxMolesShow = 3;
        [SerializeField] private float _timeOffsetShown = 0.5f;
        [SerializeField] private float _timeLife = 1f;
        [SerializeField] private int _depthHeight = 64;
        [SerializeField] private SandboxMesh _sandbox;
        [SerializeField] private Material _matDepth;

        private readonly List<Mole> _moles = new List<Mole>();
        
        private int _hitMask;
        private CameraRenderToTexture _renderDepth;
        private readonly DelayedDisposeNativeArray<half4> _depth = new DelayedDisposeNativeArray<half4>();
        private int2 _depthSize; 
        private float _initialMoleSize;
        private int _score;
        private bool _isGameStarted;

        private void Start() {
            _hitMask = LayerMask.GetMask("interactable");

            _renderDepth = _cam.gameObject.AddComponent<CameraRenderToTexture>();
            _renderDepth.MaxResolution = _depthHeight;
            _renderDepth.Enable(_matDepth, RenderTextureFormat.ARGBHalf, OnNewDepthFrame, CreateCommandBufferDepth);

            _initialMoleSize = math.cmax(_tplMole.transform.localScale);
            _tplMole.gameObject.SetActive(false);
            
            Prefs.Calibration.OnChanged += OnCalibrationChanged;
            Prefs.Sandbox.OnChanged += OnCalibrationChanged;
            OnCalibrationChanged();

            GameEvent.OnStart += StartGame;
            GameEvent.OnStop += StopGame;
            
            SpawnAll();
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

        private void SpawnAll() {
            var areas = SpawnArea.Areas.OrderBy(t => t.transform.localPosition.x);
            var player = 0;
            foreach (var area in areas) {
                foreach (var spawn in area.Spawns) {
                    _moles.Add(SpawnMole(player, spawn.position, spawn.rotation));
                }
                ++player;
            }
        }

        private Mole SpawnMole(int player, Vector3 worldPos, Quaternion worldRot) {
            var newMole = Instantiate(_tplMole, worldPos, worldRot, _tplMole.transform.parent);
            newMole.Player = player;
            newMole.gameObject.SetActive(true);
            return newMole;
        }

        private IEnumerator MolesShowing(int player) {
            var moles = _moles.Where(m => m.Player == player).ToArray();
            yield return new WaitForSeconds(Random.value * _timeOffsetShown);
            while (true) {
                var countShown = moles.Count(m => m.State == MoleState.SHOWED);
                if (countShown < _maxMolesShow) {
                    if (moles.Where(m => m.State == MoleState.HIDED).TryRandom(out var mole)) {
                        mole.Show(_timeLife);
                    }
                }
                yield return new WaitForSeconds((0.5f + Random.value) * _timeOffsetShown);
            }
        }

        private void Update() {
            if (Input.GetMouseButtonDown(0)) {
                var screen = new float2(_cam.pixelWidth, _cam.pixelHeight);
                var pos = new float2(Input.mousePosition.x, Input.mousePosition.y);
                Fire(pos / screen);
            }
        }

        private void Fire(Vector2 viewPos) {
            if (!_isGameStarted) return;
            
            var ray = _cam.ViewportPointToRay(viewPos);
            if (Physics.Raycast(ray, out var hit, _cam.farClipPlane, _hitMask)) {
                var mole = hit.collider.GetComponent<Mole>();
                if (mole != null) {
                    ++GameScore.PlayerScore[mole.Player];
                    mole.Bang();
                }
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
                    if (_depth.o[x + y * _depthSize.x].y > 0) {
                        Fire(new float2(x, y) / _depthSize);
                    }
                }
            }

            foreach (var mole in _moles) {
                _gameField.PlaceOnSurface(mole.transform, GetDepth);
            }
        }

        private float GetDepth(Vector2 viewPos) {
            var p = (int2)((float2)viewPos * _depthSize);
            p = math.clamp(p, int2.zero, _depthSize - new int2(1, 1));
            return _depth.o[p.x + p.y * _depthSize.x].x;
        }

        private void OnCalibrationChanged() {
            var cam = _cam.GetComponent<SandboxCamera>();
            if (cam != null) {
                cam.OnCalibrationChanged();
                SetSizes(Prefs.Sandbox.ZeroDepth);
            } else {
                SetSizes(1.66f); //for testing
            }
        }

        private void SetSizes(float dist) {
            var verticalSize = MathHelper.IsoscelesTriangleSize(dist, _cam.fieldOfView);
            var size = verticalSize * _initialMoleSize;
            _tplMole.transform.localScale = Vector3.one * size;
            foreach (var mole in _moles) {
                mole.transform.localScale = Vector3.one * size;
            }
            _gameField.AlignToCamera(_cam, dist);
            _gameField.SetWidth(0f);
        }

        private void ResetMoles() {
            foreach (var moles in _moles) {
                moles.Hide(true);
            }
        }

        private void StartGame() {
            ResetMoles();
            _isGameStarted = true;
            for (int i = 0; i < _moles.Count; ++i) {
                GameScore.PlayerScore[i] = 0;
                StartCoroutine(nameof(MolesShowing), i);
            }
            
        }

        private void StopGame() {
            _isGameStarted = false;
            StopCoroutine(nameof(MolesShowing));
        }
    }
}