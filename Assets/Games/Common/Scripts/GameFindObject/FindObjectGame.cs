﻿using System.Collections;
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

namespace Games.Common.GameFindObject {
    public class FindObjectGame : MonoBehaviour {
        [SerializeField] private Camera _cam;
        [SerializeField] private Interactable[] _tplItems;
        [SerializeField] private GameField _gameField;
        [SerializeField] private int _maxItems = 9;
        [SerializeField] private float _minItemTypeFullnes = 0.7f;
        [SerializeField] private float _timeOffsetSpown = 1f;
        [SerializeField] private int _depthHeight = 64;
        [SerializeField] private SandboxMesh _sandbox;
        [SerializeField] private Material _matDepth;

        private List<Interactable> _items = new List<Interactable>();
        
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
            _renderDepth.Enable(_matDepth, RenderTextureFormat.R8, OnNewDepthFrame, CreateCommandBufferDepth);

            _initialItemSize = math.cmax(_tplItems.First().transform.localScale);
            foreach (var item in _tplItems) {
                item.gameObject.SetActive(false);
            }

            Interactable.OnDestroyed += OnItemDestroyed;
            Prefs.Calibration.OnChanged += OnCalibrationChanged;
            Prefs.Sandbox.OnChanged += OnCalibrationChanged;
            OnCalibrationChanged();

            GameEvent.OnStart += StartGame;
            GameEvent.OnStop += StopGame;
        }

        private void OnDestroy() {
            GameEvent.OnStart -= StartGame;
            GameEvent.OnStop -= StopGame;
            
            Prefs.Sandbox.OnChanged -= OnCalibrationChanged;
            Prefs.Calibration.OnChanged -= OnCalibrationChanged;
            Interactable.OnDestroyed -= OnItemDestroyed;
            if (_renderDepth != null) {
                _renderDepth.Disable();
            }
            _depth.Dispose();
        }

        private IEnumerator Spawning() {
            var itemTypes = _tplItems.Select(i => i.ItemType).ToArray();

            for (int i = 0; i < _maxItems / 2; ++i) {
                var type = itemTypes.Random();
                SpawnItem(_tplItems.First(it => it.ItemType == type));
            }
            
            while (true) {
                if (_items.Count < _maxItems) {
                    var minCount = int.MaxValue;
                    var spownType = -1;
                    foreach (var type in itemTypes) {
                        var count = _items.Count(i => i.ItemType == type);
                        if (count < minCount) {
                            minCount = count;
                            spownType = type;
                        }
                    }

                    if (spownType < 0 || minCount / ((float)_items.Count / itemTypes.Length) > _minItemTypeFullnes) {
                        spownType = itemTypes.Random();
                    }

                    SpawnItem(_tplItems.First(i => i.ItemType == spownType));
                }
                yield return new WaitForSeconds(_timeOffsetSpown);
            }
        }

        private void SpawnItem(Interactable tpl) {
            var stayAway = _items.Select(b => b.transform.position).ToArray();
            var stayAwayDist = math.cmax(tpl.transform.localScale);
            if (SpawnArea.AnyGetRandomSpawn(out var worldPos, out var worldRot, stayAway, stayAwayDist)) {
                var newItem = Instantiate(tpl, worldPos, worldRot, tpl.transform.parent);
                newItem.gameObject.SetActive(true);
                _items.Add(newItem);
            }
        }

        private void OnItemDestroyed(Interactable interactable) {
            _items.Remove(interactable);
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
                var item = hit.collider.GetComponent<Interactable>();
                if (item != null) {
                    var neededType = RandomChooseItemOnGameStart.Instance.ItemId;
                    if (item.ItemType == neededType) {
                        ++GameScore.Score;
                        item.Bang(true);
                    } else {
                        item.Bang(false);
                    }
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
                    if (_depth.o[x + y * _depthSize.x] > 0) {
                        Fire(new float2(x, y) / _depthSize);
                    }
                }
            }
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
            var size = verticalSize * _initialItemSize;
            foreach (var item in _tplItems.Concat(_items)) {
                item.transform.localScale = Vector3.one * size;
            }
            _gameField.AlignToCamera(_cam, dist);
            _gameField.SetWidth(size);
        }

        private void ClearBalls() {
            foreach (var balloon in _items) {
                balloon.Dead();
            }
            _items.Clear();
        }

        private void StartGame() {
            ClearBalls();
            GameScore.Score = 0;
            _isGameStarted = true;
            StartCoroutine(nameof(Spawning));
        }

        private void StopGame() {
            _isGameStarted = false;
            StopCoroutine(nameof(Spawning));
        }
    }
}