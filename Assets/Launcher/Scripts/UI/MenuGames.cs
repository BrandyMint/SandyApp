using UnityEngine;

namespace Launcher.UI {
    public class MenuGames : MonoBehaviour {
        [SerializeField] private BtnGame _tplBtnGame;
        
        private void Awake() {
            _tplBtnGame.gameObject.SetActive(false);
            for (int i = 0; i < GamesList.Count; ++i) {
                CreateBtn(i);
            }
        }

        private void CreateBtn(int i) {
            var newBtn = Instantiate(_tplBtnGame, _tplBtnGame.transform.parent, false);
            newBtn.Set(i);
            newBtn.gameObject.SetActive(true);
        }
    }
}