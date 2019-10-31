using System.Collections;
using Launcher.KeyMapping;
using UnityEngine;

namespace DepthSensorCalibration {
    public class CalibrationController2 : MonoBehaviour {
        [SerializeField] private FrameFromCamera _imgSandbox;
        [SerializeField] private SensorDistSampler _sampler;
        [SerializeField] private LineRenderer _lineArea;

        private float _timer = 0.5f;

        private void Start() {
            _imgSandbox.gameObject.SetActive(false);
            _timer = Prefs.Calibration.SensorSwitchingViewTimer;
            StartCoroutine(SwitchingView());
            
            _lineArea.gameObject.SetActive(false);
            _sampler.OnDistReceive += OnDistReceive;
            _sampler.OnSampleAreaPoints += ShowSampleArea;
            KeyMapper.AddListener(KeyEvent.SET_DEPTH_ZERO, SampleSensorDist);
        }

        private void OnDestroy() {
            StopAllCoroutines();

            KeyMapper.RemoveListener(KeyEvent.SET_DEPTH_ZERO, SampleSensorDist);
            if (_sampler != null) {
                _sampler.OnDistReceive -= OnDistReceive;
                _sampler.OnSampleAreaPoints -= ShowSampleArea;
            }
        }

        private IEnumerator SwitchingView() {
            while (true) {
                yield return new WaitForSecondsRealtime(_timer);
                _imgSandbox.gameObject.SetActive(!_imgSandbox.gameObject.activeSelf);
                _imgSandbox.TakeFrame();
            }
        }
        
        private void SampleSensorDist() {
            
        }
        
        private void OnDistReceive(float obj) {
            
        }
        
        private void ShowSampleArea(Vector3[] obj) {
            
        }
    }
}