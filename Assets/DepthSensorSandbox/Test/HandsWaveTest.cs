#if !UNITY_EDITOR
    #undef HANDS_WAVE_STEP_DEBUG
#endif
using DepthSensor.Buffer;
using DepthSensorSandbox.Processing;
using UnityEngine;
using UnityEngine.UI;
using Utilities;

namespace DepthSensorSandbox.Test {
    public class HandsWaveTest : MonoBehaviour {
        [SerializeField] private Button _btnStep;
        [SerializeField] private Button _btnContinue;
        [SerializeField] private RawImage _image;
        [SerializeField] private Text _txtWawe;

        private bool _waitStep = true;
        private HandsProcessing _hands;
        
        private void Start() {
            _hands = DepthSensorSandboxProcessor.Instance.Hands;
#if HANDS_WAVE_STEP_DEBUG
            _hands.WaveBarrier.AddParticipant();
#endif
            _btnStep.onClick.AddListener(OnBtnStep);
            _btnContinue.onClick.AddListener(OnBtnContinue);
            DepthSensorSandboxProcessor.OnNewFrame += OnNewFrame;
        }

        private void OnDestroy() {
#if HANDS_WAVE_STEP_DEBUG
            if (DepthSensorSandboxProcessor.Instance != null && DepthSensorSandboxProcessor.Instance.Hands != null)
                DepthSensorSandboxProcessor.Instance.Hands.WaveBarrier.RemoveParticipant();
#endif
            DepthSensorSandboxProcessor.OnNewFrame -= OnNewFrame;
        }

        private static void OnNewFrame(DepthBuffer depth, MapDepthToCameraBuffer map) { }

        private void OnBtnContinue() {
            _waitStep = false;
        }

        private void OnBtnStep() {
            _waitStep = true;
            ShowWave();
        }

        private void Update() {
            if (!_waitStep) {
                ShowWave();
            }
        }

        private void ShowWave() {
#if HANDS_WAVE_STEP_DEBUG
            if (_hands.HandsMask.texture.filterMode != FilterMode.Point)
                _hands.HandsMask.texture.filterMode = FilterMode.Point;
            _hands.HandsMask.UpdateTexture();
            _image.texture = _hands.HandsMask.texture;
            _txtWawe.text = _hands.CurrWave.ToString();
            _hands.WaveBarrier.SignalAndWait(3000);
#else
            _txtWawe.text = "define HANDS_WAVE_STEP_DEBUG";
#endif
        }
    }
}