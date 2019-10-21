using UnityEngine;

namespace Launcher.UI {
    public class BtnGame : BtnGoTo {
        [SerializeField] private GameObject _imgDisabled;
        
        protected override void Awake() {
            base.Awake();
            var disabled = string.IsNullOrEmpty(_scenePath);
            _btn.interactable = !disabled;
            _imgDisabled.SetActive(disabled);
        }
    }
}