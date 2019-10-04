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
            _btnStep.onClick.AddListener(OnBtnStep);
            _btnContinue.onClick.AddListener(OnBtnContinue);
            DepthSensorSandboxProcessor.OnNewFrame += OnNewFrame;
        }

        private void OnDestroy() {
            DepthSensorSandboxProcessor.OnNewFrame -= OnNewFrame;
        }

        private void OnNewFrame(DepthBuffer depth, MapDepthToCameraBuffer map) { }

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
            if (!_hands.EvWaveReady.WaitOne(1000)) return;
            
            var t = _image.texture as Texture2D;
            if (TexturesHelper.ReCreateIfNeed(ref t, _hands.HandsMask.width, _hands.HandsMask.height,
                TextureFormat.R8)) {
                t.filterMode = FilterMode.Point;
                _image.texture = t;
                _image.gameObject.SetActive(true);
            }
            
            t.SetPixelData(_hands.HandsMask.data, 0);
            t.Apply(false);
            _txtWawe.text = _hands.CurrWave.ToString();
            _hands.EvRequestNextWave.Set();
#else
            _txtWawe.text = "define HANDS_WAVE_STEP_DEBUG";
#endif
        }
    }
}