using UnityEngine;

namespace DepthSensorCalibration {
    public class KinectFieldSettingsBinder : MonoBehaviour {
        private Canvas canvas;

        void Start() {
            canvas = GetComponentInParent<Canvas>();

            RectTransform rect = GetComponent<RectTransform>();
            Vector3 v = new Vector3(KinectSettings.PosX, KinectSettings.PosY, 1);
            rect.localPosition = v;
            var size = KinectSettings.INITIAL_SIZE * KinectSettings.Size;
            rect.localScale = new Vector3(size, size, 1);
        }

        private void Update() {
            if (transform.hasChanged) {
                UpdateBorders();
                transform.hasChanged = false;
            }
        }

        private void UpdateBorders() {
            /*if (!hmc) return;*/

            RectTransform rect = GetComponent<RectTransform>();
            Vector2[] viewPortPoints = {
                new Vector3(0, 0, 0),
                new Vector3(1, 1, 0)
            };
            Vector2[] normalized = new Vector2[2];
            for (int i = 0; i < viewPortPoints.Length; i++) {
                var p = canvas.worldCamera.ViewportToWorldPoint(viewPortPoints[i]);
                p = rect.InverseTransformPoint(p);
                normalized[i] = Vector2.one - Rect.PointToNormalized(rect.rect, p);
            }
            /*hmc.SetBorders(
                normalized[1].x, normalized[1].y,
                normalized[0].x, normalized[0].y
            );*/
        }
    }
}