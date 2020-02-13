using UnityEngine;
using UnityEngine.UI;

namespace Launcher.UI {
    [RequireComponent(typeof(Button))]
    public class BtnGoTo : MonoBehaviour {
        [SerializeField][HideInInspector] protected string _scenePath;
        
        protected Button _btn;

        protected virtual void Awake() {
            _btn = GetComponent<Button>();
            _btn.onClick.AddListener(OnBtn);
        }

        private void OnBtn() {
            Scenes.GoToWithChecking(_scenePath);
        }
    }
}