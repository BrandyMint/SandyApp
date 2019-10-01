using UnityEngine;

namespace Launcher.MultiMonitorSupport {
    public class MultiMonitorFix : MonoBehaviour {
        private void Start() {
            MultiMonitor.FixCamerasIn(gameObject);
        }
    }
}