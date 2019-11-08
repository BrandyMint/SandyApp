using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Games.Common.Game {
    public class GameScore : MonoBehaviour {
        [SerializeField] private Text _txtScore;
        
        private static int _score;

        public static int Score {
            get => _score;
            set {
                if (_score != value) {
                    _score = value;
                    foreach (var instance in _instances) {
                        instance.UpdateScore();
                    }
                }
            }
        }

        private static readonly List<GameScore> _instances = new List<GameScore>();

        private void Awake() {
            _instances.Add(this);
            UpdateScore();
        }

        private void OnDestroy() {
            _instances.Remove(this);
        }

        private void UpdateScore() {
            _txtScore.text = Score.ToString();
        } 
    }
}