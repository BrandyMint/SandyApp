using System.Collections.Generic;
using Games.Common.Game;
using Games.Common.GameFindObject;
using UnityEngine;

namespace Games.Tubs {
    public class TubesGame : FindObjectGame {
        [SerializeField] private TubesGenerator _generator;
        
        
        private readonly HashSet<InteractableSimple> _fixing = new HashSet<InteractableSimple>();

        protected override void Start() {
            base.Start();
            GameEvent.OnCountdown += OnCountdown;
            _handsRaycaster.OnPostProcessDepthFrame += PostProcessDepthFrame;
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
        
        private void PostProcessDepthFrame() {
            CheckStopFixing();
            _fixing.Clear();
        }

        protected override void OnFireItem(IInteractable item, Vector2 viewPos) {
            item.Bang(true);
            _fixing.Add((InteractableSimple) item);
        }

        protected override void OnItemDestroyed(InteractableSimple interactable) {
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