using System;
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
        [Serializable]
        public class TexturePack {
            public List<Texture> textures;
        }
        
        [SerializeField] private float _timeShowWrong = 1f;
        [SerializeField] private TexturePack[] _texturePacks; 
        
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
            var pack = _texturePacks.Random();
            var textures = pack.textures.Concat(pack.textures).ToList();
            var tpl = _tplItems.First();
            foreach (var spawnArea in SpawnArea.Areas) {
                foreach (var spawn in spawnArea.Spawns) {
                    var t = textures.Random();
                    textures.Remove(t);
                    var newItem = (Card) Instantiate(tpl, spawn.position, spawn.rotation, tpl.transform.parent);
                    newItem.gameObject.SetActive(true);
                    newItem.GetComponentInChildren<Renderer>();
                    newItem.ItemType = pack.textures.IndexOf(t);
                    newItem.SetTexture(t);
                    _items.Add(newItem);
                }
            }
            yield break;
        }

        protected override void OnFireItem(IInteractable item, Vector2 viewPos) {
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
                    if (GameScore.Score >= _items.Count / 2) {
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