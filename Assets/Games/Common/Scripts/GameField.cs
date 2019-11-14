using System;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using Utilities;

namespace Games.Balloons {
    public class GameField : MonoBehaviour {
        [SerializeField] private Transform[] _offsetedByWidth;
        [SerializeField] private Transform[] _offsetedByWidthAndSize;

        protected class BorderInfo {
            public readonly Transform transform;
            public readonly float3 startPos;
            public readonly float3 startScale;

            public BorderInfo(Component t) {
                transform = t.transform;
                startPos = transform.localPosition;
                startScale = transform.localScale;
            }
        }
        protected BorderInfo[] _borders;
        protected float _width = 1f;
        protected float _distFromCamera;
        private Vector3 _startScale;

        protected virtual void Awake() {
            _startScale = transform.localScale;
            _borders = transform.GetComponentsOnlyInChildren<Collider>()
                .Select(c => new BorderInfo(c)).ToArray();
        }

        public void SetWidth(float w) {
            _width = w;
            UpdateWidth();
        }

        protected float3 GetScaledWidth() {
            return _width / 2f / math.float3(math.abs(transform.localScale));
        }

        protected virtual void UpdateWidth() {
            var scaledWidth = GetScaledWidth();
            foreach (var b in _borders) {
                if (_offsetedByWidthAndSize.Contains(b.transform)) {
                    var pos = scaledWidth * b.startPos;
                    b.transform.localPosition = b.startPos + pos * 1.5f;
                } else
                if (_offsetedByWidth.Contains(b.transform)) {
                    var pos = scaledWidth * b.startPos;
                    b.transform.localPosition = b.startPos * b.startScale / 2f + pos;
                }
            }
        }

        public void AlignToCamera(Camera cam, float dist) {
            _distFromCamera = dist;
            transform.rotation = cam.transform.rotation;
            var dir = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 1.0f)).direction;
            var pos = cam.transform.position + dir * dist;
            var vertical = Math.Abs(Prefs.Calibration.Oblique) < 0.5f 
                ? MathHelper.IsoscelesTriangleSize(dist, cam.fieldOfView)
                : MathHelper.RightTriangleSize(dist, cam.fieldOfView);
            pos.y += vertical * Prefs.Calibration.Oblique / 2f;
            transform.position = pos;
            transform.localScale = _startScale * new float3(
                vertical * cam.aspect,
                vertical,
                1f
            );
            
            UpdateWidth();
        }

        public bool PlaceOnSurface(Transform moleTransform, Func<Vector2, float> getDepth) {
            var p = transform.InverseTransformPoint(moleTransform.position);
            var depth = getDepth((new Vector2(p.x, p.y) + Vector2.one) / 2f);
            if (depth > 0f) {
                p.z = depth -_distFromCamera;
                moleTransform.position = transform.TransformPoint(p);
                return true;
            }
            return false;
        }
    }
}