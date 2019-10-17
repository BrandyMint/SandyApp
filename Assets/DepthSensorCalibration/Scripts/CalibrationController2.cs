using System.Collections;
using UnityEngine;

namespace DepthSensorCalibration {
    public class CalibrationController2 : MonoBehaviour {
        [SerializeField] private FrameFromCamera _imgSandbox;
        
        private float _timer = 0.5f;

        private void Start() {
            _imgSandbox.gameObject.SetActive(false);
            _timer = Prefs.Calibration.SensorSwitchingViewTimer;
            StartCoroutine(SwitchingView());
        }

        private void OnDestroy() {
            StopAllCoroutines();
        }

        private IEnumerator SwitchingView() {
            while (true) {
                yield return new WaitForSecondsRealtime(_timer);
                _imgSandbox.gameObject.SetActive(!_imgSandbox.gameObject.activeSelf);
                _imgSandbox.TakeFrame();
            }
        }
    }
}