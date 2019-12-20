﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DepthSensorCalibration;
using DepthSensorSandbox.Visualisation;
using Games.Common;
using Games.Common.Game;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using Utilities;

namespace Games.Balloons {
    public class BalloonsGame : MonoBehaviour {
        [SerializeField] private Camera _cam;
        [SerializeField] private Balloon _tplBalloon;
        [SerializeField] private BalloonsGameField _gameField;
        [SerializeField] private float _maxBallons = 12;
        [SerializeField] private float _timeOffsetSpown = 2f;
        [SerializeField] private float _startForce = 3f;
        [SerializeField] private float _explosionForceMult = 2f;
        [SerializeField] private float _explosionRadiusMult = 3f;
        [SerializeField] private int _depthHeight = 64;
        [SerializeField] private SandboxMesh _sandbox;
        [SerializeField] private Material _matDepth;

        private List<Balloon> _balloons = new List<Balloon>();
        
        private int _hitMask;
        private CameraRenderToTexture _renderDepth;
        private readonly DelayedDisposeNativeArray<byte> _depth = new DelayedDisposeNativeArray<byte>();
        private int2 _depthSize; 
        private float _initialBallSize;
        private int _score;
        private bool _isGameStarted;

        private void Start() {
            _hitMask = LayerMask.GetMask("interactable");

            _renderDepth = _cam.gameObject.AddComponent<CameraRenderToTexture>();
            _renderDepth.InvokesOnlyOnProcessedFrame = true;
            _renderDepth.MaxResolution = _depthHeight;
            _renderDepth.Enable(_matDepth, RenderTextureFormat.R8, OnNewDepthFrame, CreateCommandBufferDepth);

            _initialBallSize = math.cmax(_tplBalloon.transform.localScale);
            _tplBalloon.gameObject.SetActive(false);

            Balloon.OnDestroyed += OnBalloonDestroyed;
            Balloon.OnCollisionEntered += OnBalloonCollisionEnter;
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
            Balloon.OnCollisionEntered -= OnBalloonCollisionEnter;
            Balloon.OnDestroyed -= OnBalloonDestroyed;
            if (_renderDepth != null) {
                _renderDepth.Disable();
            }
            _depth.Dispose();
        }

        private IEnumerator Spawning() {
            while (true) {
                if (_balloons.Count < _maxBallons) {
                    SpawnBalloon();
                }
                yield return new WaitForSeconds(_timeOffsetSpown);
            }
        }

        private void SpawnBalloon() {
            var stayAway = _balloons.Select(b => b.transform.position).ToArray();
            var stayAwayDist = math.cmax(_tplBalloon.transform.localScale) * 1.5f;
            if (SpawnArea.AnyGetRandomSpawn(out var worldPos, out var worldRot, stayAway, stayAwayDist)) {
                var newBalloon = Instantiate(_tplBalloon, worldPos, worldRot, _tplBalloon.transform.parent);
                
                var force = newBalloon.GetComponent<ConstantForce>();
                force.force *= new float3(_gameField.transform.localScale);
                    
                var rigid = newBalloon.GetComponent<Rigidbody>();
                newBalloon.gameObject.SetActive(true);
                var dir = newBalloon.transform.rotation * Vector3.forward;
                rigid.AddForce(_startForce * Mathf.Abs(_gameField.transform.localScale.y) * dir);
                _balloons.Add(newBalloon);
            }
        }

        private void OnBalloonDestroyed(Balloon balloon) {
            _balloons.Remove(balloon);
        }

        private void OnBalloonCollisionEnter(Balloon balloon, Collision collision) {
            if (collision.collider == _gameField.ExitBorder) {
                balloon.Dead();
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
                var balloon = hit.collider.GetComponent<Balloon>();
                if (balloon != null) {
                    ++GameScore.Score;
                    balloon.Bang();
                    var force =  Mathf.Abs(_gameField.transform.localScale.y) * _startForce * _explosionForceMult;
                    var radius = math.cmax(balloon.transform.lossyScale) * _explosionRadiusMult;
                    var pos = balloon.transform.position;
                    foreach (var b in _balloons) {
                        if (b != balloon) {
                            b.GetComponent<Rigidbody>().AddExplosionForce(force, pos, radius);
                        }
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
                SetSizes(2f); //for testing
            }
        }

        private void SetSizes(float dist) {
            var size = _gameField.Scale * _initialBallSize;
            _tplBalloon.transform.localScale = Vector3.one * size;
            _gameField.AlignToCamera(_cam, dist);
            _gameField.SetWidth(size * 2f);
        }

        private void ClearBalls() {
            foreach (var balloon in _balloons) {
                balloon.Dead();
            }
            _balloons.Clear();
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