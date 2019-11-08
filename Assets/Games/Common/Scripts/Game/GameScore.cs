using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Games.Common.Game {
    public class GameScore : MonoBehaviour {
        [SerializeField] private Text _txtScore;
        [SerializeField] private Text[] _txtPlayerScores = { };

        public class PlayerScores {
            private readonly Dictionary<int, int> _scores = new Dictionary<int, int>();
            
            public int this[int i] {
                get {
                    if (_scores.TryGetValue(i, out var score)) {
                        return score;
                    }
                    return 0;
                }
                set {
                    _scores[i] = value;
                    UpdateScores();
                }
            }
        }

        public static int Score {
            get => _score;
            set {
                if (_score != value) {
                    _score = value;
                    UpdateScores();
                }
            }
        }

        public static readonly PlayerScores PlayerScore = new PlayerScores();

        private static readonly List<GameScore> _instances = new List<GameScore>();
        private static int _score;

        private void Awake() {
            _instances.Add(this);
            UpdateScore();
        }

        private void OnDestroy() {
            _instances.Remove(this);
        }

        private void UpdateScore() {
            if (_txtScore != null)
                _txtScore.text = Score.ToString();
            for (int i = 0; i < _txtPlayerScores.Length; ++i) {
                _txtPlayerScores[i].text = PlayerScore[i].ToString();
            }
        }

        private static void UpdateScores() {
            foreach (var instance in _instances) {
                instance.UpdateScore();
            }
        }
    }
}