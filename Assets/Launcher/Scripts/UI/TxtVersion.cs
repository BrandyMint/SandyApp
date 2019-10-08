using UnityEngine;
using UnityEngine.UI;

namespace Launcher.UI {
    [RequireComponent(typeof(Text))]
    public class TxtVersion : MonoBehaviour {
        private void Awake() {
            GetComponent<Text>().text = Application.version;
        }
    }
}