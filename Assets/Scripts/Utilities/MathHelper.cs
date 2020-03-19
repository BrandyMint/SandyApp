using System;
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
            return 2f * RightTriangleAngle(b / 2f, h);
        }
        
        //     /a\
        //    / | \
        //   /  |  \
        //  /  h|   \
        // /____|____\
        //      b
        public static float IsoscelesTriangleHeight(float b, float a) {
            return RightTriangleHeight(b / 2f, a / 2f);
        }
        
        //     /a\
        //    / | \
        //   /  |  \
        //  /  h|   \
        // /____|____\
        //      b
        public static float IsoscelesTriangleSize(float h, float a) {
            return  2f * RightTriangleSize(h, a / 2f);
        }
        
        //   |\
        //   |a\
        //   |  \
        //  h|   \
        //   |____\
        //     b
        public static float RightTriangleAngle(float b, float h) {
            return Mathf.Rad2Deg * Mathf.Atan2(b, h);
        }
        
        //   |\
        //   |a\
        //   |  \
        //  h|   \
        //   |____\
        //     b
        public static float RightTriangleHeight(float b, float a) {
            return b / Mathf.Tan( a * Mathf.Deg2Rad);
        }
        
        //   |\
        //   |a\
        //   |  \
        //  h|   \
        //   |____\
        //     b
        public static float RightTriangleSize(float h, float a) {
            return  h * Mathf.Tan( a * Mathf.Deg2Rad);
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
        
        public static Vector2 Div(Vector2 p1, Vector2 p2) {
            return new Vector2(p1.x / p2.x, p1.y / p2.y);
        }
        
        public static Vector3 Div(Vector3 p1, Vector3 p2) {
            return new Vector3(p1.x / p2.x, p1.y / p2.y, p1.z / p2.z);
        }
        
        public static Vector2 Mul(Vector2 p1, Vector2 p2) {
            return new Vector2(p1.x * p2.x, p1.y * p2.y);
        }
        
        public static Vector3 Mul(Vector3 p1, Vector3 p2) {
            return new Vector3(p1.x * p2.x, p1.y * p2.y, p1.z * p2.z);
        }

        public static T GetMedian<T>(params T[] a) where T: IComparable<T> {
            var n = a.Length;
            int low, high;
            int median;
            int middle, ll, hh;

            low = 0; high = n-1; median = (low + high) / 2;
            for (;;) {
                if (high <= low) /* One element only */
                    return a[median];

                if (high == low + 1) {  /* Two elements only */
                    if (a[low].CompareTo(a[high]) < 0)
                        ElemSwap(a, low, high);
                    return a[median];
                }

                /* Find median of low, middle and high items; swap into position low */
                middle = (low + high) / 2;
                if (a[middle].CompareTo(a[high]) < 0)    ElemSwap(a, middle, high);
                if (a[low].CompareTo(a[high]) < 0)       ElemSwap(a, low, high);
                if (a[middle].CompareTo(a[low]) < 0)     ElemSwap(a, middle, low);

                /* Swap low item (now in position middle) into position (low+1) */
                ElemSwap(a, middle, low + 1);

                /* Nibble from each end towards middle, swapping items when stuck */
                ll = low + 1;
                hh = high;
                for (;;) {
                    do ll++; while (a[low].CompareTo(a[ll]) < 0);
                    do hh--; while (a[hh].CompareTo(a[low]) < 0);

                    if (hh < ll)
                        break;

                    ElemSwap(a, ll, hh);
                }

                /* Swap middle item (in position low) back into correct position */
                ElemSwap(a, low, hh);

                /* Re-set active partition */
                if (hh <= median)
                    low = ll;
                if (hh >= median)
                    high = hh - 1;
            }
        }

        private static void ElemSwap<T>(T[] a, int i, int j) {
            var t = a[i];
            a[i] = a[j];
            a[j] = t;
        }

        public static void Swap<T>(ref T a, ref T b) {
            var t = a;
            a = b;
            b = t;
        }
    }
}