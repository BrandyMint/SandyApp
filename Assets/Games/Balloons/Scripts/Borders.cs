using System;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using Utilities;

namespace Games.Balloons {
    public class Borders : MonoBehaviour {
        [SerializeField] private Collider _exitBorder;
        [SerializeField] private Transform[] _offsetedByWidth;
        [SerializeField] private Transform[] _offsetedByWidthAndSize;

        private class BorderInfo {
            public readonly Transform transform;
            public readonly float3 startPos;
            public readonly float3 startScale;

            public BorderInfo(Component t) {
                transform = t.transform;
                startPos = transform.localPosition;
                startScale = transform.localScale;
            }
        }
        private BorderInfo[] _borders;
        private float _width = 1f;
        private BorderInfo[] _spawns;

        private void Awake() {
            _borders = transform.GetComponentsOnlyInChildren<Collider>()
                .Select(c => new BorderInfo(c)).ToArray();
            _spawns = transform.GetComponentsOnlyInChildren<SpawnArea>()
                .Select(c => new BorderInfo(c)).ToArray();
        }

        public Collider ExitBorder => _exitBorder;

        public void SetWidth(float w) {
            _width = w;
            UpdateWidth();
        }

        private void UpdateWidth() {
            var scaledWidth = _width / 2f / math.float3(transform.localScale);
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

            foreach (var spawn in _spawns) {
                var pos = scaledWidth * spawn.startPos / 2;
                spawn.transform.localPosition += (Vector3) pos;
                spawn.transform.localScale = spawn.startScale * (1 - scaledWidth);
            }
        }

        public void AlignToCamera(Camera cam, float dist) {
            transform.rotation = cam.transform.rotation;
            var dir = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 1.0f)).direction;
            var pos = cam.transform.position + dir * dist;
            var vertical = Math.Abs(Prefs.Calibration.Oblique) < 0.5f 
                ? MathHelper.IsoscelesTriangleSize(dist, cam.fieldOfView)
                : MathHelper.RightTriangleSize(dist, cam.fieldOfView);
            pos.y += vertical * Prefs.Calibration.Oblique / 2f;
            transform.position = pos;
            transform.localScale = new Vector3(
                vertical * cam.aspect,
                vertical,
                1f
            );
            
            UpdateWidth();
        }
    }
}