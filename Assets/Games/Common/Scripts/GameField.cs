using System;
using System.Linq;
using Games.Common.Game;
using Unity.Mathematics;
using UnityEngine;
using Utilities;

namespace Games.Common {
    public class GameField : MonoBehaviour {
        [SerializeField] private Transform[] _offsetedByWidth;
        [SerializeField] private Transform[] _offsetedByWidthAndSize;
        [SerializeField] private Texture2D _fieldTexture;
        [SerializeField] private bool _scaleZ = false;

        private Camera _lastCam;

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
            _lastCam = cam;
            _distFromCamera = dist;
            transform.rotation = cam.transform.rotation;

            var plane = PlaneOnDist(dist);
            var up = PlaneRaycastFromViewport(plane, new Vector2(0.5f, 1f));
            var down = PlaneRaycastFromViewport(plane, new Vector2(0.5f, 0f));
            var left = PlaneRaycastFromViewport(plane, new Vector2(0f, 0.5f));
            var right = PlaneRaycastFromViewport(plane, new Vector2(1f, 0.5f));
            var pos = (up + down + left + right) / 4f;
            transform.position = pos;
            var vert = Vector3.Distance(up, down);
            var hor = Vector3.Distance(left, right);
            transform.localScale = _startScale * new float3(
                hor,
                vert,
                _scaleZ ? vert : 1f
            );
            
            UpdateWidth();
        }

        public Plane PlaneOnDist(float dist) {
            var up = transform.forward;
            up = (Vector3.Dot(up, -_lastCam.transform.forward) > 0f) ? up : -up;
            return new Plane(up, _lastCam.transform.position - up * dist);
        }

        public Vector3 PlaneRaycastFromViewport(Plane plane, Vector2 uv) {
            var ray = _lastCam.ViewportPointToRay(new Vector3(uv.x, uv.y, 1f));
            if (plane.Raycast(ray, out var dist)) {
                return ray.GetPoint(dist);
            } else {
                Debug.LogError("GameFiled: Cant align to camera");
                return ray.GetPoint(1f);
            }
        }
        
        public float Scale => Mathf.Abs(transform.localScale.y);

        public Vector3 WorldSize => transform.TransformVector(new Vector3(1, 1, -1));

        public Vector3 WorldFromViewport(Vector2 viewport) {
            if (_lastCam == null)
                return transform.TransformPoint(viewport - Vector2.one / 2f);
            return _lastCam.ViewportToWorldPoint(new Vector3(viewport.x, viewport.y, _distFromCamera));
        }
        
        public Vector3 LocalFromViewport(Vector2 viewport) {
            return transform.InverseTransformPoint(WorldFromViewport(viewport));
        }

        public Vector2 ViewportFromWorld(Vector3 world) {
            if (_lastCam == null)
                return (Vector2)transform.InverseTransformPoint(world) + Vector2.one / 2f;
            return _lastCam.WorldToViewportPoint(world);
        }
        
        public Vector2 ViewportFromLocal(Vector3 local) {
            return ViewportFromWorld(transform.TransformPoint(local));
        }

        public int PlayerField(Vector2 viewPos) {
            if (_fieldTexture == null)
                return 0;
            var x = Mathf.Clamp((int) (_fieldTexture.width * viewPos.x), 0, _fieldTexture.width - 1);
            var y = Mathf.Clamp((int) (_fieldTexture.height * viewPos.y), 0, _fieldTexture.height - 1);
            var c = _fieldTexture.GetPixel(x, y);
            if (c.a < 1f)
                return Mathf.RoundToInt(c.a * GameScore.PlayerScore.Count);
            return -1;
        }

        public bool PlaceOnSurface(Transform moleTransform, Func<Vector2, float> getDepth) {
            var p = transform.InverseTransformPoint(moleTransform.position);
            var depth = getDepth(ViewportFromLocal(p));
            if (depth > 0f) {
                p.z = depth -_distFromCamera;
                moleTransform.position = transform.TransformPoint(p);
                return true;
            }
            return false;
        }
    }
}