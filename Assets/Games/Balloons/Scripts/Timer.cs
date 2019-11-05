using System;
using UnityEngine;
using UnityEngine.UI;

namespace Games.Balloons {
    public class Timer : MonoBehaviour { 
        
        [SerializeField] private Text _text;
        [SerializeField] private Image _circle;

        private event Action _onZero;
        private float _timer;
        private float _t;

        public void StartTimer(float time, Action OnZero) {
            _timer = time;
            _t = _timer;
            _onZero = OnZero;
            ShowTime(_timer, 1f);
            gameObject.SetActive(true);
        }

        public void StopTimer() {
            gameObject.SetActive(false);
            _onZero = null;
        }

        private void Update() {
            _t -= Time.deltaTime;
            if (_t < 0f) {
                _t = 0f;
                _onZero?.Invoke();
                StopTimer();
            }

            ShowTime(_t, _t/_timer);
        }

        private void ShowTime(float t, float amount) {
            if (_text != null)
                _text.text = Mathf.CeilToInt(t).ToString();
            if (_circle != null)
                _circle.fillAmount = amount;
        }
    }
}