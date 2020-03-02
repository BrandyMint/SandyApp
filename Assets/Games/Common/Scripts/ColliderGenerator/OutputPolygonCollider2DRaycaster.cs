using System.Collections.Generic;
using DepthSensorSandbox.Visualisation;
using UnityEngine;

namespace Games.Common.ColliderGenerator {
    public class OutputPolygonCollider2DRaycaster : OutputPolygonCollider2DBase {
        public HandsRaycaster Raycaster;

        private Plane _plane;

        public override void PrepareFrame() {
            _plane = Raycaster.Cam.PlaneOnTransform(collider.transform);
        }

        public override void AddShape(List<Vector2> points) {
            for (int i = 0; i < points.Count; i++) {
                var pWorld = Raycaster.ProjectToWorld(points[i]);
                var uv = Raycaster.Cam.WorldToViewportPoint(pWorld);
                Raycaster.Cam.PlaneRaycastFromViewport(_plane, uv, out pWorld);
                points[i] = collider.transform.InverseTransformPoint(pWorld);
            }
            base.AddShape(points);
        }
    }
}