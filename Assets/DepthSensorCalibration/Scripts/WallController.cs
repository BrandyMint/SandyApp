using UnityEngine;

namespace DepthSensorCalibration {
    public class WallController : MonoBehaviour {
        [SerializeField] private GameObject _manual;
        
        public enum Mode {
            MANUAL
        }

        private void Start() {
            SwitchMode(Mode.MANUAL);
        }

        public void SwitchMode(Mode mode) {
            _manual.SetActive(mode == Mode.MANUAL);
        }
    }
}