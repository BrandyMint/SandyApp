using System.Collections;
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
        [SerializeField] private Texture2D _fieldTexture;

        protected override IEnumerator Spawning() {
            while (true) {
                if (_items.Count < _maxItems) {
                    var item = SpawnItem(_tplItems.Random());
                    if (item != null) {
                        var lifeTime = item.GetComponent<LifeTime>();
                        lifeTime.time = _lifeTime;
                        lifeTime.enabled = true;
                        
                        var rigid = item.GetComponent<Rigidbody>();
                        rigid.mass *= _itemMass;
                        var force = _addForce * math.cmax(item.transform.lossyScale) 
                                              * new Vector3(_cam.pixelHeight, 0, _cam.pixelWidth).normalized;
                        rigid.AddForce(item.transform.rotation * force, ForceMode.Impulse);
                    }
                }
                yield return new WaitForSeconds(Random.Range(0.5f, 1.5f) * _timeOffsetSpown);
            }
        }

        protected override void OnFireItem(Interactable item, Vector2 viewPos) {
            var c = _fieldTexture.GetPixel((int) (_fieldTexture.width * viewPos.x), (int) (_fieldTexture.height * viewPos.y));
            if (c.a < 1f) {
                var player = Mathf.RoundToInt(c.a * GameScore.PlayerScore.Count);
                GameScore.PlayerScore[player] = Mathf.Clamp(GameScore.PlayerScore[player] + item.ItemType,
                    0, int.MaxValue);
                
                if (item.ItemType > 0) {
                    item.Bang(true);
                } else {
                    item.Bang(false);
                }
            }
        }

        protected override void StartGame() {
            for (int player = 0; player < GameScore.PlayerScore.Count; ++player) {
                GameScore.PlayerScore[player] = 0;
            }
            base.StartGame();
        }
    }
}