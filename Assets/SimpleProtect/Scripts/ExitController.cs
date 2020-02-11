using UnityEngine;

namespace SimpleProtect {
    public class NotUnlockedExitController : MonoBehaviour {
        private void Update() {
            if (Input.GetKey(KeyCode.Escape)
            || Input.GetKey(KeyCode.C)) {
                Application.Quit();
            }
        }
    }
}