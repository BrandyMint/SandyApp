using UnityEngine;
using UnityEngine.UI;

namespace Utilities {
    [RequireComponent(typeof(MaskableGraphic))]
    public class AspectRatioFitterForGraphic : AspectRatioFitter {
        protected override void OnEnable() {
            base.OnEnable();
            UpdateFit();
        }

        public void UpdateFit() {
            var tex = GetComponent<MaskableGraphic>().mainTexture;
            aspectRatio = (float) tex.width / tex.height;
        }

#if UNITY_EDITOR
        protected override void OnValidate() {
            UpdateFit();
            base.OnValidate();
        }
#endif
    }
}