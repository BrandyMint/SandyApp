using System;
using Games.Common.Game;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Games.Common.GameFindObject {
    public class RandomChooseItemOnGameStart : MonoBehaviour {
        [Serializable] public class ObjectsList {
            public GameObject[] items = {};
        }
        [SerializeField] private ObjectsList[] _types = { };
        
        public static RandomChooseItemOnGameStart Instance { get; private set; }
        
        public int ItemId { get; private set; }

        private void Awake() {
            Instance = this;

            GameEvent.OnCountdown += OnCountdown;
        }

        private void OnDestroy() {
            GameEvent.OnCountdown -= OnCountdown;
        }

        private void OnCountdown() {
            ItemId = Random.Range(0, _types.Length);
            for (int i = 0; i < _types.Length; ++i) {
                foreach (var item in _types[i].items) {
                    item.SetActive(i == ItemId);
                }
            }
        }
    }
}