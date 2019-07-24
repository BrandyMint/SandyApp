using UnityEngine;
using UnityEngine.UI;

namespace Launcher {
    [RequireComponent(typeof(Button))]
    public class BtnExit : MonoBehaviour {
        private void Awake() {
            var btn = GetComponent<Button>();
            btn.onClick.AddListener(OnBtn);
        }

        private static void OnBtn() {
            Application.Quit();
        }
    }
}