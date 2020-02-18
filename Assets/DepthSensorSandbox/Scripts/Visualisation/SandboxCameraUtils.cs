using System;
using DepthSensor.Device;
using UnityEngine;

namespace DepthSensorSandbox.Visualisation {
    public static class SandboxCameraUtils {
        public static Plane PlaneOnDist(this Transform t, float dist) {
            return PlaneOnDist(t, dist, -t.forward);
        }

        public static Plane PlaneOnDist(this Transform t, float dist, Vector3 planeUp) {
            planeUp = (Vector3.Dot(planeUp, -t.forward) > 0f) ? planeUp : -planeUp;
            return new Plane(planeUp, t.position - planeUp * dist);
        }

        public static Plane PlaneOnDist(this Camera t, float dist) {
            return PlaneOnDist(t.transform, dist);
        }

        public static Plane PlaneOnDist(this Camera t, float dist, Vector3 planeUp) {
            return PlaneOnDist(t.transform, dist, planeUp);
        }

        public static bool PlaneRaycastFromViewport(this Camera cam, Plane plane, Vector2 uv, out Vector3 pos) {
            var ray = cam.ViewportPointToRay(new Vector3(uv.x, uv.y, 1f));
            if (plane.Raycast(ray, out var dist)) {
                pos = ray.GetPoint(dist);
                return true;
            } else {
                pos = ray.GetPoint(1f);
                return false;
            }
        }

        public static Rect GetCropping(this Camera cam, float dist, Vector2 minUV, Vector2 maxUV, Func<Vector3, Vector2> toNewView01) {
            var plane = cam.PlaneOnDist(dist);

            Vector2 min = Vector2.zero, max = Vector2.one;
            if (cam.PlaneRaycastFromViewport(plane, minUV, out var camMin)) {
                min = toNewView01(camMin);
            }
            if (cam.PlaneRaycastFromViewport(plane, maxUV, out var camMax)) {
                max = toNewView01(camMax);
            }

            if (max.x < min.x) Swap(ref max.x, ref min.x);
            if (max.y < min.y) Swap(ref max.y, ref min.y);

            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
        
        public static Rect GetCroppingToDepth(this Camera cam, float dist, DepthSensorDevice device) {
            var depth = device.Depth.GetNewest();
            var minUV = Vector2.zero;
            var maxUV = Vector2.one;

            return GetCropping(cam, dist, minUV, maxUV, pWorld => {
                var p = device.CameraPosToDepthMapPos(pWorld);
                p.x /= depth.width;
                p.y /= depth.height;
                return p;
            });
        }

        private static void Swap<T>(ref T a, ref T b) {
            var t = a;
            a = b;
            b = t;
        }
    }
}