using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Games.Common.Game;
using Unity.Mathematics;
using UnityEngine;
using Utilities;

namespace Games.Common.GameFindObject {
    public class FindObjectGame : BaseGame {
        [SerializeField] protected Interactable[] _tplItems;
        [SerializeField] protected int _maxItems = 9;
        [SerializeField] private float _minItemTypeFullnes = 0.7f;
        [SerializeField] protected float _timeOffsetSpown = 1f;

        protected List<Interactable> _items = new List<Interactable>();
        
        protected float _initialItemSize;
        private int _score;

        protected override void Start() {
            _initialItemSize = math.cmax(_tplItems.First().transform.localScale);
            foreach (var item in _tplItems) {
                item.gameObject.SetActive(false);
            }
            
            base.Start();

            Interactable.OnDestroyed += OnItemDestroyed;
        }

        protected override void OnDestroy() {
            Interactable.OnDestroyed -= OnItemDestroyed;
            base.OnDestroy();
        }

        protected virtual IEnumerator Spawning() {
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

        protected virtual Interactable SpawnItem(Interactable tpl) {
            var stayAway = _items.Select(b => b.transform.position).ToArray();
            var stayAwayDist = math.cmax(tpl.transform.localScale);
            if (SpawnArea.AnyGetRandomSpawn(out var worldPos, out var worldRot, stayAway, stayAwayDist)) {
                var newItem = Instantiate(tpl, worldPos, worldRot, tpl.transform.parent);
                newItem.gameObject.SetActive(true);
                _items.Add(newItem);
                
                return newItem;
            }

            return null;
        }

        protected virtual void OnItemDestroyed(Interactable interactable) {
            _items.Remove(interactable);
        }

        protected override void OnFireItem(Interactable item, Vector2 viewPos) {
            var neededType = RandomChooseItemOnGameStart.Instance.ItemId;
            if (item.ItemType == neededType) {
                ++GameScore.Score;
                item.Bang(true);
            } else {
                item.Bang(false);
            }
        }

        protected override void SetSizes(float dist) {
            base.SetSizes(dist);
            var size = _gameField.Scale * _initialItemSize;
            foreach (var item in _tplItems.Concat(_items)) {
                item.transform.localScale = Vector3.one * size;
            }
            _gameField.SetWidth(size);
        }

        protected void ClearItems() {
            foreach (var item in _items) {
                item.Dead();
            }
            _items.Clear();
        }

        protected override void StartGame() {
            ClearItems();
            StartCoroutine(nameof(Spawning));
            base.StartGame();
        }

        protected override void StopGame() {
            base.StopGame();
            StopCoroutine(nameof(Spawning));
        }
    }
}