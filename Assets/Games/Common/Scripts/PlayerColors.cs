using System;
using UnityEngine;
using UnityEngine.UI;

namespace Games.Common {
    public class PlayerColors : MonoBehaviour {
        [Serializable]
        public class PlayerColor {
            [SerializeField] public Color color;
            [SerializeField] public Graphic[] graphics = {};
        }

        [SerializeField] private PlayerColor[] _colors = {};

        private void Awake() {
            foreach (var player in _colors) {
                foreach (var graphic in player.graphics) {
                    graphic.color *= player.color;
                }
            }
        }
    }
}