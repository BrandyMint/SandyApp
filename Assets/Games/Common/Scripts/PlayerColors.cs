using System;
using System.Collections.Generic;
using System.Linq;
using Games.Common.Game;
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

        public static PlayerColors Instance { get; private set; }

        public IEnumerable<Color> Colors => _colors.Select(c => c.color);

        public int Count => _colors.Length;

        private readonly List<List<Color>> _startColors = new List<List<Color>>();  

        private void Awake() {
            Instance = this;
            foreach (var playerColor in _colors) {
                var startGraphicsColors = playerColor.graphics.Select(graphic => graphic.color).ToList();
                _startColors.Add(startGraphicsColors);
            }

            Prefs.App.OnChanged += UpdateColors;
            UpdateColors();
        }

        private void OnDestroy() {
            Prefs.App.OnChanged -= UpdateColors;
        }

        private void UpdateColors() {
            for (int i = 0; i < _colors.Length; ++i) {
                var flipped = GameScore.GetPlayerAfterFlip(i);
                var graphics = _colors[flipped].graphics;
                var color = _colors[i].color;

                var j = 0;
                foreach (var graphic in graphics) {
                    graphic.color = _startColors[flipped][j++] * color;
                }
            }
        }
    }
}