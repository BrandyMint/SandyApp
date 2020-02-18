using System;
using UnityEngine;

namespace Utilities {
    /// <summary>
    ///   <para>Utilities for rectangle selections.</para>
    /// TODO: is copy of UnityEditor.Experimental.GraphView.RectUtils for using in Player
    /// </summary>
    public static class RectUtils {
        /// <summary>
        ///   <para>Check if a line segment overlaps a rectangle.</para>
        /// </summary>
        /// <param name="rect">Rectangle to check.</param>
        /// <param name="p1">Line segment point 1.</param>
        /// <param name="p2">Line segment point 2.</param>
        /// <returns>
        ///   <para>True if line segment overlaps rectangle. False otherwise.</para>
        /// </returns>
        public static bool IntersectsSegment(Rect rect, Vector2 p1, Vector2 p2) {
            float num1 = Mathf.Min(p1.x, p2.x);
            float num2 = Mathf.Max(p1.x, p2.x);
            if ((double) num2 > (double) rect.xMax)
                num2 = rect.xMax;
            if ((double) num1 < (double) rect.xMin)
                num1 = rect.xMin;
            if ((double) num1 > (double) num2)
                return false;
            float num3 = Mathf.Min(p1.y, p2.y);
            float num4 = Mathf.Max(p1.y, p2.y);
            float f = p2.x - p1.x;
            if ((double) Mathf.Abs(f) > 1.40129846432482E-45) {
                float num5 = (p2.y - p1.y) / f;
                float num6 = p1.y - num5 * p1.x;
                num3 = num5 * num1 + num6;
                num4 = num5 * num2 + num6;
            }

            if ((double) num3 > (double) num4) {
                float num5 = num4;
                num4 = num3;
                num3 = num5;
            }

            if ((double) num4 > (double) rect.yMax)
                num4 = rect.yMax;
            if ((double) num3 < (double) rect.yMin)
                num3 = rect.yMin;
            return (double) num3 <= (double) num4;
        }

        /// <summary>
        ///   <para>Create rectangle that encompasses two rectangles.</para>
        /// </summary>
        /// <param name="a">Rect a.</param>
        /// <param name="b">Rect b.</param>
        /// <returns>
        ///   <para>New rectangle.</para>
        /// </returns>
        public static Rect Encompass(Rect a, Rect b) {
            return new Rect() {
                xMin = Math.Min(a.xMin, b.xMin),
                yMin = Math.Min(a.yMin, b.yMin),
                xMax = Math.Max(a.xMax, b.xMax),
                yMax = Math.Max(a.yMax, b.yMax)
            };
        }

        /// <summary>
        ///   <para>Creates and returns an enlarged copy of the specified rectangle. The copy is enlarged by the specified amounts.</para>
        /// </summary>
        /// <param name="a">The original rectangle.</param>
        /// <param name="left">The amount to inflate the rectangle towards the left.</param>
        /// <param name="top">The amount to inflate the rectangle towards the top.</param>
        /// <param name="right">The amount to inflate the rectangle towards the right.</param>
        /// <param name="bottom">The amount to inflate the rectangle towards the bottom.</param>
        public static Rect Inflate(Rect a, float left, float top, float right, float bottom) {
            return new Rect() {
                xMin = a.xMin - left,
                yMin = a.yMin - top,
                xMax = a.xMax + right,
                yMax = a.yMax + bottom
            };
        }
    }
}