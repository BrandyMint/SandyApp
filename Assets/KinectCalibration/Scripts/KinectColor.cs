using System.Collections;
using System.Collections.Generic;
using DepthSensor;
using DepthSensor.Sensor;
using HumanCollider;
using UnityEngine;
using UnityEngine.UI;

namespace Launcher.KinectCalibration {
    public class KinectColor : MonoBehaviour {
        [SerializeField] private GameObject _imgDepth;
        private RawImage _imgOutput;
        private HumanMaskCreater hmc;

        private readonly Dictionary<string, float> _videoScale = new Dictionary<string, float> {
            {"Kinect1", 1f},
            {"Kinect2", 0.333f}
        };

        private void Start() {
            _imgOutput = GetComponent<RawImage>();
            hmc = HumanMaskCreater.GetInstance();
            if (hmc != null) {
                hmc.OnDepthToColorOffsetChanged += CalibrateDepthColorOffset;
                Vector3 offsetPos;
                float scaleFromDepth;
                if (hmc.GetDethToColorOffset(out offsetPos, out scaleFromDepth)) {
                    CalibrateDepthColorOffset(offsetPos, scaleFromDepth);
                }
            }
            StartCoroutine(WaitTextureAndAccept(DepthSensorManager.Instance));
        }

        private void OnEnable() {
            if (DepthSensorManager.IsInitialized()) {
                DepthSensorManager.Instance.Device.Color.Active = true;
            }
        }

        private void OnDisable() {
            if (DepthSensorManager.IsInitialized()) {
                DepthSensorManager.Instance.Device.Color.Active = false;
            }
        }

        private void OnDestroy() {
            if (DepthSensorManager.IsInitialized()) {
                DepthSensorManager.Instance.Device.Color.OnNewFrame -= OnNewFrame;
            }
            if (hmc != null) {
                hmc.OnDepthToColorOffsetChanged -= CalibrateDepthColorOffset;
            }
        }

        private IEnumerator WaitTextureAndAccept(DepthSensorManager dsm) {
            if (!dsm) yield break;
            yield return new WaitUntil(DepthSensorManager.IsInitialized);

            var rect = GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(dsm.Device.Color.width, dsm.Device.Color.height);
            rect.localScale = Vector2.one * _videoScale[dsm.Device.Platform];
            dsm.Device.Color.Active = true;
            dsm.Device.Color.OnNewFrame += OnNewFrame;
        }

        private void OnNewFrame(ColorByteSensor sColor) {
            var format = sColor.format;
            Texture2D tex = _imgOutput.texture as Texture2D;
            if (tex == null) {
                tex = new Texture2D(sColor.width, sColor.height, format, false);
                _imgOutput.texture = tex;
            }
            if (tex.width != sColor.width || tex.height != sColor.height || tex.format != format) {
                tex.Resize(sColor.width, sColor.height, format, false);
            }
            tex.LoadRawTextureData(sColor.data);
            tex.Apply();
        }

        private void CalibrateDepthColorOffset(Vector3 posOffset, float scaleFromDepth) {
            var rectColor = (RectTransform) transform;
            var rectDepth = (RectTransform) _imgDepth.transform;
            var scale =   1f / scaleFromDepth;
            rectColor.localScale = new Vector3(scale, scale, 1f);
            rectColor.localPosition = rectDepth.localPosition + posOffset * scale;
        }
    }
}
