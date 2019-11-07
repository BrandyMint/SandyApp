using DepthSensorSandbox.Visualisation;
using UnityEngine;

namespace Games.Common {
    public class GameController : MonoBehaviour {
        [SerializeField] private IGame _game;
        [SerializeField] private Timer _timerStarting;
        [SerializeField] private Timer _timerGame;
        [SerializeField] private Timer _timerScore;
        
        [SerializeField] private float _timeStart = 3f; 
        [SerializeField] private float _timeGame = 15f;
        [SerializeField] private float _timeScore = 3f;

        private void Awake() {
            OneMomentBillboard.OnReady += OnGameReady;
        }

        private void OnDestroy() {
            OneMomentBillboard.OnReady -= OnGameReady;
        }

        private void OnGameReady() {
            StateStarting();
        }

        private void StateStarting() {
            _game.StopGame();
            _timerStarting.StartTimer(_timeStart, StateGame);
        }

        private void StateGame() {
            _game.StartGame();
            _timerGame.StartTimer(_timeGame, StateScore);
        }

        private void StateScore() {
            _game.StopGame();
            _timerScore.StartTimer(_timeScore, StateStarting);
        }
    }
}