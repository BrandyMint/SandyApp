using UnityEngine;

namespace Utilities {
    public static class MathHelper {

        public static Vector3 TransformPointTo(this Transform tFrom, Transform tTo, Vector3 p) {
            p = tFrom.TransformPoint(p);
            if (tTo != null)
                p = tTo.InverseTransformPoint(p);
            return p;
        }
        
        public static Vector3 TransformDirectionTo(this Transform tFrom, Transform tTo, Vector3 p) {
            p = tFrom.TransformDirection(p);
            if (tTo != null)
                p = tTo.InverseTransformDirection(p);
            return p;
        }
        
        public static Vector3 TransformVectorTo(this Transform tFrom, Transform tTo, Vector3 p) {
            p = tFrom.TransformVector(p);
            if (tTo != null)
                p = tTo.InverseTransformVector(p);
            return p;
        }
        
        //     /a\
        //    / | \
        //   /  |  \
        //  /  h|   \
        // /____|____\
        //      b
        public static float IsoscelesTriangleAngle(float b, float h) {
            return 180f - 2 * Mathf.Acos(b / 2f / h) * Mathf.Rad2Deg;
        }
        
        //     /a\
        //    / | \
        //   /  |  \
        //  /  h|   \
        // /____|____\
        //      b
        public static float IsoscelesTriangleHeight(float b, float a) {
            return b / 2f / Mathf.Cos((180f - a) * Mathf.Deg2Rad / 2f);
        }
        
        public static bool IsConvex(Vector2[] p) {
            if (p.Length < 3)
                return false;
            bool gotNegative = false;
            bool gotPositive = false;
            int B, C;
            for (int A = 0; A < p.Length; ++A) {
                B = (A + 1) % p.Length;
                C = (B + 1) % p.Length;

                var cross = CrossProductLength(p[A], p[B], p[C]);
                if (cross < 0) {
                    gotNegative = true;
                } else if (cross > 0) {
                    gotPositive = true;
                }
                if (gotNegative && gotPositive) return false;
            }
            return true;
        }
        
        public static float CrossProductLength(Vector2 a, Vector2 b, Vector2 c) {
            // Get the vectors' coordinates.
            var BAx = a.x - b.x;
            var BAy = a.y - b.y;
            var BCx = c.x - b.x;
            var BCy = c.y - b.y;

            // Calculate the Z coordinate of the cross product.
            return (BAx * BCy - BAy * BCx);
        }
    }
}