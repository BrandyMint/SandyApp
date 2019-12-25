﻿using System.Collections;
using System.Linq;
using Games.Common;
using Games.Common.Game;
using Games.Common.GameFindObject;
using Unity.Mathematics;
using UnityEngine;
using Utilities;
using Random = UnityEngine.Random;

namespace Games.Eatable {
    public class EatableGame : FindObjectGame {
        [SerializeField] private float _addForce = 1f;
        [SerializeField] private float _lifeTime = 2f;
        [SerializeField] private float _itemMass = 1f;

        protected override IEnumerator Spawning() {
            var types = _tplItems.Select(i => i.ItemType).Distinct().ToArray();
            while (true) {
                if (_items.Count < _maxItems) {
                    var type = types.Random();
                    var item = SpawnItem(_tplItems.Where(i => i.ItemType == type).Random());
                    if (item != null) {
                        var lifeTime = item.GetComponent<LifeTime>();
                        lifeTime.time = _lifeTime;
                        lifeTime.enabled = true;
                        
                        var rigid = item.GetComponent<Rigidbody>();
                        rigid.mass *= _itemMass;
                        var force = item.transform.rotation * Vector3.forward;
                        force *= new float3(_gameField.WorldSize) * _addForce;
                        rigid.AddForce(force, ForceMode.Impulse);
                        item.transform.rotation = Random.rotation;
                    }
                }
                yield return new WaitForSeconds(Random.Range(0.5f, 1.5f) * _timeOffsetSpown);
            }
        }

        protected override void OnFireItem(Interactable item, Vector2 viewPos) {
            var player = _gameField.PlayerField(viewPos);
            if (player >= 0) {
                GameScore.PlayerScore[player] = Mathf.Clamp(GameScore.PlayerScore[player] + item.ItemType,
                    0, int.MaxValue);
                
                if (item.ItemType > 0) {
                    item.Bang(true);
                } else {
                    item.Bang(false);
                }
                item.GetComponent<LifeTime>().enabled = false;
            }
        }
    }
}