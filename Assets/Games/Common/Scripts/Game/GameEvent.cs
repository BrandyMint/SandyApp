using System;
using System.Collections.Generic;
using UnityEngine.Events;

namespace Games.Common.Game {
    [Serializable]
    public enum GameState {
        NOT_READY,
        COUNTDOWN,
        START,
        STOP,
        SCORES
    }

    public static class GameEvent {
        public static event UnityAction OnCountdown {
            add => AddListener(GameState.COUNTDOWN, value);
            remove => RemoveListener(GameState.COUNTDOWN, value);
        }
        public static event UnityAction OnStart {
            add => AddListener(GameState.START, value);
            remove => RemoveListener(GameState.START, value);
        }
        public static event UnityAction OnStop {
            add => AddListener(GameState.STOP, value);
            remove => RemoveListener(GameState.STOP, value);
        }
        public static event UnityAction OnScores {
            add => AddListener(GameState.SCORES, value);
            remove => RemoveListener(GameState.SCORES, value);
        }
        public static event UnityAction OnChangeState {
            add => _onChangedState.AddListener(value);
            remove => _onChangedState.RemoveListener(value);
        }

        public static GameState Current {
            get => _current;
            set {
                Previous = _current;
                _current = value;
                Invoke(value);
            }
        }
        
        public static GameState Previous { get; private set; } = GameState.NOT_READY;

        private static GameState _current = GameState.NOT_READY;
        private static readonly Dictionary<GameState, UnityEvent> _actions = new Dictionary<GameState, UnityEvent>();
        private static readonly UnityEvent _onChangedState = new UnityEvent();

        public static void Reset() {
            Current = GameState.NOT_READY;
            Previous = GameState.NOT_READY;
        }
        
        public static void AddListener(GameState ev, UnityAction act) {
            if (_actions.TryGetValue(ev, out var unityEvent)) {
                unityEvent.AddListener(act);
            } else {
                unityEvent = new UnityEvent();
                unityEvent.AddListener(act);
                _actions[ev] = unityEvent;
            }

            if (Current == ev) {
                act.Invoke();
            }
        }

        public static void RemoveListener(GameState ev, UnityAction act) {
            if (_actions.TryGetValue(ev, out var unityEvent)) {
                unityEvent.RemoveListener(act);
            }
        }

        private static void Invoke(GameState ev) {
            _onChangedState.Invoke();
            if (_actions.TryGetValue(ev, out var unityEvent)) {
                unityEvent.Invoke();
            }
        }
    }
}