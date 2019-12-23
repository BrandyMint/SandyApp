using System;
using UnityEngine;
using UnityEngine.UI;

namespace Games.Common.Game {
    public class GameStateStopwatch : GameStateEnabler {
        [SerializeField] private Text _text;
        [SerializeField] private string _timeFormat = null;
        
        public string LastValueString { get; protected set; }

        protected bool _timerEnabled;
        protected float _t;

        protected override void EnableOnState(bool enable) {
            base.EnableOnState(enable);
            _timerEnabled = enable;
            _t = 0f;
            UpdateTime();
        }
        
        private void Update() {
            if (_timerEnabled)
                UpdateTime();
        }

        protected virtual void UpdateTime() {
            _t += Time.deltaTime;
            ShowTime(_t);
        }
        
        protected void ShowTime(float t) {
            var txt = "";
            if (string.IsNullOrEmpty(_timeFormat))
                txt = Mathf.CeilToInt(t).ToString();
            else {
                var time = new DateTime().AddSeconds(Mathf.CeilToInt(t));
                txt = time.ToString(_timeFormat);
            }
            if (_text != null) {
                _text.text = txt;
            }

            if (_timerEnabled) {
                LastValueString = txt;
            }
        }
    }
}