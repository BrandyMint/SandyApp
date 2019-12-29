using UnityEngine;

namespace Games.Common {
    public class CorrectLeftRight : CorrectUpDown {
        protected override void OnAppChanged() {
            var flip = (Prefs.App.FlipHorizontal && _byUIFlip) ^ (Prefs.App.FlipHorizontalSandbox && _bySandboxFlip);
            var x = flip ? -1f : 1f;
            switch (_method) {
                case CorrectMethod.SCALE:
                    var scale = transform.localScale;
                    scale.x *= x * _prev;
                    transform.localScale = scale;
                    break;
                case CorrectMethod.POSITION:
                    var pos = transform.localPosition;
                    pos.x *= x * _prev;
                    transform.localPosition = pos;
                    break;
                case CorrectMethod.ROTATION:
                    var r = x * _prev < 1f ? Quaternion.AngleAxis(180f, Vector3.up) : Quaternion.identity;
                    var rot = transform.localRotation;
                    transform.localRotation = rot * r;
                    break;
            }
            _prev = x;
        }
    }
}