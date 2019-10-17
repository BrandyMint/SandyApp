using Launcher.KeyMapping;
using UnityEngine;

namespace Games.Common {
    public class UIScwitcher : MonoBehaviour {
        [SerializeField] private Canvas _uiCanvas;

        private void Start() {
            KeyMapper.AddListener(KeyEvent.SHOW_UI, SwitchUI);
            SwitchUI();
        }

        private void OnDestroy() {
            KeyMapper.RemoveListener(KeyEvent.SHOW_UI, SwitchUI);
        }

        private void SwitchUI() {
            _uiCanvas.gameObject.SetActive(!_uiCanvas.gameObject.activeSelf);
        }
    }
}