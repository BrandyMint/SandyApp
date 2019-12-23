using UnityEngine;
using UnityEngine.UI;

namespace Games.Common.Game {
    public class GameTimeLeftShow : MonoBehaviour {
        [SerializeField] private Text _txtTimeLeft;
        [SerializeField] private GameStateStopwatch _stopwatch;

        private void OnEnable() {
            _txtTimeLeft.text = _stopwatch.LastValueString;
        }
    }
}