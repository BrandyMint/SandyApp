using Games.Common.Game;
using UnityEngine;

namespace Games.Common {
    public class ShowWinLost : MonoBehaviour {
        [SerializeField] private GameObject _win;
        [SerializeField] private GameObject _lost;
        
        private void OnEnable() {
            _win.SetActive(!GameScore.Lost);
            _lost.SetActive(GameScore.Lost);
        }
    }
}