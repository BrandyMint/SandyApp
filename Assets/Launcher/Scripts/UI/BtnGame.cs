using UnityEngine;
using UnityEngine.UI;

namespace Launcher.UI {
    public class BtnGame : BtnGoTo {
        [SerializeField] private Color _currentGameColor;
        [SerializeField] private GameObject _imgDisabled;
        [SerializeField] private GameTittle _tittle;
        [SerializeField] private Image _img;
        
        protected override void Awake() {
            base.Awake();
            var disabled = string.IsNullOrEmpty(_scenePath);
            _btn.interactable = !disabled;
            _imgDisabled.SetActive(disabled);
        }

        public void Set(int i) {
            var description = GamesList.GetDescription(i);
            _scenePath = description.ScenePath;
            if (description.Icon != null)
                _img.sprite = description.Icon;
            _tittle.Set(i);
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