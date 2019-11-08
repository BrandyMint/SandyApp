using UnityEngine;
using UnityEngine.UI;

namespace Games.Common.Game {
    public class GameStateTimer : GameStateEnabler {
        [SerializeField] private Text _text;
        [SerializeField] private Image _circle;

        private bool _timerEnabled;

        protected override void EnableOnState(bool enable) {
            base.EnableOnState(enable);
            _timerEnabled = enable;
            UpdateTime();
        }

        private void Update() {
            if (_timerEnabled)
                UpdateTime();
        }

        private void UpdateTime() {
            var t = GameController.CurrStateTimeLeft;
            var amount = t / GameController.CurrStateDuration;
            ShowTime(t, amount);
        }

        private void ShowTime(float t, float amount) {
            if (_text != null)
                _text.text = Mathf.CeilToInt(t).ToString();
            if (_circle != null)
                _circle.fillAmount = amount;
        }
    }
}