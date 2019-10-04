using UnityEngine;
using UnityEngine.UI;

namespace Utilities {
    [RequireComponent(typeof(MaskableGraphic))]
    public class AspectRatioFitterForGraphic : AspectRatioFitter {
        protected override void Start() {
            base.Start();
            UpdateFit();
        }

        public void UpdateFit() {
            var tex = GetComponent<MaskableGraphic>().mainTexture;
            if (tex != null) {
                aspectRatio = (float) tex.width / tex.height;
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate() {
            UpdateFit();
            base.OnValidate();
        }
#endif
    }
}