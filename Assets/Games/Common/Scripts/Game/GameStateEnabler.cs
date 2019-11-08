using UnityEngine;

namespace Games.Common.Game {
    public class GameStateEnabler : MonoBehaviour {
        [SerializeField] protected GameState _gameState;
        [SerializeField] protected GameObject[] _objsSwitch = {};

        protected virtual void Awake() {
            EnableOnState(false);
            GameEvent.AddListener(_gameState, OnStart);
            GameEvent.OnChangeState += OnStop;
        }

        protected virtual void OnDestroy() {
            GameEvent.RemoveListener(_gameState, OnStart);
            GameEvent.OnChangeState -= OnStop;
        }

        protected virtual void EnableOnState(bool enable) {
            foreach (var obj in _objsSwitch) {
                obj.SetActive(enable);
            }
        }

        protected virtual void OnStart() {
            EnableOnState(true);
        }

        protected virtual void OnStop() {
            EnableOnState(false);
        }
    }
}