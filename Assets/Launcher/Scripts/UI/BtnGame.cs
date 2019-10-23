using UnityEngine;

namespace Launcher.UI {
    public class BtnGame : BtnGoTo {
        [SerializeField] private Color _currentGameColor;
        [SerializeField] private GameObject _imgDisabled;
        
        protected override void Awake() {
            base.Awake();
            var disabled = string.IsNullOrEmpty(_scenePath);
            _btn.interactable = !disabled;
            _imgDisabled.SetActive(disabled);
        }

        private void Start() {
            if (_scenePath == Scenes.CurrentGamePath) {
                var colors = _btn.colors;
                colors.normalColor = _currentGameColor;
                colors.highlightedColor = _currentGameColor;
                _btn.colors = colors;
                
                _btn.Select();
            }
        }
    }
}