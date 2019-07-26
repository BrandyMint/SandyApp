using UnityEngine;

namespace Launcher {
    public class InDevelopStamp : MonoBehaviour {
        private void Awake() {
#if !IN_DEVELOP
            Destroy(gameObject);
#endif
        }
    }
}