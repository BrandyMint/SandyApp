using System.Collections;
using DepthSensorSandbox.Visualisation;
using UnityEngine;

namespace DepthSensorCalibration {
    public class CalibrationController2 : MonoBehaviour {
        [SerializeField] private GameObject _imgCalibration;
        [SerializeField] private SandboxVisualizerColor _sandbox;

        private float _timer = 0.5f;

        private void Start() {
            _timer = Prefs.Calibration.SensorSwitchingViewTimer;
            Resume();
        }

        private void OnDestroy() {
            StopAllCoroutines();
        }

        private IEnumerator SwitchingView() {
            while (true) {
                yield return new WaitForSecondsRealtime(_timer);
                ShowSandbox(_imgCalibration.activeSelf);
            }
        }

        private void ShowSandbox(bool show) {
            show = _sandbox.FreezeColor = show;
            _imgCalibration.SetActive(!show);
        }

        public void Pause() {
            StopCoroutine(nameof(SwitchingView));
            ShowSandbox(true);
        }

        public void Resume() {
            ShowSandbox(false);
            StartCoroutine(nameof(SwitchingView));
        }
    }
}