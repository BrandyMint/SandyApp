using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using Utilities;

namespace Games.Balloons {
    public class Borders : MonoBehaviour {
        [SerializeField] private Transform _exitBorder;
        [SerializeField] private Transform[] _offsetedByWidth;
        [SerializeField] private Transform[] _offsetedByWidthAndSize;

        private class BorderInfo {
            public readonly Transform transform;
            public readonly float3 startPos;
            public readonly float3 startScale;
            public readonly Collider collider;

            public BorderInfo(Component t) {
                transform = t.transform;
                startPos = transform.localPosition;
                startScale = transform.localScale;
                collider = t.GetComponent<Collider>();
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

        public void SetWidth(float w) {
            _width = w;
            UpdateWidth();
        }

        private void UpdateWidth() {
            var scaledWidth = _width / 2f / math.float3(transform.localScale);
            foreach (var b in _borders) {
                if (_offsetedByWidthAndSize.Contains(b.transform)) {
                    var pos = scaledWidth * b.startPos;
                    b.transform.localPosition += (Vector3) pos;
                } else
                if (_offsetedByWidth.Contains(b.transform)) {
                    var pos = scaledWidth * b.startPos;
                    b.transform.localPosition = b.startPos * b.startScale / 2f + pos;
                }
            }

            foreach (var spawn in _spawns) {
                var pos = scaledWidth * spawn.startPos;
                spawn.transform.localPosition += (Vector3) pos;
                spawn.transform.localScale = spawn.startScale * (1 - scaledWidth);
            }
        }
    }
}