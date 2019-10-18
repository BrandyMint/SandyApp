using Launcher.KeyMapping;
using UnityEngine;

namespace Games.Common {
    public class UIScwitcher : MonoBehaviour {
        [SerializeField] private GameObject _ui;
        [SerializeField] private bool _hideOnStart = true;

        private void Start() {
            KeyMapper.AddListener(KeyEvent.SHOW_UI, SwitchUI);
            if (_hideOnStart)
                SwitchUI();
        }

        private void OnDestroy() {
            KeyMapper.RemoveListener(KeyEvent.SHOW_UI, SwitchUI);
        }

        private void SwitchUI() {
            _ui.SetActive(!_ui.activeSelf);
        }
    }
}