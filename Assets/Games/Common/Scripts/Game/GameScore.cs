using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Games.Common.Game {
    public class GameScore : MonoBehaviour {
        [SerializeField] private Text _txtScore;
        [SerializeField] private Text[] _txtPlayerScores = { };
        [SerializeField] private int _horizontalPlayers = 2;

        public class PlayerScores {
            private readonly Dictionary<int, int> _scores = new Dictionary<int, int>();
            
            public int this[int i] {
                get {
                    if (_scores.TryGetValue(i, out var score)) {
                        return score;
                    }
                    _scores[i] = 0;
                    return 0;
                }
                set {
                    _scores[i] = value;
                    UpdateScores();
                }
            }

            public int Count => _scores.Count;

            public void Clear() {
                _scores.Clear();
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
        private static int _sHorizontalPlayers;

        private void Awake() {
            _sHorizontalPlayers = _horizontalPlayers;
            PlayerScore.Clear();
            _score = 0;
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
                _txtPlayerScores[i].text = PlayerScore[GetPlayerAfterFlip(i)].ToString();
            }
        }

        private static void UpdateScores() {
            foreach (var instance in _instances) {
                instance.UpdateScore();
            }
        }

        public static int GetWinner() {
            var max = int.MinValue;
            var winner = -1;
            for (int i = 0; i < PlayerScore.Count; ++i) {
                var score = PlayerScore[i];
                if (score > max) {
                    max = score;
                    winner = i;
                } else if (score == max) {
                    winner = -1;
                }
            }

            return winner;
        }

        public static int GetPlayerAfterFlip(int player) {
            if (Prefs.App.FlipHorizontal && player < _sHorizontalPlayers)
                return _sHorizontalPlayers - 1 - player;
            if (Prefs.App.FlipVertical && player >= _sHorizontalPlayers)
                return PlayerScore.Count - 1 - player + _sHorizontalPlayers;
            return player;
        } 
    }
}