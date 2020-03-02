using System.Collections.Generic;
using System.Linq;
using DepthSensor.Buffer;
using DepthSensor.Device;
using DepthSensorSandbox.Processing;
using Games.Common;
using Games.Common.ColliderGenerator;
using Games.Common.Game;
using Unity.Mathematics;
using UnityEngine;
using Utilities;

namespace Games.Flowers {
    public class GameFlowers : BaseGameWithHandsRaycast {
        [SerializeField] private PolygonCollider2D _handsCollider;
        [SerializeField] private PolygonCollider2D _mouseCollider;
        [SerializeField] private int _mouseDebugDataSize = 128;
        [SerializeField] private float _mouseDebugSize = 0.1f;
        [SerializeField] private int _maxCount = 20;
        [SerializeField] protected float _maxSpeed = 0.01f;
        [SerializeField] private float _maxHandsDepth = 0.1f;

        private float _defaultSpeed;
        
        private Texture2D _handsTexture;

        private readonly ColliderGenerator _colliderGenerator = new ColliderGenerator();
        private readonly DataMouse _colliderGeneratorDataMouse = new DataMouse();
        private readonly OutputPolygonCollider2DRaycaster _colliderGeneratorOutput = new OutputPolygonCollider2DRaycaster();
        private readonly OutputPolygonCollider2D _colliderGeneratorOutputMouse = new OutputPolygonCollider2D();

        private readonly List<Transform> _tplsFlower = new List<Transform>();
        private readonly List<Transform> _flowers = new List<Transform>();
        

        protected override void Start() {
            _defaultSpeed = Physics2D.baumgarteScale;
            
            foreach (Transform f in transform) {
                if (f.name.StartsWith("Flower")) {
                    _tplsFlower.Add(f);
                    f.gameObject.SetActive(false);
                }
            }
            SaveInitialSizes(_tplsFlower);
            _colliderGeneratorOutput.collider = _handsCollider;
            _colliderGeneratorOutputMouse.collider = _mouseCollider;
            
            _testMouseModeHold = true;
            
            base.Start();
            SetCustomMaxHandDepth(_maxHandsDepth);
            _handsRaycaster.HandFire -= Fire;
            _handsRaycaster.CustomProcessingFrame += ProcessDepthFrame;
        }

        protected override HandsRaycaster CreateHandsRaycaster() {
            var raycaster = base.CreateHandsRaycaster();
            _colliderGeneratorOutput.Raycaster = raycaster;
            return raycaster;
        }

        protected override void OnDestroy() {
            Physics2D.baumgarteScale = _defaultSpeed;
            base.OnDestroy();
        }

        protected override void SetSizes(float dist) {
            base.SetSizes(dist);
            SetSizes(_gameField.Scale, _tplsFlower);
            Physics2D.baumgarteScale = _maxSpeed * _gameField.Scale;
        }

        protected override void StartGame() {
            _colliderGeneratorOutput.Clear();
            SpawnFlowers();
            base.StartGame();
        }

        protected override void StopGame() {
            _colliderGeneratorOutput.Clear();            
            base.StopGame();
            Clear();
        }

        private void SpawnFlowers() {
            var size = math.cmax(_tplsFlower.First().localScale);
            var z = 0f;
            var zoffset = size / _maxCount;
            for (int i = 0; i < _maxCount; ++i) {
                var tpl = _tplsFlower.Random();
                var stayAway = _flowers.Select(f => f.position).ToArray();
                
                if (SpawnArea.AnyGetRandomSpawn(out var p, out var r, stayAway, size / 2f)) {
                    var f = Instantiate(tpl, tpl.transform.parent, false);
                    z += zoffset;
                    p.z += z;
                    f.position = p;
                    f.rotation = r;
                    _flowers.Add(f);
                    f.gameObject.SetActive(true);
                }
            }
        }

        private void Clear() {
            foreach (var f in _flowers) {
                Destroy(f.gameObject);
            }
            _flowers.Clear();
        }

        protected override void Update() { }

        protected virtual void FixedUpdate() {
            if (Input.GetMouseButton(0) && _isGameStarted) {
                _colliderGeneratorDataMouse.CircleSize = _mouseDebugSize * _mouseDebugDataSize;
                var dataSize = new Vector2Int((int) (_mouseDebugDataSize * _cam.aspect), _mouseDebugDataSize);
                var sampler = Sampler.Create(dataSize.x, dataSize.y);
                _colliderGeneratorDataMouse.Sampler = sampler;
                _colliderGeneratorOutputMouse.SourceRect = sampler.GetRect(); 
                
                _colliderGeneratorDataMouse.MousePos = new Vector2(
                    (int) (Input.mousePosition.x / _cam.pixelWidth * dataSize.x),
                    (int) (Input.mousePosition.y / _cam.pixelHeight * dataSize.y)
                );
                
                _colliderGeneratorOutputMouse.Clear();
                _colliderGenerator.Generate(_colliderGeneratorDataMouse, _colliderGeneratorOutputMouse);
                _mouseCollider.enabled = !_colliderGeneratorOutputMouse.IsEmpty();
            } else {
                if (!_colliderGeneratorOutputMouse.IsEmpty()) {
                    _colliderGeneratorOutputMouse.Clear();
                    _mouseCollider.enabled = false;
                }
            }
        }

        private void ProcessDepthFrame(DepthSensorDevice device, HandsProcessing processor, DepthBuffer hands, Sampler sampler) {
            if (!_isGameStarted) return;

            _colliderGeneratorOutput.Clear();
            _colliderGenerator.Generate(_handsRaycaster, _colliderGeneratorOutput);
            _handsCollider.enabled = !_colliderGeneratorOutput.IsEmpty();
        }
    }
}