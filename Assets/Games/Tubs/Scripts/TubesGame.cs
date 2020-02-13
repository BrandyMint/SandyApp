using System.Collections.Generic;
using Games.Common.Game;
using Games.Common.GameFindObject;
using Unity.Mathematics;
using UnityEngine;

namespace Games.Tubs {
    public class TubesGame : FindObjectGame {
        [SerializeField] private TubesGenerator _generator;
        
        
        private readonly HashSet<Interactable> _fixing = new HashSet<Interactable>();

        protected override void Start() {
            base.Start();
            GameEvent.OnCountdown += OnCountdown;
        }

        protected override void OnDestroy() {
            GameEvent.OnCountdown -= OnCountdown;
            base.OnDestroy();
        }

        private void OnCountdown() {
            ClearItems();
        }

        protected override void StartGame() {
            _generator.ReGenerate(_gameField.transform);
            _initialItemSize = _generator.TubeSale;
            foreach (var tpl in _tplItems) {
                tpl.transform.localScale = Vector3.one * _initialItemSize;
            }
            base.StartGame();
        }
        
        private void Update() {
            if (Input.GetMouseButton(0)) {
                var screen = new float2(_cam.pixelWidth, _cam.pixelHeight);
                var pos = new float2(Input.mousePosition.x, Input.mousePosition.y);
                Fire(pos / screen);
            }
        }
        
        protected override void ProcessDepthFrame() {
            base.ProcessDepthFrame();
            CheckStopFixing();
            _fixing.Clear();
        }

        protected override void OnFireItem(Interactable item, Vector2 viewPos) {
            item.Bang(true);
            _fixing.Add(item);
        }

        protected override void OnItemDestroyed(Interactable interactable) {
            base.OnItemDestroyed(interactable);
            _fixing.Remove(interactable);
            if (_isGameStarted)
                ++GameScore.Score;
        }
        
        private void CheckStopFixing() {
            foreach (var item in _items) {
                if (!_fixing.Contains(item))
                    item.Bang(false);
            }
        }
    }
}