using System;
using UnityEngine;

namespace DepthSensorCalibration.HalfVisualization {
    [RequireComponent(typeof(RectTransform))]
    public class HalfUIAnchors : HalfBase {
        protected override void SetHalf(FillType type) {
            if (type == FillType.NONE) {
                gameObject.SetActive(false);
                return;
            }
            gameObject.SetActive(!Hide);

            var rect = (RectTransform) transform;
            
            switch (type) {
                case FillType.FULL:
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    break;
                case FillType.TOP:
                    rect.anchorMin = new Vector2(0f, 0.5f);
                    rect.anchorMax = new Vector2(1f, 1f);
                    break;
                case FillType.BOTTOM:
                    rect.anchorMin = new Vector2(0f, 0f);
                    rect.anchorMax = new Vector2(1f, 0.5f);
                    break;
                case FillType.LEFT:
                    rect.anchorMin = new Vector2(0f, 0f);
                    rect.anchorMax = new Vector2(0.5f, 1f);
                    break;
                case FillType.RIGHT:
                    rect.anchorMin = new Vector2(0.5f, 0f);
                    rect.anchorMax = new Vector2(1f, 1f);
                    break;
            }
        }
    }
}