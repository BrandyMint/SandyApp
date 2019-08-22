using System;
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
            aspectRatio = (float) tex.width / tex.height;
        }

        protected override void OnValidate() {
            UpdateFit();
            base.OnValidate();
        }
    }
}