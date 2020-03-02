using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Games.Common;
using Games.Common.Game;
using Games.Common.GameFindObject;
using Unity.Mathematics;
using UnityEngine;
using Utilities;
using Random = UnityEngine.Random;

namespace Games.Moles {
    public class MolesGame : BaseGameWithHandsRaycast {
        [SerializeField] private Mole _tplMole;
        [SerializeField] private int _maxMolesShow = 3;
        [SerializeField] private float _timeOffsetShown = 0.5f;
        [SerializeField] private float _timeLife = 1f;

        private readonly List<Mole> _moles = new List<Mole>();
        private float _initialMoleSize;
        private int _score;

        protected override void Start() {
            _initialMoleSize = math.cmax(_tplMole.transform.localScale);
            _tplMole.gameObject.SetActive(false);
            
            base.Start();
        }

        private void SpawnOrRespawnAll() {
            var areas = SpawnArea.Areas.OrderBy(t => t.transform.localPosition.x);
            var player = 0;
            var i = 0;
            foreach (var area in areas) {
                foreach (var spawn in area.Spawns) {
                    if (i < _moles.Count) {
                        var mole = _moles[i];
                        mole.transform.position = spawn.position;
                        mole.transform.rotation = spawn.rotation;
                        mole.ItemType = player;
                    } else {
                        _moles.Add(SpawnMole(player, spawn.position, spawn.rotation));
                    }
                    
                    ++i;
                }
                ++player;
            }
        }

        private Mole SpawnMole(int player, Vector3 worldPos, Quaternion worldRot) {
            var newMole = Instantiate(_tplMole, worldPos, worldRot, _tplMole.transform.parent);
            newMole.ItemType = player;
            newMole.gameObject.SetActive(true);
            return newMole;
        }

        private IEnumerator MolesShowing(int player) {
            var moles = _moles.Where(m => m.ItemType == player).ToArray();
            yield return new WaitForSeconds(Random.value * _timeOffsetShown);
            while (true) {
                var countShown = moles.Count(m => m.State == MoleState.SHOWED);
                if (countShown < _maxMolesShow) {
                    if (moles.Where(m => m.State == MoleState.HIDED).TryRandom(out var mole)) {
                        mole.Show(_timeLife);
                    }
                }
                yield return new WaitForSeconds((0.5f + Random.value) * _timeOffsetShown);
            }
        }
        
        protected override void OnFireItem(IInteractable item, Vector2 viewPos) {
            ++GameScore.PlayerScore[item.ItemType];
            item.Bang(true);
        }

        protected override void SetSizes(float dist) {
            base.SetSizes(dist);
            var size = _gameField.Scale * _initialMoleSize;
            _tplMole.transform.localScale = Vector3.one * size;
            SpawnOrRespawnAll();
            foreach (var mole in _moles) {
                mole.transform.localScale = Vector3.one * size;
            }
        }

        private void ResetMoles() {
            foreach (var moles in _moles) {
                moles.Hide(true);
            }
        }

        protected override void StartGame() {
            SpawnOrRespawnAll();
            ResetMoles();
            for (int i = 0; i < _moles.Count; ++i) {
                StartCoroutine(nameof(MolesShowing), i);
            }
            base.StartGame();
        }

        protected override void StopGame() {
            StopCoroutine(nameof(MolesShowing));
            base.StopGame();
        }
    }
}