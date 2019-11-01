using Launcher.KeyMapping;
using UnityEngine;

namespace Games.Common {
    public class UIScwitcher : MonoBehaviour {
        [SerializeField] private GameObject _ui;
        [SerializeField] private bool _hideOnStart = true;
        
        private bool _allowShow = true;
        private bool _userShow = true;

        private void Start() {
            KeyMapper.AddListener(KeyEvent.SHOW_UI, SwitchUI);
            if (_hideOnStart)
                SwitchUI();
        }

        private void OnDestroy() {
            KeyMapper.RemoveListener(KeyEvent.SHOW_UI, SwitchUI);
        }

        private void SwitchUI() {
            _userShow = !_userShow;
            UpdateShowUI();
        }

        private void UpdateShowUI() {
            _ui.SetActive(_allowShow && _userShow);
        }

        public bool AllowShow {
            get => _allowShow;
            set {
                _allowShow = value;
                UpdateShowUI();
            }
        }
    }
}