using System.Collections;
using DepthSensorSandbox.Visualisation;
using UnityEngine;

namespace Games.Common.Game {
    public class GameController : MonoBehaviour {
        [SerializeField] private float _timeCountdown = 3f; 
        [SerializeField] private float _timeGame = 15f;
        [SerializeField] private float _timeScore = 3f;
        
        public static float CurrStateTimeLeft { get; set; }
        public static float CurrStateDuration { get; set; }

        private Coroutine _timer;

        private void Awake() {
            GameEvent.Reset();
            GameEvent.OnChangeState += OnChangeState;
            OneMomentBillboard.OnReady += SetGameReady;
        }

        private void OnDestroy() {
            GameEvent.OnChangeState -= OnChangeState;
            OneMomentBillboard.OnReady -= SetGameReady;
        }

        private static void SetGameReady() {
            GameEvent.Current = GameState.COUNTDOWN;
        }

        private void OnChangeState() {
            switch (GameEvent.Current) {
                case GameState.COUNTDOWN:
                    StartTimer(_timeCountdown, GameState.START);
                    break;
                case GameState.START:
                    StartTimer(_timeGame, GameState.STOP);
                    break;
                case GameState.STOP:
                    StartTimer(float.Epsilon, GameState.SCORES);
                    break;
                case GameState.SCORES:
                    StartTimer(_timeScore, GameState.COUNTDOWN);
                    break;
            }
        }

        private void StartTimer(float t, GameState next) {
            if (_timer != null)
                StopCoroutine(_timer);
            _timer = StartCoroutine(Timer(t, next));
        }

        private static IEnumerator Timer(float t, GameState next) {
            CurrStateDuration = t;
            CurrStateTimeLeft = t;
            while (CurrStateTimeLeft > 0) {
                yield return null;
                CurrStateTimeLeft -= Time.deltaTime;
                if (CurrStateTimeLeft < 0f) {
                    CurrStateTimeLeft = 0f;
                    GameEvent.Current = next;
                }
            }
        }
    }
}