using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Games.Common;
using Games.Common.Game;
using Games.Common.GameFindObject;
using UnityEngine;
using Utilities;

namespace Games.Mahjong {
    public class GameMahjong : FindObjectGame {
        [SerializeField] private float _timeShowWrong = 1f;
        
        private readonly List<Card> _newOpened = new List<Card>();
        private int _showingWrong;

        protected override void Start() {
            base.Start();
            Card.OnShowed += OnCardShowed;
        }

        protected override void OnDestroy() {
            Card.OnShowed -= OnCardShowed;
            base.OnDestroy();
        }

        protected override IEnumerator Spawning() {
            foreach (var item in _tplItems) {
                var color = item.GetComponentInChildren<RandomColorRenderer>();
                color.SetRandomColor();
            }
            var tpls = _tplItems.Concat(_tplItems).ToList();
            foreach (var spawnArea in SpawnArea.Areas) {
                foreach (var spawn in spawnArea.Spawns) {
                    var tpl = tpls.Random();
                    tpls.Remove(tpl);
                    var newItem = Instantiate(tpl, spawn.position, spawn.rotation, tpl.transform.parent);
                    newItem.gameObject.SetActive(true);
                    _items.Add(newItem);
                }
            }
            yield break;
        }

        protected override void OnFireItem(Interactable item, Vector2 viewPos) {
            if (_showingWrong == 0 && _newOpened.Count < 2) {
                item.Show(true);
                _newOpened.Add((Card) item);
            }
        }

        private void OnCardShowed(Card obj) {
            if (_newOpened.Count == 2) {
                if (_newOpened[0].ItemType == _newOpened[1].ItemType) {
                    ++GameScore.Score;
                    obj.Bang(true);
                    if (GameScore.Score >= _tplItems.Length) {
                        GameEvent.Current = GameState.STOP;
                    }
                } else {
                    StartCoroutine(ShowingWrong(_newOpened.ToArray()));
                }
                _newOpened.Clear();
            }
        }

        private IEnumerator ShowingWrong(Card[] toArray) {
            ++_showingWrong;
            yield return new WaitForSeconds(_timeShowWrong);
            foreach (var card in toArray) {
                if (card != null)
                    card.Show(false);
            }
            --_showingWrong;
        }

        protected override void StartGame() {
            _showingWrong = 0;
            _newOpened.Clear();
            base.StartGame();
        }
    }
}