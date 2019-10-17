using UnityEngine;
using UnityEngine.UI;

namespace DepthSensorCalibration.HalfVisualization {
    [RequireComponent(typeof(Image))]
    public class HalfMask : HalfBase {

        protected override void SetHalf(FillType type) {
            if (type == FillType.NONE) {
                gameObject.SetActive(false);
                return;
            }
            gameObject.SetActive(!Hide);

            var img = GetComponent<Image>();
            
            img.type = Image.Type.Filled;
            var horizontal = type == FillType.LEFT || type == FillType.RIGHT;
            var formStart = type == FillType.LEFT || type == FillType.BOTTOM;
            img.fillMethod = horizontal ? Image.FillMethod.Horizontal : Image.FillMethod.Vertical;
            img.fillOrigin = formStart ? 0 : 1;
            img.fillAmount = type == FillType.FULL ? 1f : 0.5f;
        }
    }
}