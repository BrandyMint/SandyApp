using System.Linq;
using Games.Common;
using Games.Common.Game;
using Games.Common.GameFindObject;
using Unity.Mathematics;
using UnityEngine;

namespace Games.Planet {
    public class GamePlanet : FindObjectGame {
        [SerializeField] private Planet _planet;
        [SerializeField] private int _maxPlanetHP;
        [SerializeField] private CircleHP _planetHP;

        protected override void Start() {
            base.Start();
            Bullet.OnCollide += OnBulletCollide;
        }

        protected override void StartGame() {
            base.StartGame();
            _planetHP.Max = _maxPlanetHP;
            _planetHP.Val = _maxPlanetHP;
        }

        protected override void OnDestroy() {
            Bullet.OnCollide -= OnBulletCollide;
            base.OnDestroy();
        }

        protected override void SetSizes(float dist) {
            base.SetSizes(dist);
            _planet.UpdateSize(_gameField);
        }

        protected override void OnFireItem(Interactable item, Vector2 viewPos) {
            ++GameScore.Score;
            item.Bang(true);
        }

        protected override Interactable SpawnItem(Interactable tpl) {
            var stayAway = _items.Cast<Ship>().Select(b => b.Spawn).ToArray();
            var stayAwayDist = math.cmax(tpl.transform.localScale);
            if (SpawnArea.AnyGetRandomSpawn(out var worldPos, out var worldRot, stayAway, stayAwayDist)) {
                var freeZones = _planet.FlyZones.Where(z => _items.Cast<Ship>().All(s => s.FlyZone != z));
                Transform flyZone = null;
                float minDist = float.MaxValue;
                foreach (var zone in freeZones) {
                    var newDist = Vector3.Distance(worldPos, zone.position);
                    if (newDist < minDist) {
                        flyZone = zone;
                        minDist = newDist;
                    }
                }

                if (flyZone == null)
                    return null;
                
                var newItem = Instantiate(tpl, worldPos, worldRot, tpl.transform.parent);
                newItem.gameObject.SetActive(true);
                ((Ship) newItem).FlyZone = flyZone;
                _items.Add(newItem);
                
                return newItem;
            }

            return null;
        }

        private void OnBulletCollide(Bullet b, Collision coll) {
            --_planetHP.Val;
            if (_planetHP.Val <= 0) {
                GameEvent.Current = GameState.STOP;
            }
        }
    }
}