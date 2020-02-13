using Unity.Mathematics;

namespace Launcher.Flip {
    public class TransformFlipperBySandbox : TransformFlipper {
        protected override void OnAppParamChanged() {
            var scale = new float3 {
                x = Prefs.App.FlipHorizontalSandbox ? -1f : 1f,
                y = Prefs.App.FlipVerticalSandbox ? -1f : 1f,
                z = 1f
            };
            _flip.localScale = _initialScale * scale;
        }
    }
}