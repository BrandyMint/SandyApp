﻿using System.Collections;
using System.Linq;
using Games.Common;
using Games.Common.Game;
using Games.Common.GameFindObject;
using UnityEngine;
using UnityEngine.UI;

namespace Games.Slimes {
    public class GameSlimes : FindObjectGame {
        [SerializeField] private Text _txtTargetColor;
        [SerializeField] private SlimeColor[] _colors = { };

        private int _targetColorType;
        private Color _targetColor;

        protected override void Start() {
            base.Start();
            Slime.OnNeedNewColor += SetSlimeColor;
        }

        protected override void OnDestroy() {
            Slime.OnNeedNewColor -= SetSlimeColor;
            base.OnDestroy();
        }

        protected override void StartGame() {
            GetRandomColor(out var color, out _targetColorType);
            _targetColor = color.color;
            _txtTargetColor.text = color.name;
            
            base.StartGame();
        }

        protected override IEnumerator Spawning() {
            foreach (var area in SpawnArea.Areas) {
                foreach (var spawn in area.Spawns) {
                    SpawnItem(_tplItems.First());
                }
            }
            yield break;
        }

        protected override void OnFireItem(IInteractable item, Vector2 viewPos) {
            if (item.ItemType == _targetColorType) {
                ++GameScore.Score;
                item.Bang(true);
            } else {
                GameScore.Score = Mathf.Max(0, GameScore.Score - 1);
                item.Bang(false);
            }
        }

        private void SetSlimeColor(Slime slime) {
            var c = _targetColor;
            var id = _targetColorType;
            if (_items.Cast<Slime>().Any(i => !i.IsSmashed && i.ItemType == _targetColorType)) {
                do {
                    GetRandomColor(out var color, out id);
                    c = color.color;
                } while (slime.ItemType == id);
            }

            slime.SetColor(c);
            slime.ItemType = id;
        }

        private void GetRandomColor(out SlimeColor color, out int id) {
            id = Random.Range(0, _colors.Length);
            color = _colors[id];
        }
    }
}