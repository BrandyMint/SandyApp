using UnityEngine;
using UnityEngine.UI;

namespace DepthSensorCalibration {
    public class WallController : MonoBehaviour {
        [SerializeField] private GameObject _manual;
        [SerializeField] private GameObject _automatic;

        private void Start() {
            SwitchMode(CalibrationMode.MANUAL);
        }

        public void SwitchMode(CalibrationMode mode) {
            _manual.SetActive(mode == CalibrationMode.MANUAL);
            _automatic.SetActive(mode == CalibrationMode.AUTOMATIC);
            gameObject.SetActive(mode != CalibrationMode.TEST);
        }

        public Texture GetTargetTexture() {
            return _automatic.GetComponentInChildren<Image>(true).mainTexture;
        }
    }
}