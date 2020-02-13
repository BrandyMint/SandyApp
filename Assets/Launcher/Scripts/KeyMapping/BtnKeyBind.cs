using UnityEngine;
using UnityEngine.UI;

namespace Launcher.KeyMapping {
    [RequireComponent(typeof(Button))]
    public class BtnKeyBind : MonoBehaviour {
        [SerializeField] private KeyEvent _keyEvent;

        private void Awake() {
            ShortCut(GetComponent<Button>(), _keyEvent);
        }

        public static void ShortCut(Button btn, KeyEvent ev) {
            var txt = btn.GetComponentInChildren<Text>();
            var key = KeyMapper.FindFirstKey(ev);
            if (txt != null && key != null) {
                txt.text += $" [{key.ShortCut}]";
            }
            btn.onClick.AddListener(() => KeyMapper.FireEvent(ev));
        }
    }
}