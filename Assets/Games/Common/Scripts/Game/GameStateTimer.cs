using UnityEngine;
using UnityEngine.UI;

namespace Games.Common.Game {
    public class GameStateTimer : GameStateStopwatch {
        [SerializeField] private Image _circle;

        protected override void UpdateTime() {
            var t = GameController.CurrStateTimeLeft;
            var amount = t / GameController.CurrStateDuration;
            ShowTime(t, amount);
        }

        private void ShowTime(float t, float amount) {
            ShowTime(t);
            if (_circle != null)
                _circle.fillAmount = amount;
        }
    }
}